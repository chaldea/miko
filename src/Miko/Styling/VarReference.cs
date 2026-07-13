namespace Miko.Styling;

/// <summary>
/// 对某个自定义样式变量的引用（如 <c>var(--button-color)</c>）。
/// <paramref name="Name"/> 为变量名（约定以 <c>--</c> 开头），
/// <paramref name="Fallback"/> 为变量在作用域内未定义时使用的回退值。
/// </summary>
public readonly record struct VarReference(string Name, VarValue? Fallback = null);

/// <summary>
/// 样式辅助方法的静态入口。通过 <c>using static Miko.Styling.Css;</c> 即可直接书写
/// <c>Color = Var("--button-color")</c>。
/// </summary>
public static class Css
{
    /// <summary>创建对自定义变量 <paramref name="name"/> 的引用。</summary>
    public static VarReference Var(string name) => new(name);

    /// <summary>创建带回退值的变量引用；变量在作用域内未定义时使用 <paramref name="fallback"/>。</summary>
    public static VarReference Var(string name, VarValue fallback) => new(name, fallback);
}
