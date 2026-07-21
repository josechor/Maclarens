using System.Collections.Generic;

// Registro de conversaciones ya reproducidas (para las de "una sola vez": Story y Context).
// Tanda 1: solo en memoria (se reinicia cada partida). Persistencia en disco = Tanda 2.
public static class ConversationHistory
{
    private static readonly HashSet<string> played = new HashSet<string>();

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
}
