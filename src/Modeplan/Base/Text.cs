using System.Globalization;
using System.Text;

namespace Modeplan;

public static class Text
{
    public static string Show(string s)
    {
        var result = new StringBuilder();
        result.Append('"');

        foreach (var rune in s.EnumerateRunes())
        {
            result.Append(rune.Value is >= 0x20 and <= 0x7e ? rune.ToString() : "?");
        }

        result.Append('"');
        return result.ToString();
    }

    public static string Line(string key, string value)
    {
        return key + new string(' ', Math.Max(0, 10 - Width(key))) + "  " + value;
    }

    public static List<string> Table(
        IReadOnlyList<string> header,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var columnCount = header.Count;
        foreach (var row in rows)
        {
            columnCount = Math.Max(columnCount, row.Count);
        }

        var widths = new int[columnCount];
        for (var column = 0; column < header.Count; column++)
        {
            widths[column] = Width(header[column]);
        }

        foreach (var row in rows)
        {
            for (var column = 0; column < row.Count; column++)
            {
                widths[column] = Math.Max(widths[column], Width(row[column]));
            }
        }

        var result = new List<string>(rows.Count + 1)
        {
            FormatRow(header, widths)
        };

        foreach (var row in rows)
        {
            result.Add(FormatRow(row, widths));
        }

        return result;
    }

    public static string Plural(int n, string word)
    {
        return n.ToString(CultureInfo.InvariantCulture) + " " + word + (n == 1 ? "" : "s");
    }

    private static string FormatRow(IReadOnlyList<string> cells, IReadOnlyList<int> widths)
    {
        var formatted = new string[widths.Count];
        for (var column = 0; column < widths.Count; column++)
        {
            var cell = column < cells.Count ? cells[column] : "";
            formatted[column] = cell + new string(' ', widths[column] - Width(cell));
        }

        return string.Join("  ", formatted).TrimEnd(' ');
    }

    private static int Width(string value)
    {
        var width = 0;
        foreach (var _ in value.EnumerateRunes())
        {
            width++;
        }

        return width;
    }
}
