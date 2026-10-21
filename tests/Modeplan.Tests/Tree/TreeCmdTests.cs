using System.Text;

namespace Modeplan.Tests;

public sealed class TreeCmdTests
{
    private const string TreeListing = "# a small tree\n"
        + "d 0755 src/\n"
        + "f 0644 src/main.c\n"
        + "f 0755 build.sh\n"
        + "f 4755 bin/su\n"
        + "d 1777 /tmp\n"
        + "f 0666 notes.txt\n"
        + "d 0777 shared/\n"
        + "f 0600 id_rsa\n"
        + "f 0044 odd\n"
        + "d 0644 odd dir/\n";

    [Fact]
    public void Usage_is_exact()
    {
        Assert.Equal("modeplan tree LISTING EXPR [--umask OCT]", TreeCmd.Usage);
    }

    [Fact]
    public void Run_validates_options_arguments_listing_expression_and_umask_in_order()
    {
        AssertFailure(["--x"], "unknown option \"--x\"");
        AssertFailure([], "missing LISTING");
        AssertFailure(["a.txt"], "missing EXPR");
        AssertFailure(["nowhere.txt", "u+q", "extra"], "too many arguments");
        AssertFailure(["a.txt", "u+x", "--umask"], "--umask needs a value");
        AssertFailure(["a.txt", "u+x", "--umask", "1", "--umask", "2"], "repeated option --umask");
        AssertFailure(["nowhere.txt", "u+q"], "\"nowhere.txt\": cannot read the file", "nowhere.txt");
        AssertFailure(["bad.txt", "u+q"], "\"bad.txt\": ML004 invalid mode \"8\" at 1:3", "bad.txt");
        AssertFailure(["empty.txt", "u+x"], "\"empty.txt\": no entries", "empty.txt");
        AssertFailure(
            ["a.txt", "u+q", "--umask", "9"],
            "invalid expression \"u+q\": unexpected character \"q\" at column 3",
            "a.txt");
        AssertFailure(["a.txt", "u+x", "--umask", "9"], "invalid umask \"9\"", "a.txt");
        AssertFailure(["a.txt", ""], "invalid expression \"\": empty expression at column 1", "a.txt");
        AssertFailure(
            ["a.txt", "u+😀"],
            "invalid expression \"u+?\": unexpected character \"?\" at column 3",
            "a.txt");
    }

    [Fact]
    public void Run_renders_the_tree_X_and_g_w_example()
    {
        var io = CreateIo();

        AssertSuccess(
            io,
            ["tree.txt", "a+X,g-w"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n"
                + "6     d     1777  1757  \"/tmp\"\n"
                + "7     f     0666  0646  \"notes.txt\"\n"
                + "8     d     0777  0757  \"shared/\"\n"
                + "11    d     0644  0755  \"odd dir/\"\n"
                + "changed     4 of 10\n");
        Assert.Equal(new[] { "tree.txt" }, io.FileReads);
    }

    [Fact]
    public void Run_renders_other_tree_examples_with_final_modes_and_original_order()
    {
        var allClasses = CreateIo();
        AssertSuccess(
            allClasses,
            ["tree.txt", "go-rwx"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n"
                + "2     d     0755  0700  \"src/\"\n"
                + "3     f     0644  0600  \"src/main.c\"\n"
                + "4     f     0755  0700  \"build.sh\"\n"
                + "5     f     4755  4700  \"bin/su\"\n"
                + "6     d     1777  1700  \"/tmp\"\n"
                + "7     f     0666  0600  \"notes.txt\"\n"
                + "8     d     0777  0700  \"shared/\"\n"
                + "10    f     0044  0000  \"odd\"\n"
                + "11    d     0644  0600  \"odd dir/\"\n"
                + "changed     9 of 10\n");
        Assert.Equal(new[] { "tree.txt" }, allClasses.FileReads);

        var umask = CreateIo();
        // Expected rows include (6,d,1777,0777,\"/tmp\") and (11,d,0644,0744,\"odd dir/\").
        AssertSuccess(
            umask,
            ["tree.txt", "=rwX", "--umask", "077"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n"
                + "5     f     4755  0755  \"bin/su\"\n"
                + "6     d     1777  0777  \"/tmp\"\n"
                + "10    f     0044  0644  \"odd\"\n"
                + "11    d     0644  0744  \"odd dir/\"\n"
                + "changed     4 of 10\n");
        Assert.Equal(new[] { "tree.txt" }, umask.FileReads);

        var oneChanged = CreateIo();
        AssertSuccess(
            oneChanged,
            ["tree.txt", "u+r"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n"
                + "10    f     0044  0444  \"odd\"\n"
                + "changed     1 of 10\n");
        Assert.Equal(new[] { "tree.txt" }, oneChanged.FileReads);
    }

    [Fact]
    public void Run_applies_umask_numeric_single_dash_X_copy_and_final_only_semantics()
    {
        var defaultUmask = CreateIo();
        AssertSuccess(defaultUmask, ["a.txt", "+w"], 1, "changed     0 of 1\n");
        Assert.Equal(new[] { "a.txt" }, defaultUmask.FileReads);

        var noUmask = CreateIo();
        AssertSuccess(
            noUmask,
            ["a.txt", "+w", "--umask", "000"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n1     f     0644  0666  \"a\"\nchanged     1 of 1\n");
        Assert.Equal(new[] { "a.txt" }, noUmask.FileReads);

        var numeric = CreateIo();
        AssertSuccess(
            numeric,
            ["a.txt", "755"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n1     f     0644  0755  \"a\"\nchanged     1 of 1\n");
        Assert.Equal(new[] { "a.txt" }, numeric.FileReads);

        var singleDash = CreateIo();
        AssertSuccess(
            singleDash,
            ["a.txt", "-w"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n1     f     0644  0444  \"a\"\nchanged     1 of 1\n");
        Assert.Equal(new[] { "a.txt" }, singleDash.FileReads);

        var sequentialX = CreateIo();
        sequentialX.Files["a.txt"] = Encoding.UTF8.GetBytes("f 0111 a\n");
        AssertSuccess(
            sequentialX,
            ["a.txt", "a-x,a+X"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n1     f     0111  0000  \"a\"\nchanged     1 of 1\n");
        Assert.Equal(new[] { "a.txt" }, sequentialX.FileReads);

        var copy = CreateIo();
        copy.Files["a.txt"] = Encoding.UTF8.GetBytes("f 0640 a\n");
        AssertSuccess(
            copy,
            ["a.txt", "g=u"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n1     f     0640  0660  \"a\"\nchanged     1 of 1\n");
        Assert.Equal(new[] { "a.txt" }, copy.FileReads);

        var reverted = CreateIo();
        AssertSuccess(reverted, ["a.txt", "u+x,u-x"], 1, "changed     0 of 1\n");
        Assert.Equal(new[] { "a.txt" }, reverted.FileReads);
    }

    [Fact]
    public void Run_reads_stdin_and_preserves_original_line_number_and_total()
    {
        var io = CreateIo(Encoding.UTF8.GetBytes("# c\n\nf 0644 café\nd 0644 😀\nf 0755 z\n"));

        AssertSuccess(
            io,
            ["-", "a+X"],
            0,
            "LINE  KIND  OLD   NEW   PATH\n4     d     0644  0755  \"?\"\nchanged     1 of 3\n");
        Assert.Empty(io.FileReads);
        Assert.Equal(1, io.StdinReads);
    }

    private static void AssertFailure(IReadOnlyList<string> args, string expectedMessage, params string[] expectedFileReads)
    {
        var io = CreateIo();

        var failure = Assert.Throws<Failure>(() => TreeCmd.Run(args, io));

        Assert.Equal(expectedMessage, failure.Message);
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
        Assert.Equal(expectedFileReads, io.FileReads);
        Assert.Equal(0, io.StdinReads);
    }

    private static void AssertSuccess(FakeIo io, IReadOnlyList<string> args, int expectedCode, string expectedOutput)
    {
        Assert.Equal(expectedCode, TreeCmd.Run(args, io));
        Assert.Equal(new[] { expectedOutput }, io.Outputs);
        Assert.Empty(io.Errors);
    }

    private static FakeIo CreateIo(byte[]? stdin = null)
    {
        return new FakeIo(
            stdin ?? Array.Empty<byte>(),
            new Dictionary<string, byte[]?>
            {
                ["a.txt"] = Encoding.UTF8.GetBytes("f 0644 a\n"),
                ["bad.txt"] = Encoding.UTF8.GetBytes("d 8 b\n"),
                ["empty.txt"] = Encoding.UTF8.GetBytes("# nothing\n"),
                ["tree.txt"] = Encoding.UTF8.GetBytes(TreeListing)
            });
    }

    private sealed class FakeIo : IIo
    {
        private readonly byte[] stdin;

        public FakeIo(byte[] stdin, Dictionary<string, byte[]?> files)
        {
            this.stdin = stdin;
            Files = files;
        }

        public Dictionary<string, byte[]?> Files { get; }

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
            return Files.TryGetValue(path, out var bytes) ? bytes : null;
        }

        public void Out(string text) => Outputs.Add(text);

        public void Err(string text) => Errors.Add(text);
    }
}
