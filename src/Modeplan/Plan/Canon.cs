namespace Modeplan;

public static class Canon
{
    private static readonly (string Who, int Read, int Write, int Execute, int Special, char SpecialLetter)[] Classes =
    {
        ("u", 0x100, 0x080, 0x040, 0x800, 's'),
        ("g", 0x020, 0x010, 0x008, 0x400, 's'),
        ("o", 0x004, 0x002, 0x001, 0x200, 't')
    };

    public static string Build(int oldMode, int newMode)
    {
        var additions = Delta.Added(oldMode, newMode);
        var deletions = Delta.Deleted(oldMode, newMode);
        var clauses = new List<(string Text, string Who)>();

        foreach (var modeClass in Classes)
        {
            var classBits = modeClass.Read | modeClass.Write | modeClass.Execute | modeClass.Special;
            var added = additions & classBits;
            var deleted = deletions & classBits;
            if (added == 0 && deleted == 0)
            {
                continue;
            }

            var classText = added != 0 && deleted != 0
                ? "=" + Letters(
                    newMode,
                    modeClass.Read,
                    modeClass.Write,
                    modeClass.Execute,
                    modeClass.Special,
                    modeClass.SpecialLetter)
                : added != 0
                    ? "+" + Letters(
                        added,
                        modeClass.Read,
                        modeClass.Write,
                        modeClass.Execute,
                        modeClass.Special,
                        modeClass.SpecialLetter)
                    : "-" + Letters(
                        deleted,
                        modeClass.Read,
                        modeClass.Write,
                        modeClass.Execute,
                        modeClass.Special,
                        modeClass.SpecialLetter);

            var clauseIndex = -1;
            for (var index = 0; index < clauses.Count; index++)
            {
                if (string.Equals(clauses[index].Text, classText, StringComparison.Ordinal))
                {
                    clauseIndex = index;
                    break;
                }
            }

            if (clauseIndex < 0)
            {
                clauses.Add((classText, modeClass.Who));
            }
            else
            {
                clauses[clauseIndex] = (classText, clauses[clauseIndex].Who + modeClass.Who);
            }
        }

        var result = new List<string>(clauses.Count);
        foreach (var clause in clauses)
        {
            var who = clause.Who == "ugo" ? "a" : clause.Who;
            result.Add(who + clause.Text);
        }

        return string.Join(",", result);
    }

    private static string Letters(
        int mode,
        int readBit,
        int writeBit,
        int executeBit,
        int specialBit,
        char specialLetter)
    {
        var letters = new List<char>(4);
        if ((mode & readBit) != 0)
        {
            letters.Add('r');
        }

        if ((mode & writeBit) != 0)
        {
            letters.Add('w');
        }

        if ((mode & executeBit) != 0)
        {
            letters.Add('x');
        }

        if ((mode & specialBit) != 0)
        {
            letters.Add(specialLetter);
        }

        return new string(letters.ToArray());
    }
}
