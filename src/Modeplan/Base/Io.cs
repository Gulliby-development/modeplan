using System.Text;

namespace Modeplan;

public interface IIo
{
    byte[] ReadStdin();

    byte[]? ReadFile(string path);

    void Out(string text);

    void Err(string text);
}

public static class Input
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public record Source(string Display, string Text);

    public static Source Read(IIo io, string arg)
    {
        var isStdin = string.Equals(arg, "-", StringComparison.Ordinal);
        var display = isStdin ? "<stdin>" : Text.Show(arg);
        var bytes = isStdin ? io.ReadStdin() : io.ReadFile(arg);

        if (bytes is null)
        {
            throw new Failure($"{display}: cannot read the file");
        }

        string text;
        try
        {
            text = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new Failure($"{display}: input is not valid UTF-8");
        }

        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        return new Source(display, text);
    }
}
