using System;
using System.Collections.Generic;

// Excepción de parseo con número de línea. El importador la formatea como "archivo:línea — mensaje".
public class MccParseException : Exception
{
    public int Line { get; }

    public MccParseException(int line, string message) : base(message)
    {
        Line = line;
    }
}

// Resultado de parsear un archivo .mcc completo: cabecera (tipo + condiciones) + cuerpo (pasos).
public class ParsedConversation
{
    public ConversationType Type;
    public ConversationCondition Condition = new ConversationCondition();
    public List<DialogueStep> Steps = new List<DialogueStep>();
}

// Parser del lenguaje de guion .mcc. Convierte texto en pasos tipados.
// Reglas (Tanda 1):
//   # comentario ; líneas en blanco se ignoran
//   Cabecera: @story|@context|@idle [required: flagA, !flagB]
//   Línea:    Personaje [expresion]: texto
//   Elección: ? Personaje:
//               - Etiqueta:
//                   <bloque indentado de líneas>
//   Flags:    set flag  /  unset flag
//   Comando:  [nombre arg1 arg2]   (reservado Tanda 2)
// La indentación (solo espacios) define los bloques.
public static class MccParser
{
    private class RawLine
    {
        public int Number;   // 1-based
        public int Indent;   // nº de espacios iniciales
        public string Text;  // contenido sin la indentación
    }

    public static ParsedConversation Parse(string source)
    {
        var result = new ParsedConversation();
        List<RawLine> lines = Tokenize(source);

        int index = 0;
        ParseHeader(lines, ref index, result);
        result.Steps = ParseSteps(lines, ref index, BaseIndentOf(lines, index));

        if (index < lines.Count)
        {
            throw new MccParseException(lines[index].Number,
                "Indentación inesperada: la línea no encaja en ningún bloque.");
        }

        return result;
    }

    private static List<RawLine> Tokenize(string source)
    {
        var result = new List<RawLine>();
        string[] rawLines = source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        for (int i = 0; i < rawLines.Length; i++)
        {
            string raw = rawLines[i];
            int number = i + 1;

            if (raw.IndexOf('\t') >= 0)
            {
                throw new MccParseException(number, "Usa espacios para indentar, no tabuladores.");
            }

            int indent = 0;
            while (indent < raw.Length && raw[indent] == ' ')
            {
                indent++;
            }

            string content = raw.Substring(indent).TrimEnd();

            // Ignora líneas en blanco y comentarios.
            if (content.Length == 0 || content.StartsWith("#"))
            {
                continue;
            }

            result.Add(new RawLine { Number = number, Indent = indent, Text = content });
        }

        return result;
    }

    private static void ParseHeader(List<RawLine> lines, ref int index, ParsedConversation result)
    {
        if (index >= lines.Count)
        {
            throw new MccParseException(1, "El archivo está vacío: falta la cabecera (@story/@context/@idle).");
        }

        RawLine header = lines[index];
        if (!header.Text.StartsWith("@"))
        {
            throw new MccParseException(header.Number,
                "La primera línea debe ser la cabecera: @story, @context o @idle.");
        }

        // "@story   required: !intro_done"
        int firstSpace = header.Text.IndexOf(' ');
        string typeToken = firstSpace < 0 ? header.Text : header.Text.Substring(0, firstSpace);
        string rest = firstSpace < 0 ? "" : header.Text.Substring(firstSpace + 1).Trim();

        switch (typeToken)
        {
            case "@story": result.Type = ConversationType.Story; break;
            case "@context": result.Type = ConversationType.Context; break;
            case "@idle": result.Type = ConversationType.Idle; break;
            default:
                throw new MccParseException(header.Number,
                    $"Tipo de conversación desconocido '{typeToken}'. Usa @story, @context o @idle.");
        }

        if (rest.Length > 0)
        {
            ParseRequired(header.Number, rest, result.Condition);
        }

        index++;
    }

    private static void ParseRequired(int lineNumber, string rest, ConversationCondition condition)
    {
        const string prefix = "required:";
        if (!rest.StartsWith(prefix))
        {
            throw new MccParseException(lineNumber,
                $"No entiendo '{rest}' en la cabecera. Formato: required: flagA, !flagB");
        }

        string list = rest.Substring(prefix.Length).Trim();
        if (list.Length == 0)
        {
            return;
        }

        foreach (string raw in list.Split(','))
        {
            string flag = raw.Trim();
            if (flag.Length == 0)
            {
                continue;
            }

            if (flag.StartsWith("!"))
            {
                condition.forbiddenFlags.Add(flag.Substring(1).Trim());
            }
            else
            {
                condition.requiredFlags.Add(flag);
            }
        }
    }

    private static int BaseIndentOf(List<RawLine> lines, int index)
    {
        return index < lines.Count ? lines[index].Indent : 0;
    }

    // Parsea una secuencia de pasos hermanos, todos con la misma indentación 'baseIndent'.
    // Termina cuando encuentra una línea con menor indentación (dedent) o el fin.
    private static List<DialogueStep> ParseSteps(List<RawLine> lines, ref int index, int baseIndent)
    {
        var steps = new List<DialogueStep>();

        while (index < lines.Count)
        {
            RawLine line = lines[index];

            if (line.Indent < baseIndent)
            {
                break; // dedent: fin del bloque
            }

            if (line.Indent > baseIndent)
            {
                throw new MccParseException(line.Number, "Indentación inesperada.");
            }

            if (line.Text.StartsWith("?"))
            {
                steps.Add(ParseChoice(lines, ref index, baseIndent));
            }
            else
            {
                steps.Add(ParseSimpleStep(line));
                index++;
            }
        }

        return steps;
    }

    private static ChoiceStep ParseChoice(List<RawLine> lines, ref int index, int choiceIndent)
    {
        RawLine header = lines[index];
        var choice = new ChoiceStep { Line = header.Number };

        // "? Prota:" -> guarda el nombre (informativo), sin el '?' ni los ':'.
        string speaker = header.Text.Substring(1).Trim().TrimEnd(':').Trim();
        choice.Speaker = speaker;
        index++;

        while (index < lines.Count && lines[index].Indent > choiceIndent)
        {
            RawLine optLine = lines[index];

            if (!optLine.Text.StartsWith("-"))
            {
                throw new MccParseException(optLine.Number,
                    "Se esperaba una opción con el formato '- Etiqueta:'.");
            }

            int optionIndent = optLine.Indent;
            string label = optLine.Text.Substring(1).Trim();
            if (label.EndsWith(":"))
            {
                label = label.Substring(0, label.Length - 1).Trim();
            }

            if (label.Length == 0)
            {
                throw new MccParseException(optLine.Number, "La opción no tiene etiqueta de ánimo.");
            }

            index++;

            if (index >= lines.Count || lines[index].Indent <= optionIndent)
            {
                throw new MccParseException(optLine.Number,
                    $"La opción '{label}' no tiene ninguna línea de diálogo debajo.");
            }

            int bodyIndent = lines[index].Indent;
            var body = ParseSteps(lines, ref index, bodyIndent);

            choice.Options.Add(new ChoiceOption { MoodLabel = label, Body = body });
        }

        if (choice.Options.Count == 0)
        {
            throw new MccParseException(header.Number, "La elección no tiene ninguna opción.");
        }

        return choice;
    }

    private static DialogueStep ParseSimpleStep(RawLine line)
    {
        string text = line.Text;

        if (text.StartsWith("set ") || text == "set")
        {
            return MakeFlagStep(line, text.Length > 3 ? text.Substring(4).Trim() : "", true);
        }

        if (text.StartsWith("unset ") || text == "unset")
        {
            return MakeFlagStep(line, text.Length > 5 ? text.Substring(6).Trim() : "", false);
        }

        if (text.StartsWith("[") && text.EndsWith("]"))
        {
            string inner = text.Substring(1, text.Length - 2).Trim();
            string[] parts = inner.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                throw new MccParseException(line.Number, "Comando vacío '[]'.");
            }

            var args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            return new CommandStep { Line = line.Number, Name = parts[0], Args = args };
        }

        return ParseLine(line);
    }

    private static FlagStep MakeFlagStep(RawLine line, string flag, bool value)
    {
        if (flag.Length == 0)
        {
            throw new MccParseException(line.Number, (value ? "set" : "unset") + " sin nombre de flag.");
        }

        return new FlagStep { Line = line.Number, Flag = flag, Value = value };
    }

    // "Personaje [expresion]: texto"  ó  "Personaje: texto"
    private static LineStep ParseLine(RawLine line)
    {
        int colon = line.Text.IndexOf(':');
        if (colon < 0)
        {
            throw new MccParseException(line.Number,
                "Línea no reconocida. Formato esperado: 'Personaje [expresion]: texto'.");
        }

        string head = line.Text.Substring(0, colon).Trim();
        string body = line.Text.Substring(colon + 1).Trim();

        string speaker;
        string expression = null;

        int bracket = head.IndexOf('[');
        if (bracket >= 0)
        {
            int close = head.IndexOf(']', bracket);
            if (close < 0)
            {
                throw new MccParseException(line.Number, "Falta cerrar el corchete de la expresión ']'.");
            }

            speaker = head.Substring(0, bracket).Trim();
            expression = head.Substring(bracket + 1, close - bracket - 1).Trim();
        }
        else
        {
            speaker = head;
        }

        if (speaker.Length == 0)
        {
            throw new MccParseException(line.Number, "Falta el nombre del personaje antes de ':'.");
        }

        if (body.Length == 0)
        {
            throw new MccParseException(line.Number, $"'{speaker}' no dice nada (texto vacío).");
        }

        return new LineStep { Line = line.Number, Speaker = speaker, Expression = expression, Text = body };
    }
}
