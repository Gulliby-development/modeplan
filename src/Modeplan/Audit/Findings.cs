using System.Text;

namespace Modeplan;

public static class Findings
{
    public static (string Output, int ExitCode) Format(Loader.Loaded loaded, bool strict)
    {
        var output = new StringBuilder();
        var count = 0;

        foreach (var entry in loaded.Entries)
        {
            foreach (var finding in Rules.Evaluate(entry, strict))
            {
                output.Append(Text.Show(entry.Path));
                output.Append(' ');
                output.Append(finding.Code);
                output.Append(' ');
                output.Append(finding.Message);
                output.Append('\n');
                count++;
            }
        }

        if (count == 0)
        {
            output.Append("ok, ");
            output.Append(Text.Plural(loaded.Entries.Count, "path"));
            output.Append('\n');
            return (output.ToString(), 0);
        }

        output.Append(Text.Plural(count, "finding"));
        output.Append('\n');
        return (output.ToString(), 1);
    }
}
