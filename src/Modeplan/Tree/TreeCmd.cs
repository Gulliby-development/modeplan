namespace Modeplan;

public static class TreeCmd
{
    public const string Usage = "modeplan tree LISTING EXPR [--umask OCT]";

    public static int Run(IReadOnlyList<string> args, IIo io)
    {
        var parsed = Options.Parse(args, new[]
        {
            new Options.Spec("--umask", true)
        });

        if (parsed.Args.Count < 1)
        {
            throw new Failure("missing LISTING");
        }

        if (parsed.Args.Count < 2)
        {
            throw new Failure("missing EXPR");
        }

        if (parsed.Args.Count > 2)
        {
            throw new Failure("too many arguments");
        }

        var listing = Loader.Read(io, parsed.Args[0]);
        Expression expression;
        try
        {
            expression = ExprParser.Parse(parsed.Args[1]);
        }
        catch (Failure failure)
        {
            throw new Failure($"invalid expression {Text.Show(parsed.Args[1])}: {failure.Message}");
        }

        var umask = 18;
        if (parsed.Values.TryGetValue("--umask", out var umaskText))
        {
            var parsedUmask = Semantics.ParseUmask(umaskText);
            if (parsedUmask is null)
            {
                throw new Failure($"invalid umask {Text.Show(umaskText)}");
            }

            umask = parsedUmask.Value;
        }

        var changes = Walk.Evaluate(listing.Entries, expression, umask);
        var report = Rows.Render(changes, listing.Entries.Count);
        io.Out(report);
        return changes.Count > 0 ? 0 : 1;
    }
}
