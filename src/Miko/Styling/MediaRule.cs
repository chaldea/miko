namespace Miko.Styling;

public class MediaRule
{
    public MediaCondition Condition { get; set; } = null!;
    public List<StyleRule> Rules { get; set; } = new();
}
