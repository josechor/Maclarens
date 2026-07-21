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
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Controles de diálogo.
        AssetDatabase.ImportAsset(ControlsPath, ImportAssetOptions.ForceSynchronousImport);
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
        if (controls == null)
        {
            Debug.LogWarning($"TestSceneBuilder: no se pudo cargar {ControlsPath}. El diálogo no responderá a input.");
        }

        // 3. Conversaciones de ejemplo (.mcc).
        ConversationAsset intro = WriteMccIfMissing("camarero_intro", CamareroIntroMcc());
        ConversationAsset context = WriteMccIfMissing("camarero_abraham", CamareroAbrahamMcc());
        ConversationAsset idle = WriteMccIfMissing("camarero_idle", CamareroIdleMcc());

        // 4. Escena.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
        CreateCamarero(circleSprite, new[] { intro, context, idle });
        Transform playerTransform = CreatePlayer(circleSprite);

        var follow = cameraGO.AddComponent<CameraFollow2D>();
        follow.Configure(
            playerTransform,
            new Vector2(-RoomHalfWidth, -RoomHalfHeight),
            new Vector2(RoomHalfWidth, RoomHalfHeight));

        CreateInteractionUI(controls, new[] { camareroDef, protaDef });

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
        return AssetDatabase.LoadAssetAtPath<ConversationAsset>(path);
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
  - No:
      Prota [normal]: No, nada, gracias.
      Camarero [normal]: Pues aquí sigo.
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
        ConfigureInteractable(interactable, "Esto es una mesa.");

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
        ConfigureInteractable(interactable, "Esto es una botella.");
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

    private static void CreateCamarero(Sprite sprite, ConversationAsset[] conversations)
    {
        var go = new GameObject("Camarero");
        go.transform.position = new Vector3(-6f, -1f, 0f);
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.8f, 0.3f, 0.35f);
        sr.sortingOrder = 1;

        go.AddComponent<CircleCollider2D>();

        var npc = go.AddComponent<NpcInteractable>();

        var so = new SerializedObject(npc);
        so.FindProperty("interactionPrompt").stringValue = "Pulsa E para hablar";

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
