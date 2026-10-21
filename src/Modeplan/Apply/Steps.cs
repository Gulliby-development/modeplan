using System.Globalization;

namespace Modeplan;

public static class Steps
{
    public static List<string> Render(int initialMode, IReadOnlyList<Semantics.Step> steps)
    {
        var rows = new List<IReadOnlyList<string>>(steps.Count + 1)
        {
            new[] { "0", "start", Mode.Octal(initialMode), Mode.Ls(initialMode) }
        };

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            rows.Add(new[]
            {
                (index + 1).ToString(CultureInfo.InvariantCulture),
                step.Action,
                Mode.Octal(step.Mode),
                Mode.Ls(step.Mode)
            });
        }

        return Text.Table(
            new[] { "STEP", "ACTION", "MODE", "SYMBOLIC" },
            rows);
    }
}
