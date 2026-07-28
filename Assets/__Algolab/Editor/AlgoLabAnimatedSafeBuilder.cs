using System;
using UnityEditor;
using UnityEngine;

public static class AlgoLabAnimatedSafeBuilder
{
    public const string PrefabPath =
        "Assets/__Algolab/Prefabs/Objects/level3/BankExample/Safe/AlgoLabAnimatedSafe.prefab";
    private const string MaterialFolder =
        "Assets/__Algolab/Materials/level3-encapsulamiento/animated-safe";

    [MenuItem("Tools/AlgoLab/Nivel 3/Crear caja fuerte animada")]
    private static void BuildFromMenu()
    {
        BuildBatch();
    }

    public static void BuildBatch()
    {
        EnsureFolder(MaterialFolder);

        Material body = CreateMaterial("Safe_Cuerpo", new Color(0.055f, 0.095f, 0.15f, 1f), 0.72f, 0.25f);
        Material bodyEdge = CreateMaterial("Safe_Bordes", new Color(0.08f, 0.19f, 0.25f, 1f), 0.62f, 0.35f);
        Material door = CreateMaterial("Safe_Puerta", new Color(0.105f, 0.25f, 0.34f, 1f), 0.67f, 0.42f);
        Material inset = CreateMaterial("Safe_Interior", new Color(0.012f, 0.022f, 0.035f, 1f), 0.35f, 0.08f);
        Material steel = CreateMaterial("Safe_Acero", new Color(0.52f, 0.68f, 0.73f, 1f), 0.9f, 0.72f);
        Material accent = CreateMaterial("Safe_Acento", new Color(0.10f, 0.78f, 0.60f, 1f), 0.55f, 0.38f);
        Material button = CreateMaterial("Safe_Botones", new Color(0.20f, 0.27f, 0.32f, 1f), 0.62f, 0.42f);

        GameObject root = new GameObject("AlgoLabAnimatedSafe");
        try
        {
            CreateBody(root.transform, body, bodyEdge, inset, steel);
            AlgoLabAnimatedSafe controller = root.AddComponent<AlgoLabAnimatedSafe>();
            CreateDoor(root.transform, controller, door, bodyEdge, steel, accent, button, inset);
            controller.animationDuration = 0.9f;
            controller.openDoorEuler = new Vector3(0f, 108f, 0f);
            controller.dialTurnsDegrees = 210f;
            controller.handleTurnDegrees = -75f;
            controller.boltsRetractedOffset = new Vector3(-0.055f, 0f, 0f);
            controller.SetOpenInstantly(false);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (saved == null)
            {
                throw new InvalidOperationException("No se pudo guardar la caja fuerte animada.");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ALGOLAB CAJA FUERTE: prefab animado creado en " + PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateBody(Transform root, Material body, Material edge, Material inset, Material steel)
    {
        Transform shell = NewAnchor("Cuerpo", root, Vector3.zero);
        CreateCube("Respaldo", shell, new Vector3(0f, 0.63f, 0.17f), new Vector3(0.96f, 1.20f, 0.22f), body);
        CreateCube("ParedIzquierda", shell, new Vector3(-0.45f, 0.63f, -0.10f), new Vector3(0.14f, 1.18f, 0.42f), body);
        CreateCube("ParedDerecha", shell, new Vector3(0.45f, 0.63f, -0.10f), new Vector3(0.14f, 1.18f, 0.42f), body);
        CreateCube("Techo", shell, new Vector3(0f, 1.16f, -0.10f), new Vector3(0.78f, 0.14f, 0.42f), body);
        CreateCube("Base", shell, new Vector3(0f, 0.10f, -0.10f), new Vector3(0.78f, 0.14f, 0.42f), body);
        CreateCube("InteriorOscuro", shell, new Vector3(0f, 0.63f, 0.045f), new Vector3(0.72f, 0.91f, 0.045f), inset);
        CreateCube("Repisa", shell, new Vector3(0f, 0.43f, -0.005f), new Vector3(0.68f, 0.035f, 0.30f), edge);

        CreateCube("MarcoSuperior", shell, new Vector3(0f, 1.15f, -0.335f), new Vector3(0.98f, 0.09f, 0.08f), edge);
        CreateCube("MarcoInferior", shell, new Vector3(0f, 0.11f, -0.335f), new Vector3(0.98f, 0.09f, 0.08f), edge);
        CreateCube("MarcoIzquierdo", shell, new Vector3(-0.45f, 0.63f, -0.335f), new Vector3(0.09f, 0.96f, 0.08f), edge);
        CreateCube("MarcoDerecho", shell, new Vector3(0.45f, 0.63f, -0.335f), new Vector3(0.09f, 0.96f, 0.08f), edge);

        CreateCube("PieIzquierdo", shell, new Vector3(-0.31f, 0.025f, 0.05f), new Vector3(0.22f, 0.07f, 0.36f), body);
        CreateCube("PieDerecho", shell, new Vector3(0.31f, 0.025f, 0.05f), new Vector3(0.22f, 0.07f, 0.36f), body);

        CreateCylinder("BisagraSuperior", shell, new Vector3(-0.51f, 0.91f, -0.37f),
            new Vector3(0.075f, 0.11f, 0.075f), Quaternion.identity, steel, 12);
        CreateCylinder("BisagraInferior", shell, new Vector3(-0.51f, 0.34f, -0.37f),
            new Vector3(0.075f, 0.11f, 0.075f), Quaternion.identity, steel, 12);
    }

    private static void CreateDoor(
        Transform root,
        AlgoLabAnimatedSafe controller,
        Material door,
        Material edge,
        Material steel,
        Material accent,
        Material button,
        Material inset)
    {
        Transform pivot = NewAnchor("PivotePuerta", root, new Vector3(-0.42f, 0.63f, -0.405f));
        controller.doorPivot = pivot;
        Transform panel = NewAnchor("Puerta", pivot, new Vector3(0.42f, 0f, 0f));
        CreateCube("PlacaPrincipal", panel, Vector3.zero, new Vector3(0.84f, 0.98f, 0.15f), door);
        CreateCube("BordeSuperior", panel, new Vector3(0f, 0.455f, -0.09f), new Vector3(0.76f, 0.06f, 0.055f), edge);
        CreateCube("BordeInferior", panel, new Vector3(0f, -0.455f, -0.09f), new Vector3(0.76f, 0.06f, 0.055f), edge);
        CreateCube("BordeIzquierdo", panel, new Vector3(-0.37f, 0f, -0.09f), new Vector3(0.06f, 0.86f, 0.055f), edge);
        CreateCube("BordeDerecho", panel, new Vector3(0.37f, 0f, -0.09f), new Vector3(0.06f, 0.86f, 0.055f), edge);
        CreateCube("PlacaInterior", panel, new Vector3(0f, 0f, 0.09f), new Vector3(0.70f, 0.82f, 0.045f), inset);

        Transform dial = NewAnchor("DiscoCombinacion", panel, new Vector3(-0.11f, 0.16f, -0.115f));
        controller.dialPivot = dial;
        CreateCylinder("BaseDisco", dial, Vector3.zero, new Vector3(0.17f, 0.045f, 0.17f),
            Quaternion.Euler(90f, 0f, 0f), steel, 20);
        CreateCylinder("CentroDisco", dial, new Vector3(0f, 0f, -0.075f), new Vector3(0.095f, 0.035f, 0.095f),
            Quaternion.Euler(90f, 0f, 0f), edge, 16);
        CreateCube("MarcaDisco", dial, new Vector3(0f, 0.125f, -0.115f), new Vector3(0.025f, 0.055f, 0.025f), accent);

        Transform handle = NewAnchor("Manija", panel, new Vector3(-0.11f, -0.18f, -0.14f));
        controller.handlePivot = handle;
        CreateCylinder("CentroManija", handle, Vector3.zero, new Vector3(0.08f, 0.05f, 0.08f),
            Quaternion.Euler(90f, 0f, 0f), steel, 16);
        for (int i = 0; i < 3; i++)
        {
            Transform spoke = NewAnchor("BrazoManija_" + (i + 1), handle, Vector3.zero);
            spoke.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
            CreateCube("Brazo", spoke, new Vector3(0f, 0.11f, -0.015f), new Vector3(0.035f, 0.22f, 0.035f), steel);
            CreateCylinder("Agarre", spoke, new Vector3(0f, 0.235f, -0.015f), new Vector3(0.045f, 0.065f, 0.045f),
                Quaternion.identity, accent, 12);
        }

        Transform keypad = NewAnchor("Teclado", panel, new Vector3(0.23f, 0.15f, -0.115f));
        CreateCube("BaseTeclado", keypad, Vector3.zero, new Vector3(0.22f, 0.31f, 0.045f), edge);
        CreateCube("Pantalla", keypad, new Vector3(0f, 0.105f, -0.035f), new Vector3(0.15f, 0.055f, 0.025f), accent);
        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 3; col++)
        {
            CreateCube("Boton_" + row + "_" + col, keypad,
                new Vector3((col - 1) * 0.055f, 0.025f - row * 0.058f, -0.035f),
                new Vector3(0.038f, 0.038f, 0.018f), button);
        }

        Transform bolts = NewAnchor("Cerrojos", panel, new Vector3(0.36f, 0f, 0.08f));
        controller.boltRoot = bolts;
        CreateCylinder("CerrojoSuperior", bolts, new Vector3(0.10f, 0.28f, 0f),
            new Vector3(0.04f, 0.12f, 0.04f), Quaternion.Euler(0f, 0f, 90f), steel, 12);
        CreateCylinder("CerrojoCentral", bolts, new Vector3(0.10f, 0f, 0f),
            new Vector3(0.04f, 0.12f, 0.04f), Quaternion.Euler(0f, 0f, 90f), steel, 12);
        CreateCylinder("CerrojoInferior", bolts, new Vector3(0.10f, -0.28f, 0f),
            new Vector3(0.04f, 0.12f, 0.04f), Quaternion.Euler(0f, 0f, 90f), steel, 12);
    }

    private static Transform NewAnchor(string name, Transform parent, Vector3 localPosition)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        return obj.transform;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ConfigurePrimitive(obj, name, parent, position, scale, Quaternion.identity, material);
        return obj;
    }

    private static GameObject CreateCylinder(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material,
        int ignoredSides)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ConfigurePrimitive(obj, name, parent, position, scale, rotation, material);
        return obj;
    }

    private static void ConfigurePrimitive(
        GameObject obj,
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localRotation = rotation;
        obj.transform.localScale = scale;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("No se encontro un shader para la caja fuerte.");

        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string fullPath)
    {
        string[] parts = fullPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
