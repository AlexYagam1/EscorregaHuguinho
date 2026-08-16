using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Configuração de Cenas")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private List<string> levelScenes = new List<string>();

    private int currentLevelIndex = -1;

    private void Awake()
    {
        // Garante que só exista uma instância deste Manager na memória
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        DetermineCurrentLevelIndex();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetermineCurrentLevelIndex();
    }

    // Identifica automaticamente qual fase da lista está ativa no momento
    private void DetermineCurrentLevelIndex()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        currentLevelIndex = levelScenes.IndexOf(activeSceneName);
    }

    public void LoadNextLevel()
    {
        // Se o jogador estiver no Menu (index -1), inicia a primeira fase da lista
        if (currentLevelIndex == -1)
        {
            if (levelScenes.Count > 0)
            {
                SceneManager.LoadScene(levelScenes[0]);
            }
            return;
        }

        int nextIndex = currentLevelIndex + 1;

        if (nextIndex < levelScenes.Count)
        {
            SceneManager.LoadScene(levelScenes[nextIndex]);
        }
        else
        {
            // Fim das fases! Retorna ao Menu Principal
            LoadMainMenu();
        }
    }

    public void RestartCurrentLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
