namespace Modeplan.Tests;

public sealed class OptionsIoTests
{
    [Fact]
    public void Parse_keeps_non_options_and_reads_a_flag()
    {
        var parsed = Options.Parse(new[] { "-022", "-", "--dir", "x" }, Specs());

        Assert.Equal(new[] { "--dir" }, parsed.Flags);
        Assert.Empty(parsed.Values);
        Assert.Equal(new[] { "-022", "-", "x" }, parsed.Args);
    }

    [Fact]
    public void Parse_splits_values_at_the_first_equals_sign()
    {
        var parsed = Options.Parse(new[] { "--umask=1=2" }, Specs());

        Assert.Empty(parsed.Flags);
        Assert.Equal("1=2", parsed.Values["--umask"]);
        Assert.Empty(parsed.Args);
    }

    [Fact]
    public void Parse_accepts_a_next_argument_that_looks_like_an_option()
    {
        var parsed = Options.Parse(new[] { "--umask", "--dir" }, Specs());

        Assert.Empty(parsed.Flags);
        Assert.Equal("--dir", parsed.Values["--umask"]);
        Assert.Empty(parsed.Args);
    }

    [Fact]
    public void Parse_preserves_an_explicit_empty_value()
    {
        var parsed = Options.Parse(new[] { "--umask=" }, Specs());

        Assert.Empty(parsed.Flags);
        Assert.Single(parsed.Values);
        Assert.Equal(string.Empty, parsed.Values["--umask"]);
        Assert.Empty(parsed.Args);
    }

    [Fact]
    public void Parse_reports_unknown_option_names_and_whole_arguments()
    {
        AssertOptionFailure("unknown option \"--\"", "--");
        AssertOptionFailure("unknown option \"--help\"", "--help");
        AssertOptionFailure("unknown option \"--unknown=x\"", "--unknown=x");
    }

    [Fact]
    public void Parse_checks_flag_values_before_repetition()
    {
        AssertOptionFailure("--dir takes no value", "--dir=1");
        AssertOptionFailure("--dir takes no value", "--dir", "--dir=1");
    }

    [Fact]
    public void Parse_checks_missing_value_before_repetition()
    {
        AssertOptionFailure("--umask needs a value", "--umask", "1", "--umask");
    }

    [Fact]
    public void Parse_rejects_repeated_value_and_flag_options()
    {
        AssertOptionFailure("repeated option --umask", "--umask", "1", "--umask", "2");
        AssertOptionFailure("repeated option --dir", "--dir", "--dir");
    }

    [Fact]
    public void Read_routes_stdin_and_files_and_preserves_their_display_names()
    {
        var stdinIo = new FakeIo(new byte[] { 97, 10 });
        var stdin = Input.Read(stdinIo, "-");

        Assert.Equal("<stdin>", stdin.Display);
        Assert.Equal("a\n", stdin.Text);
        Assert.Equal(1, stdinIo.StdinReads);
        Assert.Empty(stdinIo.FileReads);

        var fileIo = new FakeIo(
            files: new Dictionary<string, byte[]?>(StringComparer.Ordinal)
            {
                ["a.txt"] = new byte[] { 97, 10 }
            });
        var file = Input.Read(fileIo, "a.txt");

        Assert.Equal("\"a.txt\"", file.Display);
        Assert.Equal("a\n", file.Text);
        Assert.Equal(new[] { "a.txt" }, fileIo.FileReads);
        Assert.Equal(0, fileIo.StdinReads);
        AssertNoWrites(stdinIo);
        AssertNoWrites(fileIo);
    }

    [Fact]
    public void Read_decodes_multibyte_text_and_removes_exactly_one_leading_bom()
    {
        var unicodeIo = new FakeIo(new byte[] { 195, 169, 240, 159, 152, 128 });
        Assert.Equal("é😀", Input.Read(unicodeIo, "-").Text);

        var bomIo = new FakeIo(new byte[] { 239, 187, 191, 239, 187, 191, 97 });
        Assert.Equal("\uFEFFa", Input.Read(bomIo, "-").Text);
        AssertNoWrites(unicodeIo);
        AssertNoWrites(bomIo);
    }

    [Fact]
    public void Read_rejects_each_malformed_utf8_sequence()
    {
        AssertInvalidStdin(new byte[] { 97, 255, 10 });
        AssertInvalidStdin(new byte[] { 192, 175 });
        AssertInvalidStdin(new byte[] { 237, 160, 128 });
        AssertInvalidStdin(new byte[] { 226, 130 });

        var fileIo = new FakeIo(files: new Dictionary<string, byte[]?>(StringComparer.Ordinal)
        {
            ["bad.txt"] = new byte[] { 97, 255, 10 }
        });
        var failure = Assert.Throws<Failure>(() => Input.Read(fileIo, "bad.txt"));
        Assert.Equal("\"bad.txt\": input is not valid UTF-8", failure.Message);
        AssertNoWrites(fileIo);
    }

    [Fact]
    public void Read_reports_an_unreadable_file()
    {
        var io = new FakeIo();
        var failure = Assert.Throws<Failure>(() => Input.Read(io, "nowhere.txt"));

        Assert.Equal("\"nowhere.txt\": cannot read the file", failure.Message);
        Assert.Equal(new[] { "nowhere.txt" }, io.FileReads);
        Assert.Equal(0, io.StdinReads);
        AssertNoWrites(io);
    }

    private static IReadOnlyList<Options.Spec> Specs()
    {
        return new[]
        {
            new Options.Spec("--dir", false),
            new Options.Spec("--umask", true)
        };
    }

    private static void AssertOptionFailure(string expected, params string[] argv)
    {
        var failure = Assert.Throws<Failure>(() => Options.Parse(argv, Specs()));
        Assert.Equal(expected, failure.Message);
    }

    private static void AssertInvalidStdin(byte[] bytes)
    {
        var io = new FakeIo(bytes);
        var failure = Assert.Throws<Failure>(() => Input.Read(io, "-"));

        Assert.Equal("<stdin>: input is not valid UTF-8", failure.Message);
        Assert.Equal(1, io.StdinReads);
        Assert.Empty(io.FileReads);
        AssertNoWrites(io);
    }

    private static void AssertNoWrites(FakeIo io)
    {
        Assert.Empty(io.Outputs);
        Assert.Empty(io.Errors);
    }

    private sealed class FakeIo : IIo
    {
        private readonly byte[] _stdin;
        private readonly IReadOnlyDictionary<string, byte[]?> _files;

        public FakeIo(byte[]? stdin = null, IReadOnlyDictionary<string, byte[]?>? files = null)
        {
            _stdin = stdin ?? Array.Empty<byte>();
            _files = files ?? new Dictionary<string, byte[]?>(StringComparer.Ordinal);
        }

        public List<string> FileReads { get; } = new();

        public List<string> Outputs { get; } = new();

        public List<string> Errors { get; } = new();

        public int StdinReads { get; private set; }

        public byte[] ReadStdin()
        {
            StdinReads++;
            return _stdin;
        }

        public byte[]? ReadFile(string path)
        {
            FileReads.Add(path);
            return _files.TryGetValue(path, out var bytes) ? bytes : null;
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
