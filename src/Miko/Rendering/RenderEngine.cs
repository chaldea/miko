using Miko.Animation;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Rendering;

/// <summary>
/// 渲染引擎
/// </summary>
public class RenderEngine
{
    /// <summary>
    /// 增量渲染脏区域数量阈值。脏区域超过该数量时，多次全树遍历的成本会超过一次全量渲染
    /// （见基准报告 §2 拐点 30–50），此时应回退到全量渲染。
    /// </summary>
    public int MaxIncrementalDirtyRegions { get; set; } = 30;

    private SKCanvas? _canvas;
    private Painter? _painter;

    /// <summary>
    /// 当前渲染所用的 GPU 上下文。GPU 宿主在拥有 <see cref="GRContext"/> 时设置，
    /// 供视频帧源把解码 GPU 资源零拷贝包装为 <see cref="SKImage"/>。
    /// 离屏/软件渲染下为 null，视频帧源应回退到 CPU 光栅图像。
    /// </summary>
    public GRContext? GraphicsContext { get; set; }
    private List<RectF>? _dirtyRegions;
    private readonly List<(LayoutBox box, SelectElement select, float scrollOffsetX, float scrollOffsetY)> _pendingDropdowns = new();
    private float _currentScrollOffsetX;
    private float _currentScrollOffsetY;

    public Action<SKCanvas>? OverlayCallback { get; set; }

    /// <summary>
    /// 设置画布
    /// </summary>
    public void SetCanvas(SKCanvas canvas)
    {
        _canvas = canvas;
        _painter = new Painter(canvas);
    }

    /// <summary>
    /// 全量渲染
    /// </summary>
    public void Render(LayoutBox layoutRoot)
    {
        if (_canvas == null || _painter == null)
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");

        _dirtyRegions = null;
        _pendingDropdowns.Clear();
        _currentScrollOffsetX = 0;
        _currentScrollOffsetY = 0;
        RenderBox(layoutRoot);
        FlushDropdowns();
        OverlayCallback?.Invoke(_canvas!);
    }

    public void RenderDirty(LayoutBox layoutRoot, List<RectF> dirtyRegions)
    {
        if (_canvas == null || _painter == null)
            throw new InvalidOperationException("Canvas not set. Call SetCanvas first.");

        _dirtyRegions = dirtyRegions;
        _pendingDropdowns.Clear();
        _currentScrollOffsetX = 0;
        _currentScrollOffsetY = 0;

        foreach (var region in dirtyRegions)
        {
            _painter.Save();
            _painter.ClipRect(region);
            RenderBox(layoutRoot);
            _painter.Restore();
        }

        FlushDropdowns();
        _dirtyRegions = null;
    }

    private void FlushDropdowns()
    {
        foreach (var (box, select, scrollX, scrollY) in _pendingDropdowns)
            RenderSelectDropdown(box, select, scrollX, scrollY);
        _pendingDropdowns.Clear();
    }

    /// <summary>
    /// 渲染盒子
    /// </summary>
    private void RenderBox(LayoutBox box)
    {
        if (_painter == null) return;

        if (!ShouldRender(box)) return;

        float opacity = box.ComputedStyle.Opacity;
        bool hasOpacity = opacity < 1f;
        if (hasOpacity)
        {
            byte alpha = (byte)(opacity * 255);
            _painter.SaveLayerAlpha(alpha);
        }

        bool hasTransform = box.ComputedStyle.Transform.Functions.Count > 0;
        if (hasTransform)
        {
            _painter.Save();
            ApplyTransform(box);
        }

        // 1. 绘制盒阴影（在背景之前）
        RenderBoxShadow(box);

        // 2. 绘制背景
        RenderBackground(box);

        // 3. 绘制边框
        RenderBorder(box);

        // 4. 绘制内容
        RenderContent(box);

        // 4. 递归绘制子元素
        // SelectElement 的子元素（Option）不参与正常树渲染，由 overlay pass 统一绘制下拉层
        if (box.Element is SelectElement)
        {
            if (hasTransform) _painter.Restore();
            if (hasOpacity) _painter.Restore();
            return;
        }

        // 处理 overflow 裁剪和滚动
        bool hasOverflow = box.ComputedStyle.OverflowX != Overflow.Visible ||
                           box.ComputedStyle.OverflowY != Overflow.Visible;

        if (hasOverflow && box.Children.Count > 0)
        {
            RenderChildrenWithOverflow(box);
        }
        else
        {
            foreach (var child in OrderedChildren(box))
            {
                RenderBox(child);
            }
        }

        // 5. 绘制滚动条（在裁剪区域之外）
        RenderScrollbars(box);

        if (hasTransform) _painter.Restore();
        if (hasOpacity) _painter.Restore();
    }

    /// <summary>
    /// 返回按 z-index 稳定排序后的子元素渲染顺序。z-index 仅对定位元素（position 非 static）
    /// 生效（与 CSS 一致）；相同 z-index 的元素保持文档顺序。这让带正 z-index 的定位元素
    /// （如 ion-header 的阴影）渲染在普通流兄弟之上。
    /// </summary>
    private static IEnumerable<LayoutBox> OrderedChildren(LayoutBox box)
    {
        var children = box.Children;
        // 快速路径：没有任何子元素设置了 z-index 时，避免分配与排序。
        bool anyZ = false;
        foreach (var c in children)
        {
            if (c.ComputedStyle.ZIndex != 0 && c.ComputedStyle.Position != Common.Position.Static)
            {
                anyZ = true;
                break;
            }
        }
        if (!anyZ) return children;

        // 稳定排序：仅定位元素参与 z-index 比较，其余视为 0。
        return children
            .Select((c, i) => (c, i))
            .OrderBy(t => t.c.ComputedStyle.Position != Common.Position.Static ? t.c.ComputedStyle.ZIndex : 0)
            .ThenBy(t => t.i)
            .Select(t => t.c);
    }

    private void ApplyTransform(LayoutBox box)
    {
        if (_painter == null) return;

        var borderBox = box.BoxModel.BorderBox;
        var origin = box.ComputedStyle.TransformOrigin;

        float originX = origin.X.Unit == LengthUnit.Percent
            ? borderBox.X + borderBox.Width * origin.X.Value / 100f
            : borderBox.X + origin.X.Value;
        float originY = origin.Y.Unit == LengthUnit.Percent
            ? borderBox.Y + borderBox.Height * origin.Y.Value / 100f
            : borderBox.Y + origin.Y.Value;

        _painter.Translate(originX, originY);

        foreach (var fn in box.ComputedStyle.Transform.Functions)
        {
            switch (fn)
            {
                case TransformFunction.Translate t:
                    float tx = t.X.Unit == LengthUnit.Percent
                        ? borderBox.Width * t.X.Value / 100f : t.X.Value;
                    float ty = t.Y.Unit == LengthUnit.Percent
                        ? borderBox.Height * t.Y.Value / 100f : t.Y.Value;
                    _painter.Translate(tx, ty);
                    break;
                case TransformFunction.TranslateX t:
                    float txVal = t.X.Unit == LengthUnit.Percent
                        ? borderBox.Width * t.X.Value / 100f : t.X.Value;
                    _painter.Translate(txVal, 0);
                    break;
                case TransformFunction.TranslateY t:
                    float tyVal = t.Y.Unit == LengthUnit.Percent
                        ? borderBox.Height * t.Y.Value / 100f : t.Y.Value;
                    _painter.Translate(0, tyVal);
                    break;
                case TransformFunction.Rotate r:
                    _painter.Rotate(r.Degrees);
                    break;
                case TransformFunction.Scale s:
                    _painter.Scale(s.X, s.Y);
                    break;
                case TransformFunction.ScaleX s:
                    _painter.Scale(s.X, 1f);
                    break;
                case TransformFunction.ScaleY s:
                    _painter.Scale(1f, s.Y);
                    break;
                case TransformFunction.SkewX s:
                    _painter.Skew(s.Degrees, 0);
                    break;
                case TransformFunction.SkewY s:
                    _painter.Skew(0, s.Degrees);
                    break;
                case TransformFunction.Skew s:
                    _painter.Skew(s.DegreesX, s.DegreesY);
                    break;
                case TransformFunction.Matrix m:
                    var matrix = new SKMatrix(
                        m.A, m.C, m.Tx,
                        m.B, m.D, m.Ty,
                        0, 0, 1);
                    _painter.Concat(matrix);
                    break;
            }
        }

        _painter.Translate(-originX, -originY);
    }

    /// <summary>
    /// 带溢出裁剪的子元素渲染
    /// </summary>
    private void RenderChildrenWithOverflow(LayoutBox box)
    {
        if (_painter == null) return;

        var paddingBox = box.BoxModel.PaddingBox;
        float clipWidth = paddingBox.Width;
        float clipHeight = paddingBox.Height;

        if (box.HasVerticalScrollbar)
        {
            clipWidth -= LayoutBox.ScrollbarThickness;
        }
        if (box.HasHorizontalScrollbar)
        {
            clipHeight -= LayoutBox.ScrollbarThickness;
        }

        var clipRect = new RectF(paddingBox.X, paddingBox.Y, clipWidth, clipHeight);

        float prevScrollX = _currentScrollOffsetX;
        float prevScrollY = _currentScrollOffsetY;
        _currentScrollOffsetX += box.ScrollLeft;
        _currentScrollOffsetY += box.ScrollTop;

        _painter.Save();
        _painter.ClipRect(clipRect);
        _painter.Translate(-box.ScrollLeft, -box.ScrollTop);

        foreach (var child in box.Children)
        {
            RenderBox(child);
        }

        _painter.Restore();

        _currentScrollOffsetX = prevScrollX;
        _currentScrollOffsetY = prevScrollY;
    }

    /// <summary>
    /// 渲染滚动条
    /// </summary>
    private void RenderScrollbars(LayoutBox box)
    {
        if (_painter == null) return;

        var paddingBox = box.BoxModel.PaddingBox;
        bool hasVScrollbar = box.HasVerticalScrollbar;
        bool hasHScrollbar = box.HasHorizontalScrollbar;

        if (hasVScrollbar)
        {
            float trackX = paddingBox.Right - LayoutBox.ScrollbarThickness;
            float trackHeight = paddingBox.Height - (hasHScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var trackRect = new RectF(trackX, paddingBox.Y, LayoutBox.ScrollbarThickness, trackHeight);

            _painter.DrawVerticalScrollbar(
                trackRect,
                box.ScrollTop,
                box.ScrollableContentHeight,
                trackHeight);
        }

        if (hasHScrollbar)
        {
            float trackY = paddingBox.Bottom - LayoutBox.ScrollbarThickness;
            float trackWidth = paddingBox.Width - (hasVScrollbar ? LayoutBox.ScrollbarThickness : 0);
            var trackRect = new RectF(paddingBox.X, trackY, trackWidth, LayoutBox.ScrollbarThickness);

            _painter.DrawHorizontalScrollbar(
                trackRect,
                box.ScrollLeft,
                box.ScrollableContentWidth,
                trackWidth);
        }
    }

    /// <summary>
    /// 检查是否应该渲染
    /// </summary>
    private bool ShouldRender(LayoutBox box)
    {
        // 检查是否在脏区域内
        if (_dirtyRegions != null && _dirtyRegions.Count > 0)
        {
            return _dirtyRegions.Any(r => r.IntersectsWith(box.BoxModel.BorderBox));
        }

        return true;
    }

    /// <summary>
    /// 渲染盒阴影
    /// </summary>
    private void RenderBoxShadow(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        if (style.BoxShadow == null || style.BoxShadow.Count == 0) return;

        _painter.DrawBoxShadow(
            style.BoxShadow,
            box.BoxModel.BorderBox,
            style.BorderTopLeftRadius.Value,
            style.BorderTopRightRadius.Value,
            style.BorderBottomRightRadius.Value,
            style.BorderBottomLeftRadius.Value
        );
    }

    /// <summary>
    /// 渲染背景
    /// </summary>
    private void RenderBackground(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        if (style.BackgroundColor.A > 0)
        {
            _painter.DrawBackground(
                box.BoxModel.BorderBox,
                style.BackgroundColor,
                style.BorderTopLeftRadius.Value,
                style.BorderTopRightRadius.Value,
                style.BorderBottomRightRadius.Value,
                style.BorderBottomLeftRadius.Value
            );
        }

        if (style.BackgroundImage?.Bitmap != null)
        {
            RenderBackgroundImage(style, box.BoxModel.PaddingBox);
        }
    }

    private void RenderBackgroundImage(ComputedStyle style, RectF area)
    {
        var bgImage = style.BackgroundImage!;
        var bitmap = bgImage.Bitmap!;
        var imgWidth = bgImage.OriginalWidth;
        var imgHeight = bgImage.OriginalHeight;

        float drawWidth, drawHeight;
        switch (style.BackgroundSize.Mode)
        {
            case BackgroundSizeMode.Cover:
                var coverScale = Math.Max(area.Width / imgWidth, area.Height / imgHeight);
                drawWidth = imgWidth * coverScale;
                drawHeight = imgHeight * coverScale;
                break;
            case BackgroundSizeMode.Contain:
                var containScale = Math.Min(area.Width / imgWidth, area.Height / imgHeight);
                drawWidth = imgWidth * containScale;
                drawHeight = imgHeight * containScale;
                break;
            case BackgroundSizeMode.Explicit:
                drawWidth = style.BackgroundSize.ResolveWidth(area.Width, imgWidth);
                drawHeight = style.BackgroundSize.ResolveHeight(area.Height, imgHeight);
                break;
            default:
                drawWidth = imgWidth;
                drawHeight = imgHeight;
                break;
        }

        float startX = area.X, startY = area.Y;
        switch (style.BackgroundPosition)
        {
            case BackgroundPosition.CenterTop:
            case BackgroundPosition.Center:
            case BackgroundPosition.CenterBottom:
                startX = area.X + (area.Width - drawWidth) / 2;
                break;
            case BackgroundPosition.RightTop:
            case BackgroundPosition.RightCenter:
            case BackgroundPosition.RightBottom:
                startX = area.X + area.Width - drawWidth;
                break;
        }
        switch (style.BackgroundPosition)
        {
            case BackgroundPosition.LeftCenter:
            case BackgroundPosition.Center:
            case BackgroundPosition.RightCenter:
                startY = area.Y + (area.Height - drawHeight) / 2;
                break;
            case BackgroundPosition.LeftBottom:
            case BackgroundPosition.CenterBottom:
            case BackgroundPosition.RightBottom:
                startY = area.Y + area.Height - drawHeight;
                break;
        }

        var renderBitmap = (drawWidth != imgWidth || drawHeight != imgHeight)
            ? bgImage.RenderAtSize((int)drawWidth, (int)drawHeight) ?? bitmap
            : bitmap;

        // Template icons (Ionicons SVG masks) are tinted with the element's color — CSS fill: currentColor.
        Color? tint = bgImage.IsTemplate ? style.Color : (Color?)null;

        _painter!.SaveClip(area);

        switch (style.BackgroundRepeat)
        {
            case BackgroundRepeat.Repeat:
                TileImage(renderBitmap, area, startX, startY, drawWidth, drawHeight, true, true, tint);
                break;
            case BackgroundRepeat.RepeatX:
                TileImage(renderBitmap, area, startX, startY, drawWidth, drawHeight, true, false, tint);
                break;
            case BackgroundRepeat.RepeatY:
                TileImage(renderBitmap, area, startX, startY, drawWidth, drawHeight, false, true, tint);
                break;
            case BackgroundRepeat.NoRepeat:
                _painter.DrawImage(renderBitmap, new RectF(startX, startY, drawWidth, drawHeight), tint);
                break;
        }

        _painter.Restore();
    }

    private void TileImage(SKBitmap bitmap, RectF area, float startX, float startY, float tileW, float tileH, bool repeatX, bool repeatY, Color? tint = null)
    {
        float originX = startX;
        if (repeatX)
        {
            while (originX > area.X) originX -= tileW;
        }
        float originY = startY;
        if (repeatY)
        {
            while (originY > area.Y) originY -= tileH;
        }

        float endX = repeatX ? area.Right : originX + tileW;
        float endY = repeatY ? area.Bottom : originY + tileH;

        for (float y = originY; y < endY; y += tileH)
        {
            for (float x = originX; x < endX; x += tileW)
            {
                _painter!.DrawImage(bitmap, new RectF(x, y, tileW, tileH), tint);
            }
        }
    }

    /// <summary>
    /// 渲染边框
    /// </summary>
    private void RenderBorder(LayoutBox box)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;

        bool hasVisibleBorder =
            style.ComputedBorderTop.IsVisible ||
            style.ComputedBorderRight.IsVisible ||
            style.ComputedBorderBottom.IsVisible ||
            style.ComputedBorderLeft.IsVisible;

        if (!hasVisibleBorder) return;

        _painter.DrawBorderSides(
            box.BoxModel.BorderBox,
            style.ComputedBorderTop,
            style.ComputedBorderRight,
            style.ComputedBorderBottom,
            style.ComputedBorderLeft,
            style.BorderTopLeftRadius.Value,
            style.BorderTopRightRadius.Value,
            style.BorderBottomRightRadius.Value,
            style.BorderBottomLeftRadius.Value
        );
    }

    /// <summary>
    /// 渲染内容
    /// </summary>
    private void RenderContent(LayoutBox box)
    {
        if (_painter == null) return;

        var element = box.Element;

        // 渲染输入框
        if (element is InputElement inputElement)
        {
            RenderInputElement(box, inputElement);
            return;
        }

        // 渲染下拉选择框
        if (element is SelectElement selectElement)
        {
            RenderSelectElement(box, selectElement);
            return;
        }

        // 渲染文本内容
        if (!string.IsNullOrEmpty(element.TextContent))
        {
            var style = box.ComputedStyle;
            var content = box.BoxModel.Content;

            // 文本绘制矩形与垂直对齐：
            // - button 默认像 flex 容器一样把文本居中于整个内容盒；
            // - 其余元素遵循 CSS 行盒（line box）模型：文本位于高度为 line-height 的行盒内，
            //   行距（leading）平均分配到上下两侧（half-leading），使文本在行盒内垂直居中。
            //   行盒锚定在内容盒顶部，高度取 line-height（未显式设置时回退到字体度量），
            //   因此当元素只有文本时（内容高 = line-height）文本居中于内容盒（参见 ISSUE-070），
            //   而自然行高下行盒高 ≈ 字体高，居中与顶对齐等价（无回归）。
            RectF textRect;
            if (element is Miko.Core.DomElements.ButtonElement)
            {
                textRect = content;
            }
            else
            {
                float lineBoxHeight = Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(style);
                textRect = new RectF(content.X, content.Y, content.Width, lineBoxHeight);
            }

            // flex 容器的直接文本作为匿名 flex 项参与 justify-content/align-items（见 ISSUE-085）。
            // FlexLayout 记录的对齐位移在此叠加到文本绘制矩形上（非 flex 容器偏移为 0）。
            if (box.TextContentOffsetX != 0f || box.TextContentOffsetY != 0f)
            {
                textRect = new RectF(
                    textRect.X + box.TextContentOffsetX,
                    textRect.Y + box.TextContentOffsetY,
                    textRect.Width,
                    textRect.Height);
            }

            // 根据 WhiteSpace 属性决定是否使用多行文本绘制
            bool shouldWrap = Utils.TextWrapper.ShouldWrap(style.WhiteSpace);
            bool needsMultiline = false;

            if (shouldWrap && content.Width > 0)
            {
                // 检查文本是否需要换行（预处理后的文本宽度是否超过可用宽度）
                var processedText = Utils.TextWrapper.ProcessText(element.TextContent, style.WhiteSpace);
                var (singleLineWidth, _) = Utils.TextMeasurer.MeasureText(
                    processedText,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight);

                needsMultiline = singleLineWidth > content.Width || processedText.Contains('\n');
            }

            if (needsMultiline)
            {
                float lineHeight = Layout.LayoutAlgorithms.BlockLayout.ResolveLineHeight(style);
                _painter.DrawMultilineText(
                    element.TextContent,
                    content, // 使用整个内容区域进行换行
                    style.Color,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight,
                    style.TextAlign,
                    lineHeight,
                    style.WhiteSpace,
                    element is Miko.Core.DomElements.ButtonElement ? VerticalAlign.Middle : VerticalAlign.Top
                );
            }
            else
            {
                _painter.DrawText(
                    element.TextContent,
                    textRect,
                    style.Color,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight,
                    style.TextAlign,
                    VerticalAlign.Middle
                );
            }

            if (style.TextDecoration != Common.TextDecoration.None)
            {
                _painter.DrawTextDecoration(
                    element.TextContent,
                    textRect,
                    style.Color,
                    style.FontFamily,
                    style.FontSize.Value,
                    style.FontWeight,
                    style.TextAlign,
                    style.TextDecoration,
                    VerticalAlign.Middle
                );
            }
        }

        // 渲染图片
        if (element is ImageElement imageElement)
        {
            RenderImage(box, imageElement);
        }

        // 渲染视频帧
        if (element is Miko.Core.DomElements.VideoElement videoElement)
        {
            RenderVideoFrame(box, videoElement);
        }
    }

    /// <summary>
    /// 渲染图片元素。真实图已解码时填满内容盒（保持既有行为）；尚未就绪时回退到占位图
    /// （object-fit: contain，保持纵横比），无占位图则仅保留背景色。加载完成后引擎标脏，下一帧自动切换。
    /// </summary>
    private void RenderImage(LayoutBox box, ImageElement img)
    {
        if (_painter == null) return;

        var content = box.BoxModel.Content;
        if (content.Width <= 0 || content.Height <= 0) return;

        if (img.Bitmap != null)
        {
            _painter.DrawImage(img.Bitmap, content);
            return;
        }

        if (img.PlaceholderBitmap != null)
        {
            var dst = FitContain(img.PlaceholderBitmap.Width, img.PlaceholderBitmap.Height, content);
            _painter.DrawImage(img.PlaceholderBitmap, dst);
        }
    }

    /// <summary>
    /// 把视频当前帧合成进内容盒。帧是一张 GPU 图像，DrawImage 后即与其它元素进入同一
    /// Skia 命令流，自动获得 overflow 裁剪、圆角、opacity、transform 与兄弟覆盖等链路。
    /// 首帧前回退到 poster 占位图，无 poster 时仅保留背景色（已在 RenderBackground 绘制）。
    /// </summary>
    private void RenderVideoFrame(LayoutBox box, Miko.Core.DomElements.VideoElement video)
    {
        if (_painter == null) return;

        var content = box.BoxModel.Content;
        if (content.Width <= 0 || content.Height <= 0) return;

        var frame = video.Session?.FrameSource.AcquireCurrentFrame(GraphicsContext);
        if (frame != null)
        {
            try
            {
                // 视频默认 object-fit: contain（letterbox），保持纵横比不变形。
                var dst = FitContain(frame.Width, frame.Height, content);
                _painter.DrawImage(frame, dst);
            }
            finally
            {
                video.Session!.FrameSource.ReleaseCurrentFrame();
            }
            return;
        }

        // 首帧前：绘制 poster 占位图（若已解码）。
        if (video.PosterBitmap != null)
        {
            var dst = FitContain(video.PosterBitmap.Width, video.PosterBitmap.Height, content);
            _painter.DrawImage(video.PosterBitmap, dst);
        }
    }

    /// <summary>
    /// 计算 object-fit: contain 的目标矩形：在 <paramref name="content"/> 内按源纵横比
    /// 等比缩放并居中，产生上下或左右的 letterbox 空白（由背景色填充）。
    /// </summary>
    private static RectF FitContain(float srcW, float srcH, RectF content)
    {
        if (srcW <= 0 || srcH <= 0) return content;

        float scale = Math.Min(content.Width / srcW, content.Height / srcH);
        float w = srcW * scale;
        float h = srcH * scale;
        float x = content.X + (content.Width - w) / 2f;
        float y = content.Y + (content.Height - h) / 2f;
        return new RectF(x, y, w, h);
    }

    /// <summary>
    /// 渲染输入框元素
    /// </summary>
    private void RenderInputElement(LayoutBox box, InputElement inputElement)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var contentRect = box.BoxModel.Content;
        bool isFocused = inputElement.HasState(Miko.Core.ElementState.Focus);

        switch (inputElement.Type)
        {
            case InputType.Checkbox:
                _painter.DrawCheckbox(
                    contentRect,
                    inputElement.Checked,
                    style.BorderTopColor,
                    style.Color,
                    style.BackgroundColor
                );
                break;

            case InputType.Radio:
                _painter.DrawRadio(
                    contentRect,
                    inputElement.Checked,
                    style.BorderTopColor,
                    style.Color,
                    style.BackgroundColor
                );
                break;

            case InputType.Range:
                // 获取 range 伪元素样式
                Style? trackStyle = null;
                Style? progressStyle = null;
                Style? thumbStyle = null;

                if (inputElement.PseudoElementStyles != null)
                {
                    inputElement.PseudoElementStyles.TryGetValue(PseudoElementType.RangeTrack, out trackStyle);
                    inputElement.PseudoElementStyles.TryGetValue(PseudoElementType.RangeProgress, out progressStyle);
                    inputElement.PseudoElementStyles.TryGetValue(PseudoElementType.RangeThumb, out thumbStyle);
                }

                _painter.DrawRange(
                    contentRect,
                    inputElement.NumericValue,
                    inputElement.Min,
                    inputElement.Max,
                    trackStyle,
                    progressStyle,
                    thumbStyle,
                    style.FontSize.Value  // Pass fontSize for em/rem resolution
                );
                break;

            case InputType.Password:
                if (!string.IsNullOrEmpty(inputElement.Value))
                {
                    _painter.DrawPasswordText(
                        inputElement.Value.Length,
                        contentRect,
                        style.Color,
                        style.FontSize.Value
                    );
                }
                else if (!string.IsNullOrEmpty(inputElement.Placeholder) && !isFocused)
                {
                    _painter.DrawText(
                        inputElement.Placeholder,
                        contentRect,
                        Color.Gray,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left,
                        VerticalAlign.Middle
                    );
                }
                if (isFocused)
                {
                    var maskedText = new string('●', (inputElement.Value ?? string.Empty).Length);
                    _painter.DrawTextCursor(contentRect, maskedText, inputElement.CursorPosition, style.FontFamily, style.FontSize.Value, style.FontWeight);
                }
                break;

            case InputType.Text:
            default:
                if (!string.IsNullOrEmpty(inputElement.Value))
                {
                    _painter.DrawText(
                        inputElement.Value,
                        contentRect,
                        style.Color,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left,
                        VerticalAlign.Middle
                    );
                }
                else if (!string.IsNullOrEmpty(inputElement.Placeholder) && !isFocused)
                {
                    _painter.DrawText(
                        inputElement.Placeholder,
                        contentRect,
                        Color.Gray,
                        style.FontFamily,
                        style.FontSize.Value,
                        style.FontWeight,
                        TextAlign.Left,
                        VerticalAlign.Middle
                    );
                }
                if (isFocused)
                {
                    _painter.DrawTextCursor(contentRect, inputElement.Value ?? string.Empty, inputElement.CursorPosition, style.FontFamily, style.FontSize.Value, style.FontWeight);
                }
                break;
        }
    }

    /// <summary>
    /// 渲染下拉选择框元素
    /// </summary>
    private void RenderSelectElement(LayoutBox box, SelectElement selectElement)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var borderBox = box.BoxModel.BorderBox;

        _painter.DrawSelect(
            borderBox,
            selectElement.GetDisplayText(),
            selectElement.IsOpen,
            style.BorderTopColor,
            style.BackgroundColor,
            style.Color,
            Color.Gray,
            style.FontSize.Value
        );

        if (selectElement.IsOpen)
        {
            _pendingDropdowns.Add((box, selectElement, _currentScrollOffsetX, _currentScrollOffsetY));
        }
    }

    /// <summary>
    /// 渲染下拉选项列表
    /// </summary>
    private void RenderSelectDropdown(LayoutBox box, SelectElement selectElement, float scrollOffsetX, float scrollOffsetY)
    {
        if (_painter == null) return;

        var style = box.ComputedStyle;
        var borderBox = box.BoxModel.BorderBox;

        float screenLeft = borderBox.Left - scrollOffsetX;
        float screenTop = borderBox.Bottom - scrollOffsetY;

        var options = new List<(string text, bool isSelected, bool isDisabled, bool isGroupLabel)>();
        var allOptions = selectElement.GetAllOptions();
        int optionIndex = 0;

        foreach (var child in selectElement.Children)
        {
            if (child is OptGroupElement optGroup)
            {
                options.Add((optGroup.Label ?? string.Empty, false, false, true));

                foreach (var groupChild in optGroup.Children)
                {
                    if (groupChild is OptionElement option)
                    {
                        bool isSelected = optionIndex == selectElement.SelectedIndex || option.Selected;
                        bool isDisabled = option.IsDisabled;
                        options.Add((option.TextContent ?? option.Value ?? string.Empty, isSelected, isDisabled, false));
                        optionIndex++;
                    }
                }
            }
            else if (child is OptionElement option)
            {
                bool isSelected = optionIndex == selectElement.SelectedIndex || option.Selected;
                bool isDisabled = option.IsDisabled;
                options.Add((option.TextContent ?? option.Value ?? string.Empty, isSelected, isDisabled, false));
                optionIndex++;
            }
        }

        float optionHeight = style.FontSize.Value + 8;
        float dropdownHeight = options.Count * optionHeight;
        var dropdownRect = new RectF(
            screenLeft,
            screenTop,
            borderBox.Width,
            dropdownHeight
        );

        _painter.DrawSelectDropdown(
            dropdownRect,
            options,
            Color.White,
            style.BorderTopColor,
            style.Color,
            new Color(0, 120, 215),
            Color.White,
            Color.Gray,
            Color.Gray,
            style.FontSize.Value
        );
    }
}
