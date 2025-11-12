Console.WriteLine(new Node<string, StringHelper>().MulAdd("a", "b", "c"));
Console.WriteLine(new Node<int, IntHelper>().MulAdd(1, 2, 3));

// 负责抽象偏特化部分，仅用struct实现
interface IHelper<T>
{
    T Add(T a, T b);
    T Mul(T a, T b);
}

// 负责抽象流程公共相同点
class Node<T, THelper> where THelper : struct, IHelper<T>
{
    static THelper _helper=default;
    public T MulAdd(T a, T b, T c)
    {
        return _helper.Add(_helper.Mul(a, b), c);
    }
}

struct StringHelper : IHelper<string>
{
    public string Add(string a, string b)
    {
        return string.Concat(a, "+", b);
    }
    public string Mul(string a, string b)
    {
        return string.Concat(a, "*", b);
    }
}

struct IntHelper : IHelper<int>
{
    public int Add(int a, int b)
    {
        return a + b;
    }
    public int Mul(int a, int b)
    {
        return a * b;
    }
}