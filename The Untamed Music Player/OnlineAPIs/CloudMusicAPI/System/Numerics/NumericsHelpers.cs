namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.System.Numerics;
internal static class NumericsHelpers
{
    public static void DangerousMakeTwosComplement(uint[] d)
    {
        if (d is not null && d.Length > 0)
        {
            d[0] = unchecked(~d[0] + 1);

            var i = 1;
            // first do complement and +1 as long as carry is needed
            for (; d[i - 1] == 0 && i < d.Length; i++)
            {
                d[i] = unchecked(~d[i] + 1);
            }
            // now ones complement is sufficient
            for (; i < d.Length; i++)
            {
                d[i] = ~d[i];
            }
        }
    }

    public static uint Abs(int a)
    {
        unchecked
        {
            var mask = (uint)(a >> 31);
            return ((uint)a ^ mask) - mask;
        }
    }
}
