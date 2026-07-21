using System.Collections.Generic;

// Elige qué conversación reproducir de entre las de un NPC, aplicando la prioridad
// Story > Context > Idle sobre las que están disponibles ahora mismo.
// A igual prioridad, gana la primera en el orden de la lista del NPC.
public static class ConversationSelector
{
    public static ConversationAsset Select(IReadOnlyList<ConversationAsset> conversations)
    {
        if (conversations == null)
        {
            return null;
        }

        ConversationAsset best = null;
        int bestPriority = int.MinValue;

        foreach (var convo in conversations)
        {
            if (convo == null || !convo.IsAvailable())
            {
                continue;
            }

            int priority = PriorityOf(convo.type);
            if (priority > bestPriority)
            {
                bestPriority = priority;
                best = convo;
            }
        }

        return best;
    }

    private static int PriorityOf(ConversationType type)
    {
        switch (type)
        {
            case ConversationType.Story: return 3;
            case ConversationType.Context: return 2;
            default: return 1; // Idle
        }
    }
}
