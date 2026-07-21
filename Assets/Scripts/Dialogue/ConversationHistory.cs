using System.Collections.Generic;
using UnityEngine;

// Registro de conversaciones ya reproducidas (para las de "una sola vez": Story y Context).
// Tanda 1: solo en memoria (se reinicia cada partida). Persistencia en disco = Tanda 2.
public static class ConversationHistory
{
    private static readonly HashSet<string> played = new HashSet<string>();

    // Con "Reload Domain" desactivado en Enter Play Mode Settings, los static NO se reinician
    // solos al parar y volver a darle a Play. Este atributo sí se vuelve a ejecutar en ese caso.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlaySessionStart()
    {
        ResetAll();
    }

    public static bool HasPlayed(string conversationId)
    {
        return !string.IsNullOrEmpty(conversationId) && played.Contains(conversationId);
    }

    public static void MarkPlayed(string conversationId)
    {
        if (!string.IsNullOrEmpty(conversationId))
        {
            played.Add(conversationId);
        }
    }

    public static void ResetAll()
    {
        played.Clear();
    }

    // ---------- Persistencia (usado por SaveSystem) ----------

    public static void WriteTo(SaveData data)
    {
        data.playedConversations = new List<string>(played);
    }

    public static void ReadFrom(SaveData data)
    {
        played.Clear();

        if (data?.playedConversations == null)
        {
            return;
        }

        foreach (string id in data.playedConversations)
        {
            played.Add(id);
        }
    }
}
