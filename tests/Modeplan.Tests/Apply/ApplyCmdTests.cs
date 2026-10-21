namespace Modeplan.Tests;

public sealed class ApplyCmdTests
{
    [Theory]
    [InlineData(new[] { "0644", "755" }, "0644", "755", "0755", "0755 rwxr-xr-x", 3, 0)]
    [InlineData(new[] { "0644", "u+x,go-r" }, "0644", "u+x|go-r", "0744|0700", "0700 rwx------", 3, 0)]
    [InlineData(new[] { "0000", "u+rwx-w" }, "0000", "u+rwx|u-w", "0700|0500", "0500 r-x------", 2, 0)]
    [InlineData(new[] { "0640", "g=u" }, "0640", "g=u", "0660", "0660 rw-rw----", 1, 0)]
    [InlineData(new[] { "0644", "+x" }, "0644", "+x", "0755", "0755 rwxr-xr-x", 3, 0)]
    [InlineData(new[] { "0644", "+x", "--umask", "077" }, "0644", "+x", "0744", "0744 rwxr--r--", 1, 0)]
    [InlineData(new[] { "0644", "a+x", "--umask=077" }, "0644", "a+x", "0755", "0755 rwxr-xr-x", 3, 0)]
    [InlineData(new[] { "0644", "a=rX" }, "0644", "a=rX", "0444", "0444 r--r--r--", 1, 0)]
    [InlineData(new[] { "0644", "a=rX", "--dir" }, "0644", "a=rX", "0555", "0555 r-xr-xr-x", 4, 0)]
    [InlineData(new[] { "0777", "=rw" }, "0777", "=rw", "0666", "0666 rw-rw-rw-", 3, 0)]
    [InlineData(new[] { "0755", "u+s,g+s,o+t" }, "0755", "u+s|g+s|o+t", "4755|6755|7755", "7755 rwsr-sr-t", 3, 0)]
    [InlineData(new[] { "4755", "u-s" }, "4755", "u-s", "0755", "0755 rwxr-xr-x", 1, 0)]
    [InlineData(new[] { "0600", "o+s" }, "0600", "o+s", "0600", "0600 rw-------", 0, 1)]
    [InlineData(new[] { "0111", "u+X,g=u" }, "0111", "u+X|g=u", "0111|0111", "0111 --x--x--x", 0, 1)]
    [InlineData(new[] { "0644", "u+x,u-x" }, "0644", "u+x|u-x", "0744|0644", "0644 rw-r--r--", 0, 1)]
    [InlineData(new[] { "7777", "0" }, "7777", "0", "0000", "0000 ---------", 12, 0)]
    [InlineData(new[] { "4755", "u=" }, "4755", "u=", "0055", "0055 ---r-xr-x", 4, 0)]
    [InlineData(new[] { "rw-r--r--", "-w" }, "0644", "-w", "0444", "0444 r--r--r--", 1, 0)]
    public void Run_applies_literal_examples_and_reports_net_changes(
        string[] args,
        string initialMode,
        string expectedActions,
        string expectedModes,
        string expectedResult,
        int expectedChanged,
        int expectedExit)
    {
        var io = new FakeIo();

        var exit = ApplyCmd.Run(args, io);

        Assert.Equal(expectedExit, exit);
        Assert.Single(io.Outputs);
        Assert.Empty(io.Errors);
        Assert.Empty(io.FileReads);
        Assert.Equal(0, io.StdinReads);

        var output = io.Outputs[0];
        AssertAsciiLfOutput(output);
        var lines = output.Split('\n');
        Assert.Equal(string.Empty, lines[^1]);
        Assert.Equal(new[] { "STEP", "ACTION", "MODE", "SYMBOLIC" }, Tokens(lines[0]));

        var actions = expectedActions.Split('|');
        var modes = expectedModes.Split('|');
        Assert.Equal(actions.Length, modes.Length);
        Assert.Equal(actions.Length + 5, lines.Length);
        Assert.Equal(new[] { "0", "start", initialMode }, Tokens(lines[1]).Take(3));

        for (var index = 0; index < actions.Length; index++)
        {
            Assert.Equal(
                new[] { (index + 1).ToString(), actions[index], modes[index] },
                Tokens(lines[index + 2]).Take(3));
        }

        Assert.Equal(new[] { "result" }.Concat(expectedResult.Split(' ')), Tokens(lines[^3]));
        Assert.Equal(
            new[] { "changed", expectedChanged.ToString(), expectedChanged == 1 ? "bit" : "bits" },
            Tokens(lines[^2]));
    }

    [Fact]
    public void Run_writes_exact_lf_tables_once_and_exposes_usage()
    {
        Assert.Equal("modeplan apply MODE EXPR [--dir] [--umask OCT]", ApplyCmd.Usage);

        var changedIo = new FakeIo();
        var changedExit = ApplyCmd.Run(new[] { "0644", "u+x,go-r" }, changedIo);

        Assert.Equal(0, changedExit);
        AssertSingleOutput(
            changedIo,
            "STEP  ACTION  MODE  SYMBOLIC\n" +
            "0     start   0644  rw-r--r--\n" +
            "1     u+x     0744  rwxr--r--\n" +
            "2     go-r    0700  rwx------\n" +
            "result      0700 rwx------\n" +
            "changed     3 bits\n");

        var unchangedIo = new FakeIo();
        var unchangedExit = ApplyCmd.Run(new[] { "0600", "o+s" }, unchangedIo);

        Assert.Equal(1, unchangedExit);
        AssertSingleOutput(
            unchangedIo,
            "STEP  ACTION  MODE  SYMBOLIC\n" +
            "0     start   0600  rw-------\n" +
            "1     o+s     0600  rw-------\n" +
            "result      0600 rw-------\n" +
            "changed     0 bits\n");
    }

    [Theory]
    [InlineData(new[] { "--x" }, "unknown option \"--x\"")]
    [InlineData(new[] { "0644", "--x" }, "unknown option \"--x\"")]
    [InlineData(new[] { "0644", "u+x", "--dir=1" }, "--dir takes no value")]
    [InlineData(new[] { "0644", "u+x", "--umask" }, "--umask needs a value")]
    [InlineData(new[] { "0644", "u+x", "--umask", "1", "--umask", "2" }, "repeated option --umask")]
    [InlineData(new[] { "0644", "u+x", "--dir", "--dir" }, "repeated option --dir")]
    [InlineData(new string[0], "missing MODE")]
    [InlineData(new[] { "0644" }, "missing EXPR")]
    [InlineData(new[] { "9", "u+q", "extra" }, "too many arguments")]
    [InlineData(new[] { "9", "u+q" }, "invalid mode \"9\"")]
    [InlineData(new[] { "", "u+x" }, "invalid mode \"\"")]
    [InlineData(new[] { "0644", "u+x", "--umask", "088" }, "invalid umask \"088\"")]
    [InlineData(new[] { "0644", "u+x", "--umask", "0022" }, "invalid umask \"0022\"")]
    [InlineData(new[] { "0644", "" }, "invalid expression \"\": empty expression at column 1")]
    [InlineData(new[] { "0644", "u+q", "--umask", "9" }, "invalid expression \"u+q\": unexpected character \"q\" at column 3")]
    [InlineData(new[] { "0644", "ug" }, "invalid expression \"ug\": missing operator at column 3")]
    [InlineData(new[] { "0644", "u+x," }, "invalid expression \"u+x,\": missing operator at column 5")]
    [InlineData(new[] { "0644", "0123a" }, "invalid expression \"0123a\": unexpected character \"a\" at column 5")]
    [InlineData(new[] { "0644", "01234" }, "invalid expression \"01234\": too many digits at column 5")]
    [InlineData(new[] { "0644", "g=uw" }, "invalid expression \"g=uw\": unexpected character \"w\" at column 4")]
    [InlineData(new[] { "0644", "u+😀" }, "invalid expression \"u+?\": unexpected character \"?\" at column 3")]
    public void Run_propagates_literal_failures_without_io(string[] args, string expectedMessage)
    {
        var io = new FakeIo();

        var failure = Assert.Throws<Failure>(() => ApplyCmd.Run(args, io));

        Assert.Equal(expectedMessage, failure.Message);
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
        Assert.Empty(io.FileReads);
        Assert.Equal(0, io.StdinReads);
    }

    private static string[] Tokens(string line)
    {
        return line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static void AssertSingleOutput(FakeIo io, string expected)
    {
        Assert.Single(io.Outputs);
        Assert.Equal(expected, io.Outputs[0]);
        Assert.Empty(io.Errors);
        Assert.Empty(io.FileReads);
        Assert.Equal(0, io.StdinReads);
        AssertAsciiLfOutput(io.Outputs[0]);
    }

    private static void AssertAsciiLfOutput(string output)
    {
        Assert.DoesNotContain('\r', output);
        Assert.EndsWith("\n", output);
        Assert.All(output, character => Assert.InRange((int)character, 0, 127));
    }

    private sealed class FakeIo : IIo
    {
        public List<string> FileReads { get; } = new();

        public List<string> Outputs { get; } = new();

        public List<string> Errors { get; } = new();

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
