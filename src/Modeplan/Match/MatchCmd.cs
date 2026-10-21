using System.Globalization;

namespace Modeplan;

public static class MatchCmd
{
    public const string Usage = "modeplan match LISTING TEST... [--kind f|d]";

    public static int Run(IReadOnlyList<string> args, IIo io)
    {
        var parsed = Options.Parse(args, [new Options.Spec("--kind", true)]);
        if (parsed.Args.Count == 0)
        {
            throw new Failure("missing LISTING");
        }

        if (parsed.Args.Count == 1)
        {
            throw new Failure("missing TEST");
        }

        var listing = Loader.Read(io, parsed.Args[0]);
        var tests = new List<PermTest.ParsedTest>();
        for (var index = 1; index < parsed.Args.Count; index++)
        {
            tests.Add(PermTest.Parse(parsed.Args[index]));
        }

        bool? kind = null;
        if (parsed.Values.TryGetValue("--kind", out var kindValue))
        {
            if (!string.Equals(kindValue, "f", StringComparison.Ordinal)
                && !string.Equals(kindValue, "d", StringComparison.Ordinal))
            {
                throw new Failure("invalid kind " + Text.Show(kindValue));
            }

            kind = string.Equals(kindValue, "d", StringComparison.Ordinal);
        }

        var matches = Select.MatchingEntries(listing.Entries, tests, kind);
        var lines = new List<string>();
        if (matches.Count > 0)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (var entry in matches)
            {
                rows.Add(
                [
                    entry.Line.ToString(CultureInfo.InvariantCulture),
                    entry.Dir ? "d" : "f",
                    Mode.Octal(entry.Mode),
                    Mode.Ls(entry.Mode),
                    Text.Show(entry.Path)
                ]);
            }

            lines.AddRange(Text.Table(["LINE", "KIND", "MODE", "SYMBOLIC", "PATH"], rows));
        }

        var matchedCount = matches.Count.ToString(CultureInfo.InvariantCulture);
        var entryCount = listing.Entries.Count.ToString(CultureInfo.InvariantCulture);
        lines.Add(Text.Line("matched", matchedCount + " of " + entryCount));
        io.Out(string.Join("\n", lines) + "\n");

        return matches.Count > 0 ? 0 : 1;
    }
}

