using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class TestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TestRoom.unity";
    private const string ArtDir = "Assets/Art/Generated";
    private const string DialogueDir = "Assets/Dialogue";
    private const string CharactersDir = "Assets/Dialogue/Characters";
    private const string ControlsPath = "Assets/Input/DialogueControls.inputactions";
    private const string FlagTalkedToAbraham = "TalkedToAbraham";

    // Mitad del ancho/alto del escenario (suelo). La cámara se ajusta para no mostrar nunca
    // fuera de estos límites en aspectos habituales (4:3 a 16:9).
    private const float RoomHalfWidth = 8.5f;
    private const float RoomHalfHeight = 5.5f;

    [MenuItem("McClarens/Build Test Room Scene")]
    public static void Build()
    {
        try
        {
            BuildInternal();
            Debug.Log("TestSceneBuilder: scene built successfully at " + ScenePath);
        }
        catch (Exception e)
        {
            Debug.LogError("TestSceneBuilder failed: " + e);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            throw;
        }
    }

    private static void BuildInternal()
    {
        Sprite circleSprite = GetOrCreateSprite("PlayerCircle", 64, true);
        Sprite squareSprite = GetOrCreateSprite("WhiteSquare", 32, false);

        // 1. Personajes (deben existir ANTES de importar los .mcc, para que el importador valide).
        CharacterDef camareroDef = GetOrCreateCharacter("Camarero", PortraitSide.Left,
            new[] { "normal", "feliz", "enfadado" }, circleSprite);
        CharacterDef protaDef = GetOrCreateCharacter("Prota", PortraitSide.Right,
            new[] { "normal" }, circleSprite);
        CharacterDef maestro1Def = GetOrCreateCharacter("Maestro1", PortraitSide.Left,
            new[] { "normal", "enfadado" }, circleSprite);
        CharacterDef maestro2Def = GetOrCreateCharacter("Maestro2", PortraitSide.Left,
            new[] { "normal", "enfadado" }, circleSprite);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Controles de diálogo.
        AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
        if (controls == null)
        {
            Debug.LogWarning($"TestSceneBuilder: no se pudo cargar {ControlsPath}. El diálogo no responderá a input.");
        }

        // 3. Escena. Se crea ANTES de cargar las conversaciones porque
        // EditorSceneManager.NewScene descarga los assets recién importados/creados
        // (no están "asentados" en el AssetDatabase todavía) e invalida las referencias.
        // Esto afecta igual a los ConversationAsset que a los CharacterDef, así que ambos
        // se recargan desde disco DESPUÉS de este punto (ver más abajo).
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Recarga de los CharacterDef creados en el paso 1: las referencias de antes de
        // NewScene quedan invalidadas y se serializarían como null en el CharacterRegistryLoader.
        camareroDef = AssetDatabase.LoadAssetAtPath<CharacterDef>($"{CharactersDir}/Camarero.asset");
        protaDef = AssetDatabase.LoadAssetAtPath<CharacterDef>($"{CharactersDir}/Prota.asset");
        maestro1Def = AssetDatabase.LoadAssetAtPath<CharacterDef>($"{CharactersDir}/Maestro1.asset");
        maestro2Def = AssetDatabase.LoadAssetAtPath<CharacterDef>($"{CharactersDir}/Maestro2.asset");

        // 4. Conversaciones de ejemplo (.mcc).
        ConversationAsset intro = WriteMccIfMissing("camarero_intro", CamareroIntroMcc());
        ConversationAsset password = WriteMccIfMissing("camarero_password", CamareroPasswordMcc());
        ConversationAsset continua = WriteMccIfMissing("camarero_continua", CamareroContinuaMcc());
        ConversationAsset contextMesa = WriteMccIfMissing("camarero_context_mesa", CamareroContextMesaMcc());
        ConversationAsset contextBotella = WriteMccIfMissing("camarero_context_botella", CamareroContextBotellaMcc());
        ConversationAsset context = WriteMccIfMissing("camarero_abraham", CamareroAbrahamMcc());
        ConversationAsset recuerda = WriteMccIfMissing("camarero_recuerda", CamareroRecuerdaMcc());
        ConversationAsset sinCopas = WriteMccIfMissing("camarero_sin_copas", CamareroSinCopasMcc());
        ConversationAsset idle = WriteMccIfMissing("camarero_idle", CamareroIdleMcc());

        ConversationAsset maestro1Ok = WriteMccIfMissing("maestro1_ok", Maestro1OkMcc());
        ConversationAsset maestro1Sp3 = WriteMccIfMissing("maestro1_sin_password_3", MaestroSinPassword3Mcc("Maestro1", "intentos_maestro1"));
        ConversationAsset maestro1Sp6 = WriteMccIfMissing("maestro1_sin_password_6", MaestroSinPassword6Mcc("Maestro1", "intentos_maestro1"));
        ConversationAsset maestro1SinPassword = WriteMccIfMissing("maestro1_sin_password", MaestroSinPasswordNormalMcc("Maestro1", "intentos_maestro1"));
        ConversationAsset maestro1IdlePost = WriteMccIfMissing("maestro1_idle_post", MaestroIdlePostMcc("Maestro1"));

        ConversationAsset maestro2Ok = WriteMccIfMissing("maestro2_ok", Maestro2OkMcc());
        ConversationAsset maestro2Sp3 = WriteMccIfMissing("maestro2_sin_password_3", MaestroSinPassword3Mcc("Maestro2", "intentos_maestro2"));
        ConversationAsset maestro2Sp6 = WriteMccIfMissing("maestro2_sin_password_6", MaestroSinPassword6Mcc("Maestro2", "intentos_maestro2"));
        ConversationAsset maestro2SinPassword = WriteMccIfMissing("maestro2_sin_password", MaestroSinPasswordNormalMcc("Maestro2", "intentos_maestro2"));
        ConversationAsset maestro2IdlePost = WriteMccIfMissing("maestro2_idle_post", MaestroIdlePostMcc("Maestro2"));

        var cameraGO = new GameObject("Main Camera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        cameraGO.tag = "MainCamera";
        cameraGO.AddComponent<AudioListener>();

        CreateFloor(squareSprite);
        CreateWalls(squareSprite);
        CreateTables(squareSprite);
        CreateBottle(circleSprite);
        CreateAbraham(circleSprite);
        CreateExitDoor(squareSprite);
        CreateNpc(circleSprite, "Camarero", new Vector3(-6f, -1f, 0f), new Color(0.8f, 0.3f, 0.35f),
            "Pulsa E para hablar",
            new[] { intro, password, continua, contextMesa, contextBotella, context, recuerda, sinCopas, idle });
        CreateNpc(circleSprite, "Maestro1", new Vector3(-6f, 3f, 0f), new Color(0.5f, 0.3f, 0.7f),
            "Pulsa E para hablar",
            new[] { maestro1Ok, maestro1Sp3, maestro1Sp6, maestro1SinPassword, maestro1IdlePost });
        CreateNpc(circleSprite, "Maestro2", new Vector3(6f, -3.5f, 0f), new Color(0.2f, 0.5f, 0.55f),
            "Pulsa E para hablar",
            new[] { maestro2Ok, maestro2Sp3, maestro2Sp6, maestro2SinPassword, maestro2IdlePost });
        Transform playerTransform = CreatePlayer(circleSprite);

        var follow = cameraGO.AddComponent<CameraFollow2D>();
        follow.Configure(
            playerTransform,
            new Vector2(-RoomHalfWidth, -RoomHalfHeight),
            new Vector2(RoomHalfWidth, RoomHalfHeight));

        CreateInteractionUI(controls, new[] { camareroDef, protaDef, maestro1Def, maestro2Def });

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ---------- Personajes ----------

    private static CharacterDef GetOrCreateCharacter(string name, PortraitSide side, string[] expressionIds, Sprite placeholder)
    {
        Directory.CreateDirectory(CharactersDir);
        string path = $"{CharactersDir}/{name}.asset";

        CharacterDef existing = AssetDatabase.LoadAssetAtPath<CharacterDef>(path);
        if (existing != null)
        {
            return existing; // respeta ediciones a mano
        }

        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.characterName = name;
        def.defaultSide = side;
        def.expressions = new List<CharacterDef.Expression>();
        foreach (string id in expressionIds)
        {
            def.expressions.Add(new CharacterDef.Expression { id = id, portrait = placeholder });
        }

        AssetDatabase.CreateAsset(def, path);
        AssetDatabase.SaveAssets();
        return def;
    }

    // ---------- Conversaciones ----------

    private static ConversationAsset WriteMccIfMissing(string name, string content)
    {
        Directory.CreateDirectory(DialogueDir);
        string path = $"{DialogueDir}/{name}.mcc";

        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var asset = AssetDatabase.LoadAssetAtPath<ConversationAsset>(path);

        // La primera vez que MccImporter procesa un .mcc en este dominio, el AssetDatabase a
        // veces no tiene el resultado listo aunque se haya pedido import síncrono. Un Refresh +
        // segundo intento lo resuelve sin depender de volver a pulsar el menú manualmente.
        if (asset == null)
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            asset = AssetDatabase.LoadAssetAtPath<ConversationAsset>(path);
        }

        if (asset == null)
        {
            Debug.LogError($"TestSceneBuilder: no se pudo cargar el ConversationAsset de {path} tras reintentar.");
        }

        return asset;
    }

    private static string CamareroIntroMcc()
    {
        return
@"@story   required: !intro_done

Camarero [normal]: Vaya, despertaste... por fin.
Camarero [feliz]: <color=#ffcc00>¡Estás encerrado, majo!</color>

? Prota:
  - Chulo:
      Prota [normal]: Tú tranquilo. He salido de sitios peores que este antro.
      Camarero [feliz]: Ja. Ese es el espíritu.
  - Nervioso:
      Prota [normal]: ¿C-cómo que encerrado? ¡Yo solo venía a por una caña!
      Camarero [normal]: Respira, hombre. Hay salida.
  - ¿Eres tonto?:
      Camarero [enfadado]: Oye, un respeto, que llevo aquí toda la noche.
      Prota [normal]: Vale, vale... perdón.

Camarero [normal]: Total. Habla con los Maestros y te abro la puerta.
set intro_done
";
    }

    private static string CamareroAbrahamMcc()
    {
        return
@"@context   required: TalkedToAbraham

Camarero [normal]: Veo que intentaste hablar con Abraham. Ese no te hace ni caso si no le llevas algo de comer.
";
    }

    private static string CamareroIdleMcc()
    {
        return
@"@idle

Camarero [normal]: Estoy limpiando vasos. ¿Necesitas algo?

? Prota:
  - Sí:
      Prota [normal]: Sí, ponme una caña.
      Camarero [feliz]: Marchando.
      add copas
  - No:
      Prota [normal]: No, nada, gracias.
      Camarero [normal]: Pues aquí sigo.
";
    }

    // Tope de copas: cuando el contador 'copas' llega a 2, esta conversación (colocada ANTES que
    // camarero_idle en la lista del NPC) gana el desempate y el Camarero deja de servir. No tiene
    // elección: solo la negativa. Idle repetible, así que responde lo mismo cada vez.
    private static string CamareroSinCopasMcc()
    {
        return
@"@idle   required: copas >= 2

Camarero [normal]: Ni hablar, que te conozco. No puedes tomar más.
";
    }

    // Ejemplo de encadenado de historia entre 3 personajes (Camarero + 2 de prueba) usando
    // flags: intro_done -> password_given -> maestro1_done + maestro2_done -> chapter2_done.
    private static string CamareroPasswordMcc()
    {
        return
@"@story   required: intro_done, !password_given

Camarero [normal]: Antes de que sigas, escucha bien. Para que esos dos te hagan caso, necesitas la contraseña.
Camarero [feliz]: Es ""vidrioo"". No lo olvides.
Camarero [normal]: Ve a hablar con Maestro1 y con Maestro2. Con los dos, ¿eh?

set password_given
";
    }

    // Historia solo disponible cuando AMBOS maestros ya te han hecho caso (AND de dos flags
    // distintas en el required de la cabecera).
    private static string CamareroContinuaMcc()
    {
        return
@"@story   required: maestro1_done, maestro2_done, !chapter2_done

Camarero [feliz]: Vaya, ya hablaste con los dos. Bien hecho.
Camarero [normal]: Toma, la llave de la puerta. Ya te puedes largar.
Camarero [normal]: La salida está al sur. Sal por ahí cuando quieras.

set chapter2_done
set has_key
";
    }

    // Idle "recordatorio": misma prioridad (Idle) que camarero_idle, pero listada ANTES en el
    // NpcInteractable del Camarero, así que gana el desempate mientras su condición se cumpla.
    private static string CamareroRecuerdaMcc()
    {
        return
@"@idle   required: password_given, !chapter2_done

Camarero [normal]: ¿Ya hablaste con Maestro1 y Maestro2? Habla con ellos, va.
";
    }

    // Dos Context distintos, ambos disponibles solo con su propia flag. Si el jugador
    // interactuó con mesa Y botella antes de hablar con el Camarero, se desempata por orden
    // de la lista (mesa va primero en CreateNpc); la que quede pendiente sale la vez siguiente.
    private static string CamareroContextMesaMcc()
    {
        return
@"@context   required: interacted_table

Camarero [normal]: Esas mesas las compré de saldo hace siglos. No preguntes.
";
    }

    private static string CamareroContextBotellaMcc()
    {
        return
@"@context   required: interacted_bottle

Camarero [normal]: Esa botella es más vieja que el propio local. No la toques mucho.
";
    }

    // Piden la contraseña mientras el jugador no la sepa (!password_given). Cada intento fallido
    // suma 1 al contador propio del personaje, y al 3º (contador == 2) y 6º (== 5) intento salta un
    // mensaje especial. Las tres conversaciones son @idle: el desempate lo decide el ORDEN en la
    // lista del NPC — las condicionadas por contador van ANTES que la normal (ver CreateNpc), si no
    // la normal (sin condición de contador) las taparía siempre.
    private static string MaestroSinPasswordNormalMcc(string characterName, string counter)
    {
        return
$@"@idle   required: !password_given

{characterName} [normal]: ¿Contraseña?
{characterName} [enfadado]: Si no la sabes, no hay nada que hablar.
add {counter}
";
    }

    // 3ª vez: el contador vale 2 (dos intentos previos ya lo subieron a 2).
    private static string MaestroSinPassword3Mcc(string characterName, string counter)
    {
        return
$@"@idle   required: !password_given, {counter} == 2

{characterName} [enfadado]: ¿Otra vez? Van tres. Que NO, pesado.
add {counter}
";
    }

    // 6ª vez: el contador vale 5.
    private static string MaestroSinPassword6Mcc(string characterName, string counter)
    {
        return
$@"@idle   required: !password_given, {counter} == 5

{characterName} [enfadado]: Seis veces ya. Admiro tu insistencia, pero sigue siendo que no.
add {counter}
";
    }

    // Idle de relleno sin condición: solo gana el desempate una vez maestroX_done es true
    // (la conversación de arriba deja de estar disponible al no cumplirse ya su required).
    private static string MaestroIdlePostMcc(string characterName)
    {
        return
$@"@idle

{characterName} [normal]: Ya hablamos. Ve con el Camarero.
";
    }

    private static string Maestro1OkMcc()
    {
        return
@"@story   required: password_given, !maestro1_done

Maestro1 [normal]: ""Vidrioo""... vale, la sabes. Adelante.

set maestro1_done
";
    }

    private static string Maestro2OkMcc()
    {
        return
@"@story   required: password_given, !maestro2_done

Maestro2 [normal]: ""Vidrioo""... vale, la sabes. Adelante.

set maestro2_done
";
    }

    // ---------- Escenario ----------

    private static void CreateFloor(Sprite sprite)
    {
        var go = new GameObject("Floor");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.15f, 0.15f, 0.15f);
        sr.sortingOrder = -10;
        go.transform.localScale = new Vector3(RoomHalfWidth * 2f, RoomHalfHeight * 2f, 1f);
    }

    private static void CreateWalls(Sprite sprite)
    {
        var parent = new GameObject("Walls").transform;

        CreateWall(parent, sprite, "Wall_North", new Vector2(0f, 5.25f), new Vector2(17f, 0.5f));
        CreateWall(parent, sprite, "Wall_South", new Vector2(0f, -5.25f), new Vector2(17f, 0.5f));
        CreateWall(parent, sprite, "Wall_East", new Vector2(8.25f, 0f), new Vector2(0.5f, 10.5f));
        CreateWall(parent, sprite, "Wall_West", new Vector2(-8.25f, 0f), new Vector2(0.5f, 10.5f));
    }

    private static void CreateWall(Transform parent, Sprite sprite, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.4f, 0.4f, 0.42f);

        go.AddComponent<BoxCollider2D>();
    }

    private static void CreateTables(Sprite sprite)
    {
        var parent = new GameObject("Tables").transform;

        CreateTable(parent, sprite, "Table_1", new Vector2(-3f, 1.5f), new Vector2(2f, 1f));
        CreateTable(parent, sprite, "Table_2", new Vector2(3f, -1.5f), new Vector2(1.5f, 1.5f));
        CreateTable(parent, sprite, "Table_3", new Vector2(0f, 3.5f), new Vector2(1f, 1.5f));
    }

    private static GameObject CreateTable(Transform parent, Sprite sprite, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.55f, 0.35f, 0.2f);

        go.AddComponent<BoxCollider2D>();

        var interactable = go.AddComponent<SimpleInteractable>();
        ConfigureInteractable(interactable, "Esto es una mesa.", null, new List<string> { "interacted_table" });

        return go;
    }

    private static void CreateBottle(Sprite sprite)
    {
        var go = new GameObject("Bottle");
        go.transform.position = new Vector3(-5f, -3f, 0f);
        go.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.15f, 0.55f, 0.25f);
        sr.sortingOrder = 1;

        go.AddComponent<CircleCollider2D>();

        var interactable = go.AddComponent<SimpleInteractable>();
        ConfigureInteractable(interactable, "Esto es una botella.", null, new List<string> { "interacted_bottle" });
    }

    private static void CreateAbraham(Sprite sprite)
    {
        var go = new GameObject("Abraham");
        go.transform.position = new Vector3(5f, 2f, 0f);
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.9f, 0.65f, 0.15f);
        sr.sortingOrder = 1;

        go.AddComponent<CircleCollider2D>();

        var interactable = go.AddComponent<SimpleInteractable>();
        ConfigureInteractable(
            interactable,
            "Abraham sigue bailando sin parar. Ni te mira. Ni te oye.",
            "Pulsa E para hablar",
            new List<string> { FlagTalkedToAbraham });
    }

    // Puerta de salida, encajada en la pared sur. Cerrada hasta que el Camarero da la llave (has_key);
    // al salir con la llave, ExitDoorInteractable termina el juego y vuelve al menú.
    private static void CreateExitDoor(Sprite sprite)
    {
        var go = new GameObject("ExitDoor");
        go.transform.position = new Vector3(0f, -4.9f, 0f);
        go.transform.localScale = new Vector3(1.6f, 1.2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.45f, 0.28f, 0.12f);
        sr.sortingOrder = 2;

        go.AddComponent<BoxCollider2D>();
        go.AddComponent<ExitDoorInteractable>();
    }

    private static void CreateNpc(Sprite sprite, string name, Vector3 position, Color color, string prompt, ConversationAsset[] conversations)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 1;

        go.AddComponent<CircleCollider2D>();

        var npc = go.AddComponent<NpcInteractable>();

        var so = new SerializedObject(npc);
        so.FindProperty("interactionPrompt").stringValue = prompt;

        var listProp = so.FindProperty("conversations");
        listProp.arraySize = conversations.Length;
        for (int i = 0; i < conversations.Length; i++)
        {
            listProp.GetArrayElementAtIndex(i).objectReferenceValue = conversations[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureInteractable(SimpleInteractable interactable, string message, string prompt = null, List<string> setFlags = null)
    {
        var so = new SerializedObject(interactable);
        so.FindProperty("message").stringValue = message;

        if (!string.IsNullOrEmpty(prompt))
        {
            so.FindProperty("interactionPrompt").stringValue = prompt;
        }

        if (setFlags != null && setFlags.Count > 0)
        {
            SetStringListProperty(so.FindProperty("setFlagsOnInteract"), setFlags);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStringListProperty(SerializedProperty listProp, List<string> values)
    {
        listProp.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            listProp.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }

    private static Transform CreatePlayer(Sprite sprite)
    {
        var go = new GameObject("Player");
        go.transform.position = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.25f, 0.85f, 0.9f);
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var indicatorGO = new GameObject("FacingIndicator");
        indicatorGO.transform.SetParent(go.transform, false);
        indicatorGO.transform.localScale = new Vector3(0.18f, 0.18f, 1f);

        var indicatorSr = indicatorGO.AddComponent<SpriteRenderer>();
        indicatorSr.sprite = sprite;
        indicatorSr.color = new Color(0.05f, 0.1f, 0.15f);
        indicatorSr.sortingOrder = 11;

        var topDown = go.AddComponent<TopDownController>();
        topDown.SetFacingIndicator(indicatorGO.transform, 0.42f);

        go.AddComponent<Interactor>();
        go.AddComponent<PlayerPersistence>(); // guarda/restaura la posición del jugador

        return go.transform;
    }

    // ---------- UI ----------

    private static void CreateInteractionUI(InputActionAsset controls, CharacterDef[] characters)
    {
        var canvasGO = new GameObject("UI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            var uiModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Prompt y mensajes simples siguen en uGUI legacy (no forman parte de esta tanda).
        GameObject promptPanel = CreateUiPanel(canvasGO.transform, "PromptPanel", new Vector2(0f, 90f), new Vector2(560f, 60f));
        Text promptText = CreateUiText(promptPanel.transform, font, 26);

        GameObject messagePanel = CreateUiPanel(canvasGO.transform, "MessagePanel", new Vector2(0f, 130f), new Vector2(1000f, 150f));
        Text messageText = CreateUiText(messagePanel.transform, font, 30);

        var ui = canvasGO.AddComponent<InteractionUI>();
        ui.Configure(promptPanel, promptText, messagePanel, messageText);

        CreateDialogueUi(canvasGO.transform, controls, characters);
    }

    private static void CreateDialogueUi(Transform canvasParent, InputActionAsset controls, CharacterDef[] characters)
    {
        GameObject dialoguePanel = CreateUiPanel(canvasParent, "DialoguePanel", new Vector2(0f, 60f), new Vector2(1300f, 320f));

        // Retratos, encima de la caja: NPC a la izquierda, jugador a la derecha.
        Image leftPortrait = CreatePortrait(canvasParent, "LeftPortrait", -560f);
        Image rightPortrait = CreatePortrait(canvasParent, "RightPortrait", 560f);

        TMP_Text speakerText = CreateTmpText(dialoguePanel.transform, "SpeakerText", 30, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-40f, 40f));
        speakerText.fontStyle = FontStyles.Bold;
        speakerText.color = new Color(1f, 0.85f, 0.4f);

        TMP_Text lineText = CreateTmpText(dialoguePanel.transform, "LineText", 28, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(-40f, 150f));

        TMP_Text continueText = CreateTmpText(dialoguePanel.transform, "ContinueHint", 22, TextAlignmentOptions.BottomRight,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-40f, 30f));
        continueText.color = new Color(0.8f, 0.8f, 0.8f);
        continueText.text = "Pulsa E o haz clic para continuar";

        var continueButton = continueText.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = continueText;

        var choicesContainer = new GameObject("ChoicesContainer");
        choicesContainer.transform.SetParent(dialoguePanel.transform, false);
        var choicesRect = choicesContainer.AddComponent<RectTransform>();
        choicesRect.anchorMin = new Vector2(0f, 0f);
        choicesRect.anchorMax = new Vector2(1f, 0f);
        choicesRect.pivot = new Vector2(0.5f, 0f);
        choicesRect.anchoredPosition = new Vector2(0f, 10f);
        choicesRect.sizeDelta = new Vector2(-40f, 150f);

        var layout = choicesContainer.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.spacing = 4f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var choiceTexts = new TMP_Text[4];
        var choiceButtons = new Button[4];
        for (int i = 0; i < choiceTexts.Length; i++)
        {
            var choiceGO = new GameObject($"Choice_{i}");
            choiceGO.transform.SetParent(choicesContainer.transform, false);

            var layoutElement = choiceGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 34f;

            var choiceText = choiceGO.AddComponent<TextMeshProUGUI>();
            choiceText.fontSize = 26;
            choiceText.alignment = TextAlignmentOptions.Left;
            choiceText.color = new Color(0.6f, 0.9f, 1f);

            var choiceButton = choiceGO.AddComponent<Button>();
            choiceButton.targetGraphic = choiceText;

            choiceTexts[i] = choiceText;
            choiceButtons[i] = choiceButton;
        }

        var dialogueUI = canvasParent.gameObject.AddComponent<DialogueUI>();
        dialogueUI.Configure(dialoguePanel, speakerText, lineText, continueText, choicesContainer, choiceTexts, leftPortrait, rightPortrait);

        var runnerGO = new GameObject("DialogueRunner");
        var runner = runnerGO.AddComponent<DialogueRunner>();
        runner.ConfigureButtons(choiceButtons, continueButton);
        runner.ConfigureControls(controls);

        var registryLoader = runnerGO.AddComponent<CharacterRegistryLoader>();
        var loaderSo = new SerializedObject(registryLoader);
        var charsProp = loaderSo.FindProperty("characters");
        charsProp.arraySize = characters.Length;
        for (int i = 0; i < characters.Length; i++)
        {
            charsProp.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
        }
        loaderSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image CreatePortrait(Transform canvasParent, string name, float x)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvasParent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, 395f);
        rect.sizeDelta = new Vector2(170f, 170f);

        var image = go.AddComponent<Image>();
        image.preserveAspect = true;
        image.enabled = false;

        return image;
    }

    private static TMP_Text CreateTmpText(
        Transform parent, string name, float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;

        return text;
    }

    private static GameObject CreateUiPanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        return go;
    }

    private static Text CreateUiText(Transform parent, Font font, int fontSize)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20f, 10f);
        rect.offsetMax = new Vector2(-20f, -10f);

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private static Sprite GetOrCreateSprite(string name, int size, bool circle)
    {
        string pngPath = $"{ArtDir}/{name}.png";

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(ArtDir);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float radius = size / 2f;
        var center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool filled = !circle || Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius;
                pixels[y * size + x] = filled ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }
}
