namespace Miko.Styling;

public class MediaCondition
{
    public float? MinWidth { get; set; }
    public float? MaxWidth { get; set; }
    public float? MinHeight { get; set; }
    public float? MaxHeight { get; set; }

    public bool Matches(ViewportInfo viewport)
    {
        if (MinWidth.HasValue && viewport.Width < MinWidth.Value) return false;
        if (MaxWidth.HasValue && viewport.Width > MaxWidth.Value) return false;
        if (MinHeight.HasValue && viewport.Height < MinHeight.Value) return false;
        if (MaxHeight.HasValue && viewport.Height > MaxHeight.Value) return false;
        return true;
    }
}
