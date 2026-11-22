# Miko Editor

Run the INI editor from the repository root.

```sh
dotnet run --project examples/Components/MikoApp.Editor.Desktop
```

Edit an assignment, use **Format** to normalize its spacing, then **Show changes** to see additions and deletions against the original file. **Edit** returns to the same buffer. The theme button changes the editor palette in either mode.

## Rendering measurements

```sh
dotnet test tests/Miko.Components.Tests -c Release --filter FullyQualifiedName~Tests.Editor --logger "console;verbosity=detailed"
```

The test opens UTF-8 files from a warm filesystem cache and paints C# text on an 800 × 440 CPU surface with a warmed-up font backend. Open time includes reading the file, building the document index and drawing the first frame. The 120 timed host frames scroll from the first line to the last and back.

| Content lines | File bytes | Open to first paint | Managed bytes allocated on open | Retained managed heap increase | Scroll p95 | Slowest frame | Frames over 16.67 ms |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 80 | 2,620 | 0.44 ms | 160,072 | 55,968 | 0.15 ms | 0.39 ms | 0 / 120 |
| 100,000 | 3,877,780 | 4.77 ms | 16,859,752 | 8,357,464 | 1.47 ms | 3.16 ms | 0 / 120 |

Measured on Apple M6, macOS arm64, .NET 11 Release. Retained memory includes the loaded string and line index. Native allocations and window presentation are outside this measurement. Tests assert the number of tokenized rows stays proportional to the viewport, and exercise editing at the end of a one-million-character line.
