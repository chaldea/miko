# 组件单元测试快速入门

## 安装

在测试项目中添加引用:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <ItemGroup>
    <ProjectReference Include="..\..\src\Miko.Testing\Miko.Testing.csproj" />
    <ProjectReference Include="..\..\src\Miko.Razor.Compiler\Miko.Razor.Compiler.csproj"
                      ReferenceOutputAssembly="false"
                      SkipGetTargetFrameworkProperties="true"
                      Private="false" />
  </ItemGroup>
  
  <!-- 配置自定义 Razor 编译器 -->
  <Target Name="_OverrideRazorSourceGenerator" AfterTargets="_PrepareRazorSourceGenerators">
    <PropertyGroup>
      <_CustomRazorGeneratorDir>$(MSBuildThisFileDirectory)..\..\src\Miko.Razor.Compiler\bin\$(Configuration)\net9.0\</_CustomRazorGeneratorDir>
    </PropertyGroup>
    <ItemGroup>
      <Analyzer Remove="@(_RazorAnalyzer)" />
      <_RazorAnalyzer Remove="@(_RazorAnalyzer)" />
    </ItemGroup>
    <ItemGroup>
      <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.CodeAnalysis.Razor.Compiler.dll" />
      <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.AspNetCore.Razor.Utilities.Shared.dll" />
      <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.Extensions.ObjectPool.dll" />
      <Analyzer Include="@(_RazorAnalyzer)" />
    </ItemGroup>
  </Target>
</Project>
```

## 基本用法

### 1. 创建测试基类

```csharp
using Miko.Testing;

public abstract class ComponentTestBase : IDisposable
{
    protected TestContext Context { get; }

    protected ComponentTestBase()
    {
        Context = new TestContext();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
```

### 2. 编写测试

```csharp
using Shouldly;
using Xunit;

public class MyComponentTests : ComponentTestBase
{
    [Fact]
    public void MyComponent_RendersCorrectly()
    {
        // Act - 渲染组件
        var cut = Context.Render<MyComponent>();

        // Assert - 验证 DOM 结构
        cut.Root.TagName.ShouldBe("div");
        cut.Root.Class.ShouldBe("my-component");
    }

    [Fact]
    public void MyComponent_AcceptsParameters()
    {
        // Act - 渲染带参数的组件
        var cut = Context.Render<MyComponent>(parameters =>
        {
            parameters.Add(nameof(MyComponent.Title), "Hello");
            parameters.Add(nameof(MyComponent.IsActive), true);
        });

        // Assert - 验证参数效果
        cut.GetTextContent().ShouldContain("Hello");
        cut.Root.Class.ShouldContain("active");
    }
}
```

## 常见测试模式

### DOM 结构测试

```csharp
[Fact]
public void Component_HasCorrectStructure()
{
    var cut = Context.Render<MyComponent>();

    // 验证根元素
    cut.Root.TagName.ShouldBe("div");
    
    // 验证子元素
    cut.Root.Children.Count.ShouldBe(2);
    cut.Root.Children[0].TagName.ShouldBe("header");
    
    // 查找特定元素
    var button = cut.FindById("submit-button");
    button.ShouldNotBeNull();
}
```

### 参数测试

```csharp
[Fact]
public void Component_RespondsToParameters()
{
    // 测试不同参数值
    var cut1 = Context.Render<MyComponent>(p => p.Add("Mode", "light"));
    var cut2 = Context.Render<MyComponent>(p => p.Add("Mode", "dark"));

    cut1.Root.Class.ShouldBe("component light");
    cut2.Root.Class.ShouldBe("component dark");
}
```

### 样式测试

```csharp
[Fact]
public void Component_AppliesStyles()
{
    var cut = Context.Render<MyComponent>();

    // 验证内联样式
    cut.Root.Style.ShouldNotBeNull();
    cut.Root.Style!.Display.ShouldBe(Display.Flex);

    // 验证计算样式
    var computedStyle = cut.GetComputedStyle(cut.Root);
    computedStyle.ShouldNotBeNull();
}
```

### 布局测试 (针对布局组件)

```csharp
[Fact]
public void LayoutComponent_HasCorrectBoxModel()
{
    var cut = Context.Render<LayoutComponent>();

    // 获取盒模型
    var boxModel = cut.GetBoxModel(cut.Root);
    boxModel.ShouldNotBeNull();
    
    // 验证尺寸
    boxModel!.Content.Width.ShouldBeGreaterThan(0);
    boxModel.Content.Height.ShouldBeGreaterThan(0);
}
```

## API 参考

### TestContext

```csharp
var context = new TestContext();

// 设置视口大小
context.ViewportWidth = 1024f;
context.ViewportHeight = 768f;

// 添加样式表
context.AddStyleSheet(styleSheet);

// 渲染组件
var cut = context.Render<MyComponent>();
```

### ComponentUnderTest

```csharp
// DOM 访问
Element root = cut.Root;
List<Element> all = cut.GetAllElements();

// 元素查询
Element? byId = cut.FindById("my-id");
List<Element> byClass = cut.FindByClass("my-class");
List<Element> byTag = cut.FindByTagName("div");

// 样式和布局
ComputedStyle? style = cut.GetComputedStyle(element);
LayoutBox? layout = cut.FindLayoutBox(element);
BoxModel? box = cut.GetBoxModel(element);

// 文本内容
string text = cut.GetTextContent();
```

### 扩展方法

```csharp
// 流式断言
cut.ShouldContainId("my-id");
cut.ShouldContainClass("my-class");
cut.ShouldContainTag("div");
cut.ShouldContainText("expected text");
```

## 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test tests/MyComponent.Tests/

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~MyComponentTests"

# 运行特定测试方法
dotnet test --filter "FullyQualifiedName~MyComponentTests.MyTest"
```

## 示例项目

参考 `tests/Miko.Ionic.Tests/` 查看完整的测试示例。

## 最佳实践

1. **测试组件契约,不测试实现细节**
2. **每个测试只验证一个方面**
3. **使用描述性的测试名称**
4. **测试边界情况和错误情况**
5. **只测试关键样式,不过度测试 CSS**

## 更多信息

查看 [README.md](README.md) 了解完整文档。
