namespace Modeplan;

public static class Semantics
{
    private const int UserRwx = 0x1C0;
    private const int GroupRwx = 0x038;
    private const int OtherRwx = 0x007;
    private const int AllRwx = UserRwx | GroupRwx | OtherRwx;
    private const int UserSpecial = 0x800;
    private const int GroupSpecial = 0x400;
    private const int OtherSpecial = 0x200;

    public record Step(string Action, int Mode);

    public static int? ParseUmask(string text)
    {
        if (text.Length is < 1 or > 3)
        {
            return null;
        }

        var value = 0;
        foreach (var character in text)
        {
            if (character is < '0' or > '7')
            {
                return null;
            }

            value = (value << 3) | (character - '0');
        }

        return value;
    }

    public static int Run(Act a, int mode, bool dir, int umask)
    {
        var selectsUser = a.Who.Contains('u') || a.Who.Contains('a') || a.Who.Length == 0;
        var selectsGroup = a.Who.Contains('g') || a.Who.Contains('a') || a.Who.Length == 0;
        var selectsOther = a.Who.Contains('o') || a.Who.Contains('a') || a.Who.Length == 0;
        var implicitWho = a.Who.Length == 0;

        var selectedRwx = (selectsUser ? UserRwx : 0)
            | (selectsGroup ? GroupRwx : 0)
            | (selectsOther ? OtherRwx : 0);
        var affectedRwx = implicitWho ? selectedRwx & ~umask : selectedRwx;
        var specialBits = (selectsUser ? UserSpecial : 0)
            | (selectsGroup ? GroupSpecial : 0)
            | (selectsOther ? OtherSpecial : 0);

        var permissions = (PermissionBits(a.Perms, mode, dir, selectsUser, selectsGroup, selectsOther) & affectedRwx)
            | SpecialPermissionBits(a.Who, a.Perms);

        return a.Op switch
        {
            '+' => mode | permissions,
            '-' => mode & ~permissions,
            '=' => (mode & ~(affectedRwx | specialBits)) | permissions,
            _ => mode
        };
    }

    public static IReadOnlyList<Step> Steps(Expression e, int mode, bool dir, int umask)
    {
        if (e.Numeric)
        {
            return new[] { new Step(e.Text, e.Value) };
        }

        var steps = new List<Step>(e.Actions.Count);
        foreach (var action in e.Actions)
        {
            mode = Run(action, mode, dir, umask);
            steps.Add(new Step(action.Text, mode));
        }

        return steps;
    }

    private static int PermissionBits(
        string perms,
        int mode,
        bool dir,
        bool selectsUser,
        bool selectsGroup,
        bool selectsOther)
    {
        var perClass = 0;
        foreach (var permission in perms)
        {
            perClass |= permission switch
            {
                'r' => 4,
                'w' => 2,
                'x' => 1,
                'X' when dir || (mode & AllExecute) != 0 => 1,
                'u' => (mode >> 6) & 7,
                'g' => (mode >> 3) & 7,
                'o' => mode & 7,
                _ => 0
            };
        }

        return (selectsUser ? (perClass << 6) & UserRwx : 0)
            | (selectsGroup ? (perClass << 3) & GroupRwx : 0)
            | (selectsOther ? perClass & OtherRwx : 0);
    }

    private static int SpecialPermissionBits(string who, string perms)
    {
        if (!perms.Contains('s'))
        {
            return perms.Contains('t') ? OtherSpecial : 0;
        }

        var selectsUser = who.Length == 0 || who.Contains('u') || who.Contains('a');
        var selectsGroup = who.Length == 0 || who.Contains('g') || who.Contains('a');
        return (selectsUser ? UserSpecial : 0)
            | (selectsGroup ? GroupSpecial : 0)
            | (perms.Contains('t') ? OtherSpecial : 0);
    }

    private static int SpecialPermissionBitsFromText(string perms)
    {
        return perms.Contains('t') ? OtherSpecial : 0;
    }

    private const int AllExecute = 0x049;
}
