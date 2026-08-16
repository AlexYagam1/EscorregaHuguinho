using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float slideSpeed = 10f;
    
    [Header("Máscaras de Colisão (Layers)")]
    [SerializeField] private LayerMask obstacleLayer; // Parede (Camada 1)
    [SerializeField] private LayerMask goalLayer;     // Objetivo (Camada 1)
    [SerializeField] private LayerMask floorLayer;    // Chão Normal (Camada 0)
    [SerializeField] private LayerMask iceLayer;      // Gelo (Camada 0)
    [SerializeField] private LayerMask hazardLayer;   // Espinhos/Buraco (Camada -0.5)

    [Header("Input System")]
    [SerializeField] private InputActionReference moveAction;

    private Vector3 slideDirection = Vector3.zero;
    private Vector3 targetPosition;
    private bool isSliding = false;
    private float gridStep = 1f; // Tamanho do bloco na grade

    private void OnEnable() => moveAction.action.Enable();
    private void OnDisable() => moveAction.action.Disable();

    private void Start()
    {
        // Garante que o jogador comece alinhado à grade
        targetPosition = new Vector3(
            Mathf.Round(transform.position.x),
            transform.position.y,
            Mathf.Round(transform.position.z)
        );
        transform.position = targetPosition;
    }

    void Update()
    {
        if (!isSliding)
        {
            CheckInput();
        }
        else
        {
            MovePlayer();
        }
    }

    private void CheckInput()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        if (input == Vector2.zero) return;

        // Define a direção baseada no maior input (evita diagonal)
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            slideDirection = new Vector3(Mathf.Sign(input.x), 0, 0);
        }
        else
        {
            slideDirection = new Vector3(0, 0, Mathf.Sign(input.y));
        }

        // Antes de mover, verifica se há parede imediatamente na frente
        if (CheckObstacle(transform.position, slideDirection))
        {
            slideDirection = Vector3.zero;
            return;
        }

        CalculateNextTarget();
        isSliding = true;

    }

    private void MovePlayer()
    {
        // Move suavemente em direção ao alvo da grade
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, slideSpeed * Time.deltaTime);

        // Se chegou ao bloco alvo, decide o próximo passo
        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition; // Snapping perfeito
            EvaluateTileUnderneath();
        }
    }

    private void EvaluateTileUnderneath()
    {
        // 1. Checa se pisou em Espinhos/Buraco
        if (Physics.Raycast(transform.position, Vector3.down, 1f, hazardLayer))
        {
            StartCoroutine(RestartLevel());
            return;
        }

        // 2. Checa se pisou no Chão Normal (Para o movimento)
        if (Physics.Raycast(transform.position, Vector3.down, 1f, floorLayer))
        {
            StopMovement();
            return;
        }

        // 3. Checa se pisou no Gelo (Continua deslizando)
        if (Physics.Raycast(transform.position, Vector3.down, 1f, iceLayer))
        {
            // Antes de continuar, verifica se o próximo bloco tem obstáculo ou objetivo
            if (CheckObstacle(transform.position, slideDirection))
            {
                StopMovement();
            }
            else
            {
                CalculateNextTarget();
            }
        }
    }

    private bool CheckObstacle(Vector3 origin, Vector3 direction)
    {
        // Verifica se há objetivo na direção do movimento
        if (Physics.Raycast(origin, direction, gridStep, goalLayer))
        {
            LoadNextLevel();
            return true;
        }

        // Verifica se há parede
        return Physics.Raycast(origin, direction, gridStep, obstacleLayer);
    }

    private void CalculateNextTarget()
    {
        targetPosition = transform.position + slideDirection * gridStep;
    }

    private void StopMovement()
    {
        isSliding = false;
        slideDirection = Vector3.zero;
    }

    IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("Fim do jogo! Não há mais fases.");
        }
    }

    private void OnDrawGizmos()
    {
        // Desenha o sensor para baixo
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1f);

        // Desenha o sensor frontal de colisão
        if (isSliding)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + slideDirection * gridStep);
        }
    }
}
