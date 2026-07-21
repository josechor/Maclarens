using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Genera por código la escena del menú principal (MainMenu.unity) y la deja como primera escena
// en Build Settings, con TestRoom detrás. Mismo enfoque que TestSceneBuilder: todo generado, para
// poder reconstruirlo sin depender de clics en el Editor.
public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/TestRoom.unity";
    private const string GameSceneName = "TestRoom";

    [MenuItem("McClarens/Build Main Menu Scene")]
    public static void Build()
    {
        try
        {
            BuildInternal();
            RegisterScenesInBuildSettings();
            Debug.Log("MainMenuBuilder: menú construido en " + ScenePath +
                      " y registrado como escena inicial en Build Settings.");
        }
        catch (Exception e)
        {
            Debug.LogError("MainMenuBuilder failed: " + e);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            throw;
        }
    }

    private static void BuildInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGO = new GameObject("Main Camera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.09f, 0.12f);
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        cameraGO.tag = "MainCamera";
        cameraGO.AddComponent<AudioListener>();

        // Canvas
        var canvasGO = new GameObject("MenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem con el nuevo Input System (necesario para que respondan los clics).
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            var uiModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Título / línea de estado.
        Text statusText = CreateLabel(canvasGO.transform, font, "McClarens", 64, new Vector2(0f, 260f), new Vector2(1000f, 140f));
        statusText.fontStyle = FontStyle.Bold;

        // Botones, apilados verticalmente. El controller decide cuáles se ven en cada momento.
        Button playButton = CreateButton(canvasGO.transform, font, "Jugar", new Vector2(0f, 90f));
        Button continueButton = CreateButton(canvasGO.transform, font, "Continuar", new Vector2(0f, 90f));
        Button newGameButton = CreateButton(canvasGO.transform, font, "Nueva partida", new Vector2(0f, 90f));
        Button deleteButton = CreateButton(canvasGO.transform, font, "Borrar partida", new Vector2(0f, -20f));
        Button backButton = CreateButton(canvasGO.transform, font, "Volver", new Vector2(0f, -130f));
        Button quitButton = CreateButton(canvasGO.transform, font, "Salir", new Vector2(0f, -20f));

        var controller = canvasGO.AddComponent<MainMenuController>();
        controller.Configure(playButton, continueButton, newGameButton, deleteButton, backButton, quitButton, statusText, GameSceneName);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // Deja MainMenu como escena 0 (la que arranca el juego) y TestRoom detrás, si existe.
    private static void RegisterScenesInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        if (File.Exists(GameScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
        }
        else
        {
            Debug.LogWarning($"MainMenuBuilder: {GameScenePath} no existe todavía. Genera la escena de juego " +
                             "con McClarens → Build Test Room Scene o el botón Jugar no podrá cargarla.");
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------- Helpers de UI (uGUI legacy, sin TMP) ----------

    private static Text CreateLabel(Transform parent, Font font, string text, int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var label = go.AddComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = text;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        return label;
    }

    private static Button CreateButton(Transform parent, Font font, string caption, Vector2 anchoredPos)
    {
        var go = new GameObject($"Button_{caption}");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(420f, 90f);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.26f, 0.95f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGO.AddComponent<Text>();
        text.font = font;
        text.fontSize = 34;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = caption;

        return button;
    }
}
