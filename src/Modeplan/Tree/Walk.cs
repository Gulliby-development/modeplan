namespace Modeplan;

public static class Walk
{
    public record Change(Listing.Entry Entry, int Mode);

    public static IReadOnlyList<Change> Evaluate(
        IReadOnlyList<Listing.Entry> entries,
        Expression expression,
        int umask)
    {
        var changes = new List<Change>();
        foreach (var entry in entries)
        {
            var steps = Semantics.Steps(expression, entry.Mode, entry.Dir, umask);
            var finalMode = steps.Count == 0 ? entry.Mode : steps[^1].Mode;
            if (finalMode != entry.Mode)
            {
                changes.Add(new Change(entry, finalMode));
            }
        }

        return changes;
    }
}
