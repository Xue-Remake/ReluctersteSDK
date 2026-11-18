// 本工程的根命名空间是 ReluctersteSDK.Math，会遮蔽 System.Math。
// 统一用 SystemMath 别名访问 BCL 数学函数，避免裸 Math. 在 ReluctersteSDK.* 命名空间下解析到命名空间。
global using SystemMath = System.Math;
