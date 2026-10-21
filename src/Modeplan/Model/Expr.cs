namespace Modeplan;

public record Act(string Who, char Op, string Perms)
{
    public string Text => Who + Op + Perms;
}

public record Expression(string Text, bool Numeric, int Value, IReadOnlyList<Act> Actions);
