using System.Collections.Generic;

public enum CounterComparator
{
    GreaterOrEqual, // >=
    Greater,        // >
    LessOrEqual,    // <=
    Less,           // <
    Equal,          // ==
    NotEqual        // !=
}

// Una comparación numérica en un required: (ej. "contador_copas >= 3").
[System.Serializable]
public class CounterCondition
{
    public string counter;
    public CounterComparator comparator;
    public int value;

    public bool Matches()
    {
        int current = GameFlags.GetCount(counter);

        switch (comparator)
        {
            case CounterComparator.GreaterOrEqual: return current >= value;
            case CounterComparator.Greater: return current > value;
            case CounterComparator.LessOrEqual: return current <= value;
            case CounterComparator.Less: return current < value;
            case CounterComparator.Equal: return current == value;
            case CounterComparator.NotEqual: return current != value;
            default: return false;
        }
    }
}

// Condiciones de disponibilidad de una conversación (leídas de la cabecera @tipo required: ...).
// Reutiliza el patrón del antiguo ConditionalDialogue.Matches().
[System.Serializable]
public class ConversationCondition
{
    public List<string> requiredFlags = new List<string>();  // deben estar activas
    public List<string> forbiddenFlags = new List<string>(); // deben estar SIN activar (prefijo ! en el .mcc)
    public List<CounterCondition> counterConditions = new List<CounterCondition>(); // comparaciones numéricas

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

        foreach (var counterCondition in counterConditions)
        {
            if (!counterCondition.Matches())
            {
                return false;
            }
        }

        return true;
    }
}
