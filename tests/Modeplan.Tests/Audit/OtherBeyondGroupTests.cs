using System.Text;

namespace Modeplan.Tests;

public sealed class OtherBeyondGroupTests
{
    [Theory]
    [InlineData("f 0604 a\n", true, 1, "\"a\" MA009 other has r that group lacks\n1 finding\n")]
    [InlineData("f 0601 a\n", true, 1, "\"a\" MA005 owner lacks x that group or other has\n\"a\" MA009 other has x that group lacks\n2 findings\n")]
    [InlineData("f 0705 a\n", true, 1, "\"a\" MA009 other has rx that group lacks\n1 finding\n")]
    [InlineData("f 0700 a\n", true, 0, "ok, 1 path\n")]
    [InlineData("f 0644 a\n", true, 0, "ok, 1 path\n")]
    [InlineData("f 0755 a\n", true, 0, "ok, 1 path\n")]
    [InlineData("f 0604 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("f rw----r-- a\n", true, 1, "\"a\" MA009 other has r that group lacks\n1 finding\n")]
    [InlineData("d 0702 a\n", true, 1, "\"a\" MA001 world-writable directory without the sticky bit\n\"a\" MA009 other has w that group lacks\n2 findings\n")]
    [InlineData("f 0666 a\n", true, 1, "\"a\" MA001 world-writable file\n\"a\" MA007 group-writable\n2 findings\n")]
    [InlineData("f 0662 a\n", true, 1, "\"a\" MA001 world-writable file\n\"a\" MA007 group-writable\n2 findings\n")]
    [InlineData("f 4601 a\n", true, 1, "\"a\" MA002 setuid bit without owner execute\n\"a\" MA005 owner lacks x that group or other has\n\"a\" MA009 other has x that group lacks\n3 findings\n")]
    [InlineData("d 4705 a\n", true, 1, "\"a\" MA008 setuid bit on a directory\n\"a\" MA009 other has rx that group lacks\n2 findings\n")]
    [InlineData("f 0604 a\nf 0644 b\nd 0701 c/\n", true, 1, "\"a\" MA009 other has r that group lacks\n\"c/\" MA009 other has x that group lacks\n2 findings\n")]
    public void Audit_reports_other_permissions_beyond_group(string listing, bool strict, int expectedStatus, string expectedOutput)
    {
        var io = new FakeIo { Stdin = Encoding.UTF8.GetBytes(listing) };
        string[] args = strict ? new[] { "audit", "-", "--strict" } : new[] { "audit", "-" };

        var status = Cli.Run(args, io);

        Assert.Equal(expectedStatus, status);
        Assert.Equal(new[] { expectedOutput }, io.Outputs);
        Assert.Empty(io.Errors);
    }

    private sealed class FakeIo : IIo
    {
        public byte[] Stdin { get; set; } = Array.Empty<byte>();

        public List<string> Outputs { get; } = new();

        public List<string> Errors { get; } = new();

        public byte[] ReadStdin()
        {
            return Stdin;
        }

        public byte[]? ReadFile(string path)
        {
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
