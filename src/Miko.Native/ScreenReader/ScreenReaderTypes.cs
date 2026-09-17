namespace Miko.Native.ScreenReader;

/// <summary>屏幕阅读器开关状态。</summary>
/// <param name="Value">屏幕阅读器（VoiceOver / TalkBack / 讲述人）是否启用。</param>
public sealed record ScreenReaderState(bool Value);

/// <summary>朗读选项。</summary>
public sealed record SpeakOptions
{
    /// <summary>要朗读的文本（必填）。</summary>
    public required string Value { get; init; }

    /// <summary>朗读语言，ISO 639-1 代码（Android）。</summary>
    public string? Language { get; init; }
}
