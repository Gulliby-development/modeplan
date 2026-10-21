namespace Modeplan;

public static class Options
{
    public record Spec(string Name, bool Takes);

    public record Parsed(
        IReadOnlySet<string> Flags,
        IReadOnlyDictionary<string, string> Values,
        IReadOnlyList<string> Args);

    public static Parsed Parse(IReadOnlyList<string> argv, IReadOnlyList<Spec> specs)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var args = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < argv.Count; index++)
        {
            var argument = argv[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                args.Add(argument);
                continue;
            }

            var equals = argument.IndexOf('=');
            var name = equals < 0 ? argument : argument[..equals];
            Spec? spec = null;
            foreach (var candidate in specs)
            {
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    spec = candidate;
                    break;
                }
            }

            if (spec is null)
            {
                throw new Failure($"unknown option {Text.Show(argument)}");
            }

            if (!spec.Takes && equals >= 0)
            {
                throw new Failure($"{name} takes no value");
            }

            if (spec.Takes)
            {
                string value;
                if (equals >= 0)
                {
                    value = argument[(equals + 1)..];
                }
                else if (index + 1 < argv.Count)
                {
                    value = argv[++index];
                }
                else
                {
                    throw new Failure($"{name} needs a value");
                }

                if (!seen.Add(name))
                {
                    throw new Failure($"repeated option {name}");
                }

                values.Add(name, value);
            }
            else
            {
                if (!seen.Add(name))
                {
                    throw new Failure($"repeated option {name}");
                }

                flags.Add(name);
            }
        }

        return new Parsed(flags, values, args);
    }
}
