using System.Numerics;

namespace Modeplan;

public static class Changes
{
    public static int Count(int initialMode, int finalMode)
    {
        var difference = (uint)((initialMode ^ finalMode) & 0xFFF);
        return BitOperations.PopCount(difference);
    }
}
