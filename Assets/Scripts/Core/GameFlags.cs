using System.Collections.Generic;

public static class GameFlags
{
    private static readonly HashSet<string> setFlags = new HashSet<string>();

    public static void Set(string flag)
    {
        if (!string.IsNullOrEmpty(flag))
        {
            setFlags.Add(flag);
        }
    }

    public static void Clear(string flag)
    {
        setFlags.Remove(flag);
    }

    public static bool IsSet(string flag)
    {
        return !string.IsNullOrEmpty(flag) && setFlags.Contains(flag);
    }

    public static void ResetAll()
    {
        setFlags.Clear();
    }
}
