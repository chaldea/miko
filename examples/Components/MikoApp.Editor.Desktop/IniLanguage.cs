using System.Text;
using Miko.Components.Editor;
using Miko.Highlight;

namespace MikoApp.Editor.Desktop;

internal sealed class IniLanguage : IEditorLanguageService, IEditorFormatter
{
    public IReadOnlyList<CodeToken>? TokenizeLine(EditorDocument document, int line)
    {
        var text = document.GetLine(line).Span;
        var content = text.TrimStart();
        if (content.IsEmpty) return null;
        int start = text.Length - content.Length;
        if (content[0] is ';' or '#') return [new(CodeTokenType.Comment, start, content.Length)];
        if (content[0] == '[') return [new(CodeTokenType.Type, start, content.Length)];
        int equal = content.IndexOf('=');
        if (equal <= 0) return null;
        return [new(CodeTokenType.Keyword, start, equal), new(CodeTokenType.String, start + equal + 1, content.Length - equal - 1)];
    }

    public string Format(EditorDocument document)
    {
        var result = new StringBuilder(document.Text.Length);
        for (int i = 0; i < document.LineCount; i++)
        {
            var line = document.GetLine(i).Span;
            var content = line.TrimStart();
            int equal = content.IndexOf('=');
            if (equal <= 0 || content[0] is ';' or '#' or '[') result.Append(line);
            else result.Append(content[..equal].TrimEnd()).Append(" = ").Append(content[(equal + 1)..].Trim());
            if (i + 1 < document.LineCount)
            {
                int end = document.GetLineStart(i) + line.Length;
                result.Append(document.Text.AsSpan(end, document.GetLineStart(i + 1) - end));
            }
        }
        return result.ToString();
    }
}
