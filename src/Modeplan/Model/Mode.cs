namespace Modeplan;

public static class Mode
{
    public static int? Parse(string text)
    {
        if (text.Length is >= 1 and <= 4)
        {
            var value = 0;
            foreach (var character in text)
            {
                if (character < '0' || character > '7')
                {
                    return null;
                }

                value = (value << 3) | (character - '0');
            }

            return value;
        }

        if (text.Length != 9)
        {
            return null;
        }

        var mode = 0;
        if (!TryParseClass(text, 0, 256, 128, 64, 2048, 's', 'S', ref mode)
            || !TryParseClass(text, 3, 32, 16, 8, 1024, 's', 'S', ref mode)
            || !TryParseClass(text, 6, 4, 2, 1, 512, 't', 'T', ref mode))
        {
            return null;
        }

        return mode;
    }

    public static string Octal(int mode)
    {
        return string.Create(4, mode, static (digits, value) =>
        {
            digits[0] = (char)('0' + ((value >> 9) & 7));
            digits[1] = (char)('0' + ((value >> 6) & 7));
            digits[2] = (char)('0' + ((value >> 3) & 7));
            digits[3] = (char)('0' + (value & 7));
        });
    }

    public static string Ls(int mode)
    {
        var characters = new[]
        {
            (mode & 256) != 0 ? 'r' : '-',
            (mode & 128) != 0 ? 'w' : '-',
            ExecuteCharacter(mode, 64, 2048, 's', 'S'),
            (mode & 32) != 0 ? 'r' : '-',
            (mode & 16) != 0 ? 'w' : '-',
            ExecuteCharacter(mode, 8, 1024, 's', 'S'),
            (mode & 4) != 0 ? 'r' : '-',
            (mode & 2) != 0 ? 'w' : '-',
            ExecuteCharacter(mode, 1, 512, 't', 'T'),
        };

        return new string(characters);
    }

    private static bool TryParseClass(
        string text,
        int index,
        int readBit,
        int writeBit,
        int executeBit,
        int specialBit,
        char specialLower,
        char specialUpper,
        ref int mode)
    {
        if (text[index] == 'r')
        {
            mode |= readBit;
        }
        else if (text[index] != '-')
        {
            return false;
        }

        if (text[index + 1] == 'w')
        {
            mode |= writeBit;
        }
        else if (text[index + 1] != '-')
        {
            return false;
        }

        var execute = text[index + 2];
        if (execute == 'x')
        {
            mode |= executeBit;
        }
        else if (execute == specialLower)
        {
            mode |= executeBit | specialBit;
        }
        else if (execute == specialUpper)
        {
            mode |= specialBit;
        }
        else if (execute != '-')
        {
            return false;
        }

        return true;
    }

    private static char ExecuteCharacter(int mode, int executeBit, int specialBit, char specialLower, char specialUpper)
    {
        var hasExecute = (mode & executeBit) != 0;
        var hasSpecial = (mode & specialBit) != 0;

        if (hasSpecial)
        {
            return hasExecute ? specialLower : specialUpper;
        }

        return hasExecute ? 'x' : '-';
    }
}
