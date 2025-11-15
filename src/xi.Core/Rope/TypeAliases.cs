// ========================================
// M3-M4 过渡期类型别名 - M4 完成后删除此文件
// ========================================
//
// 使用说明：
// - M3 阶段（接口验证）：RopeNode 指向字符串特化 Node
// - M4 阶段（完整切换）：注释当前行，启用下方泛型版本
// - 切换后验证所有测试通过，然后删除本文件

// M3: 字符串特化路径（当前启用）
global using RopeNode = Xi.Core.Rope.Tree.Node;

// M4: 泛型路径（待启用）
// global using RopeNode = Xi.Core.Rope.Tree.Node<Xi.Core.Rope.RopeInfo, string, Xi.Core.Rope.Tree.StringLeafOperations>;
