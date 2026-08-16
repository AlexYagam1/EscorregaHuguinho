using UnityEngine;
using UnityEditor;

public class GridLevelEditor : EditorWindow
{
    // Parents na Hierarquia
    private GameObject parentWall;
    private GameObject parentIce;
    private GameObject parentGround;
    private GameObject parentSpike;
    private GameObject parentGoal;

    // Prefabs
    private GameObject prefabWall;
    private GameObject prefabIce;
    private GameObject prefabGround;
    private GameObject prefabSpike;
    private GameObject prefabGoal;

    // Nomes das Camadas (Layers)
    private string layerWall = "Parede";
    private string layerIce = "Gelo";
    private string layerGround = "Chao";
    private string layerSpike = "Espinho";
    private string layerGoal = "Objetivo";

    // Configurações do Pincel
    private bool paintModeActive = false;
    private enum ToolType { Parede, Gelo, Chao, Espinho, Objetivo }
    private ToolType selectedTool = ToolType.Chao;
    private float gridHeight = 0f; // Altura padrão Y onde os blocos serão criados

    // Controle para arrastar e desenhar sem duplicar no mesmo frame
    private Vector3 lastActionPosition = new Vector3(float.NaN, float.NaN, float.NaN);

    [MenuItem("EscorregaHuguinho/Grid Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<GridLevelEditor>("Grid Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Configurações do EscorregaHuguinho Grid Editor", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        GUILayout.Label("1. Parents da Hierarquia (Onde os clones vão)", EditorStyles.miniBoldLabel);
        parentWall = (GameObject)EditorGUILayout.ObjectField("Parent Parede", parentWall, typeof(GameObject), true);
        parentIce = (GameObject)EditorGUILayout.ObjectField("Parent Gelo", parentIce, typeof(GameObject), true);
        parentGround = (GameObject)EditorGUILayout.ObjectField("Parent Chao", parentGround, typeof(GameObject), true);
        parentSpike = (GameObject)EditorGUILayout.ObjectField("Parent Espinho", parentSpike, typeof(GameObject), true);
        parentGoal = (GameObject)EditorGUILayout.ObjectField("Parent Objetivo", parentGoal, typeof(GameObject), true);

        EditorGUILayout.Space();
        GUILayout.Label("2. Prefabs de Origem", EditorStyles.miniBoldLabel);
        prefabWall = (GameObject)EditorGUILayout.ObjectField("Prefab Parede", prefabWall, typeof(GameObject), false);
        prefabIce = (GameObject)EditorGUILayout.ObjectField("Prefab Gelo", prefabIce, typeof(GameObject), false);
        prefabGround = (GameObject)EditorGUILayout.ObjectField("Prefab Chao", prefabGround, typeof(GameObject), false);
        prefabSpike = (GameObject)EditorGUILayout.ObjectField("Prefab Espinho", prefabSpike, typeof(GameObject), false);
        prefabGoal = (GameObject)EditorGUILayout.ObjectField("Prefab Objetivo", prefabGoal, typeof(GameObject), false);

        EditorGUILayout.Space();
        GUILayout.Label("3. Configuração das Layers", EditorStyles.miniBoldLabel);
        layerWall = EditorGUILayout.TextField("Layer Parede", layerWall);
        layerIce = EditorGUILayout.TextField("Layer Gelo", layerIce);
        layerGround = EditorGUILayout.TextField("Layer Chao", layerGround);
        layerSpike = EditorGUILayout.TextField("Layer Espinho", layerSpike);
        layerGoal = EditorGUILayout.TextField("Layer Objetivo", layerGoal);

        EditorGUILayout.Space();
        GUILayout.Label("4. Ferramenta de Pintura", EditorStyles.miniBoldLabel);
        selectedTool = (ToolType)EditorGUILayout.EnumPopup("Bloco Selecionado", selectedTool);
        gridHeight = EditorGUILayout.FloatField("Altura do Grid (Y)", gridHeight);

        EditorGUILayout.Space();
        
        string buttonText = paintModeActive ? "DESATIVAR Modo Pintura" : "ATIVAR Modo Pintura";
        GUI.backgroundColor = paintModeActive ? Color.green : Color.white;
        if (GUILayout.Button(buttonText, GUILayout.Height(40)))
        {
            paintModeActive = !paintModeActive;
        }
        GUI.backgroundColor = Color.white;

        if (paintModeActive)
        {
            EditorGUILayout.HelpBox(
                "COMO DESENHAR NA CENA:\n" +
                "• Segure [ SHIFT ] + Clique e Arraste para desenhar continuamente.\n" +
                "• Segure [ CTRL ] + Clique e Arraste para apagar continuamente.", 
                MessageType.Info
            );
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!paintModeActive) return;

        // Impede que a seleção padrão da Scene View atrapalhe a pintura
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event currentEvent = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));

        if (gridPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            
            Vector3 snappedPosition = new Vector3(
                Mathf.Round(hitPoint.x),
                gridHeight,
                Mathf.Round(hitPoint.z)
            );

            // Desenha o quadrado visual de preview
            Handles.color = Color.yellow;
            Handles.DrawWireCube(snappedPosition + Vector3.up * 0.1f, new Vector3(0.9f, 0.1f, 0.9f));
            sceneView.Repaint();

            // Detecta Clique ou Arrastar do mouse
            bool isDrawingEvent = (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && currentEvent.button == 0;

            if (isDrawingEvent)
            {
                // Só executa se o mouse tiver se movido para uma nova célula do grid (evita repetições inúteis)
                if (snappedPosition != lastActionPosition)
                {
                    if (currentEvent.shift)
                    {
                        PlaceBlock(snappedPosition);
                        lastActionPosition = snappedPosition;
                    }
                    else if (currentEvent.control)
                    {
                        DeleteBlockAt(snappedPosition);
                        lastActionPosition = snappedPosition;
                    }
                }
                currentEvent.Use(); // Consome o evento do Unity
            }

            // Reseta a posição de controle quando soltar o mouse
            if (currentEvent.type == EventType.MouseUp)
            {
                lastActionPosition = new Vector3(float.NaN, float.NaN, float.NaN);
            }
        }
    }

    private void PlaceBlock(Vector3 position)
    {
        GameObject prefabToSpawn = null;
        GameObject parentToUse = null;
        string layerToApply = "";

        switch (selectedTool)
        {
            case ToolType.Parede:
                prefabToSpawn = prefabWall;
                parentToUse = parentWall;
                layerToApply = layerWall;
                break;
            case ToolType.Gelo:
                prefabToSpawn = prefabIce;
                parentToUse = parentIce;
                layerToApply = layerIce;
                break;
            case ToolType.Chao:
                prefabToSpawn = prefabGround;
                parentToUse = parentGround;
                layerToApply = layerGround;
                break;
            case ToolType.Espinho:
                prefabToSpawn = prefabSpike;
                parentToUse = parentSpike;
                layerToApply = layerSpike;
                break;
            case ToolType.Objetivo:
                prefabToSpawn = prefabGoal;
                parentToUse = parentGoal;
                layerToApply = layerGoal;
                break;
        }

        if (prefabToSpawn == null) return;

        // Evita duplicados na mesma posição
        if (parentToUse != null)
        {
            foreach (Transform child in parentToUse.transform)
            {
                if (Vector3.Distance(child.position, position) < 0.1f)
                {
                    return; 
                }
            }
        }

        GameObject newBlock = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
        newBlock.transform.position = position;

        if (parentToUse != null)
        {
            newBlock.transform.SetParent(parentToUse.transform);
        }

        int layerIndex = LayerMask.NameToLayer(layerToApply);
        if (layerIndex != -1)
        {
            newBlock.layer = layerIndex;
            foreach (Transform child in newBlock.transform)
            {
                child.gameObject.layer = layerIndex;
            }
        }

        Undo.RegisterCreatedObjectUndo(newBlock, "Criar Bloco do Grid");
    }

    private void DeleteBlockAt(Vector3 position)
    {
        GameObject[] parents = { parentWall, parentIce, parentGround, parentSpike, parentGoal };
        
        foreach (var parent in parents)
        {
            if (parent == null) continue;

            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.transform.GetChild(i);
                if (Vector3.Distance(child.position, position) < 0.1f)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    return;
                }
            }
        }
    }
}
