namespace Modeplan;

public static class ApplyCmd
{
    public const string Usage = "modeplan apply MODE EXPR [--dir] [--umask OCT]";

    public static int Run(IReadOnlyList<string> args, IIo io)
    {
        var parsed = Options.Parse(
            args,
            new[]
            {
                new Options.Spec("--dir", false),
                new Options.Spec("--umask", true)
            });

        if (parsed.Args.Count == 0)
        {
            throw new Failure("missing MODE");
        }

        if (parsed.Args.Count == 1)
        {
            throw new Failure("missing EXPR");
        }

        if (parsed.Args.Count > 2)
        {
            throw new Failure("too many arguments");
        }

        var mode = Mode.Parse(parsed.Args[0]);
        if (mode is null)
        {
            throw new Failure($"invalid mode {Text.Show(parsed.Args[0])}");
        }

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

        var initialMode = mode.Value;
        var steps = Semantics.Steps(
            expression,
            initialMode,
            parsed.Flags.Contains("--dir"),
            umask);
        var finalMode = steps.Count == 0 ? initialMode : steps[^1].Mode;
        var changed = Changes.Count(initialMode, finalMode);
        var lines = Steps.Render(initialMode, steps);
        lines.Add(Text.Line("result", Mode.Octal(finalMode) + " " + Mode.Ls(finalMode)));
        lines.Add(Text.Line("changed", Text.Plural(changed, "bit")));

        io.Out(string.Join("\n", lines) + "\n");
        return changed != 0 ? 0 : 1;
    }
}
