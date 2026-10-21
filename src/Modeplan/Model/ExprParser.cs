namespace Modeplan;

public static class ExprParser
{
    public static Expression Parse(string text)
    {
        if (text.Length == 0)
        {
            throw new Failure("empty expression at column 1");
        }

        if (IsDigit(text[0]))
        {
            return ParseNumeric(text);
        }

        return ParseSymbolic(text);
    }

    private static Expression ParseNumeric(string text)
    {
        var value = 0;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character < '0' || character > '7')
            {
                throw Unexpected(text, index);
            }

            if (index >= 4)
            {
                throw new Failure("too many digits at column 5");
            }

            value = (value << 3) | (character - '0');
        }

        return new Expression(text, true, value, Array.Empty<Act>());
    }

    private static Expression ParseSymbolic(string text)
    {
        var actions = new List<Act>();
        var position = 0;

        while (true)
        {
            var whoStart = position;
            while (position < text.Length && IsWho(text[position]))
            {
                position++;
            }

            var who = text[whoStart..position];
            if (position == text.Length || text[position] == ',')
            {
                throw MissingOperator(position);
            }

            if (!IsOperator(text[position]))
            {
                throw Unexpected(text, position);
            }

            while (position < text.Length && IsOperator(text[position]))
            {
                var op = text[position++];
                var permsStart = position;

                if (position < text.Length && IsCopy(text[position]))
                {
                    position++;
                    if (position < text.Length && !IsOperator(text[position]) && text[position] != ',')
                    {
                        throw Unexpected(text, position);
                    }
                }
                else
                {
                    while (position < text.Length && IsPermission(text[position]))
                    {
                        position++;
                    }
                }

                actions.Add(new Act(who, op, text[permsStart..position]));
            }

            if (position == text.Length)
            {
                return new Expression(text, false, 0, actions);
            }

            if (text[position] != ',')
            {
                throw Unexpected(text, position);
            }

            position++;
            if (position == text.Length)
            {
                throw MissingOperator(position);
            }
        }
    }

    private static Failure MissingOperator(int position)
    {
        return new Failure($"missing operator at column {position + 1}");
    }

    private static Failure Unexpected(string text, int position)
    {
        var length = IsSurrogatePairAt(text, position) ? 2 : 1;
        var character = text.Substring(position, length);
        return new Failure($"unexpected character {Text.Show(character)} at column {position + 1}");
    }

    private static bool IsSurrogatePairAt(string text, int position)
    {
        return position + 1 < text.Length
            && text[position] is >= '\uD800' and <= '\uDBFF'
            && text[position + 1] is >= '\uDC00' and <= '\uDFFF';
    }

    private static bool IsDigit(char character)
    {
        return character is >= '0' and <= '9';
    }

    private static bool IsWho(char character)
    {
        return character is 'u' or 'g' or 'o' or 'a';
    }

    private static bool IsOperator(char character)
    {
        return character is '+' or '-' or '=';
    }

    private static bool IsCopy(char character)
    {
        return character is 'u' or 'g' or 'o';
    }

    private static bool IsPermission(char character)
    {
        return character is 'r' or 'w' or 'x' or 'X' or 's' or 't';
    }
}
