using System.Collections.Generic;

namespace BlockingModel.Core;

/// <summary>
/// 标记当前建模工程要聚焦的阻塞点类型。
/// </summary>
public enum BlockingPointKind
{
	CursorLifecycle,
	GenericNode,
	MetricInteroperation,
	ChunkEnumeration,
	GraphemeNavigation,
	BreaksTree,
}

/// <summary>
/// 描述某个阻塞点在 mini blocking model 中需要覆盖的最小语义。
/// </summary>
/// <param name="Kind">阻塞点类型。</param>
/// <param name="Objective">当前要验证的工程/类型系统设计目标。</param>
/// <param name="RustFocus">Rust 侧需构造的最小骨架提示。</param>
/// <param name="CSharpFocus">C# 侧需构造的最小骨架提示。</param>
public sealed record BlockingPointSpec(
	BlockingPointKind Kind,
	string Objective,
	string RustFocus,
	string CSharpFocus);

/// <summary>
/// 提供阻塞点到建模任务的对照表，供测试与 CLI 骨架共享。
/// </summary>
public static class BlockingPointRegistry
{
	private static readonly IReadOnlyDictionary<BlockingPointKind, BlockingPointSpec> _specs =
		new Dictionary<BlockingPointKind, BlockingPointSpec>
		{
			[BlockingPointKind.CursorLifecycle] = new(
				BlockingPointKind.CursorLifecycle,
				"验证拥有型 cursor + descriptor 的失效策略",
				"CursorDescriptor + CursorState stub crate",
				"NodeCursor mini tree + version ticket"),
			[BlockingPointKind.GenericNode] = new(
				BlockingPointKind.GenericNode,
				"在不依赖 Rope 全量实现的情况下复现 Node<TInfo, TLeaf> 约束",
				"Node<TInfo, L> builder skeleton",
				"TypeAliases + TreeBuilder.Generic minimal sample"),
			[BlockingPointKind.MetricInteroperation] = new(
				BlockingPointKind.MetricInteroperation,
				"度量转换 shim 的 API/Safety 合同",
				"convert_* helpers without Rope",
				"IMetricAdapter contract + sample"),
			[BlockingPointKind.ChunkEnumeration] = new(
				BlockingPointKind.ChunkEnumeration,
				"Chunk/Line 迭代器的 owned descriptor 接口",
				"ChunkDescriptor facade",
				"RopeChunkEnumerator diagnostics stub"),
			[BlockingPointKind.GraphemeNavigation] = new(
				BlockingPointKind.GraphemeNavigation,
				"字素降级策略 + telemetry",
				"GraphemeCursor trace exporter",
				"Degraded navigator + counters"),
			[BlockingPointKind.BreaksTree] = new(
				BlockingPointKind.BreaksTree,
				"Breaks tree + search shim",
				"BreaksDescriptor exporter",
				"BreaksModel aggregator"),
		};

	/// <summary>
	/// 返回所有阻塞点规格列表，供测试或上层工具枚举。
	/// </summary>
	public static IEnumerable<BlockingPointSpec> All => _specs.Values;

	/// <summary>
	/// 获取指定阻塞点的规格，如果不存在会抛出异常，帮助测试及早暴露缺口。
	/// </summary>
	public static BlockingPointSpec Get(BlockingPointKind kind)
	{
		if (_specs.TryGetValue(kind, out var spec))
		{
			return spec;
		}

		throw new KeyNotFoundException($"未注册的阻塞点: {kind}。");
	}
}
