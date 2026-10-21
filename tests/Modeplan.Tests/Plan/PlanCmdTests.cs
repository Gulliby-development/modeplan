namespace Modeplan.Tests;

public sealed class PlanCmdTests
{
    [Fact]
    public void Usage_is_the_plan_command_usage()
    {
        Assert.Equal("modeplan plan OLD NEW", PlanCmd.Usage);
    }

    [Fact]
    public void Run_validates_options_arity_and_old_before_new_without_side_effects()
    {
        AssertFailure("missing OLD");
        AssertFailure("missing NEW", "0644");
        AssertFailure("too many arguments", "0644", "0755", "0600");
        AssertFailure("too many arguments", "8", "9", "0");
        AssertFailure("unknown option \"--x\"", "--x");
        AssertFailure("unknown option \"--help\"", "8", "9", "--help");
        AssertFailure("unknown option \"--\"", "--");
        AssertFailure("invalid mode \"8\"", "8", "9");
        AssertFailure("invalid mode \"rwxrwxrwxx\"", "0644", "rwxrwxrwxx");
        AssertFailure("invalid mode \"6?4\"", "6\t4", "644");
        AssertFailure("invalid mode \"\"", "", "644");
        AssertFailure("invalid mode \"-\"", "-", "644");
    }

    [Theory]
    [InlineData("0644", "4755", "u+xs,go+x", "4 bits", 0)]
    [InlineData("0644", "0755", "a+x", "3 bits", 0)]
    [InlineData("1777", "0755", "g-w,o-wt", "3 bits", 0)]
    [InlineData("0750", "0705", "g-rx,o+rx", "4 bits", 0)]
    [InlineData("0640", "0660", "g+w", "1 bit", 0)]
    [InlineData("4755", "0644", "u-xs,go-x", "4 bits", 0)]
    [InlineData("0000", "7777", "ug+rwxs,o+rwxt", "12 bits", 0)]
    [InlineData("0600", "0640", "g+r", "1 bit", 0)]
    [InlineData("0700", "0070", "u-rwx,g+rwx", "6 bits", 0)]
    [InlineData("0600", "0300", "u=wx", "2 bits", 0)]
    [InlineData("0644", "0644", "-", "0 bits", 1)]
    [InlineData("0000", "4000", "u+s", "1 bit", 0)]
    [InlineData("0000", "6000", "ug+s", "2 bits", 0)]
    [InlineData("0000", "1000", "o+t", "1 bit", 0)]
    [InlineData("0404", "0101", "uo=x", "4 bits", 0)]
    [InlineData("7777", "0000", "ug-rwxs,o-rwxt", "12 bits", 0)]
    public void Run_builds_canonical_expressions_and_the_literal_changed_count(
        string oldText,
        string newText,
        string expectedSymbolic,
        string expectedChanged,
        int expectedCode)
    {
        AssertPlanCase(oldText, newText, expectedSymbolic, expectedChanged, expectedCode);
    }

    [Theory]
    [InlineData(
        "0644",
        "4755",
        0,
        "old         0644 rw-r--r--\nnew         4755 rwsr-xr-x\nsymbolic    u+xs,go+x\nnumeric     4755\nchanged     4 bits\n")]
    [InlineData(
        "0644",
        "0644",
        1,
        "old         0644 rw-r--r--\nnew         0644 rw-r--r--\nsymbolic    -\nnumeric     644\nchanged     0 bits\n")]
    [InlineData(
        "1",
        "0",
        0,
        "old         0001 --------x\nnew         0000 ---------\nsymbolic    o-x\nnumeric     0\nchanged     1 bit\n")]
    [InlineData(
        "rw-r--r--",
        "rwsr-xr-x",
        0,
        "old         0644 rw-r--r--\nnew         4755 rwsr-xr-x\nsymbolic    u+xs,go+x\nnumeric     4755\nchanged     4 bits\n")]
    public void Run_renders_the_exact_output_once(
        string oldText,
        string newText,
        int expectedCode,
        string expectedOutput)
    {
        var io = new FakeIo();

        var code = PlanCmd.Run(new[] { oldText, newText }, io);

        Assert.Equal(expectedCode, code);
        Assert.Single(io.Outputs);
        Assert.Equal(expectedOutput, io.Outputs[0]);
        Assert.DoesNotContain('\r', io.Outputs[0]);
        Assert.Empty(io.Errors);
        AssertNoReads(io);
    }

    private static void AssertPlanCase(
        string oldText,
        string newText,
        string expectedSymbolic,
        string expectedChanged,
        int expectedCode)
    {
        var io = new FakeIo();

        var code = PlanCmd.Run(new[] { oldText, newText }, io);

        Assert.Equal(expectedCode, code);
        Assert.Single(io.Outputs);
        Assert.Contains("symbolic    " + expectedSymbolic + "\n", io.Outputs[0]);
        Assert.Contains("changed     " + expectedChanged + "\n", io.Outputs[0]);
        Assert.DoesNotContain('\r', io.Outputs[0]);
        Assert.Empty(io.Errors);
        AssertNoReads(io);

        if (expectedSymbolic != "-")
        {
            var oldMode = Mode.Parse(oldText)!.Value;
            var newMode = Mode.Parse(newText)!.Value;
            var actualSymbolic = io.Outputs[0].Split('\n')[2]["symbolic    ".Length..];

            AssertReplay(actualSymbolic, oldMode, newMode, false, Semantics.ParseUmask("022")!.Value);
            AssertReplay(actualSymbolic, oldMode, newMode, true, Semantics.ParseUmask("077")!.Value);
            AssertReplay(actualSymbolic, oldMode, newMode, false, Semantics.ParseUmask("000")!.Value);
        }
    }

    private static void AssertReplay(string symbolic, int oldMode, int newMode, bool isDirectory, int umask)
    {
        var expression = ExprParser.Parse(symbolic);
        var steps = Semantics.Steps(expression, oldMode, isDirectory, umask);

        Assert.NotEmpty(steps);
        Assert.Equal(newMode, steps[steps.Count - 1].Mode);
    }

    private static void AssertFailure(string expectedMessage, params string[] args)
    {
        var io = new FakeIo();

        var failure = Assert.Throws<Failure>(() => PlanCmd.Run(args, io));

        Assert.Equal(expectedMessage, failure.Message);
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
        AssertNoReads(io);
    }

    private static void AssertNoReads(FakeIo io)
    {
        Assert.Equal(0, io.StdinReads);
        Assert.Empty(io.FileReads);
    }

    private sealed class FakeIo : IIo
    {
        public List<string> Outputs { get; } = new();

        public List<string> Errors { get; } = new();

        public List<string> FileReads { get; } = new();

        public int StdinReads { get; private set; }

        public byte[] ReadStdin()
        {
            StdinReads++;
            return Array.Empty<byte>();
        }

        public byte[]? ReadFile(string path)
        {
            FileReads.Add(path);
            return null;
        }

        public void Out(string text)
        {
            Outputs.Add(text);
        }

        public void Err(string text)
        {
            Errors.Add(text);
        }
    }
}
