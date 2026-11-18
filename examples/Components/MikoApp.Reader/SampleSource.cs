using System.Text;
using Miko.Components.Reader;

namespace MikoApp.Reader;

/// <summary>Directory and page callbacks can be replaced independently with requests to a remote API.</summary>
public static class SampleSource
{
    public static IReaderSource Create(bool comic, string? title = null, string? paragraph = null) => new DelegateReaderSource(
        _ => Task.FromResult(new ReaderDocument(title ?? (comic ? "山间来信 · 绘本" : "山间来信"), Array.AsReadOnly(
            Enumerable.Range(0, comic ? 5 : 3).Select(i => new ReaderEntry(i.ToString(), $"第 {i + 1} {(comic ? "页" : "章")}")).ToArray()))),
        (entry, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            ReaderContent content = comic ? new ReaderImageContent(Encoding.UTF8.GetBytes($"""
                <svg xmlns="http://www.w3.org/2000/svg" width="600" height="900" viewBox="0 0 600 900">
                  <rect width="600" height="900" fill="#f4ead6"/>
                  <rect x="30" y="30" width="540" height="530" rx="8" fill="#9db9b0"/>
                  <circle cx="420" cy="150" r="55" fill="#f5dd99"/>
                  <path d="M30 500L220 180L380 440L480 300L570 490V560H30Z" fill="#46625b"/>
                  <rect x="30" y="590" width="540" height="230" rx="8" fill="#d9ccae"/>
                  <path d="M50 790Q200 600 350 760T550 710" fill="none" stroke="#778d82" stroke-width="12"/>
                  <text x="300" y="865" text-anchor="middle" font-size="26" fill="#354a43">{int.Parse(entry.Id) + 1}</text>
                </svg>
                """)) : new ReaderTextContent($"{entry.Title}  清晨的山路\n\n" + string.Join("\n\n", Enumerable.Repeat(
                    (paragraph ?? "清晨推开窗，山谷里还留着薄薄的雾。沿着石阶往下走，溪流的声音一点点清晰起来。路边的树叶上挂着水珠，远处有人打开了第一扇门。") + "\n" +
                    "A letter arrived from the mountains. Its pages held the quiet of a long walk, the sound of rain, and a story waiting to be read.", 12)));
            return Task.FromResult(content);
        });
}
