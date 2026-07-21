// Tipos de conversación, en orden de prioridad de reproducción:
// Story (principal, una vez) > Context (reacción a evento, una vez) > Idle (relleno, repetible).
public enum ConversationType
{
    Story,
    Context,
    Idle
}
