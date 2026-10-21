using System.Text;

namespace Modeplan.Tests;

public sealed class MatchCmdTests
{
    [Fact]
    public void Usage_matches_the_public_command_contract()
    {
        Assert.Equal("modeplan match LISTING TEST... [--kind f|d]", MatchCmd.Usage);
    }

    [Theory]
    [InlineData("644", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n1     f     0644  rw-r--r--  \"a\"\nmatched     1 of 4\n")]
    [InlineData("755", 1, "matched     0 of 4\n")]
    [InlineData("4755", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n2     f     4755  rwsr-xr-x  \"b\"\nmatched     1 of 4\n")]
    [InlineData("-022", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n3     d     0777  rwxrwxrwx  \"c\"\nmatched     1 of 4\n")]
    [InlineData("/002", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n3     d     0777  rwxrwxrwx  \"c\"\nmatched     1 of 4\n")]
    [InlineData("-4000", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n2     f     4755  rwsr-xr-x  \"b\"\nmatched     1 of 4\n")]
    [InlineData("/4000", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n2     f     4755  rwsr-xr-x  \"b\"\nmatched     1 of 4\n")]
    [InlineData("-0", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n1     f     0644  rw-r--r--  \"a\"\n2     f     4755  rwsr-xr-x  \"b\"\n3     d     0777  rwxrwxrwx  \"c\"\n4     f     0000  ---------  \"z\"\nmatched     4 of 4\n")]
    [InlineData("/0", 1, "matched     0 of 4\n")]
    [InlineData("0", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n4     f     0000  ---------  \"z\"\nmatched     1 of 4\n")]
    [InlineData("0000", 0, "LINE  KIND  MODE  SYMBOLIC   PATH\n4     f     0000  ---------  \"z\"\nmatched     1 of 4\n")]
    public void Run_evaluates_exact_all_bit_and_any_bit_tests(
        string test,
        int expectedExitCode,
        string expectedOutput)
    {
        AssertFileRun(
            new[] { "listing.txt", test },
            "listing.txt",
            "f 0644 a\nf 4755 b\nd 0777 c\nf 0000 z\n",
            expectedExitCode,
            expectedOutput);
    }

    [Fact]
    public void Run_matches_the_full_mode_in_original_order()
    {
        AssertFileRun(
            new[] { "listing.txt", "7777" },
            "listing.txt",
            "f 7777 full\nf 0000 zero\n",
            0,
            "LINE  KIND  MODE  SYMBOLIC   PATH\n1     f     7777  rwsrwsrwt  \"full\"\nmatched     1 of 2\n");
    }

    [Theory]
    [InlineData("", "invalid test \"\"")]
    [InlineData("-", "invalid test \"-\"")]
    [InlineData("/", "invalid test \"/\"")]
    [InlineData("8", "invalid test \"8\"")]
    [InlineData("9", "invalid test \"9\"")]
    [InlineData("00000", "invalid test \"00000\"")]
    [InlineData("-00000", "invalid test \"-00000\"")]
    [InlineData("/00000", "invalid test \"/00000\"")]
    [InlineData("+x", "invalid test \"+x\"")]
    [InlineData("rwxr-xr-x", "invalid test \"rwxr-xr-x\"")]
    [InlineData("//0", "invalid test \"//0\"")]
    [InlineData(" 644", "invalid test \" 644\"")]
    [InlineData("６44", "invalid test \"?44\"")]
    [InlineData("--0", "unknown option \"--0\"")]
    public void Run_rejects_malformed_tests_without_writing(string test, string expectedFailure)
    {
        var io = FileIo(
            "a.txt",
            "f 0644 a\n");

        var failure = Assert.Throws<Failure>(() => MatchCmd.Run(new[] { "a.txt", test }, io));

        Assert.Equal(expectedFailure, failure.Message);
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
        if (test == "--0")
        {
            Assert.Empty(io.FileReads);
        }
        else
        {
            Assert.Equal(new[] { "a.txt" }, io.FileReads);
        }
    }

    [Fact]
    public void Run_validates_arguments_listing_tests_and_kind_in_contract_order()
    {
        AssertFailure(Array.Empty<string>(), "missing LISTING");
        AssertFailure(new[] { "a.txt" }, "missing TEST");
        AssertFailure(new[] { "--x" }, "unknown option \"--x\"");
        AssertFailure(new[] { "--kind" }, "--kind needs a value");
        AssertFailure(
            new[] { "a.txt", "644", "--kind", "f", "--kind", "d" },
            "repeated option --kind");
        AssertFailure(
            new[] { "nowhere.txt", "8", "--kind", "x" },
            "\"nowhere.txt\": cannot read the file",
            "nowhere.txt");
        AssertFailure(
            new[] { "empty.txt", "8" },
            "\"empty.txt\": no entries",
            "empty.txt");
        AssertFailure(
            new[] { "bad.txt", "8" },
            "\"bad.txt\": ML004 invalid mode \"8\" at 3:3",
            "bad.txt");
        AssertFailure(
            new[] { "a.txt", "644", "+x", "8", "--kind", "x" },
            "invalid test \"+x\"",
            "a.txt");
        AssertFailure(
            new[] { "a.txt", "8", "--kind", "x" },
            "invalid test \"8\"",
            "a.txt");
        AssertFailure(
            new[] { "a.txt", "644", "--kind", "x" },
            "invalid kind \"x\"",
            "a.txt");
    }

    [Fact]
    public void Run_accepts_both_kind_option_forms_and_variadic_and_tests()
    {
        const string listing = "f 0644 a\nf 4755 b\nd 0777 c\nf 0000 z\n";
        const string fileOnlyOutput = "LINE  KIND  MODE  SYMBOLIC   PATH\n1     f     0644  rw-r--r--  \"a\"\nmatched     1 of 4\n";

        AssertFileRun(
            new[] { "listing.txt", "644", "--kind=f" },
            "listing.txt",
            listing,
            0,
            fileOnlyOutput);
        AssertFileRun(
            new[] { "listing.txt", "644", "--kind", "f" },
            "listing.txt",
            listing,
            0,
            fileOnlyOutput);
        AssertFileRun(
            new[] { "listing.txt", "-111", "/4000" },
            "listing.txt",
            listing,
            0,
            "LINE  KIND  MODE  SYMBOLIC   PATH\n2     f     4755  rwsr-xr-x  \"b\"\nmatched     1 of 4\n");
    }

    [Fact]
    public void Run_renders_the_literal_tree_selections_and_keeps_all_entries_in_denominators()
    {
        const string tree = "# a small tree\nd 0755 src/\nf 0644 src/main.c\nf 0755 build.sh\nf 4755 bin/su\nd 1777 /tmp\nf 0666 notes.txt\nd 0777 shared/\nf 0600 id_rsa\nf 0044 odd\nd 0644 odd dir/\n";

        AssertFileRun(
            new[] { "tree.txt", "-111" },
            "tree.txt",
            tree,
            0,
            "LINE  KIND  MODE  SYMBOLIC   PATH\n2     d     0755  rwxr-xr-x  \"src/\"\n4     f     0755  rwxr-xr-x  \"build.sh\"\n5     f     4755  rwsr-xr-x  \"bin/su\"\n6     d     1777  rwxrwxrwt  \"/tmp\"\n8     d     0777  rwxrwxrwx  \"shared/\"\nmatched     5 of 10\n");
        AssertFileRun(
            new[] { "tree.txt", "0644", "--kind", "d" },
            "tree.txt",
            tree,
            0,
            "LINE  KIND  MODE  SYMBOLIC   PATH\n11    d     0644  rw-r--r--  \"odd dir/\"\nmatched     1 of 10\n");
        AssertFileRun(
            new[] { "tree.txt", "/002", "--kind", "f" },
            "tree.txt",
            tree,
            0,
            "LINE  KIND  MODE  SYMBOLIC   PATH\n7     f     0666  rw-rw-rw-  \"notes.txt\"\nmatched     1 of 10\n");
        AssertFileRun(
            new[] { "tree.txt", "-0", "--kind", "d" },
            "tree.txt",
            tree,
            0,
            "LINE  KIND  MODE  SYMBOLIC   PATH\n2     d     0755  rwxr-xr-x  \"src/\"\n6     d     1777  rwxrwxrwt  \"/tmp\"\n8     d     0777  rwxrwxrwx  \"shared/\"\n11    d     0644  rw-r--r--  \"odd dir/\"\nmatched     4 of 10\n");
        AssertFileRun(
            new[] { "tree.txt", "-111", "/4000" },
            "tree.txt",
            tree,
            0,
            "LINE  KIND  MODE  SYMBOLIC   PATH\n5     f     4755  rwsr-xr-x  \"bin/su\"\nmatched     1 of 10\n");
        AssertFileRun(
            new[] { "tree.txt", "0644", "/002" },
            "tree.txt",
            tree,
            1,
            "matched     0 of 10\n");
        AssertFileRun(
            new[] { "tree.txt", "7777" },
            "tree.txt",
            tree,
            1,
            "matched     0 of 10\n");
    }

    [Fact]
    public void Run_reads_stdin_sanitizes_paths_and_writes_one_ascii_lf_output()
    {
        const string listing = "# c\n\nf 0644 café😀\nd 0755 dir\n";
        var matchingIo = new FakeIo(Encoding.UTF8.GetBytes(listing));

        var matchingExitCode = MatchCmd.Run(new[] { "-", "644" }, matchingIo);

        Assert.Equal(0, matchingExitCode);
        Assert.Equal(
            new[] { "LINE  KIND  MODE  SYMBOLIC   PATH\n3     f     0644  rw-r--r--  \"caf??\"\nmatched     1 of 2\n" },
            matchingIo.Outputs);
        Assert.Empty(matchingIo.Errors);
        Assert.Equal(1, matchingIo.StdinReads);
        Assert.Empty(matchingIo.FileReads);
        AssertAsciiLfOutput(matchingIo.Outputs[0]);

        var noMatchIo = new FakeIo(Encoding.UTF8.GetBytes(listing));

        var noMatchExitCode = MatchCmd.Run(new[] { "-", "/0" }, noMatchIo);

        Assert.Equal(1, noMatchExitCode);
        Assert.Equal(new[] { "matched     0 of 2\n" }, noMatchIo.Outputs);
        Assert.Empty(noMatchIo.Errors);
        Assert.Equal(1, noMatchIo.StdinReads);
        Assert.Empty(noMatchIo.FileReads);
        AssertAsciiLfOutput(noMatchIo.Outputs[0]);
    }

    private static void AssertFileRun(
        string[] args,
        string listingPath,
        string listing,
        int expectedExitCode,
        string expectedOutput)
    {
        var io = FileIo(listingPath, listing);

        var exitCode = MatchCmd.Run(args, io);

        Assert.Equal(expectedExitCode, exitCode);
        Assert.Equal(new[] { expectedOutput }, io.Outputs);
        Assert.Empty(io.Errors);
        Assert.Equal(new[] { listingPath }, io.FileReads);
        Assert.Equal(0, io.StdinReads);
        AssertAsciiLfOutput(io.Outputs[0]);
    }

    private static void AssertFailure(
        string[] args,
        string expectedFailure,
        string? expectedFileRead = null)
    {
        var io = new FakeIo(
            files: new Dictionary<string, byte[]?>(StringComparer.Ordinal)
            {
                ["a.txt"] = Encoding.UTF8.GetBytes("f 0644 a\n"),
                ["empty.txt"] = Encoding.UTF8.GetBytes("# nothing"),
                ["bad.txt"] = Encoding.UTF8.GetBytes("# l\nf 0644 a\nd 8 b")
            });

        var failure = Assert.Throws<Failure>(() => MatchCmd.Run(args, io));

        Assert.Equal(expectedFailure, failure.Message);
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
        if (expectedFileRead is null)
        {
            Assert.Empty(io.FileReads);
        }
        else
        {
            Assert.Equal(new[] { expectedFileRead }, io.FileReads);
        }
        Assert.Equal(0, io.StdinReads);
    }

    private static FakeIo FileIo(string path, string listing)
    {
        return new FakeIo(
            files: new Dictionary<string, byte[]?>(StringComparer.Ordinal)
            {
                [path] = Encoding.UTF8.GetBytes(listing)
            });
    }

    private static void AssertAsciiLfOutput(string output)
    {
        Assert.DoesNotContain('\r', output);
        Assert.DoesNotContain('\uFEFF', output);
        Assert.All(output, character =>
            Assert.True(character == '\n' || character is >= ' ' and <= '~'));
    }

    private sealed class FakeIo : IIo
    {
        private readonly byte[] stdin;
        private readonly IReadOnlyDictionary<string, byte[]?> files;

        public FakeIo(byte[]? stdin = null, IReadOnlyDictionary<string, byte[]?>? files = null)
        {
            this.stdin = stdin ?? Array.Empty<byte>();
            this.files = files ?? new Dictionary<string, byte[]?>(StringComparer.Ordinal);
        }

        public List<string> FileReads { get; } = [];
        public int StdinReads { get; private set; }
        public List<string> Outputs { get; } = [];
        public List<string> Errors { get; } = [];

        public byte[] ReadStdin()
        {
            StdinReads++;
            return stdin;
        }

        public byte[]? ReadFile(string path)
        {
            FileReads.Add(path);
            return files.TryGetValue(path, out var bytes) ? bytes : null;
        }

        public void Out(string text) => Outputs.Add(text);

        public void Err(string text) => Errors.Add(text);
    }
}
