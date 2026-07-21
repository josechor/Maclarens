using System.Collections.Generic;

// Registro global de personajes en runtime, para que el diálogo resuelva un nombre ("Camarero")
// a su CharacterDef. Se rellena con CharacterRegistryLoader al arrancar la escena.
public static class CharacterRegistry
{
    private static readonly Dictionary<string, CharacterDef> byName = new Dictionary<string, CharacterDef>();

    public static void Register(CharacterDef def)
    {
        if (def == null || string.IsNullOrEmpty(def.characterName))
        {
            return;
        }

        byName[def.characterName] = def;
    }

    public static CharacterDef Get(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            return null;
        }

        byName.TryGetValue(characterName, out CharacterDef def);
        return def;
    }

    public static void Clear()
    {
        byName.Clear();
    }
}
