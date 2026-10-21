namespace Modeplan;

public static class PermTest
{
    public sealed record ParsedTest(char Prefix, int Mask);

    public static ParsedTest Parse(string test)
    {
        var prefix = test.Length > 0 && test[0] is '-' or '/' ? test[0] : '\0';
        var start = prefix == '\0' ? 0 : 1;
        var length = test.Length - start;

        if (length is < 1 or > 4)
        {
            throw InvalidTest(test);
        }

        for (var index = start; index < test.Length; index++)
        {
            if (test[index] is < '0' or > '7')
            {
                throw InvalidTest(test);
            }
        }

        var mask = Mode.Parse(test[start..]);
        if (mask is null)
        {
            throw InvalidTest(test);
        }

        return new ParsedTest(prefix, mask.Value);
    }

    public static bool Matches(ParsedTest test, int mode)
    {
        return test.Prefix switch
        {
            '-' => (mode & test.Mask) == test.Mask,
            '/' => (mode & test.Mask) != 0,
            _ => mode == test.Mask
        };
    }

    private static Failure InvalidTest(string test)
    {
        return new Failure("invalid test " + Text.Show(test));
    }
}
