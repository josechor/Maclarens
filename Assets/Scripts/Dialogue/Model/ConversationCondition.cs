using System.Collections.Generic;

// Condiciones de disponibilidad de una conversación (leídas de la cabecera @tipo required: ...).
// Reutiliza el patrón del antiguo ConditionalDialogue.Matches().
[System.Serializable]
public class ConversationCondition
{
    public List<string> requiredFlags = new List<string>();  // deben estar activas
    public List<string> forbiddenFlags = new List<string>(); // deben estar SIN activar (prefijo ! en el .mcc)

    public bool Matches()
    {
        foreach (var flag in requiredFlags)
        {
            if (!GameFlags.IsSet(flag))
            {
                return false;
            }
        }

        foreach (var flag in forbiddenFlags)
        {
            if (GameFlags.IsSet(flag))
            {
                return false;
            }
        }

        return true;
    }
}
