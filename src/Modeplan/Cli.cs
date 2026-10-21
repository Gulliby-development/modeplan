namespace Modeplan;

public static class Cli
{
    public const string Usage =
        "usage: modeplan apply MODE EXPR [--dir] [--umask OCT]\n"
        + "       modeplan plan OLD NEW\n"
        + "       modeplan match LISTING TEST... [--kind f|d]\n"
        + "       modeplan audit LISTING [--strict]\n"
        + "       modeplan tree LISTING EXPR [--umask OCT]\n";

    private static readonly Dictionary<string, Func<IReadOnlyList<string>, IIo, int>> Commands =
        new(StringComparer.Ordinal)
        {
            ["apply"] = ApplyCmd.Run,
            ["plan"] = PlanCmd.Run,
            ["match"] = MatchCmd.Run,
            ["audit"] = AuditCmd.Run,
            ["tree"] = TreeCmd.Run
        };

    public static int Run(string[] argv, IIo io)
    {
        try
        {
            if (argv.Length == 1 && string.Equals(argv[0], "--help", StringComparison.Ordinal))
            {
                io.Out(Usage);
                return 0;
            }

            if (argv.Length == 0)
            {
                throw new Failure("missing command (apply, plan, match, audit or tree)");
            }

            var command = argv[0];
            if (!Commands.TryGetValue(command, out var run))
            {
                throw new Failure("unknown command " + Text.Show(command));
            }

            return run(argv[1..], io);
        }
        catch (Failure failure)
        {
            io.Err("modeplan: " + failure.Message + "\n");
            return 2;
        }
    }
}
