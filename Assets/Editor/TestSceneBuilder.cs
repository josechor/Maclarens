using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TestRoom.unity";
    private const string ArtDir = "Assets/Art/Generated";

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
        cam.orthographicSize = 6.5f;
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        cameraGO.tag = "MainCamera";
        cameraGO.AddComponent<AudioListener>();

        CreateFloor(squareSprite);
        CreateWalls(squareSprite);
        CreateTables(squareSprite);
        CreatePlayer(circleSprite);

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
        go.transform.localScale = new Vector3(17f, 11f, 1f);
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

    private static void CreateTable(Transform parent, Sprite sprite, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.55f, 0.35f, 0.2f);

        go.AddComponent<BoxCollider2D>();
    }

    private static void CreatePlayer(Sprite sprite)
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

        go.AddComponent<TopDownController>();
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
