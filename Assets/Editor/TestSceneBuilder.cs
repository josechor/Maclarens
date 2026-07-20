using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class TestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TestRoom.unity";
    private const string ArtDir = "Assets/Art/Generated";
    private const string DialogueDir = "Assets/Dialogue";

    // Mitad del ancho/alto del escenario (suelo). La cámara se ajusta para
    // no mostrar nunca fuera de estos límites en aspectos habituales (4:3 a 16:9).
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
        CreateNpc(circleSprite);
        Transform playerTransform = CreatePlayer(circleSprite);

        var follow = cameraGO.AddComponent<CameraFollow2D>();
        follow.Configure(
            playerTransform,
            new Vector2(-RoomHalfWidth, -RoomHalfHeight),
            new Vector2(RoomHalfWidth, RoomHalfHeight));

        CreateInteractionUI();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

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

    private static void CreateNpc(Sprite sprite)
    {
        var go = new GameObject("NPC_Placeholder");
        go.transform.position = new Vector3(5f, 2f, 0f);
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.9f, 0.65f, 0.15f);
        sr.sortingOrder = 1;

        go.AddComponent<CircleCollider2D>();

        var npc = go.AddComponent<NpcInteractable>();
        DialogueData dialogue = GetOrCreateSampleDialogue();

        var so = new SerializedObject(npc);
        so.FindProperty("interactionPrompt").stringValue = "Pulsa E para hablar";
        so.FindProperty("dialogue").objectReferenceValue = dialogue;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static DialogueData GetOrCreateSampleDialogue()
    {
        string path = $"{DialogueDir}/Antonio_Intro.asset";
        Directory.CreateDirectory(DialogueDir);

        DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        bool isNew = data == null;

        if (isNew)
        {
            data = ScriptableObject.CreateInstance<DialogueData>();
        }

        data.nodes = BuildSampleDialogueNodes();

        if (isNew)
        {
            AssetDatabase.CreateAsset(data, path);
        }
        else
        {
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();

        return data;
    }

    private static List<DialogueNode> BuildSampleDialogueNodes()
    {
        return new List<DialogueNode>
        {
            // 0: pregunta con 3 actitudes posibles
            new DialogueNode
            {
                speakerName = "Antonio",
                line = "Oye... ¿tú también has oído hablar del vidrio?",
                choices = new List<DialogueChoice>
                {
                    new DialogueChoice { moodLabel = "Ignorarlo", resultingLine = "(Silencio)", nextNodeIndex = 1 },
                    new DialogueChoice { moodLabel = "Responder borde", resultingLine = "¿A ti qué más te da?", nextNodeIndex = 2 },
                    new DialogueChoice { moodLabel = "Furioso", resultingLine = "¡QUÉ VIDRIO NI QUÉ NARICES!", nextNodeIndex = 3 }
                }
            },
            // 1: reacción a "Ignorarlo"
            new DialogueNode
            {
                speakerName = "Antonio",
                line = "...vale, tampoco hacía falta contestar. Como quieras.",
                nextNodeIndex = 4
            },
            // 2: reacción a "Responder borde"
            new DialogueNode
            {
                speakerName = "Antonio",
                line = "Uy, con genio. Me caes bien.",
                nextNodeIndex = 4
            },
            // 3: reacción a "Furioso"
            new DialogueNode
            {
                speakerName = "Antonio",
                line = "¡Eh, tranquilo! Era solo una pregunta.",
                nextNodeIndex = 4
            },
            // 4: converge, sigue normal
            new DialogueNode
            {
                speakerName = "Antonio",
                line = "Bueno, ¿qué se te ha perdido por el McClarens?",
                nextNodeIndex = 5
            },
            // 5: final
            new DialogueNode
            {
                speakerName = "Antonio",
                line = "Ya me contarás con calma. Suerte ahí fuera.",
                nextNodeIndex = -1
            }
        };
    }

    private static void ConfigureInteractable(SimpleInteractable interactable, string message, string prompt = null)
    {
        var so = new SerializedObject(interactable);
        so.FindProperty("message").stringValue = message;

        if (!string.IsNullOrEmpty(prompt))
        {
            so.FindProperty("interactionPrompt").stringValue = prompt;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
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

    private static void CreateInteractionUI()
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
            // Sin esto, el módulo puede quedarse sin acciones de puntero/clic
            // asignadas y el ratón no interactúa con ningún elemento de UI.
            uiModule.AssignDefaultActions();
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject promptPanel = CreateUiPanel(canvasGO.transform, "PromptPanel", new Vector2(0f, 90f), new Vector2(560f, 60f));
        Text promptText = CreateUiText(promptPanel.transform, font, 26);

        GameObject messagePanel = CreateUiPanel(canvasGO.transform, "MessagePanel", new Vector2(0f, 130f), new Vector2(1000f, 150f));
        Text messageText = CreateUiText(messagePanel.transform, font, 30);

        var ui = canvasGO.AddComponent<InteractionUI>();
        ui.Configure(promptPanel, promptText, messagePanel, messageText);

        CreateDialogueUi(canvasGO.transform, font);
    }

    private static void CreateDialogueUi(Transform canvasParent, Font font)
    {
        GameObject dialoguePanel = CreateUiPanel(canvasParent, "DialoguePanel", new Vector2(0f, 60f), new Vector2(1300f, 320f));

        Text speakerText = CreateAnchoredText(dialoguePanel.transform, "SpeakerText", font, 26, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-40f, 40f));
        speakerText.fontStyle = FontStyle.Bold;
        speakerText.color = new Color(1f, 0.85f, 0.4f);

        Text lineText = CreateAnchoredText(dialoguePanel.transform, "LineText", font, 26, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(-40f, 90f));
        lineText.horizontalOverflow = HorizontalWrapMode.Wrap;

        Text continueText = CreateAnchoredText(dialoguePanel.transform, "ContinueHint", font, 20, TextAnchor.LowerRight,
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

        var choiceTexts = new Text[4];
        var choiceButtons = new Button[4];
        for (int i = 0; i < choiceTexts.Length; i++)
        {
            var choiceGO = new GameObject($"Choice_{i}");
            choiceGO.transform.SetParent(choicesContainer.transform, false);

            var layoutElement = choiceGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 32f;

            var choiceText = choiceGO.AddComponent<Text>();
            choiceText.font = font;
            choiceText.fontSize = 24;
            choiceText.alignment = TextAnchor.MiddleLeft;
            choiceText.color = new Color(0.6f, 0.9f, 1f);

            var choiceButton = choiceGO.AddComponent<Button>();
            choiceButton.targetGraphic = choiceText;

            choiceTexts[i] = choiceText;
            choiceButtons[i] = choiceButton;
        }

        var dialogueUI = canvasParent.gameObject.AddComponent<DialogueUI>();
        dialogueUI.Configure(dialoguePanel, speakerText, lineText, continueText, choicesContainer, choiceTexts);

        var runnerGO = new GameObject("DialogueRunner");
        var runner = runnerGO.AddComponent<DialogueRunner>();
        runner.ConfigureButtons(choiceButtons, continueButton);
    }

    private static Text CreateAnchoredText(
        Transform parent, string name, Font font, int fontSize, TextAnchor alignment,
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

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.verticalOverflow = VerticalWrapMode.Overflow;

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
