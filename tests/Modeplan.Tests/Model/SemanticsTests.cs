namespace Modeplan.Tests;

public sealed class SemanticsTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("022", 18)]
    [InlineData("777", 511)]
    [InlineData("0000", null)]
    [InlineData("088", null)]
    [InlineData("", null)]
    [InlineData("１２", null)]
    public void ParseUmask_accepts_only_one_to_three_ascii_octal_digits(string text, int? expected)
    {
        Assert.Equal(expected, Semantics.ParseUmask(text));
    }

    [Theory]
    [InlineData("", '+', "x", 420, false, 18, 493)]
    [InlineData("", '+', "x", 420, false, 63, 484)]
    [InlineData("a", '=', "rX", 420, false, 18, 292)]
    [InlineData("a", '=', "rX", 420, true, 18, 365)]
    [InlineData("", '=', "rw", 511, false, 18, 438)]
    [InlineData("o", '+', "s", 384, false, 18, 384)]
    [InlineData("u", '-', "s", 2541, false, 18, 493)]
    [InlineData("u", '+', "t", 0, false, 511, 512)]
    [InlineData("a", '+', "s", 0, false, 511, 3072)]
    [InlineData("", '+', "s", 0, false, 511, 3072)]
    [InlineData("u", '=', "", 4095, false, 0, 1599)]
    [InlineData("g", '=', "u", 2541, false, 0, 2557)]
    [InlineData("uu", '+', "x", 0, false, 0, 64)]
    [InlineData("", '-', "w", 511, false, 18, 383)]
    [InlineData("", '=', "", 4095, false, 511, 511)]
    public void Run_applies_one_action_with_class_copy_special_bit_and_umask_rules(
        string who, char op, string perms, int mode, bool dir, int umask, int expected)
    {
        Assert.Equal(expected, Semantics.Run(new Act(who, op, perms), mode, dir, umask));
    }

    [Fact]
    public void Steps_applies_actions_left_to_right_to_the_result_of_each_previous_action()
    {
        var expression = ExprParser.Parse("u+x,go-r");

        var steps = Semantics.Steps(expression, 420, false, 18);

        Assert.Equal(
            new[]
            {
                new Semantics.Step("u+x", 484),
                new Semantics.Step("go-r", 448)
            },
            steps);
    }

    [Fact]
    public void Steps_preserves_special_bits_across_sequential_actions()
    {
        var expression = ExprParser.Parse("u+s,g+s,o+t");

        var steps = Semantics.Steps(expression, 493, false, 18);

        Assert.Equal(
            new[]
            {
                new Semantics.Step("u+s", 2541),
                new Semantics.Step("g+s", 3565),
                new Semantics.Step("o+t", 4077)
            },
            steps);
    }

    [Fact]
    public void Steps_reads_copy_permissions_from_the_mode_after_prior_actions()
    {
        var expression = ExprParser.Parse("u+x,g=u");

        var steps = Semantics.Steps(expression, 384, false, 18);

        Assert.Equal(
            new[]
            {
                new Semantics.Step("u+x", 448),
                new Semantics.Step("g=u", 504)
            },
            steps);
    }

    [Fact]
    public void Steps_evaluates_uppercase_x_against_the_mode_before_that_action()
    {
        var expression = ExprParser.Parse("u-x,g+X");

        var steps = Semantics.Steps(expression, 64, false, 18);

        Assert.Equal(
            new[]
            {
                new Semantics.Step("u-x", 0),
                new Semantics.Step("g+X", 0)
            },
            steps);
    }

    [Fact]
    public void Steps_replaces_the_whole_mode_with_one_numeric_step()
    {
        var expression = ExprParser.Parse("755");

        var steps = Semantics.Steps(expression, 4095, true, 511);

        Assert.Equal(new[] { new Semantics.Step("755", 493) }, steps);
    }
}
