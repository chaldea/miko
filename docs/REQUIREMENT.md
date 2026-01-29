# 需求设计文档

Miko是基于C#语言，使用C#类型构建DOM树，并使用SkiaSharp作为渲染引擎，渲染各个元素，功能类似简化版本的浏览器，渲染流水线与浏览器类似，但是不需要解析html文本内容和样式，而是直接通过代码构建dom树和样式表，也不需要支持脚本。


- 需要创建各种DOM元素类型，包括div,h1-h6,span,p,input,button,img等。
- 支持样式表和样式对象，支持样式选择器。
- 支持盒子模型(样式中的display约束都需要支持)
- 渲染时需要支持脏区域重绘

```csharp

var css = new List<StyleSheet>();    // 构建样式表

var div = new DivElement();          // 创建Div元素
div.Class = ".container";            // 样式选择器
div.Style = new Style() { ... };     // 设置具体的样式对象，行间样式
div.Children.Add(new H1Element());   // 添加子元素
div.Children.Add(new DivElement());  // 添加子元素

var layoutEngine = new LayoutEngine();
var layout = layoutEngine.Layout(div, css);  // 依据DOM树，创建盒子模型，计算并合并所有元素的样式

var renderEngine = new RenderEngine();
renderEngine.Render(layout);

```


