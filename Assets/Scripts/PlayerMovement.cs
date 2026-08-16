using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float slideSpeed = 10f;

    [Header("Configurações de Delay (Chão Comum)")]
    [SerializeField] private float stepDelay = 0.5f;     
    [SerializeField] private float fastStepDelay = 0.25f; 

    [Header("Máscaras de Colisão (Layers)")]
    [SerializeField] private LayerMask obstacleLayer; 
    [SerializeField] private LayerMask goalLayer;     
    [SerializeField] private LayerMask floorLayer;    
    [SerializeField] private LayerMask iceLayer;      
    [SerializeField] private LayerMask hazardLayer;   

    [Header("Input System")]
    [SerializeField] private InputActionReference moveAction;

    private Vector3 slideDirection = Vector3.zero;
    private Vector3 targetPosition;
    private bool isSliding = false;
    private bool isWaitingDelay = false; 
    private float gridStep = 1f;

    private void OnEnable() => moveAction.action.Enable();
    private void OnDisable() => moveAction.action.Disable();

    private void Start()
    {
        targetPosition = new Vector3(
            Mathf.Round(transform.position.x),
            transform.position.y,
            Mathf.Round(transform.position.z)
        );
        transform.position = targetPosition;
    }

    void Update()
    {
        if (isWaitingDelay) return;

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

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            slideDirection = new Vector3(Mathf.Sign(input.x), 0, 0);
        }
        else
        {
            slideDirection = new Vector3(0, 0, Mathf.Sign(input.y));
        }

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
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, slideSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            transform.position = targetPosition;
            EvaluateTileUnderneath();
        }
    }

    private void EvaluateTileUnderneath()
    {
        // Subimos a origem do raio em 0.1 para garantir que ele comece DEPOIS do pé do Hugo e atinja o chão de forma segura
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        float rayDistance = 1.2f;

        // 1. Detecta Espinhos (Hazard)
        if (Physics.Raycast(rayOrigin, Vector3.down, rayDistance, hazardLayer))
        {
            StartCoroutine(RestartLevel());
            return;
        }

        // 2. Detecta Chão Comum (Floor)
        if (Physics.Raycast(rayOrigin, Vector3.down, rayDistance, floorLayer))
        {
            StartCoroutine(ApplyFloorDelay());
            return;
        }

        // 3. Detecta Gelo (Ice)
        if (Physics.Raycast(rayOrigin, Vector3.down, rayDistance, iceLayer))
        {
            if (CheckObstacle(transform.position, slideDirection))
            {
                StopMovement();
            }
            else
            {
                CalculateNextTarget();
            }
            return;
        }

        // FALLBACK: Se o Hugo parou em um bloco sem nenhuma das Layers acima, 
        // ele para de deslizar para não travar o jogo.
        StopMovement();
    }

    private IEnumerator ApplyFloorDelay()
    {
        StopMovement();
        isWaitingDelay = true;

        bool isShiftPressed = Keyboard.current != null && 
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        float currentDelay = isShiftPressed ? fastStepDelay : stepDelay;

        yield return new WaitForSeconds(currentDelay);

        isWaitingDelay = false;
    }

    private bool CheckObstacle(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, gridStep, goalLayer))
        {
            LoadNextLevel();
            return true;
        }

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

    private IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(1.5f);

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.RestartCurrentLevel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void LoadNextLevel()
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadNextLevel();
        }
        else
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.Log("Fim do jogo!");
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Desenha o sensor corrigido no editor do Unity
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, (transform.position + Vector3.up * 0.1f) + Vector3.down * 1.2f);

        if (isSliding)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + slideDirection * gridStep);
        }
    }
}
