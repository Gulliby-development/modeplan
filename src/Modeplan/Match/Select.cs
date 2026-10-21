namespace Modeplan;

public static class Select
{
    public static IReadOnlyList<Listing.Entry> MatchingEntries(
        IReadOnlyList<Listing.Entry> entries,
        IReadOnlyList<PermTest.ParsedTest> tests,
        bool? kind)
    {
        var selected = new List<Listing.Entry>();

        foreach (var entry in entries)
        {
            if (kind.HasValue && entry.Dir != kind.Value)
            {
                continue;
            }

            var matches = true;
            foreach (var test in tests)
            {
                if (!PermTest.Matches(test, entry.Mode))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                selected.Add(entry);
            }
        }

        return selected;
    }
}

