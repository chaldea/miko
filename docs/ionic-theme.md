# Ionic 主题与按需样式方案

## 设计目标

将主题值、样式生成与组件实例分开管理。应用启动仅加载全局工具规则；组件首次渲染时，按照组件族、当前模式和有效 Token 注册样式。相同配置的实例共享规则，不同配置通过宿主上的 `ion-theme-{hash}` 类隔离。

## 数据模型

- `IonicTheme` 聚合 `ButtonToken`、`AccordionToken` 等 37 个 Token 类型。组件 Token 位于各自的 `src/Miko.Ionic/Components/<组件名>/` 目录；共享的 `IonicToken` 基类和 `PaletteToken` 位于 `src/Miko.Ionic/Components/Core/`。原有全部主题属性均已迁入组件 Token；跨组件共用的品牌色保存在 `PaletteToken`。
- Token 记录被显式赋值的属性，因此 `0`、透明色、可空属性的 `null` 都是有效覆盖值。没有赋值的属性继承父级；集合按整体复制，防止局部修改影响父主题。
- 有效主题的优先级：当前模式默认值 < `IOptions<IonicOptions>.Value.Theme` < 外层 `ConfigProvider` < 内层 `ConfigProvider`。
- `new IonicTheme()` 表示局部覆盖；`IonicTheme.CreateMd()` / `CreateIos()` 返回完整默认主题。局部定制宜使用前者，以保留未设置属性的继承行为。
- Token 的复制与指纹计算使用直接属性访问，不需要运行时反射。数值、颜色、长度和阴影集合按值计算指纹，不使用对象引用或集合自身的 `GetHashCode()`。

## 注册与隔离

`AddIonic` 注册一个应用级 `IonicStyleRegistry`，并将其共享样式表加入应用。`IonicComponentBase` 在首次构建和后续重渲染时解析主题、注册规则、附加隔离类。子类仍可扩展组件，不需要逐个组件重复写注入逻辑。

缓存键由组件族、MD/iOS 模式、该组件依赖的 Token 指纹组成。组件族沿用现有样式生成器的组织方式，例如 Card/CardHeader/CardContent 共用 Card 样式。Button 的依赖包括 Button、Toolbar 和 Palette；改变 Card Token 不会产生另一份 Button 样式。

隔离在 Miko 已解析的选择器树上完成：定位规则所属组件宿主，并附加哈希类，保留祖先、子代、兄弟和分组关系。例如 `.ion-slot-start .ion-toggle` 的隔离类附在 Toggle 上，插槽本身无需携带该类。额外的宿主边界约束阻止外层规则进入采用不同主题的同类组件。列表条目的基础规则已拆到 `BaseItemStyles`，列表上下文中的边框仍由条目的 Token 决定。

规则一旦注册便保留到应用结束，返回曾用主题直接复用原规则；不按实例删除规则。这样多个同时可见的页面或主题不会因组件卸载而相互影响。连续生成大量不同主题会相应增加规则数，主题编辑器可在提交配置时应用主题。

共享样式表通过 `Add`、`AddRule`、`AddPseudoElementRule` 和 `AddMediaRule` 更新版本。布局缓存同时检查样式表版本，保证后续注入的规则生效。应用自己的样式层仍高于 Ionic 的 `-1` 层。

## 使用方式

代码配置：

```csharp
builder.AddIonic(options =>
{
    options.Theme.Button = new ButtonToken
    {
        SolidBackground = Color.Red,
        SolidColor = Color.White,
    };
});
```

局部主题，可继续嵌套：

```razor
<IonButton>默认</IonButton>
<ConfigProvider IonicTheme="@LocalTheme">
    <IonButton>局部主题</IonButton>
</ConfigProvider>

@code {
    private readonly IonicTheme LocalTheme = new()
    {
        Button = new ButtonToken { SolidBackground = Color.Red },
    };
}
```

配置绑定遵循标准 Options 注册顺序。应用负责加载 JSON 配置文件：

```csharp
builder.Services.Configure<IonicOptions>(configuration.GetSection("Ionic"));
builder.AddIonic();
```

```json
{
  "Ionic": {
    "Platform": "Ios",
    "Theme": {
      "Button": {
        "SolidBackground": "#0e7490",
        "SolidColor": "#ffffff",
        "FontSize": 18,
        "MinHeight": "48px",
        "LetterSpacing": "0.05em"
      }
    }
  }
}
```

颜色支持十六进制字符串；长度支持 `px`、`rem`、`em`、`%`、`vw`、`vh`、`auto`、`fit-content` 和无单位数字。复合长度运算继续通过 C# 的 `Length` API 配置。

`Platform` 优先决定 Ionic 模式；未指定时兼容 `Mode = Ios` 和完整 iOS 主题，否则跟随宿主平台及模拟器变化。Ionic 配置不替换宿主本身的 `IPlatformInfo` 服务。

## 示例与兼容

IonicDemo 首页的 Dark Mode 开关使用双向绑定。共享 `ThemeService` 保留选择，同时更新应用主题，使后续导航页面和控制器创建的弹层采用该主题。首页通过 `ConfigProvider` 展示局部覆盖；其他示例页面由应用级主题提供值。示例自身的说明文字、页面背景等应用样式通过 `demo-dark` 补充深色配色。

目前 Miko 路由先构建页面，再把页面元素放入布局，因此仅在布局里包裹 `ConfigProvider` 无法向已构建的路由页面传递级联值。示例使用应用主题承接跨页面配置；普通 Razor 子树内的 Provider 支持嵌套和动态重渲染。

保留 `IonicTheme` 原平铺属性作为兼容转发，集中在 `IonicTheme.Compatibility.cs`。现有样式生成器继续使用这些转发属性，以保持默认视觉行为。保留 `IonicStyleSheetFactory.Create/CreateAllModes` 供直接构建 DOM 的调用方使用；`AddIonic` 使用按需路径。新的 Options 类型为 `IonicOptions`，旧显式 `Action<IonicConfiguration>` 调用应迁移到该类型。

## 验证范围

新增测试覆盖：启动时仅有全局规则、所有组件族在两种模式下可注册、相同值复用、无关 Token 不影响复用、跨组件依赖、阴影集合和区域设置、嵌套部分覆盖、配置绑定、模式切换、主题切回复用、组件重渲染、列表条目和祖先上下文选择器，以及动态样式表的布局缓存失效。

验证命令：

```powershell
dotnet test tests/Miko.Ionic.Tests/Miko.Ionic.Tests.csproj
dotnet test tests/Miko.Tests/Miko.Tests.csproj
dotnet build examples/Ionic/IonicDemo.Desktop/IonicDemo.Desktop.csproj
```
