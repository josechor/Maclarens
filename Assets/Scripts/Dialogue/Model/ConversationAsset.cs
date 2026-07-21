using System.Collections.Generic;
using UnityEngine;

// Asset producido por MccImporter a partir de un archivo .mcc. Guarda la cabecera ya parseada
// (tipo + condiciones, baratas de consultar por el selector) y el texto fuente. El cuerpo se
// reparsea a pasos al iniciar la conversación (coste despreciable, evita serializar jerarquías
// polimórficas de pasos).
public class ConversationAsset : ScriptableObject
{
    public string conversationId;
    public ConversationType type;
    public ConversationCondition condition = new ConversationCondition();

    [TextArea(5, 40)]
    public string source;

    // Story y Context ocurren una sola vez; Idle es repetible.
    public bool IsOnce => type != ConversationType.Idle;

    // ¿Puede reproducirse ahora? Cumple condiciones de flags y, si es de una vez, no se jugó aún.
    public bool IsAvailable()
    {
        if (!condition.Matches())
        {
            return false;
        }

        if (IsOnce && ConversationHistory.HasPlayed(conversationId))
        {
            return false;
        }

        return true;
    }

    // Reparsea el cuerpo a pasos ejecutables. Devuelve null si el texto no es válido.
    public List<DialogueStep> ParseSteps()
    {
        try
        {
            return MccParser.Parse(source).Steps;
        }
        catch (MccParseException e)
        {
            Debug.LogError($"{conversationId}.mcc:{e.Line} — {e.Message}");
            return null;
        }
    }
}
