using UnityEngine;
// Importa o novo sistema de input da Unity (Input System) - mais moderno que o Input Manager antigo
using UnityEngine.InputSystem;

// Garante automaticamente que o GameObject tenha um CharacterController ao adicionar este script
[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float walkSpeed = 4f;        // Velocidade de caminhada (unidades por segundo)
    [SerializeField] private float runSpeed = 8f;         // Velocidade de corrida (unidades por segundo)
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
    // Flag que indica se o SHIFT está pressionado (correr)
    private bool isRunning;

    // Referência à câmera principal para calcular a direção relativa
    private Transform cameraTransform;

    // Referência ao componente Animator para controlar as animações
    private Animator anim;


    // Awake é chamado quando o script é carregado (antes do Start)
    void Awake()
    {
        // Pega o componente CharacterController anexado a este GameObject
        controller = GetComponent<CharacterController>();

        // Pega a referência da câmera principal (a que tem a tag "MainCamera" na cena)
        // Isso permite que o movimento do personagem seja relativo à direção da câmera
        cameraTransform = Camera.main.transform;

        // Busca automaticamente o componente Animator no modelo 3D (que é filho do Player)
        anim = GetComponentInChildren<Animator>();
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

            // LEITURA DA CORRIDA (SHIFT)
            // isRunning = true se o SHIFT (esquerdo ou direito) estiver pressionado no teclado
            // OU se o botão do meio (LeftStick) no controle estiver pressionado
            isRunning = (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)) ||
                        (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        }

        // --- MOVIMENTO RELATIVO À CÂMERA ---
        // Converte o input 2D (X, Y) para direção 3D (X, 0, Z)
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // --- ATUALIZAÇÃO DAS ANIMAÇÕES E MOVIMENTO ---
        if (direction.magnitude >= 0.1f)
        {
            // --- PASSO 1: Descobrir para onde a câmera está olhando no plano horizontal ---
            float cameraYaw = cameraTransform.eulerAngles.y;

            // --- PASSO 2: Calcular a direção do movimento no ESPAÇO DA CÂMERA ---
            Vector3 moveDir = Quaternion.Euler(0f, cameraYaw, 0f) * direction;

            // --- PASSO 3: Rotacionar o personagem para a direção do movimento ---
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // --- PASSO 4: Mover o personagem (com velocidade diferente se estiver correndo) ---
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);

            // --- PASSO 5: Controlar as animações ---
            if (anim != null)
            {
                // Descobre o valor alvo da animação (0.5 para andar, 1.0 para correr)
                float targetAnimSpeed = isRunning ? 1.0f : 0.5f;

                // Pega o valor atual que está no Animator
                float currentAnimSpeed = anim.GetFloat("Speed");

                // Sobe o valor suavemente até o alvo para evitar cortes bruscos
                float newAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 5f);
                anim.SetFloat("Speed", newAnimSpeed);
            }
        }
        else
        {
            // Se não houver movimento, reduz o valor da velocidade do Animator até chegar a 0
            if (anim != null)
            {
                float currentAnimSpeed = anim.GetFloat("Speed");
                float newAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, 0.0f, Time.deltaTime * 8f);
                anim.SetFloat("Speed", newAnimSpeed);
            }
        }


        // --- PULO ---
        if (jumpPressed && isGrounded)
        {
            // Calcula a velocidade vertical necessária para atingir a altura do pulo
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Dispara a animação de pulo no Animator
            if (anim != null)
            {
                anim.SetTrigger("Jump");
            }
        }

        // Aplica a gravidade: reduz a velocidade vertical a cada frame
        velocity.y += gravity * Time.deltaTime;
        // Aplica o movimento vertical (gravidade/pulo) ao personagem
        controller.Move(velocity * Time.deltaTime);
    }
}