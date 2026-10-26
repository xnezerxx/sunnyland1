using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("ДВИЖЕНИЕ (По горизонтали)")]
    [Tooltip("Скорость бега персонажа")]
    public float speed = 7f;
    [Tooltip("Плавность разгона и торможения (чем выше, тем быстрее разгон)")]
    public float acceleration = 50f;

    [Header("ПРЫЖКИ И ГРАВИТАЦИЯ")]
    [Tooltip("Сила обычного прыжка")]
    public float jumpForce = 14f;
    [Tooltip("Количество прыжков (2 = двойной прыжок)")]
    public int maxJumps = 2;
    [Tooltip("Усиление гравитации при падении (чтобы прыжок не был затянутым)")]
    public float fallMultiplier = 2.5f;
    [Tooltip("Усиление гравитации, если игрок отпустил кнопку прыжка раньше времени")]
    public float lowJumpMultiplier = 2f;

    [Header("ПРОВЕРКА ЗЕМЛИ И СТЕН")]
    public Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    [Space(5)]
    public Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.3f;

    [Header("КАРАБКАНИЕ И СКОЛЬЖЕНИЕ ПО СТЕНАМ")]
    [Tooltip("Скорость сползания по стене вниз")]
    public float wallSlidingSpeed = 2f;
    [Tooltip("Сила отталкивания от стены при прыжке")]
    public Vector2 wallJumpForce = new Vector2(10f, 15f);

    [Header("КОЙОТ-ТАЙМ И БУФЕР ПРЫЖКА (Для удобства управления)")]
    [Tooltip("Время (в сек), в течение которого можно прыгнуть, даже если уже сошел с платформы")]
    public float coyoteTime = 0.15f;
    [Tooltip("Время (в сек), в течение которого игра помнит нажатие прыжка перед приземлением")]
    public float jumpBufferTime = 0.15f;

    [Header("ЭФФЕКТЫ И ЧАСТИЦЫ (Необязательно)")]
    public ParticleSystem dustParticles;

    // Внутренние переменные физики и ввода
    private Rigidbody2D rb;
    private Animator anim; // Подключится автоматически, если есть на объекте
    private float horizontalInput;
    private int remainingJumps;
    private bool isFacingRight = true;

    // Стейты (состояния)
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;

    // Таймеры
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float wallJumpDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Настройка физики для идеальной плавности
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Создаем физический материал прямо в коде, чтобы персонаж никогда не залипал на стенах
        PhysicsMaterial2D noFrictionMat = new PhysicsMaterial2D("PlayerNoFriction");
        noFrictionMat.friction = 0f;
        noFrictionMat.bounciness = 0f;

        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            col.sharedMaterial = noFrictionMat;
        }

        remainingJumps = maxJumps;
    }

    void Update()
    {
        // 1. Считывание ввода
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 2. Обработка Таймеров (Койот-тайм и Буфер)
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            remainingJumps = maxJumps; // Сброс счетчика прыжков на земле
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 3. Логика Прыжков
        if (jumpBufferCounter > 0f)
        {
            // Обычный прыжок или Койот-прыжок
            if (coyoteTimeCounter > 0f || remainingJumps > 1)
            {
                Jump();
            }
            // Прыжок от стены
            else if (isWallSliding)
            {
                WallJump();
            }
        }

        // 4. Проверка условий скольжения по стене
        CheckWallSliding();

        // 5. Разворот спрайта (только если не прыгаем от стены в этот момент)
        if (!isWallJumping)
        {
            if (horizontalInput > 0 && !isFacingRight) Flip();
            else if (horizontalInput < 0 && isFacingRight) Flip();
        }

        // 6. Управление анимациями (если компонент Animator добавлен на персонажа)
        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
            anim.SetBool("IsGrounded", isGrounded);
            anim.SetBool("IsWallSliding", isWallSliding);
            anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }
    }

    void FixedUpdate()
    {
        // Сенсоры окружения
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (wallCheck != null)
            isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right * (isFacingRight ? 1 : -1), wallCheckDistance, groundLayer);

        // Движение по горизонтали
        if (!isWallJumping)
        {
            // Плавный разгон и торможение с использованием силы acceleration
            float targetSpeed = horizontalInput * speed;
            float speedDif = targetSpeed - rb.linearVelocity.x;
            float movement = speedDif * acceleration;
            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }

        // Логика скольжения по стене
        if (isWallSliding)
        {
            if (rb.linearVelocity.y < -wallSlidingSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlidingSpeed);
            }
        }

        // Улучшенная ("тяжелая") гравитация для приятного геймплея
        if (rb.linearVelocity.y < 0 && !isWallSliding) // Падаем вниз
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space)) // Отпустили пробел в полете раньше времени
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        remainingJumps--;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        CreateDust();
    }

    private void CheckWallSliding()
    {
        // Скользим, если касаемся стены, не на земле, и жмем кнопку в сторону стены
        if (isTouchingWall && !isGrounded && horizontalInput != 0)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        isWallJumping = true;
        wallJumpDirection = isFacingRight ? -1 : 1;

        // Импульс отталкивания от стены вверх и в противоположную сторону
        rb.linearVelocity = new Vector2(wallJumpForce.x * wallJumpDirection, wallJumpForce.y);
        jumpBufferCounter = 0f;
        remainingJumps = maxJumps - 1; // Оставляем возможность совершить двойной прыжок после отскока

        // Разворачиваем персонажа в сторону отскока
        if ((wallJumpDirection > 0 && !isFacingRight) || (wallJumpDirection < 0 && isFacingRight))
        {
            Flip();
        }

        Invoke(nameof(StopWallJumping), 0.15f); // Короткое окно, когда игрок не может сразу прервать полет назад
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f); // Профессиональный разгон через поворот объекта, а не scale
        CreateDust();
    }

    private void CreateDust()
    {
        if (dustParticles != null)
        {
            dustParticles.Play();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Отрисовка датчиков в окне сцены
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Vector3 direction = transform.right * wallCheckDistance;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + direction);
        }
    }
}

