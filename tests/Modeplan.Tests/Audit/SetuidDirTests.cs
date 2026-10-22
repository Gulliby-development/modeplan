using System.Text;

namespace Modeplan.Tests;

public sealed class SetuidDirTests
{
    [Theory]
    [InlineData("d 4755 a\n", false, 1, "\"a\" MA008 setuid bit on a directory\n1 finding\n")]
    [InlineData("d rwsr-xr-x a\n", false, 1, "\"a\" MA008 setuid bit on a directory\n1 finding\n")]
    [InlineData("d 6755 a\n", false, 1, "\"a\" MA008 setuid bit on a directory\n1 finding\n")]
    [InlineData("d 7777 a\n", false, 1, "\"a\" MA008 setuid bit on a directory\n1 finding\n")]
    [InlineData("d 4777 a\n", false, 1, "\"a\" MA001 world-writable directory without the sticky bit\n\"a\" MA008 setuid bit on a directory\n2 findings\n")]
    [InlineData("d 4644 a\n", false, 1, "\"a\" MA002 setuid bit without owner execute\n\"a\" MA006 directory readable but not searchable for owner\n\"a\" MA008 setuid bit on a directory\n3 findings\n")]
    [InlineData("d 4775 a\n", true, 1, "\"a\" MA007 group-writable\n\"a\" MA008 setuid bit on a directory\n2 findings\n")]
    [InlineData("d 2755 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("d 1777 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("f 4755 a\n", false, 0, "ok, 1 path\n")]
    [InlineData("d 4755 src/\nf 4755 bin/su\nd 0755 lib/\nd 4700 var/\n", false, 1, "\"src/\" MA008 setuid bit on a directory\n\"var/\" MA008 setuid bit on a directory\n2 findings\n")]
    public void Audit_reports_a_setuid_directory(string listing, bool strict, int expectedStatus, string expectedOutput)
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
