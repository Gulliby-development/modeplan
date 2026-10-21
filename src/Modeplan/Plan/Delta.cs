namespace Modeplan;

public static class Delta
{
    private const int AllModeBits = 4095;

    public static int Added(int oldMode, int newMode)
    {
        return newMode & ~oldMode & AllModeBits;
    }

    public static int Deleted(int oldMode, int newMode)
    {
        return oldMode & ~newMode & AllModeBits;
    }

    public static int ChangedBits(int oldMode, int newMode)
    {
        return System.Numerics.BitOperations.PopCount((uint)(Added(oldMode, newMode) | Deleted(oldMode, newMode)));
    }
}
