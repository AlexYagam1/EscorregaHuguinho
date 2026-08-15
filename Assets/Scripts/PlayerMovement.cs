using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float slideSpeed = 12f;
    [SerializeField] private float raycastDistance = 0.6f; // Distância para detectar a parede (metade do tamanho do cubo + margem)
    [SerializeField] private LayerMask obstacleLayer; // Camada que define o que é parede

    [Header("Input System")]
    [SerializeField] private InputActionReference moveAction;

    private Vector3 slideDirection = Vector3.zero;
    private bool isSliding = false;

    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }

    void Update()
    {
        if (!isSliding)
        {
            CheckInput();
        }
        else
        {
            ExecuteSlide();
        }
    }

    private void CheckInput()
    {
        // Lê a direção do WASD (retorna um Vector2)
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        // Se o jogador não pressionou nada, não faz nada
        if (input == Vector2.zero) return;

        // Evita movimentos diagonais, priorizando o eixo com maior pressão
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            // Movimento Horizontal (Esquerda/Direita)
            slideDirection = new Vector3(Mathf.Sign(input.x), 0, 0);
        }
        else
        {
            // Movimento Vertical (Cima/Baixo)
            slideDirection = new Vector3(0, 0, Mathf.Sign(input.y));
        }

        isSliding = true;
    }

    private void ExecuteSlide()
    {
        // Dispara um raio invisível para frente na direção do movimento para detectar colisão
        if (Physics.Raycast(transform.position, slideDirection, raycastDistance, obstacleLayer))
        {
            // Se encontrar parede, para o deslizamento
            isSliding = false;
            slideDirection = Vector3.zero;
            return;
        }

        // Move o personagem de forma contínua
        transform.Translate(slideDirection * slideSpeed * Time.deltaTime, Space.World);
    }

    // Desenha o raio de colisão no editor do Unity para nos ajudar a testar
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + slideDirection * raycastDistance);
    }
}
