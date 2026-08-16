using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configurações de Navegação")]
    [Tooltip("Digite exatamente o nome da primeira cena de jogo que será carregada.")]
    [SerializeField] private string firstLevelSceneName;

    /// <summary>
    /// Método chamado pelo botão "Play".
    /// </summary>
    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(firstLevelSceneName))
        {
            // Carrega a cena definida no Inspetor
            SceneManager.LoadScene(firstLevelSceneName);
        }
        else
        {
            Debug.LogError("Erro: O nome da primeira cena não foi definido no MainMenuManager!");
        }
    }

    /// <summary>
    /// Método chamado pelo botão "Sair".
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Fechando o jogo... (Isso funciona apenas na Build final)");
        
        // Fecha a aplicação (funciona no jogo compilado)
        Application.Quit();
    }
}
