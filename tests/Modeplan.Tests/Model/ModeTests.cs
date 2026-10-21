namespace Modeplan.Tests;

public sealed class ModeTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("0000", 0)]
    [InlineData("7777", 4095)]
    [InlineData("4755", 2541)]
    [InlineData("rwsr-xr-x", 2541)]
    [InlineData("rwxrwxrwt", 1023)]
    [InlineData("--S--S--T", 3584)]
    [InlineData("---------", 0)]
    [InlineData("rws------", 2496)]
    [InlineData("rwS------", 2432)]
    [InlineData("---rws---", 1080)]
    [InlineData("---rwS---", 1072)]
    [InlineData("------rwt", 519)]
    [InlineData("------rwT", 518)]
    [InlineData("00000", null)]
    [InlineData("8", null)]
    [InlineData("６４４", null)]
    [InlineData("6\t4", null)]
    [InlineData("rwxrwxrwxx", null)]
    [InlineData("rwt------", null)]
    [InlineData("", null)]
    public void Parse_accepts_only_twelve_bit_octal_and_symbolic_modes(string text, int? expected)
    {
        Assert.Equal(expected, Mode.Parse(text));
    }

    [Theory]
    [InlineData(0, "0000", "---------")]
    [InlineData(4095, "7777", "rwsrwsrwt")]
    [InlineData(2541, "4755", "rwsr-xr-x")]
    [InlineData(1023, "1777", "rwxrwxrwt")]
    [InlineData(3584, "7000", "--S--S--T")]
    [InlineData(2496, "4700", "rws------")]
    [InlineData(2432, "4600", "rwS------")]
    [InlineData(1080, "2070", "---rws---")]
    [InlineData(1072, "2060", "---rwS---")]
    [InlineData(519, "1007", "------rwt")]
    [InlineData(518, "1006", "------rwT")]
    public void Octal_and_ls_format_all_mode_bits(int mode, string octal, string symbolic)
    {
        Assert.Equal(octal, Mode.Octal(mode));
        Assert.Equal(symbolic, Mode.Ls(mode));
    }
}
