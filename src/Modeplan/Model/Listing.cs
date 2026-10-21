using System.Text;

namespace Modeplan;

public static class Listing
{
    public record Entry(int Line, bool Dir, int Mode, string Path);

    public static IReadOnlyList<Entry> Parse(string text)
    {
        var entries = new List<Entry>();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var lineNumber = 1;
        var lineStart = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                ParseLine(text[lineStart..index], lineNumber, entries, paths);
                lineStart = index + 1;
                lineNumber++;
            }
        }

        if (lineStart < text.Length)
        {
            ParseLine(text[lineStart..], lineNumber, entries, paths);
        }

        return entries;
    }

    private static void ParseLine(
        string line,
        int lineNumber,
        ICollection<Entry> entries,
        ISet<string> paths)
    {
        if (line.EndsWith('\r'))
        {
            line = line[..^1];
        }

        var first = 0;
        while (first < line.Length && IsBlank(line[first]))
        {
            first++;
        }

        if (first == line.Length || line[first] == '#')
        {
            return;
        }

        CheckControls(line, lineNumber);

        var kindStart = first;
        var kindEnd = ScanTokenEnd(line, kindStart);
        var kind = line[kindStart..kindEnd];
        var cursor = SkipBlanks(line, kindEnd);
        var hasMode = cursor < line.Length;
        var modeStart = cursor;
        var modeEnd = hasMode ? ScanTokenEnd(line, modeStart) : modeStart;

        if (kind != "f" && kind != "d")
        {
            throw Error("ML002", $"invalid kind {Text.Show(kind)}", lineNumber, Column(line, kindStart));
        }

        if (!hasMode)
        {
            throw Error("ML003", "missing mode", lineNumber, EndColumn(line));
        }

        var modeText = line[modeStart..modeEnd];
        var mode = Mode.Parse(modeText);
        if (mode is null)
        {
            throw Error("ML004", $"invalid mode {Text.Show(modeText)}", lineNumber, Column(line, modeStart));
        }

        var pathStart = SkipBlanks(line, modeEnd);
        var pathEnd = TrimTrailingBlanks(line, pathStart);
        if (pathStart == pathEnd)
        {
            throw Error("ML005", "missing path", lineNumber, EndColumn(line));
        }

        var path = line[pathStart..pathEnd];
        if (!paths.Add(path))
        {
            throw Error("ML006", $"repeated path {Text.Show(path)}", lineNumber, Column(line, pathStart));
        }

        entries.Add(new Entry(lineNumber, kind == "d", mode.Value, path));
    }

    private static void CheckControls(string line, int lineNumber)
    {
        var column = 1;
        foreach (var rune in line.EnumerateRunes())
        {
            if ((rune.Value is >= 0 and <= 0x1f && rune.Value != '\t') || rune.Value == 0x7f)
            {
                throw Error("ML001", $"unexpected character {Text.Show(rune.ToString())}", lineNumber, column);
            }

            column++;
        }
    }

    private static Failure Error(string code, string problem, int line, int column)
    {
        return new Failure($"{code} {problem} at {line}:{column}");
    }

    private static int ScanTokenEnd(string line, int start)
    {
        var index = start;
        while (index < line.Length && !IsBlank(line[index]))
        {
            index++;
        }

        return index;
    }

    private static int SkipBlanks(string line, int start)
    {
        var index = start;
        while (index < line.Length && IsBlank(line[index]))
        {
            index++;
        }

        return index;
    }

    private static int TrimTrailingBlanks(string line, int start)
    {
        var end = line.Length;
        while (end > start && IsBlank(line[end - 1]))
        {
            end--;
        }

        return end;
    }

    private static bool IsBlank(char character)
    {
        return character is ' ' or '\t';
    }

    private static int Column(string line, int utf16Index)
    {
        var column = 1;
        foreach (var _ in line.AsSpan(0, utf16Index).ToString().EnumerateRunes())
        {
            column++;
        }

        return column;
    }

    private static int EndColumn(string line)
    {
        return Column(line, line.Length);
    }
}
