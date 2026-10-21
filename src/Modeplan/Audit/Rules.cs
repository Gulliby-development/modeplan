namespace Modeplan;

public static class Rules
{
    public static IReadOnlyList<(string Code, string Message)> Evaluate(Listing.Entry entry, bool strict)
    {
        var findings = new List<(string Code, string Message)>();

        var otherCanWrite = (entry.Mode & 0x002) != 0;
        if (otherCanWrite && (!entry.Dir || (entry.Mode & 0x200) == 0))
        {
            findings.Add((
                "MA001",
                entry.Dir
                    ? "world-writable directory without the sticky bit"
                    : "world-writable file"));
        }

        if ((entry.Mode & 0x800) != 0 && (entry.Mode & 0x040) == 0)
        {
            findings.Add(("MA002", "setuid bit without owner execute"));
        }

        if ((entry.Mode & 0x400) != 0 && (entry.Mode & 0x008) == 0)
        {
            findings.Add(("MA003", "setgid bit without group execute"));
        }

        if ((entry.Mode & 0x200) != 0 && !entry.Dir)
        {
            findings.Add(("MA004", "sticky bit on a file"));
        }

        var missingOwnerLetters = string.Concat(
            (entry.Mode & 0x100) == 0 && (entry.Mode & (0x020 | 0x004)) != 0 ? "r" : "",
            (entry.Mode & 0x080) == 0 && (entry.Mode & (0x010 | 0x002)) != 0 ? "w" : "",
            (entry.Mode & 0x040) == 0 && (entry.Mode & (0x008 | 0x001)) != 0 ? "x" : "");
        if (missingOwnerLetters.Length != 0)
        {
            findings.Add((
                "MA005",
                $"owner lacks {missingOwnerLetters} that group or other has"));
        }

        if (entry.Dir)
        {
            if (IsReadableButNotSearchable(entry.Mode, 0x100, 0x040))
            {
                findings.Add(("MA006", "directory readable but not searchable for owner"));
            }
            else if (IsReadableButNotSearchable(entry.Mode, 0x020, 0x008))
            {
                findings.Add(("MA006", "directory readable but not searchable for group"));
            }
            else if (IsReadableButNotSearchable(entry.Mode, 0x004, 0x001))
            {
                findings.Add(("MA006", "directory readable but not searchable for other"));
            }
        }

        if (strict && (entry.Mode & 0x010) != 0)
        {
            findings.Add(("MA007", "group-writable"));
        }

        return findings;
    }

    private static bool IsReadableButNotSearchable(int mode, int readBit, int executeBit)
    {
        return (mode & readBit) != 0 && (mode & executeBit) == 0;
    }
}
