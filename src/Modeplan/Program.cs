using System.Text;

namespace Modeplan;

public static class Program
{
    public static int Main(string[] args)
    {
        var io = new ConsoleIo();
        return Cli.Run(args, io);
    }

    private sealed class ConsoleIo : IIo
    {
        private static readonly UTF8Encoding Utf8 = new(false);

        public byte[] ReadStdin()
        {
            using var buffer = new MemoryStream();
            Console.OpenStandardInput().CopyTo(buffer);
            return buffer.ToArray();
        }

        public byte[]? ReadFile(string path)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (System.Security.SecurityException)
            {
                return null;
            }
        }

        public void Out(string text)
        {
            Write(Console.OpenStandardOutput(), text);
        }

        public void Err(string text)
        {
            Write(Console.OpenStandardError(), text);
        }

        private static void Write(Stream stream, string text)
        {
            var bytes = Utf8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
