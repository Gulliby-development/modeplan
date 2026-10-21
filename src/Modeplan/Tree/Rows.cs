using System.Globalization;

namespace Modeplan;

public static class Rows
{
    public static string Render(IReadOnlyList<Walk.Change> changes, int entryCount)
    {
        var lines = new List<string>();
        if (changes.Count > 0)
        {
            var rows = new List<IReadOnlyList<string>>(changes.Count);
            foreach (var change in changes)
            {
                var entry = change.Entry;
                rows.Add(new[]
                {
                    entry.Line.ToString(CultureInfo.InvariantCulture),
                    entry.Dir ? "d" : "f",
                    Mode.Octal(entry.Mode),
                    Mode.Octal(change.Mode),
                    Text.Show(entry.Path)
                });
            }

            lines.AddRange(Text.Table(
                new[] { "LINE", "KIND", "OLD", "NEW", "PATH" },
                rows));
        }

        lines.Add(Text.Line(
            "changed",
            changes.Count.ToString(CultureInfo.InvariantCulture)
                + " of "
                + entryCount.ToString(CultureInfo.InvariantCulture)));
        return string.Join("\n", lines) + "\n";
    }
}
