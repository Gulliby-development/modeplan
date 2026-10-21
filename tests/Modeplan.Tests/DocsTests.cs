namespace Modeplan.Tests;

public sealed class DocsTests
{
    [Fact]
    public void Readme_contains_all_required_headings()
    {
        var readme = ReadReadme();

        Assert.Contains("\n## Install\n", readme);
        Assert.Contains("\n## Usage\n", readme);
        Assert.Contains("\n## Commands\n", readme);
        Assert.Contains("\n## Exit codes\n", readme);
        Assert.Contains("\n## Development\n", readme);
    }

    [Fact]
    public void Readme_contains_the_exact_usage_and_tree_listing_blocks()
    {
        var readme = ReadReadme();

        Assert.Contains(
            "```text\n" +
            "usage: modeplan apply MODE EXPR [--dir] [--umask OCT]\n" +
            "       modeplan plan OLD NEW\n" +
            "       modeplan match LISTING TEST... [--kind f|d]\n" +
            "       modeplan audit LISTING [--strict]\n" +
            "       modeplan tree LISTING EXPR [--umask OCT]\n" +
            "```\n",
            readme);
        Assert.Contains(
            "```text\n" +
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
            "d 0644 odd dir/\n" +
            "```\n",
            readme);
    }

    [Fact]
    public void Readme_contains_the_exact_apply_and_audit_output_blocks()
    {
        var readme = ReadReadme();

        Assert.Contains(
            "```text\n" +
            "STEP  ACTION  MODE  SYMBOLIC\n" +
            "0     start   0644  rw-r--r--\n" +
            "1     u+x     0744  rwxr--r--\n" +
            "2     go-r    0700  rwx------\n" +
            "result      0700 rwx------\n" +
            "changed     3 bits\n" +
            "```\n",
            readme);
        Assert.Contains(
            "```text\n" +
            "\"notes.txt\" MA001 world-writable file\n" +
            "\"shared/\" MA001 world-writable directory without the sticky bit\n" +
            "\"odd\" MA005 owner lacks r that group or other has\n" +
            "\"odd dir/\" MA006 directory readable but not searchable for owner\n" +
            "4 findings\n" +
            "```\n",
            readme);
    }

    private static string ReadReadme()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "README.md");
            if (File.Exists(candidate))
            {
                return NormalizeLineEndings(File.ReadAllText(candidate));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate README.md by walking upward from AppContext.BaseDirectory.");
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}
