# 字体管理器实现

在Style中支持FontFamily属性，其中FontFamily="字体1,字体2"; 支持多字体，同时支持字体回退，也需要支持中英文混排。


# 自定义字体加载

字体管理器要支持加载自定义字体，比如WOFF2格式等。加载后，就可以在Style中使用该字体。

其中由于SkiaSharp无法直接加载WOFF2格式的字体，还需要实现WOFF2的解码器。

# 验证结果

在单元测试项目创建相关测试用例，用以验证字体管理器是否正常工作。

# 验证WOFF2格式能否使用

在Miko.Examples.Bootstrap工程中的Fonts文件夹中提供了bootstrap官方的字体库。可以把它添加到单元测试工程，用于验证具体的码点是否能够渲染。其中bootstrap-icons.css样式文件有相关的码点信息。

