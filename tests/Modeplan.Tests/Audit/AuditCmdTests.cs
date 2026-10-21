using System.Text;

namespace Modeplan;

public sealed class AuditCmdTests
{
    [Fact]
    public void Usage_is_exact()
    {
        Assert.Equal("modeplan audit LISTING [--strict]", AuditCmd.Usage);
    }

    [Theory]
    [InlineData("-", "f 0666 a\n", false, 1, "\"a\" MA001 world-writable file\n1 finding\n")]
    [InlineData("-", "d 0777 a\n", false, 1, "\"a\" MA001 world-writable directory without the sticky bit\n1 finding\n")]
    [InlineData("-", "d 1777 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("-", "f 4644 a\n", false, 1, "\"a\" MA002 setuid bit without owner execute\n1 finding\n")]
    [InlineData("-", "f 2705 a\n", false, 1, "\"a\" MA003 setgid bit without group execute\n1 finding\n")]
    [InlineData("-", "f 1644 a\n", false, 1, "\"a\" MA004 sticky bit on a file\n1 finding\n")]
    [InlineData("-", "f 4755 a\nf 2755 b\n", false, 0, "ok, 2 paths\n")]
    [InlineData("-", "f 0077 a\n", false, 1, "\"a\" MA001 world-writable file\n\"a\" MA005 owner lacks rwx that group or other has\n2 findings\n")]
    [InlineData("-", "f 0412 a\n", false, 1, "\"a\" MA001 world-writable file\n\"a\" MA005 owner lacks wx that group or other has\n2 findings\n")]
    [InlineData("-", "d 0444 a\n", false, 1, "\"a\" MA006 directory readable but not searchable for owner\n1 finding\n")]
    [InlineData("-", "d 0744 a\n", false, 1, "\"a\" MA006 directory readable but not searchable for group\n1 finding\n")]
    [InlineData("-", "d 0754 a\n", false, 1, "\"a\" MA006 directory readable but not searchable for other\n1 finding\n")]
    [InlineData("-", "f 0444 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("-", "d 0311 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("-", "f 0660 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("-", "f 0660 a\n", true, 1, "\"a\" MA007 group-writable\n1 finding\n")]
    [InlineData("-", "d 1777 a\n", true, 1, "\"a\" MA007 group-writable\n1 finding\n")]
    [InlineData("-", "f 7646 a\n", true, 1, "\"a\" MA001 world-writable file\n\"a\" MA002 setuid bit without owner execute\n\"a\" MA003 setgid bit without group execute\n\"a\" MA004 sticky bit on a file\n4 findings\n")]
    [InlineData("-", "f 0644 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("-", "f 0666 café😀\tx\n", false, 1, "\"caf???x\" MA001 world-writable file\n1 finding\n")]
    [InlineData("tree.txt", "# a small tree\nd 0755 src/\nf 0644 src/main.c\nf 0755 build.sh\nf 4755 bin/su\nd 1777 /tmp\nf 0666 notes.txt\nd 0777 shared/\nf 0600 id_rsa\nf 0044 odd\nd 0644 odd dir/\n", false, 1, "\"notes.txt\" MA001 world-writable file\n\"shared/\" MA001 world-writable directory without the sticky bit\n\"odd\" MA005 owner lacks r that group or other has\n\"odd dir/\" MA006 directory readable but not searchable for owner\n4 findings\n")]
    [InlineData("risk.txt", "f 4644 run-suid\nf 2705 run-sgid\nf 1644 sticky-file\nf rw-rw-rw- open\nd rwxr-xr-t ok/\nd r--r--r-- list/\nd -wx--x--x drop/\nf 0000 none\n", false, 1, "\"run-suid\" MA002 setuid bit without owner execute\n\"run-sgid\" MA003 setgid bit without group execute\n\"sticky-file\" MA004 sticky bit on a file\n\"open\" MA001 world-writable file\n\"list/\" MA006 directory readable but not searchable for owner\n5 findings\n")]
    [InlineData("risk.txt", "f 4644 run-suid\nf 2705 run-sgid\nf 1644 sticky-file\nf rw-rw-rw- open\nd rwxr-xr-t ok/\nd r--r--r-- list/\nd -wx--x--x drop/\nf 0000 none\n", true, 1, "\"run-suid\" MA002 setuid bit without owner execute\n\"run-sgid\" MA003 setgid bit without group execute\n\"sticky-file\" MA004 sticky bit on a file\n\"open\" MA001 world-writable file\n\"open\" MA007 group-writable\n\"list/\" MA006 directory readable but not searchable for owner\n6 findings\n")]
    public void Audit_outputs_match_the_literal_examples(string inputName, string listing, bool strict, int expectedStatus, string expectedOutput)
    {
        var io = new FakeIo();
        if (inputName == "-")
        {
            io.Stdin = Encoding.UTF8.GetBytes(listing);
        }
        else
        {
            io.Files.Add(inputName, Encoding.UTF8.GetBytes(listing));
        }

        string[] args = inputName == "-"
            ? strict ? new[] { "-", "--strict" } : new[] { "-" }
            : strict ? new[] { inputName, "--strict" } : new[] { inputName };

        var status = AuditCmd.Run(args, io);

        Assert.Equal(expectedStatus, status);
        Assert.Equal(new[] { expectedOutput }, io.Outputs);
        Assert.Equal(0, io.ErrCalls);
        Assert.Equal(inputName == "-" ? 1 : 0, io.StdinReadCalls);
        Assert.Equal(inputName == "-" ? 0 : 1, io.FileReadCalls);
        if (inputName != "-")
        {
            Assert.Equal(inputName, io.LastReadFilePath);
        }
    }

    [Theory]
    [InlineData(new string[] { "--write" }, "unknown option \"--write\"")]
    [InlineData(new string[] { "tree.txt", "--strict=1" }, "--strict takes no value")]
    [InlineData(new string[] { "tree.txt", "--strict", "--strict" }, "repeated option --strict")]
    [InlineData(new string[] { }, "missing LISTING")]
    [InlineData(new string[] { "--strict" }, "missing LISTING")]
    [InlineData(new string[] { "a", "b" }, "too many arguments")]
    [InlineData(new string[] { "a", "b", "--write" }, "unknown option \"--write\"")]
    public void Option_and_arity_failures_precede_all_io(string[] args, string expectedMessage)
    {
        var io = new FakeIo();

        var failure = Assert.Throws<Failure>(() => AuditCmd.Run(args, io));

        Assert.Equal(expectedMessage, failure.Message);
        Assert.Equal(0, io.StdinReadCalls);
        Assert.Equal(0, io.FileReadCalls);
        Assert.Empty(io.Outputs);
        Assert.Equal(0, io.ErrCalls);
    }

    [Theory]
    [InlineData("missing", "\"nowhere.txt\": cannot read the file")]
    [InlineData("utf8", "<stdin>: input is not valid UTF-8")]
    [InlineData("empty", "\"empty.txt\": no entries")]
    [InlineData("bad", "\"bad.txt\": ML004 invalid mode \"8\" at 3:3")]
    public void Loader_failures_propagate_without_output(string scenario, string expectedMessage)
    {
        var io = new FakeIo();
        string[] args;

        switch (scenario)
        {
            case "missing":
                args = new[] { "nowhere.txt" };
                break;
            case "utf8":
                args = new[] { "-" };
                io.Stdin = new byte[] { 97, 255, 10 };
                break;
            case "empty":
                args = new[] { "empty.txt" };
                io.Files.Add("empty.txt", Encoding.UTF8.GetBytes("# nothing"));
                break;
            default:
                args = new[] { "bad.txt" };
                io.Files.Add("bad.txt", Encoding.UTF8.GetBytes("# l\nf 0644 a\nd 8 b\n"));
                break;
        }

        var failure = Assert.Throws<Failure>(() => AuditCmd.Run(args, io));

        Assert.Equal(expectedMessage, failure.Message);
        Assert.Empty(io.Outputs);
        Assert.Equal(0, io.ErrCalls);
        Assert.Equal(scenario == "utf8" ? 1 : 0, io.StdinReadCalls);
        Assert.Equal(scenario == "utf8" ? 0 : 1, io.FileReadCalls);
        if (scenario == "missing")
        {
            Assert.Equal("nowhere.txt", io.LastReadFilePath);
        }
        else if (scenario == "empty")
        {
            Assert.Equal("empty.txt", io.LastReadFilePath);
        }
        else if (scenario == "bad")
        {
            Assert.Equal("bad.txt", io.LastReadFilePath);
        }
    }

    private sealed class FakeIo : IIo
    {
        public byte[] Stdin { get; set; } = Array.Empty<byte>();

        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

        public List<string> Outputs { get; } = new();

        public int StdinReadCalls { get; private set; }

        public int FileReadCalls { get; private set; }

        public int ErrCalls { get; private set; }

        public string? LastReadFilePath { get; private set; }

        public byte[] ReadStdin()
        {
            StdinReadCalls++;
            return Stdin;
        }

        public byte[]? ReadFile(string path)
        {
            FileReadCalls++;
            LastReadFilePath = path;
            return Files.TryGetValue(path, out var contents) ? contents : null;
        }

        public void Out(string text)
        {
            Outputs.Add(text);
        }

        public void Err(string text)
        {
            ErrCalls++;
        }
    }
}
