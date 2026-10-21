using System.Text;

namespace Modeplan.Tests;

public sealed class ListingTests
{
    [Fact]
    public void Parse_preserves_entries_and_skips_comments_and_blank_lines()
    {
        Assert.Equal(
            new[] { new Listing.Entry(3, false, 420, "a") },
            Listing.Parse("# c\u0001\n \t\nf 0644 a\n"));

        Assert.Equal(
            new[]
            {
                new Listing.Entry(1, true, 493, "dir name"),
                new Listing.Entry(2, false, 0, "a\tb")
            },
            Listing.Parse("d rwxr-xr-x dir name\t \r\nf 0 a\tb\r"));

        Assert.Equal(
            new[]
            {
                new Listing.Entry(1, false, 420, "é"),
                new Listing.Entry(2, false, 420, "😀"),
                new Listing.Entry(3, false, 420, "\u0085\u00a0")
            },
            Listing.Parse("f 0644 é\nf 0644 😀\nf 0644 \u0085\u00a0"));

        Assert.Equal(
            new[]
            {
                new Listing.Entry(1, false, 0, "A"),
                new Listing.Entry(2, false, 0, "a")
            },
            Listing.Parse("f 0 A\nf 0 a"));

        Assert.Empty(Listing.Parse("\n# nothing\n"));
    }

    [Theory]
    [InlineData("f 0644 café\u0001", "ML001 unexpected character \"?\" at 1:12")]
    [InlineData("f 0644 😀\u0001", "ML001 unexpected character \"?\" at 1:9")]
    [InlineData("# c\n\nf 0644 a\r\r\n", "ML001 unexpected character \"?\" at 3:9")]
    [InlineData("f 0644 a\u007f", "ML001 unexpected character \"?\" at 1:9")]
    [InlineData("\u0001", "ML001 unexpected character \"?\" at 1:1")]
    [InlineData("x 8 a\u0001", "ML001 unexpected character \"?\" at 1:6")]
    [InlineData("# c\n   x 0644 a", "ML002 invalid kind \"x\" at 2:4")]
    [InlineData("x", "ML002 invalid kind \"x\" at 1:1")]
    [InlineData("f", "ML003 missing mode at 1:2")]
    [InlineData("d  \t ", "ML003 missing mode at 1:6")]
    [InlineData("# c\nf 0x4 b", "ML004 invalid mode \"0x4\" at 2:3")]
    [InlineData("  d\t9 a", "ML004 invalid mode \"9\" at 1:5")]
    [InlineData("f 8", "ML004 invalid mode \"8\" at 1:3")]
    [InlineData("f 0644", "ML005 missing path at 1:7")]
    [InlineData("f 0644   ", "ML005 missing path at 1:10")]
    [InlineData("f 0644 a\nf 0600 a", "ML006 repeated path \"a\" at 2:8")]
    [InlineData("f 0644 a\n# c\nf 0600 a", "ML006 repeated path \"a\" at 3:8")]
    [InlineData("f 0644 a b\nf 0644 a b ", "ML006 repeated path \"a b\" at 2:8")]
    [InlineData("f 0644 é\nd rwxr-xr-x\té", "ML006 repeated path \"?\" at 2:13")]
    public void Parse_reports_the_first_validation_problem_and_code_point_column(string text, string expected)
    {
        var failure = Assert.Throws<Failure>(() => Listing.Parse(text));

        Assert.Equal(expected, failure.Message);
    }

    [Fact]
    public void Loader_reads_named_file_and_preserves_display_and_entries()
    {
        var io = new FakeIo(
            Array.Empty<byte>(),
            new Dictionary<string, byte[]?> { ["a.txt"] = Encoding.UTF8.GetBytes("f 0644 a") });

        var loaded = Loader.Read(io, "a.txt");

        Assert.Equal("\"a.txt\"", loaded.Display);
        Assert.Equal(new[] { new Listing.Entry(1, false, 420, "a") }, loaded.Entries);
        Assert.Equal(new[] { "a.txt" }, io.FileReads);
        Assert.Equal(0, io.StdinReads);
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
    }

    [Fact]
    public void Loader_reads_standard_input_for_dash()
    {
        var io = new FakeIo(
            Encoding.UTF8.GetBytes("f 0644 a"),
            new Dictionary<string, byte[]?>());

        var loaded = Loader.Read(io, "-");

        Assert.Equal("<stdin>", loaded.Display);
        Assert.Equal(new[] { new Listing.Entry(1, false, 420, "a") }, loaded.Entries);
        Assert.Equal(1, io.StdinReads);
        Assert.Empty(io.FileReads);
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
    }

    [Fact]
    public void Loader_decorates_listing_errors_and_rejects_empty_listings()
    {
        var bad = new FakeIo(
            Array.Empty<byte>(),
            new Dictionary<string, byte[]?>
            {
                ["bad.txt"] = Encoding.UTF8.GetBytes("# l\nf 0644 a\nd 8 b"),
                ["empty.txt"] = Encoding.UTF8.GetBytes("# nothing")
            });

        Assert.Equal(
            "\"bad.txt\": ML004 invalid mode \"8\" at 3:3",
            Assert.Throws<Failure>(() => Loader.Read(bad, "bad.txt")).Message);
        Assert.Equal(
            "\"empty.txt\": no entries",
            Assert.Throws<Failure>(() => Loader.Read(bad, "empty.txt")).Message);
        Assert.Equal(new[] { "bad.txt", "empty.txt" }, bad.FileReads);
        Assert.Empty(bad.Outputs);
        Assert.Empty(bad.Errors);
    }

    [Fact]
    public void Loader_preserves_unreadable_and_invalid_utf8_input_failures()
    {
        var unreadable = new FakeIo(Array.Empty<byte>(), new Dictionary<string, byte[]?>());

        Assert.Equal(
            "\"nowhere.txt\": cannot read the file",
            Assert.Throws<Failure>(() => Loader.Read(unreadable, "nowhere.txt")).Message);
        Assert.Equal(new[] { "nowhere.txt" }, unreadable.FileReads);

        var invalidUtf8 = new FakeIo(
            new byte[] { 0x61, 0xff, 0x0a },
            new Dictionary<string, byte[]?>());

        Assert.Equal(
            "<stdin>: input is not valid UTF-8",
            Assert.Throws<Failure>(() => Loader.Read(invalidUtf8, "-")).Message);
        Assert.Equal(1, invalidUtf8.StdinReads);
        Assert.Empty(invalidUtf8.FileReads);
        Assert.Empty(unreadable.Outputs);
        Assert.Empty(unreadable.Errors);
        Assert.Empty(invalidUtf8.Outputs);
        Assert.Empty(invalidUtf8.Errors);
    }

    private sealed class FakeIo : IIo
    {
        private readonly byte[] stdin;
        private readonly IReadOnlyDictionary<string, byte[]?> files;

        public FakeIo(byte[] stdin, IReadOnlyDictionary<string, byte[]?> files)
        {
            this.stdin = stdin;
            this.files = files;
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
