using System.Diagnostics;
using System.Text;

namespace Modeplan.Tests;

public sealed class UnitTest1
{
    [Theory]
    [InlineData(new string[0], "modeplan: missing command (apply, plan, match, audit or tree)\n")]
    [InlineData(new[] { "fold", "x" }, "modeplan: unknown command \"fold\"\n")]
    [InlineData(new[] { "--help", "x" }, "modeplan: unknown command \"--help\"\n")]
    [InlineData(new[] { "😀" }, "modeplan: unknown command \"?\"\n")]
    [InlineData(new[] { "audit", "tree.txt", "--write" }, "modeplan: unknown option \"--write\"\n")]
    [InlineData(new[] { "audit", "tree.txt", "--strict=1" }, "modeplan: --strict takes no value\n")]
    [InlineData(new[] { "apply", "0644", "u+x", "--umask" }, "modeplan: --umask needs a value\n")]
    [InlineData(new[] { "apply", "0644", "u+x", "--umask", "1", "--umask", "2" }, "modeplan: repeated option --umask\n")]
    [InlineData(new[] { "apply", "--x" }, "modeplan: unknown option \"--x\"\n")]
    [InlineData(new[] { "apply", "0644" }, "modeplan: missing EXPR\n")]
    [InlineData(new[] { "match", "tree.txt" }, "modeplan: missing TEST\n")]
    [InlineData(new[] { "plan", "0644", "0755", "0600" }, "modeplan: too many arguments\n")]
    [InlineData(new[] { "apply", "9", "u+q" }, "modeplan: invalid mode \"9\"\n")]
    [InlineData(new[] { "plan", "8", "9" }, "modeplan: invalid mode \"8\"\n")]
    [InlineData(new[] { "apply", "0644", "u+q", "--umask", "9" }, "modeplan: invalid expression \"u+q\": unexpected character \"q\" at column 3\n")]
    [InlineData(new[] { "apply", "0644", "u+x", "--umask", "088" }, "modeplan: invalid umask \"088\"\n")]
    [InlineData(new[] { "apply", "0644", "u+😀" }, "modeplan: invalid expression \"u+?\": unexpected character \"?\" at column 3\n")]
    public void Run_reports_literal_dispatch_errors_without_reading_or_writing_stdout(
        string[] args,
        string expectedError)
    {
        var io = new FakeIo();

        var exit = Cli.Run(args, io);

        Assert.Equal(2, exit);
        Assert.Empty(io.Outputs);
        Assert.Equal(new[] { expectedError }, io.Errors);
        Assert.Empty(io.FileReads);
        Assert.Equal(0, io.StdinReads);
    }

    [Fact]
    public void Run_exposes_help_and_dispatches_all_five_commands_with_exact_output()
    {
        Assert.Equal(
            "usage: modeplan apply MODE EXPR [--dir] [--umask OCT]\n" +
            "       modeplan plan OLD NEW\n" +
            "       modeplan match LISTING TEST... [--kind f|d]\n" +
            "       modeplan audit LISTING [--strict]\n" +
            "       modeplan tree LISTING EXPR [--umask OCT]\n",
            Cli.Usage);

        var helpIo = new FakeIo();
        var helpExit = Cli.Run(new[] { "--help" }, helpIo);
        Assert.Equal(0, helpExit);
        Assert.Equal(new[] { Cli.Usage }, helpIo.Outputs);
        Assert.Empty(helpIo.Errors);
        Assert.Empty(helpIo.FileReads);
        Assert.Equal(0, helpIo.StdinReads);

        var applyIo = new FakeIo();
        var applyExit = Cli.Run(new[] { "apply", "0644", "u+x,go-r" }, applyIo);
        AssertSingleResult(
            applyIo,
            0,
            "STEP  ACTION  MODE  SYMBOLIC\n0     start   0644  rw-r--r--\n1     u+x     0744  rwxr--r--\n2     go-r    0700  rwx------\nresult      0700 rwx------\nchanged     3 bits\n",
            applyExit);

        var planIo = new FakeIo();
        var planExit = Cli.Run(new[] { "plan", "0644", "0644" }, planIo);
        AssertSingleResult(
            planIo,
            1,
            "old         0644 rw-r--r--\nnew         0644 rw-r--r--\nsymbolic    -\nnumeric     644\nchanged     0 bits\n",
            planExit);

        var matchIo = new FakeIo();
        var matchExit = Cli.Run(new[] { "match", "a.txt", "7777" }, matchIo);
        AssertSingleResult(matchIo, 1, "matched     0 of 1\n", matchExit, "a.txt");

        var auditIo = new FakeIo();
        var auditExit = Cli.Run(new[] { "audit", "a.txt" }, auditIo);
        AssertSingleResult(auditIo, 0, "ok, 1 path\n", auditExit, "a.txt");

        var treeIo = new FakeIo();
        var treeExit = Cli.Run(new[] { "tree", "a.txt", "u+x" }, treeIo);
        AssertSingleResult(
            treeIo,
            0,
            "LINE  KIND  OLD   NEW   PATH\n" +
            "1     f     0644  0744  \"a\"\n" +
            "changed     1 of 1\n",
            treeExit,
            "a.txt");
    }

    [Theory]
    [InlineData(new[] { "tree", "nowhere.txt", "u+q" }, "modeplan: \"nowhere.txt\": cannot read the file\n", "nowhere.txt")]
    [InlineData(new[] { "audit", "empty.txt" }, "modeplan: \"empty.txt\": no entries\n", "empty.txt")]
    [InlineData(new[] { "audit", "bad.txt" }, "modeplan: \"bad.txt\": ML004 invalid mode \"8\" at 3:3\n", "bad.txt")]
    [InlineData(new[] { "match", "tree.txt", "8", "--kind", "x" }, "modeplan: invalid test \"8\"\n", "tree.txt")]
    [InlineData(new[] { "tree", "tree.txt", "u+q", "--umask", "9" }, "modeplan: invalid expression \"u+q\": unexpected character \"q\" at column 3\n", "tree.txt")]
    public void Run_loads_inputs_and_preserves_validation_precedence(
        string[] args,
        string expectedError,
        string expectedFileRead)
    {
        var io = new FakeIo();

        var exit = Cli.Run(args, io);

        Assert.Equal(2, exit);
        Assert.Empty(io.Outputs);
        Assert.Equal(new[] { expectedError }, io.Errors);
        Assert.Equal(new[] { expectedFileRead }, io.FileReads);
        Assert.Equal(0, io.StdinReads);
    }

    [Fact]
    public async Task Run_process_rejects_malformed_utf8_on_standard_input()
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("modeplan.dll");
        startInfo.ArgumentList.Add("audit");
        startInfo.ArgumentList.Add("-");

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start());

        using var stdout = new MemoryStream();
        using var stderr = new MemoryStream();
        var stdoutDrain = process.StandardOutput.BaseStream.CopyToAsync(stdout);
        var stderrDrain = process.StandardError.BaseStream.CopyToAsync(stderr);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            await process.StandardInput.BaseStream.WriteAsync(new byte[] { 0x61, 0xFF, 0x0A }, timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(stdoutDrain, stderrDrain).WaitAsync(timeout.Token);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        Assert.Equal(2, process.ExitCode);
        Assert.Empty(stdout.ToArray());
        Assert.Equal(
            Encoding.UTF8.GetBytes("modeplan: <stdin>: input is not valid UTF-8\n"),
            stderr.ToArray());
    }

    private static void AssertSingleResult(
        FakeIo io,
        int expectedExit,
        string expectedOutput,
        int actualExit,
        params string[] expectedFileReads)
    {
        Assert.Equal(expectedExit, actualExit);
        Assert.Equal(new[] { expectedOutput }, io.Outputs);
        Assert.Empty(io.Errors);
        Assert.Equal(expectedFileReads, io.FileReads);
        Assert.Equal(0, io.StdinReads);
    }

    private sealed class FakeIo : IIo
    {
        private static readonly IReadOnlyDictionary<string, byte[]?> Files =
            new Dictionary<string, byte[]?>(StringComparer.Ordinal)
            {
                ["a.txt"] = Encoding.UTF8.GetBytes("f 0644 a\n"),
                ["empty.txt"] = Encoding.UTF8.GetBytes("# nothing\n"),
                ["bad.txt"] = Encoding.UTF8.GetBytes("# l\nf 0644 a\nd 8 b\n"),
                ["tree.txt"] = Encoding.UTF8.GetBytes(
                    "# a small tree\n" +
                    "d 0755 src/\n" +
                    "f 0644 src/main.c\n" +
                    "f 0755 build.sh\n" +
                    "f 4755 bin/su\n" +
                    "d 1777 /tmp\n" +
                    "f 0666 notes.txt\n" +
                    "d 0777 shared/\n" +
                    "f 0600 id_rsa\n" +
                    "f 0044 odd\n" +
                    "d 0644 odd dir/\n")
            };

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
            return Files.TryGetValue(path, out var bytes) ? bytes : null;
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
