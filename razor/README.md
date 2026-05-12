# Razor编译器

## 为Razor项目使用自定义编译器

```bash
# 创建razor项目
dotnet new razorclasslib
```

修改.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <Target Name="_OverrideRazorSourceGenerator" AfterTargets="_PrepareRazorSourceGenerators">
    <PropertyGroup>
      <!--使用自定义程序集目录，注意本地运行时需要依据工程所在目录进行修改-->
      <_CustomRazorGeneratorDir>$(MSBuildThisFileDirectory)..\Microsoft.CodeAnalysis.Razor.Compiler\bin\Debug\net9.0\</_CustomRazorGeneratorDir>
    </PropertyGroup>
    <ItemGroup>
      <!--清理SDK默认的Razor分析器-->
      <Analyzer Remove="@(_RazorAnalyzer)" />
      <_RazorAnalyzer Remove="@(_RazorAnalyzer)" />
    </ItemGroup>
    <ItemGroup>
      <!--使用自定义的Razor分析器-->
      <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.CodeAnalysis.Razor.Compiler.dll" />
      <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.AspNetCore.Razor.Utilities.Shared.dll" />
      <_RazorAnalyzer Include="$(_CustomRazorGeneratorDir)Microsoft.Extensions.ObjectPool.dll" />
      <Analyzer Include="@(_RazorAnalyzer)" />
    </ItemGroup>
  </Target>
</Project>

```

## Razor编译器说明

```
Microsoft.CodeAnalysis.Razor.Compiler/
├── Language         # Razor语言相关
│   └── Components   # 和Razor组件相关
│       └── ComponentsApi.cs  # 组件生成相关参数内容
└── SourceGenerators # SG代码

```

其中`ComponentsApi.cs`文件中定义了所有Razor组件的内容，也是需要进行重新替换的部分。