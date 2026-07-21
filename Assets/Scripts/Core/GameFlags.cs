using System.Collections.Generic;
using UnityEngine;

public static class GameFlags
{
    private static readonly HashSet<string> setFlags = new HashSet<string>();
    private static readonly Dictionary<string, int> counters = new Dictionary<string, int>();

    // Con "Reload Domain" desactivado en Enter Play Mode Settings, los static NO se reinician
    // solos al parar y volver a darle a Play. Este atributo sí se vuelve a ejecutar en ese caso.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlaySessionStart()
    {
        ResetAll();
    }

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

    // Contadores enteros: independientes de los flags booleanos de arriba, pero mismo ciclo
    // de vida (en memoria, se reinician cada partida hasta que exista persistencia en disco).
    public static void Increment(string counter, int amount = 1)
    {
        if (string.IsNullOrEmpty(counter))
        {
            return;
        }

        counters.TryGetValue(counter, out int current);
        counters[counter] = current + amount;
    }

    public static void SetCount(string counter, int value)
    {
        if (!string.IsNullOrEmpty(counter))
        {
            counters[counter] = value;
        }
    }

    public static int GetCount(string counter)
    {
        if (string.IsNullOrEmpty(counter))
        {
            return 0;
        }

        counters.TryGetValue(counter, out int value);
        return value;
    }

    public static void ResetAll()
    {
        setFlags.Clear();
        counters.Clear();
    }

    // ---------- Persistencia (usado por SaveSystem) ----------

    // Copia el estado actual a un SaveData (flags + contadores como listas paralelas).
    public static void WriteTo(SaveData data)
    {
        data.flags = new List<string>(setFlags);

        data.counterNames = new List<string>(counters.Count);
        data.counterValues = new List<int>(counters.Count);
        foreach (var kv in counters)
        {
            data.counterNames.Add(kv.Key);
            data.counterValues.Add(kv.Value);
        }
    }

    // Reemplaza el estado actual por el de un SaveData.
    public static void ReadFrom(SaveData data)
    {
        setFlags.Clear();
        counters.Clear();

        if (data == null)
        {
            return;
        }

        if (data.flags != null)
        {
            foreach (string flag in data.flags)
            {
                setFlags.Add(flag);
            }
        }

        if (data.counterNames != null && data.counterValues != null)
        {
            int n = System.Math.Min(data.counterNames.Count, data.counterValues.Count);
            for (int i = 0; i < n; i++)
            {
                counters[data.counterNames[i]] = data.counterValues[i];
            }
        }
    }
}
