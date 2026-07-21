using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

// Importador de archivos .mcc. Al guardar un archivo:
//   1. Lo parsea (errores de sintaxis con archivo:línea en la Consola).
//   2. Valida que cada personaje y expresión existan (sin sustituciones automáticas).
//   3. Produce un ConversationAsset con la cabecera y el texto fuente.
[ScriptedImporter(1, "mcc")]
public class MccImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string text = File.ReadAllText(ctx.assetPath);
        string id = Path.GetFileNameWithoutExtension(ctx.assetPath);

        var asset = ScriptableObject.CreateInstance<ConversationAsset>();
        asset.conversationId = id;
        asset.source = text;

        try
        {
            ParsedConversation parsed = MccParser.Parse(text);
            asset.type = parsed.Type;
            asset.condition = parsed.Condition;

            ValidateCharacters(parsed.Steps, ctx);
        }
        catch (MccParseException e)
        {
            ctx.LogImportError($"{ctx.assetPath}:{e.Line} — {e.Message}");
        }

        ctx.AddObjectToAsset("conversation", asset);
        ctx.SetMainObject(asset);
    }

    private void ValidateCharacters(List<DialogueStep> steps, AssetImportContext ctx)
    {
        Dictionary<string, CharacterDef> characters = LoadCharacters(ctx);

        // Si no hay ningún personaje definido todavía (p.ej. primer arranque del proyecto),
        // no bloqueamos la importación: la validación se hará cuando existan.
        if (characters.Count == 0)
        {
            return;
        }

        ValidateSteps(steps, characters, ctx);
    }

    private void ValidateSteps(List<DialogueStep> steps, Dictionary<string, CharacterDef> characters, AssetImportContext ctx)
    {
        foreach (var step in steps)
        {
            if (step is LineStep line)
            {
                if (!characters.TryGetValue(line.Speaker, out CharacterDef def))
                {
                    ctx.LogImportError($"{ctx.assetPath}:{line.Line} — el personaje '{line.Speaker}' no existe (¿falta un CharacterDef?).");
                    continue;
                }

                if (!string.IsNullOrEmpty(line.Expression) && !def.HasExpression(line.Expression))
                {
                    ctx.LogImportError($"{ctx.assetPath}:{line.Line} — la expresión '{line.Expression}' no existe en {line.Speaker}.");
                }
            }
            else if (step is ChoiceStep choice)
            {
                foreach (var option in choice.Options)
                {
                    ValidateSteps(option.Body, characters, ctx);
                }
            }
        }
    }

    private Dictionary<string, CharacterDef> LoadCharacters(AssetImportContext ctx)
    {
        var result = new Dictionary<string, CharacterDef>();

        foreach (string guid in AssetDatabase.FindAssets("t:CharacterDef"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Reimporta esta conversación si cambia algún personaje (nombre/expresiones).
            ctx.DependsOnSourceAsset(path);

            var def = AssetDatabase.LoadAssetAtPath<CharacterDef>(path);
            if (def != null && !string.IsNullOrEmpty(def.characterName))
            {
                result[def.characterName] = def;
            }
        }

        return result;
    }
}
