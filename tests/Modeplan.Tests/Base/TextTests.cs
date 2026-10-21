namespace Modeplan.Tests;

public sealed class TextTests
{
    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("a\t\u007f\u00e9\U0001f600", "\"a????\"")]
    [InlineData(" ~", "\" ~\"")]
    [InlineData("\u0085\u00a0", "\"??\"")]
    public void Show_uses_ascii_printability_and_code_point_replacements(string input, string expected)
    {
        Assert.Equal(expected, Text.Show(input));
    }

    [Theory]
    [InlineData("result", "0700", "result      0700")]
    [InlineData("abcdefghijk", "v", "abcdefghijk  v")]
    [InlineData("😀", "v", "😀           v")]
    [InlineData("", "", "            ")]
    [InlineData("0123456789", "v", "0123456789  v")]
    public void Line_pads_keys_by_code_point_width(string key, string value, string expected)
    {
        Assert.Equal(expected, Text.Line(key, value));
    }

    [Fact]
    public void Table_sizes_columns_from_header_and_rows_and_removes_trailing_spaces()
    {
        IReadOnlyList<string> header = new[] { "A", "B" };
        IReadOnlyList<IReadOnlyList<string>> rows = new IReadOnlyList<string>[]
        {
            new[] { "xx", "y" },
            new[] { "z", "" }
        };

        Assert.Equal(new[] { "A   B", "xx  y", "z" }, Text.Table(header, rows));
    }

    // AC-004 input: [["😀","B"],[["a","c"]]] => ["😀  B","a  c"]
    [Fact]
    public void Table_counts_emoji_as_one_character_and_returns_header_without_rows()
    {
        IReadOnlyList<string> header = new[] { "😀", "B" };
        IReadOnlyList<IReadOnlyList<string>> rows = new IReadOnlyList<string>[]
        {
            new[] { "a", "c" }
        };

        Assert.Equal(new[] { "😀  B", "a  c" }, Text.Table(header, rows));
        Assert.Equal(new[] { "😀  B" }, Text.Table(header, Array.Empty<IReadOnlyList<string>>()));
    }

    [Theory]
    [InlineData(0, "bit", "0 bits")]
    [InlineData(1, "bit", "1 bit")]
    [InlineData(2, "bit", "2 bits")]
    public void Plural_uses_singular_only_for_one(int count, string word, string expected)
    {
        Assert.Equal(expected, Text.Plural(count, word));
    }
}
