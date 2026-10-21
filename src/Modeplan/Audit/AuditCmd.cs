namespace Modeplan;

public static class AuditCmd
{
    public const string Usage = "modeplan audit LISTING [--strict]";

    private static readonly IReadOnlyList<Options.Spec> OptionSpecs =
        new[] { new Options.Spec("--strict", false) };

    public static int Run(IReadOnlyList<string> args, IIo io)
    {
        var parsed = Options.Parse(args, OptionSpecs);
        if (parsed.Args.Count == 0)
        {
            throw new Failure("missing LISTING");
        }

        if (parsed.Args.Count > 1)
        {
            throw new Failure("too many arguments");
        }

        var loaded = Loader.Read(io, parsed.Args[0]);
        var result = Findings.Format(loaded, parsed.Flags.Contains("--strict"));
        io.Out(result.Output);
        return result.ExitCode;
    }
}
