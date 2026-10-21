namespace Modeplan;

public static class Loader
{
    public record Loaded(string Display, IReadOnlyList<Listing.Entry> Entries);

    public static Loaded Read(IIo io, string arg)
    {
        var source = Input.Read(io, arg);
        IReadOnlyList<Listing.Entry> entries;
        try
        {
            entries = Listing.Parse(source.Text);
        }
        catch (Failure failure)
        {
            throw new Failure($"{source.Display}: {failure.Message}");
        }

        if (entries.Count == 0)
        {
            throw new Failure($"{source.Display}: no entries");
        }

        return new Loaded(source.Display, entries);
    }
}
