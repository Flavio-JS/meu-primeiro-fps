using UnityEngine;
// Importa o novo sistema de input da Unity (Input System) - necessário para ler o mouse
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Alvos")]
    [SerializeField] private Transform target; // Referência ao objeto que a câmera vai seguir (o CameraFollowTarget)

    [Header("Configurações do Mouse")]
    [SerializeField] private float mouseSensitivity = 100f; // Sensibilidade do movimento do mouse (quanto maior, mais rápido)
    [SerializeField] private float distanceToTarget = 3f;  // Distância fixa entre a câmera e o personagem

    [Header("Limites da Câmera (Clamp)")]
    [SerializeField] private float minVerticalAngle = -35f; // Ângulo mínimo para olhar para baixo (evita virar de ponta-cabeça)
    [SerializeField] private float maxVerticalAngle = 60f;  // Ângulo máximo para olhar para cima

    // Acumuladores de rotação: guardam o quanto já rotacionamos no total
    private float rotationX = 0f; // Rotação vertical acumulada (olhar para cima/baixo)
    private float rotationY = 0f; // Rotação horizontal acumulada (olhar para esquerda/direita)

    void Start()
    {
        // Tranca o cursor do mouse no centro da tela (comum em jogos FPS / 3ª pessoa)
        Cursor.lockState = CursorLockMode.Locked;
        // Esconde o cursor do mouse para não atrapalhar a visão
        Cursor.visible = false;

        // Verificação de segurança: se o alvo não foi atribuído no Inspector, avisa no console
        if (target == null)
        {
            Debug.LogError("Por favor, atribua o alvo 'CameraFollowTarget' no script da Câmera!");
        }
    }

    // LateUpdate roda DEPOIS do Update do Player (e de todos os outros scripts)
    // Isso é importante para evitar trepidações: a câmera se move após o personagem já ter se movido
    void LateUpdate()
    {
        // Se não tem alvo ou mouse, não faz nada (proteção contra erros)
        if (target == null || Mouse.current == null) return;

        // --- 1. LEITURA DO MOVIMENTO DO MOUSE ---
        // Pega o quanto o mouse se moveu neste frame (delta = diferença desde o último frame)
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        // Aplica a sensibilidade e o Time.deltaTime para o movimento ser consistente em qualquer FPS
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        // --- 2. ACUMULA AS ROTAÇÕES ---
        // Horizontal: soma o movimento do mouse no eixo X (gira o personagem para os lados)
        rotationY += mouseX;
        // Vertical: SUBTRAI o movimento do mouse no eixo Y (invertido para ser natural: 
        // mover o mouse para cima = olhar para cima, mover para baixo = olhar para baixo)
        rotationX -= mouseY;

        // --- 3. LIMITA A ROTAÇÃO VERTICAL ---
        // Impede que a câmera gire além dos limites (evita ângulos estranhos)
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        // --- 4. APLICA A ROTAÇÃO NA CÂMERA ---
        // Cria uma rotação (Quaternion) a partir dos ângulos acumulados nos eixos X e Y
        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        // Aplica essa rotação à câmera (ela agora "olha" na direção correta)
        transform.rotation = targetRotation;

        // --- 5. POSICIONA A CÂMERA ATRÁS DO ALVO ---
        // Cria um vetor de deslocamento para trás (no eixo Z negativo) com a distância definida
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distanceToTarget);
        // Rotaciona esse deslocamento junto com a câmera e soma à posição do alvo
        // Resultado: a câmera fica sempre atrás do personagem na direção que estamos olhando
        Vector3 position = targetRotation * negDistance + target.position;

        // Aplica a posição calculada à câmera
        transform.position = position;
    }
}


