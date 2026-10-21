namespace Modeplan.Tests;

public sealed class ExprTests
{
    [Theory]
    [InlineData("755", true, 493, "")]
    [InlineData("0", true, 0, "")]
    [InlineData("u+rwx-w", false, null, "u|+|rwx|u+rwx;u|-|w|u-w")]
    [InlineData("ug=rw-x", false, null, "ug|=|rw|ug=rw;ug|-|x|ug-x")]
    [InlineData("u+x,go-w", false, null, "u|+|x|u+x;go|-|w|go-w")]
    [InlineData("g=u", false, null, "g|=|u|g=u")]
    [InlineData("a=rX", false, null, "a|=|rX|a=rX")]
    [InlineData("+", false, null, "|+||+")]
    [InlineData("=", false, null, "|=||=")]
    [InlineData("u=", false, null, "u|=||u=")]
    [InlineData("-w", false, null, "|-|w|-w")]
    [InlineData("o-t", false, null, "o|-|t|o-t")]
    public void Parse_preserves_expression_and_action_fields(
        string text,
        bool numeric,
        int? expectedValue,
        string expectedActions)
    {
        var expression = ExprParser.Parse(text);

        Assert.Equal(text, expression.Text);
        Assert.Equal(numeric, expression.Numeric);
        if (numeric)
        {
            Assert.Equal(expectedValue, expression.Value);
        }

        var expected = expectedActions.Length == 0
            ? Array.Empty<string[]>()
            : expectedActions.Split(';').Select(action => action.Split('|')).ToArray();

        Assert.Equal(expected.Length, expression.Actions.Count);
        for (var index = 0; index < expected.Length; index++)
        {
            var expectedAction = expected[index];
            var actualAction = expression.Actions[index];

            Assert.Equal(expectedAction[0], actualAction.Who);
            Assert.Equal(expectedAction[1][0], actualAction.Op);
            Assert.Equal(expectedAction[2], actualAction.Perms);
            Assert.Equal(expectedAction[3], actualAction.Text);
        }
    }

    [Theory]
    [InlineData("", "empty expression at column 1")]
    [InlineData("8", "unexpected character \"8\" at column 1")]
    [InlineData("64a", "unexpected character \"a\" at column 3")]
    [InlineData("0123a", "unexpected character \"a\" at column 5")]
    [InlineData("01234", "too many digits at column 5")]
    [InlineData("ug", "missing operator at column 3")]
    [InlineData(",u+x", "missing operator at column 1")]
    [InlineData("u+x,", "missing operator at column 5")]
    [InlineData("ux", "unexpected character \"x\" at column 2")]
    [InlineData("u+q", "unexpected character \"q\" at column 3")]
    [InlineData("g=uw", "unexpected character \"w\" at column 4")]
    [InlineData("g=wu", "unexpected character \"u\" at column 4")]
    [InlineData("U+x", "unexpected character \"U\" at column 1")]
    [InlineData("a b", "unexpected character \" \" at column 2")]
    [InlineData("u+😀", "unexpected character \"?\" at column 3")]
    public void Parse_reports_the_first_error_at_its_code_point_column(string text, string message)
    {
        var failure = Assert.Throws<Failure>(() => ExprParser.Parse(text));

        Assert.Equal(message, failure.Message);
    }
}
