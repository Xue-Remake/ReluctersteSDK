# ReluctersteSDK（聚合包）

把下面 11 个子模块**打成一个包**的聚合项目，方便一次性引用整套工具集。

- 工程：`ReluctersteSDK/ReluctersteSDK.csproj`
- 包 ID：`ReluctersteSDK`
- 版本：0.13.0.0
- 目标框架：net10.0
- License：MIT
- 包输出：`E:\Work\资产仓库\NuGetPackage`

## 引用了哪些模块

```xml
<ProjectReference Include="..\ReluctersteSDK.Config\..." />
<ProjectReference Include="..\ReluctersteSDK.Commands\..." />
<ProjectReference Include="..\ReluctersteSDK.DataStructures\..." />
<ProjectReference Include="..\ReluctersteSDK.Debugger\..." />
<ProjectReference Include="..\ReluctersteSDK.EventSystem\..." />
<ProjectReference Include="..\ReluctersteSDK.Llm\..." />
<ProjectReference Include="..\ReluctersteSDK.Math\..." />
<ProjectReference Include="..\ReluctersteSDK.Game\..." />
<ProjectReference Include="..\ReluctersteSDK.Paths\..." />
<ProjectReference Include="..\ReluctersteSDK.Registry\..." />
<ProjectReference Include="..\ReluctersteSDK.Sqlite\..." />
```

外部依赖：`Microsoft.Extensions.DependencyInjection` 10.0.14、`MonoGame.Framework.DesktopGL` 3.8.6.1。

> 聚合包包含 Game，因此会拖入 MonoGame。**纯服务端项目请按需引用单个子包**；
> 只做数学计算时引 `ReluctersteSDK.Math`，它零 MonoGame 依赖。

## 怎么选：聚合包 vs 单包

| 场景 | 建议 |
|---|---|
| 游戏项目（本来就有 MonoGame） | 直接引 `ReluctersteSDK` |
| 控制台工具 / 服务端 | 只引需要的子包 |
| 只想试一下 | 引聚合包 |

## 打包

各库工程都设置了 `GeneratePackageOnBuild=True`，**`dotnet build` 就会自动出 `.nupkg`**：

```powershell
# 正常构建（会写 E:\Work\资产仓库\NuGetPackage）
dotnet build ReluctersteSDK.slnx

# 只想验证编译、不想动 NuGet 输出目录
dotnet build ReluctersteSDK.slnx -p:GeneratePackageOnBuild=false
```

## ⚠️ 关于 XML 文档与警告

所有库工程都开了 `<GenerateDocumentationFile>True</GenerateDocumentationFile>`，因此：
- 好处：NuGet 包里带 XML 文档，IDE 里能看到中文注释；
- 代价：**任何缺少 XML 注释的公共成员都会产生 `CS1591` 警告**。当前警告几乎全是这一条。

新增公共 API 时记得写 `///` 注释，否则会持续累积警告。

## ⚠️ 版本号需要手工同步

版本号写死在 12 个 `.csproj` 里（没有 `Directory.Build.props` 统一管理），
改版本时要全部改一遍，漏掉一个就会出现"聚合包 0.10.1，子包还是 0.10.0"的情况。

建议后续引入根目录 `Directory.Build.props`：

```xml
<Project>
  <PropertyGroup>
    <Version>0.13.0.0</Version>
    <Authors>Xue-Remake</Authors>
    <Company>Relucterste Digital Studio</Company>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <PackageOutputPath>E:\Work\资产仓库\NuGetPackage</PackageOutputPath>
  </PropertyGroup>
</Project>
```

## ⚠️ 死代码：CoordinatesPackage

`ReluctersteSDK/CoordinatesPackage/` 下的 5 个文件（`Point2D`、`LinearDamping`、`CartesianVector`、`PolarVector`、`Vector2DUtils`）
在 csproj 里被显式排除：

```xml
<ItemGroup>
  <Compile Remove="CoordinatesPackage\**" />
  <EmbeddedResource Remove="CoordinatesPackage\**" />
  <None Remove="CoordinatesPackage\**" />
</ItemGroup>
```

它们的功能已迁移到 `ReluctersteSDK.Math`（向量）；原来的运动点组件已由 `ReluctersteSDK.Game` 的 ECS 运动组件（`Velocity` / `Acceleration` / `Damping`）取代。
这些文件**不参与编译**，属于待清理的历史遗留，别在里面改代码——改了也没有任何效果。
