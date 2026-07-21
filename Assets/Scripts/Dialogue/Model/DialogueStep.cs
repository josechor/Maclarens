using System.Collections.Generic;

// Un paso de diálogo ya parseado. La SECUENCIA de pasos es el orden de reproducción:
// no hay índices que enlacen unos con otros (a diferencia del sistema anterior).
public abstract class DialogueStep
{
    // Línea del archivo .mcc de la que salió este paso (1-based). Para errores claros.
    public int Line;
}

// Una línea hablada: "Personaje [expresion]: texto".
public class LineStep : DialogueStep
{
    public string Speaker;
    public string Expression; // puede ser null => se usará la expresión por defecto del personaje
    public string Text;
}

// "set flag" / "unset flag": modifica GameFlags en ese punto de la conversación.
public class FlagStep : DialogueStep
{
    public string Flag;
    public bool Value; // true = set, false = unset
}

// "? Personaje:" con opciones. Cada opción abre un bloque (Body) que, al terminar,
// reconverge automáticamente al paso siguiente a este ChoiceStep.
public class ChoiceStep : DialogueStep
{
    public string Speaker; // normalmente "Prota"; informativo
    public List<ChoiceOption> Options = new List<ChoiceOption>();
}

public class ChoiceOption
{
    public string MoodLabel;                 // lo que ve el botón: una actitud, no el texto
    public List<DialogueStep> Body = new List<DialogueStep>();
}

// Comando embebido "[nombre arg1 arg2]". Reservado para Tanda 2 (cinemáticas). El parser
// lo reconoce ya; el runner de Tanda 1 lo ignora con un aviso.
public class CommandStep : DialogueStep
{
    public string Name;
    public string[] Args;
}
