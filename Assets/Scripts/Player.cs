using UnityEngine;
// Importa o novo sistema de input da Unity (Input System) - mais moderno que o Input Manager antigo
using UnityEngine.InputSystem;

// Garante automaticamente que o GameObject tenha um CharacterController ao adicionar este script
[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float speed = 6f;            // Velocidade de movimento do personagem (unidades por segundo)
    [SerializeField] private float turnSmoothTime = 0.1f; // Tempo de suavização da rotação ao virar (quanto menor, mais rápido)

    [Header("Pulo e Gravidade")]
    [SerializeField] private float jumpHeight = 1f;       // Altura máxima do pulo
    [SerializeField] private float gravity = -9.81f;      // Força da gravidade (valor negativo = puxa pra baixo)

    // Referência ao componente CharacterController que gerencia colisão e movimento
    private CharacterController controller;
    // Velocidade atual do personagem (incluindo velocidade vertical da gravidade/pulo)
    private Vector3 velocity;
    // Diz se o personagem está tocando o chão (útil para saber se pode pular)
    private bool isGrounded;
    // Usado pelo SmoothDampAngle para suavizar a rotação do personagem
    private float turnSmoothVelocity;

    // Armazena a direção do input do jogador (ex: WASD ou analógico)
    private Vector2 moveInput;
    // Flag que indica se o botão de pulo foi pressionado neste frame
    private bool jumpPressed;

    // Referência à câmera principal para calcular a direção relativa
    private Transform cameraTransform;


    // Awake é chamado quando o script é carregado (antes do Start)
    void Awake()
    {
        // Pega o componente CharacterController anexado a este GameObject
        controller = GetComponent<CharacterController>();

        // Pega a referência da câmera principal (a que tem a tag "MainCamera" na cena)
        // Isso permite que o movimento do personagem seja relativo à direção da câmera
        cameraTransform = Camera.main.transform;
    }


    // Update é chamado uma vez por frame (a cada quadro do jogo)
    void Update()
    {
        // Verifica se o personagem está tocando o chão
        isGrounded = controller.isGrounded;

        // Se está no chão e caindo (velocity.y negativo), gruda no chão
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequena força negativa para manter o personagem "colado" no chão
        }

        // --- LEITURA DE INPUT (WASD / Controle) ---
        // Verifica se há teclado ou gamepad conectado
        if (Gamepad.current != null || Keyboard.current != null)
        {
            // LEITURA DO TECLADO (WASD)
            if (Keyboard.current != null)
            {
                // Calcula o eixo X: D = +1 (direita), A = -1 (esquerda), nada = 0
                float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
                // Calcula o eixo Z: W = +1 (frente), S = -1 (trás), nada = 0
                float z = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
                moveInput = new Vector2(x, z);
            }

            // LEITURA DO CONTROLE (Analógico esquerdo)
            // Só lê o analógico se o teclado não estiver sendo pressionado (sqrMagnitude == 0)
            if (Gamepad.current != null && moveInput.sqrMagnitude == 0)
            {
                moveInput = Gamepad.current.leftStick.ReadValue();
            }

            // LEITURA DO PULO
            // jumpPressed = true se apertou Espaço no teclado OU botão Sul (A no Xbox, X no PS) no controle
            jumpPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                          (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        // --- MOVIMENTO RELATIVO À CÂMERA ---
        // Agora o movimento do personagem é calculado com base na direção que a CÂMERA está olhando,
        // e não mais com base nos eixos fixos do mundo (norte/sul/leste/oeste).
        // Isso significa que apertar W sempre faz o personagem andar para "frente da tela".

        // Converte o input 2D (X, Y) para direção 3D (X, 0, Z)
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // Só movimenta se a direção tiver magnitude significativa (evita micro-movimentos)
        if (direction.magnitude >= 0.1f)
        {
            // --- PASSO 1: Descobrir para onde a câmera está olhando no plano horizontal ---
            // Pega a rotação Y (horizontal) da câmera e ignora a rotação X (vertical)
            // para que o personagem não tente andar "para cima" quando a câmera olha para o chão
            float cameraYaw = cameraTransform.eulerAngles.y;

            // --- PASSO 2: Calcular a direção do movimento no ESPAÇO DA CÂMERA ---
            // Pegamos o input do jogador (WASD) e rotacionamos ele de acordo com o ângulo da câmera.
            // Exemplo: se a câmera está virada 90° para a direita, apertar W faz o personagem
            // andar para a direita no mundo (que é a "frente" da câmera).
            // Quaternion.Euler(0, cameraYaw, 0) cria uma rotação apenas no eixo Y.
            // Multiplicar o vetor direction por essa rotação "gira" a direção do input.
            Vector3 moveDir = Quaternion.Euler(0f, cameraYaw, 0f) * direction;

            // --- PASSO 3: Rotacionar o personagem para a direção do movimento ---
            // Calcula o ângulo alvo (para onde o personagem deve olhar) baseado na direção do movimento
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            // Suaviza a rotação do personagem em direção ao ângulo alvo (evita giros instantâneos)
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            // Aplica a rotação suavizada no eixo Y (rotação horizontal)
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // --- PASSO 4: Mover o personagem ---
            // Move o personagem na direção calculada (moveDir), na velocidade definida
            // Time.deltaTime garante que o movimento seja independente da taxa de quadros (FPS)
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }


        // --- PULO ---
        // Se o botão de pulo foi pressionado E o personagem está no chão
        if (jumpPressed && isGrounded)
        {
            // Calcula a velocidade vertical necessária para atingir a altura do pulo
            // Fórmula: v = raiz(2 * altura * -gravidade)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Aplica a gravidade: reduz a velocidade vertical a cada frame
        velocity.y += gravity * Time.deltaTime;
        // Aplica o movimento vertical (gravidade/pulo) ao personagem
        controller.Move(velocity * Time.deltaTime);
    }
}


