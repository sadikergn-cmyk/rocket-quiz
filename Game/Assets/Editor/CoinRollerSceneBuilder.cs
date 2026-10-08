using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the CoinRoller scene from code so it can be regenerated at any time.
public static class CoinRollerSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/CoinRoller.unity";
    private const string MaterialFolder = "Assets/Materials";

    [MenuItem("Tools/Build CoinRoller Scene")]
    public static string Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material groundMat = GetMaterial("Ground", new Color(0.30f, 0.55f, 0.35f));
        Material platformMat = GetMaterial("Platform", new Color(0.45f, 0.50f, 0.65f));
        Material playerMat = GetMaterial("Player", new Color(0.90f, 0.25f, 0.20f));
        Material coinMat = GetMaterial("Coin", new Color(1.00f, 0.80f, 0.10f));

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        CreateBox("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(24f, 1f, 24f), groundMat);
        CreateBox("Platform A", new Vector3(-7f, 0.5f, 6f), new Vector3(5f, 1f, 5f), platformMat);
        CreateBox("Platform B", new Vector3(7f, 0.75f, 6f), new Vector3(5f, 1.5f, 5f), platformMat);
        GameObject ramp = CreateBox("Ramp", new Vector3(7f, 0.2f, 1.6f), new Vector3(3f, 0.4f, 5f), platformMat);
        ramp.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 0.5f, -6f);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        var body = player.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.linearDamping = 1.5f;
        body.angularDamping = 0.5f;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        player.AddComponent<PlayerController>();

        var coinsRoot = new GameObject("Coins");
        Vector3[] coinPositions =
        {
            new Vector3(0f, 0.8f, -2f),
            new Vector3(-4f, 0.8f, -4f),
            new Vector3(4f, 0.8f, -4f),
            new Vector3(-8f, 0.8f, -8f),
            new Vector3(8f, 0.8f, -8f),
            new Vector3(0f, 0.8f, 8f),
            new Vector3(-7f, 1.8f, 6f),
            new Vector3(7f, 2.3f, 6f),
        };
        for (int i = 0; i < coinPositions.Length; i++)
        {
            CreateCoin("Coin " + (i + 1), coinPositions[i], coinMat, coinsRoot.transform);
        }

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.fieldOfView = 55f;
        cameraGo.AddComponent<AudioListener>();
        cameraGo.transform.position = player.transform.position + new Vector3(0f, 9f, -9f);
        cameraGo.transform.LookAt(player.transform);
        var follow = cameraGo.AddComponent<CameraFollow>();
        SetReference(follow, "target", player.transform);

        Text scoreText;
        Text messageText;
        CreateHud(out scoreText, out messageText);

        var managerGo = new GameObject("GameManager");
        var manager = managerGo.AddComponent<GameManager>();
        SetReference(manager, "scoreText", scoreText);
        SetReference(manager, "messageText", messageText);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        return ScenePath + " coins=" + coinPositions.Length;
    }

    private static Material GetMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        string path = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        box.isStatic = true;
        return box;
    }

    private static void CreateCoin(string name, Vector3 position, Material material, Transform parent)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = name;
        coin.transform.SetParent(parent);
        coin.transform.position = position;
        coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
        coin.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(coin.GetComponent<Collider>());
        var trigger = coin.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.2f, 8f, 1.2f);
        coin.AddComponent<Coin>();
    }

    private static void CreateHud(out Text scoreText, out Text messageText)
    {
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        scoreText = CreateText("ScoreText", canvasGo.transform, font, 48, TextAnchor.UpperLeft);
        RectTransform scoreRect = scoreText.rectTransform;
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(40f, -30f);
        scoreRect.sizeDelta = new Vector2(700f, 80f);
        scoreText.text = "Altın: 0 / 0";

        messageText = CreateText("MessageText", canvasGo.transform, font, 90, TextAnchor.MiddleCenter);
        RectTransform messageRect = messageText.rectTransform;
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(1500f, 400f);
        messageText.text = "KAZANDIN!";
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    private static void SetReference(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            throw new System.InvalidOperationException(target.GetType().Name + " has no field " + field);
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
