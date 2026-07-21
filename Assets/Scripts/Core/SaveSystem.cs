using System;
using System.IO;
using UnityEngine;

// Guardado en disco de la partida. Una sola ranura: un único archivo JSON en la carpeta de datos
// persistentes del sistema (Application.persistentDataPath, distinta en cada plataforma/usuario).
//
// El estado vive en clases static (GameFlags, ConversationHistory) durante el juego; este sistema
// solo lo vuelca a disco (Save) y lo restaura (Load). El autoguardado se dispara desde el juego
// (al terminar una conversación y tras interactuar con algo que cambia el estado).
public static class SaveSystem
{
    private const string FileName = "mcclarens_save.json";

    // Flag global que marca la partida como terminada (se pone al salir por la puerta con la llave).
    public const string CompletedFlag = "game_completed";

    // Se calcula al vuelo: Application.persistentDataPath no es válido fuera de Play.
    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    // La posición del jugador es de escena, no un static global como los flags. Para no acoplar el
    // guardado a la escena, quien conozca al jugador (PlayerPersistence) registra aquí un proveedor
    // que devuelve su posición actual; Save() lo consulta si existe.
    public static System.Func<Vector2> PlayerPositionProvider;

    // Posición leída del último Load(), a la espera de que la escena de juego la aplique al jugador.
    public static bool HasPendingPosition { get; private set; }
    private static Vector2 pendingPosition;

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    // Lee el archivo solo para saber si esa partida ya está terminada, SIN aplicarla al runtime.
    // Lo usa el menú para no ofrecer "Continuar" en una partida completada.
    public static bool SavedGameIsCompleted()
    {
        if (!HasSave())
        {
            return false;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            return data?.flags != null && data.flags.Contains(CompletedFlag);
        }
        catch
        {
            return false;
        }
    }

    // La escena de juego llama a esto al arrancar: si Load() dejó una posición pendiente, la
    // devuelve (y la consume) para colocar al jugador donde estaba.
    public static bool TryConsumePendingPosition(out Vector2 position)
    {
        position = pendingPosition;
        bool had = HasPendingPosition;
        HasPendingPosition = false;
        return had;
    }

    // Vuelca el estado actual (flags, contadores, historial) a disco, sobrescribiendo el archivo.
    public static void Save()
    {
        try
        {
            var data = new SaveData();
            GameFlags.WriteTo(data);
            ConversationHistory.WriteTo(data);

            if (PlayerPositionProvider != null)
            {
                Vector2 pos = PlayerPositionProvider();
                data.hasPlayerPosition = true;
                data.playerX = pos.x;
                data.playerY = pos.y;
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: no se pudo guardar en '{SavePath}': {e.Message}");
        }
    }

    // Carga el archivo y restaura el estado en runtime. Devuelve false si no hay archivo o está corrupto.
    public static bool Load()
    {
        if (!HasSave())
        {
            return false;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                Debug.LogError("SaveSystem: el archivo de guardado está vacío o corrupto.");
                return false;
            }

            GameFlags.ReadFrom(data);
            ConversationHistory.ReadFrom(data);

            HasPendingPosition = data.hasPlayerPosition;
            if (data.hasPlayerPosition)
            {
                pendingPosition = new Vector2(data.playerX, data.playerY);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: no se pudo cargar '{SavePath}': {e.Message}");
            return false;
        }
    }

    // Empieza una partida nueva: limpia el estado en memoria y crea el archivo inicial (vacío).
    public static void NewGame()
    {
        GameFlags.ResetAll();
        ConversationHistory.ResetAll();
        HasPendingPosition = false; // el jugador arranca en su posición por defecto de la escena
        Save();
    }

    // Borra la partida guardada del disco (el estado en memoria no se toca).
    public static void Delete()
    {
        try
        {
            if (HasSave())
            {
                File.Delete(SavePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: no se pudo borrar '{SavePath}': {e.Message}");
        }
    }
}
