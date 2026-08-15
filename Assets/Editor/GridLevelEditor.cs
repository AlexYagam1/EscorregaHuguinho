using UnityEngine;
using UnityEditor;

public class GridLevelEditor : EditorWindow
{
    // Parents na Hierarquia
    private GameObject parentWall;
    private GameObject parentIce;
    private GameObject parentGround;

    // Prefabs
    private GameObject prefabWall;
    private GameObject prefabIce;
    private GameObject prefabGround;

    // Nomes das Camadas (Layers)
    private string layerWall = "Parede";
    private string layerIce = "Gelo";
    private string layerGround = "Chao";

    // Configurações do Pincel
    private bool paintModeActive = false;
    private enum ToolType { Parede, Gelo, Chao }
    private ToolType selectedTool = ToolType.Chao;
    private float gridHeight = 0f; // Altura padrão Y onde os blocos serão criados

    [MenuItem("EscorregaHuguinho/Grid Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<GridLevelEditor>("Grid Editor");
    }

    private void OnEnable()
    {
        // Inscreve o método para desenhar e interagir na Scene View do Unity
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

        EditorGUILayout.Space();
        GUILayout.Label("2. Prefabs de Origem", EditorStyles.miniBoldLabel);
        prefabWall = (GameObject)EditorGUILayout.ObjectField("Prefab Parede", prefabWall, typeof(GameObject), false);
        prefabIce = (GameObject)EditorGUILayout.ObjectField("Prefab Gelo", prefabIce, typeof(GameObject), false);
        prefabGround = (GameObject)EditorGUILayout.ObjectField("Prefab Chao", prefabGround, typeof(GameObject), false);

        EditorGUILayout.Space();
        GUILayout.Label("3. Configuração das Layers", EditorStyles.miniBoldLabel);
        layerWall = EditorGUILayout.TextField("Layer Parede", layerWall);
        layerIce = EditorGUILayout.TextField("Layer Gelo", layerIce);
        layerGround = EditorGUILayout.TextField("Layer Chao", layerGround);

        EditorGUILayout.Space();
        GUILayout.Label("4. Ferramenta de Pintura", EditorStyles.miniBoldLabel);
        selectedTool = (ToolType)EditorGUILayout.EnumPopup("Bloco Selecionado", selectedTool);
        gridHeight = EditorGUILayout.FloatField("Altura do Grid (Y)", gridHeight);

        EditorGUILayout.Space();
        
        // Botão de Ativação
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
                "COMO USAR NA CENA:\n" +
                "• Segure [ SHIFT ] + [ Clique Esquerdo ] para pintar o bloco.\n" +
                "• Segure [ CTRL ] + [ Clique Esquerdo ] para apagar um bloco na posição.", 
                MessageType.Info
            );
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!paintModeActive) return;

        // Desativa a seleção padrão do Unity na Scene View para não atrapalhar o clique
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event currentEvent = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));

        if (gridPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            
            // Arredonda a posição para se alinhar perfeitamente ao grid (Snapping)
            Vector3 snappedPosition = new Vector3(
                Mathf.Round(hitPoint.x),
                gridHeight,
                Mathf.Round(hitPoint.z)
            );

            // Desenha um quadrado visual de preview na Scene View
            Handles.color = Color.yellow;
            Handles.DrawWireCube(snappedPosition + Vector3.up * 0.1f, new Vector3(0.9f, 0.1f, 0.9f));
            sceneView.Repaint();

            // PINTAR: SHIFT + Clique Esquerdo
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && currentEvent.shift)
            {
                PlaceBlock(snappedPosition);
                currentEvent.Use(); // Consome o evento do mouse
            }

            // APAGAR: CTRL + Clique Esquerdo
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && currentEvent.control)
            {
                DeleteBlockAt(snappedPosition);
                currentEvent.Use(); // Consome o evento do mouse
            }
        }
    }

    private void PlaceBlock(Vector3 position)
    {
        GameObject prefabToSpawn = null;
        GameObject parentToUse = null;
        string layerToApply = "";

        // Define qual bloco criar baseado na seleção
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
        }

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[Grid Editor] Erro: Prefab correspondente não foi configurado na janela!");
            return;
        }

        // Evita duplicados na mesma posição exata sob o mesmo pai
        if (parentToUse != null)
        {
            foreach (Transform child in parentToUse.transform)
            {
                if (Vector3.Distance(child.position, position) < 0.1f)
                {
                    return; // Já existe um bloco aqui
                }
            }
        }

        // Instancia o objeto mantendo o vínculo com o Prefab (Boas práticas no Unity)
        GameObject newBlock = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
        newBlock.transform.position = position;

        if (parentToUse != null)
        {
            newBlock.transform.SetParent(parentToUse.transform);
        }

        // Aplica a Layer
        int layerIndex = LayerMask.NameToLayer(layerToApply);
        if (layerIndex != -1)
        {
            newBlock.layer = layerIndex;
            foreach (Transform child in newBlock.transform)
            {
                child.gameObject.layer = layerIndex;
            }
        }

        // Permite usar o Ctrl+Z para desfazer a criação do bloco
        Undo.RegisterCreatedObjectUndo(newBlock, "Criar Bloco do Grid");
    }

    private void DeleteBlockAt(Vector3 position)
    {
        GameObject[] parents = { parentWall, parentIce, parentGround };
        
        foreach (var parent in parents)
        {
            if (parent == null) continue;

            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.transform.GetChild(i);
                if (Vector3.Distance(child.position, position) < 0.1f)
                {
                    // Deleta permitindo desfazer com CTRL+Z
                    Undo.DestroyObjectImmediate(child.gameObject);
                    return;
                }
            }
        }
    }
}
