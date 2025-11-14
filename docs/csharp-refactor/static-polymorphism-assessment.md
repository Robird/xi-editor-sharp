# 静态多态 Helper 机制评估

## 背景
`docs/notebook/GenericStaticPolymorphism.cs` 展示了使用 `Node<T, THelper>` 模式，通过结构体约束和接口组合实现“静态多态”——以泛型参数在编译期绑定不同 Helper 的思路。在 Rope 迁移中，我们期望用类似手法把 `Node` 从 `string` 特化抽象成 `Node<TInfo, TLeaf, TLeafOps>`。本笔记梳理该模式在 .NET 9/C# 13 下的可行性，并用最小示例记录潜在风险。

## 现有样例回顾
```csharp
interface IHelper<T>
{
    T Add(T a, T b);
    T Mul(T a, T b);
}

class Node<T, THelper>
    where THelper : struct, IHelper<T>
{
    private static readonly THelper Helper = new();

    public T MulAdd(T a, T b, T c)
        => Helper.Add(Helper.Mul(a, b), c);
}

struct StringHelper : IHelper<string> { /* ... */ }
struct IntHelper : IHelper<int> { /* ... */ }
```
- **约束使用 struct**：避免在 Helper 调用时发生装箱，JIT 会发出受限 `callvirt`，在大多数情况下可以内联。
- **静态字段缓存 Helper**：每个封闭泛型类型拥有自己的 Helper 实例，方法调用成本接近手写普通静态函数。

## 能力验证
### 1. 常量/策略可通过静态抽象成员暴露
```csharp
interface ILeafOperations<TLeaf>
{
    static abstract int MinLeafSize { get; }
    static abstract int MaxLeafSize { get; }

    int GetLength(TLeaf leaf);
    bool TryAppend(ref TLeaf target, ReadOnlySpan<char> source);
}

struct StringLeafOps : ILeafOperations<string>
{
    public static int MinLeafSize => 511;
    public static int MaxLeafSize => 1024;

    public int GetLength(string leaf) => leaf.Length;
    public bool TryAppend(ref string target, ReadOnlySpan<char> source)
    {
        target = string.Concat(target, source.ToString());
        return target.Length <= MaxLeafSize;
    }
}

int min = StringLeafOps.MinLeafSize; // 编译通过
```
> **启示**：Rope 中的叶片约束、拆分策略等常量可放在 `TLeafOps` 的静态成员里，`Node<TInfo, TLeaf, TLeafOps>` 可以直接在泛型上下文引用，避免实例调用。

### 2. 避免装箱成功
下面的测试验证在数值 Helper 上无托管理堆分配：
```csharp
struct IntHelper : IHelper<int>
{
    public int Add(int a, int b) => a + b;
    public int Mul(int a, int b) => a * b;
}

var before = GC.GetAllocatedBytesForCurrentThread();
var node = new Node<int, IntHelper>();
var result = node.MulAdd(2, 40, 1);
var after = GC.GetAllocatedBytesForCurrentThread();
Debug.Assert(after == before);
```
在 .NET 9 下，JIT 生成的 IL 针对 `Node<int, IntHelper>.MulAdd` 只有一次受限 `callvirt`，未发生装箱或额外分配，说明该模式对纯值类型 Helper 是安全的。

## 潜在风险与对策
### 风险一：默认初始化不会触发结构体构造逻辑
如果 Helper 依赖自定义无参构造器进行配置，如下代码会产生静默错误：
```csharp
struct ScalingHelper : IHelper<int>
{
    private readonly int _factor;

    public ScalingHelper() => _factor = 10;

    public int Add(int a, int b) => _factor + a + b;
    public int Mul(int a, int b) => _factor * a * b;
}

var node = new Node<int, ScalingHelper>();
Console.WriteLine(node.MulAdd(1, 2, 3)); // 输出 5，而非预期的 23
```
`static readonly THelper Helper = default;` 会得到零初始化的结构体，构造器不会执行，`_factor` 仍为 0。解决办法：
- 使用 `new()` 约束并改为 `private static readonly THelper Helper = new();`
- 或在 Helper 内提供显式的 `static` 工厂，供节点调用。

### 风险二：Helper 存在可变状态或非线程安全字段
因为 Helper 被缓存为静态字段，若其方法修改内部状态，将在所有线程/实例间共享：
```csharp
struct CountingHelper : IHelper<int>
{
    private int _calls;

    public int Add(int a, int b) => ++_calls; // 修改状态
    public int Mul(int a, int b) => ++_calls;
}

var node = new Node<int, CountingHelper>();
Console.WriteLine(node.MulAdd(1, 2, 3)); // 第一次输出 2
Console.WriteLine(node.MulAdd(1, 2, 3)); // 第二次输出 4（共享副作用）
```
对 Rope 而言，Helper 应视为纯函数集合；建议：
- 对 Helper 方法标记 `readonly`，并在代码评审中禁止修改 `_helper` 的内部字段。
- 若确实需要可变状态（例如缓存统计），应在调用点改为局部实例而非静态共享。

### 风险三：需要额外依赖注入或大型资源
某些实现需要在 Helper 中持有 `ArrayPool<T>` 或其他外部资源：
```csharp
struct PooledLeafOps : ILeafOperations<char[]>
{
    private readonly ArrayPool<char> _pool;

    public PooledLeafOps(ArrayPool<char> pool) => _pool = pool;
    // ...
}

// Node 使用默认 Helper 实例，无法注入自定义 pool：_pool 变为 null
```
由于 Helper 需要 `struct` 约束且节点通过 `new()`/`default` 获得实例，无法直接注入资源。可选方案：
- 将资源暴露为 `static` 属性（例如 `ArrayPool<char>.Shared`）。
- 或允许 `Node` 构造函数接收 Helper 实例，在泛型运行时传递（需要牺牲纯静态多态的零成本特性）。

### 风险四：接口方法的只读语义需要显式声明
若 Helper 方法未标记 `readonly`，定位到 `ref readonly` 叶片时，编译器会生成防御性拷贝，增加额外复制开销。例如：
```csharp
struct SpanHelper : IHelper<ReadOnlySpan<char>>
{
    public readonly ReadOnlySpan<char> Add(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        => throw new NotSupportedException();

    public ReadOnlySpan<char> Mul(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        => throw new NotSupportedException(); // 未加 readonly
}
```
`Mul` 调用会触发复制，可能导致性能回退。建议在 Helper 接口上统一使用 `readonly` 限定（或以 `in`/`ref` 参数保证不复制）。

## 结论与建议
1. **模式可用**：结构体 Helper + 泛型约束可以支撑 Rope 泛型化所需的静态多态，尤其在无状态 Helper 场景下可获得近似手写的性能。
2. **需要规范化 Helper 约束**：
   - 在接口层显式要求 `static abstract` 常量、`readonly` 方法。
   - `Node` 端使用 `new()` 约束确保 Helper 的显式初始化逻辑被执行。
3. **提前识别需要共享资源的 Helper**：如果后续要支持基于 `ArrayPool<char>` 的叶片，应设计备用构造/工厂方案。
4. **文档化最佳实践**：为团队编写 Helper 时提供 checklist，防止隐性状态或默认初始化陷阱。

综上，当前“静态多态”能力能够满足 Rope 泛型化的核心需求，但需在 Helper 设计规范和实例化方式上补充约束，以避免上述隐患。未来在 `xi-editor-ph7` fork 为 `Node<TInfo, TLeaf, TLeafOps>` 提供迁移友好 helper 时，应保持命名与接口与本文示例一致，便于双端协同。
