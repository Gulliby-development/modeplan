namespace Modeplan;

public static class PlanCmd
{
    public const string Usage = "modeplan plan OLD NEW";

    public static int Run(IReadOnlyList<string> args, IIo io)
    {
        var parsed = Options.Parse(args, Array.Empty<Options.Spec>());
        if (parsed.Args.Count == 0)
        {
            throw new Failure("missing OLD");
        }

        if (parsed.Args.Count == 1)
        {
            throw new Failure("missing NEW");
        }

        if (parsed.Args.Count > 2)
        {
            throw new Failure("too many arguments");
        }

        var oldText = parsed.Args[0];
        var oldMode = Mode.Parse(oldText)
            ?? throw new Failure("invalid mode " + Text.Show(oldText));

        var newText = parsed.Args[1];
        var newMode = Mode.Parse(newText)
            ?? throw new Failure("invalid mode " + Text.Show(newText));

        var changed = Delta.ChangedBits(oldMode, newMode);
        var symbolic = Canon.Build(oldMode, newMode);
        var numeric = Mode.Octal(newMode).TrimStart('0');
        if (numeric.Length == 0)
        {
            numeric = "0";
        }

        var output = Text.Line("old", Mode.Octal(oldMode) + " " + Mode.Ls(oldMode)) + "\n"
            + Text.Line("new", Mode.Octal(newMode) + " " + Mode.Ls(newMode)) + "\n"
            + Text.Line("symbolic", symbolic.Length == 0 ? "-" : symbolic) + "\n"
            + Text.Line("numeric", numeric) + "\n"
            + Text.Line("changed", Text.Plural(changed, "bit")) + "\n";

        io.Out(output);
        return changed == 0 ? 1 : 0;
    }
}
