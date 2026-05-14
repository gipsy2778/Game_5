using UnityEngine;

public class EnemyTank : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Attack }
    public EnemyState currentState = EnemyState.Idle;

    [Header("References")]
    public Transform turret;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("VFX & SFX")]
    public GameObject muzzleFlash;
    public AudioClip shootSound;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotateSpeed = 120f;

    [Header("Engine SFX")]
    public AudioClip moveSound;
    private AudioSource moveSource;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float attackRange = 5f;
    public float preferredDistance = 4f;

    [Header("Vision")]
    public LayerMask visionMask;

    [Header("Combat")]
    public float fireRate = 1.2f;
    public float bulletSpeed = 8f;

    [Header("Obstacle Avoidance")]
    public float obstacleCheckDistance = 1.2f;
    public LayerMask obstacleMask;

    [Header("Idle Behavior")]
    public float idleLookAngle = 60f;
    public float idleRotateSpeed = 120f;
    public float idleMoveDistance = 1.2f;
    public float idleMoveSpeed = 1.5f;
    public float idlePauseTime = 1.2f;
    [Range(0f, 1f)] public float moveChance = 0.3f;

    private Rigidbody2D rb;
    private Transform player;
    private float nextFireTime;

    private enum IdleState { Choosing, Rotating, Moving, Pausing }
    private IdleState idleState = IdleState.Choosing;

    private float targetAngle;
    private Vector2 moveTarget;
    private float pauseTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // ✅ AudioSource dibuat SEKALI di Start
        moveSource = gameObject.AddComponent<AudioSource>();
        moveSource.clip = moveSound;
        moveSource.loop = true;
        moveSource.playOnAwake = false;

        if (SFXManager.instance != null)
            moveSource.outputAudioMixerGroup = SFXManager.instance.sfxSource.outputAudioMixerGroup;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool canSee = CanSeePlayer();

        // ===== STATE =====
        if (canSee && distance <= attackRange)
            currentState = EnemyState.Attack;
        else if (canSee && distance <= detectionRange)
            currentState = EnemyState.Chase;
        else
            currentState = EnemyState.Idle;

        if (canSee)
            RotateTurretToPlayer();

        if (currentState == EnemyState.Attack && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:

                if (moveSource.isPlaying)
                    moveSource.Stop();

                HandleIdle();
                break;

            case EnemyState.Chase:

                if (!moveSource.isPlaying && moveSound != null)
                    moveSource.Play();

                MoveToward(player.position);
                break;

            case EnemyState.Attack:

                if (moveSource.isPlaying)
                    moveSource.Stop();

                RotateBodyToward(player.position);

                if (distance > preferredDistance)
                    TryMove(transform.up);
                else if (distance < preferredDistance * 0.7f)
                    TryMove(-transform.up);
                else
                    rb.velocity = Vector2.zero;

                break;
        }
    }

    // ================= IDLE =================

    void HandleIdle()
    {
        switch (idleState)
        {
            case IdleState.Choosing:

                if (Random.value < moveChance)
                {
                    moveTarget = (Vector2)transform.position + (Vector2)transform.up * idleMoveDistance;
                    idleState = IdleState.Moving;
                }
                else
                {
                    float dir = Random.value > 0.5f ? 1f : -1f;
                    targetAngle = transform.eulerAngles.z + (idleLookAngle * dir);
                    idleState = IdleState.Rotating;
                }

                break;

            case IdleState.Rotating:

                Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    idleRotateSpeed * Time.fixedDeltaTime
                );

                if (Quaternion.Angle(transform.rotation, targetRot) < 1f)
                {
                    pauseTimer = idlePauseTime;
                    idleState = IdleState.Pausing;
                }

                break;

            case IdleState.Moving:

                if (!IsBlocked(transform.up))
                    rb.velocity = transform.up * idleMoveSpeed;
                else
                    rb.velocity = Vector2.zero;

                if (Vector2.Distance(transform.position, moveTarget) < 0.1f)
                {
                    rb.velocity = Vector2.zero;
                    pauseTimer = idlePauseTime;
                    idleState = IdleState.Pausing;
                }

                break;

            case IdleState.Pausing:

                rb.velocity = Vector2.zero;
                pauseTimer -= Time.fixedDeltaTime;

                if (pauseTimer <= 0f)
                    idleState = IdleState.Choosing;

                break;
        }
    }

    // ================= MOVEMENT =================

    void MoveToward(Vector2 target)
    {
        RotateBodyToward(target);
        TryMove(transform.up);
    }

    void TryMove(Vector2 direction)
    {
        Vector2 origin = (Vector2)transform.position;

        Vector2 forward = direction.normalized;
        Vector2 left = Quaternion.Euler(0, 0, 30f) * forward;
        Vector2 right = Quaternion.Euler(0, 0, -30f) * forward;

        bool frontHit = Physics2D.Raycast(origin, forward, obstacleCheckDistance, obstacleMask);
        bool leftHit = Physics2D.Raycast(origin, left, obstacleCheckDistance, obstacleMask);
        bool rightHit = Physics2D.Raycast(origin, right, obstacleCheckDistance, obstacleMask);

        if (!frontHit)
            rb.velocity = forward * moveSpeed;
        else if (!leftHit)
            rb.velocity = left * moveSpeed;
        else if (!rightHit)
            rb.velocity = right * moveSpeed;
        else
            rb.velocity = Vector2.zero;
    }

    bool IsBlocked(Vector2 direction)
    {
        Vector2 origin = (Vector2)transform.position;

        Vector2 forward = direction.normalized;
        Vector2 left = Quaternion.Euler(0, 0, 25f) * forward;
        Vector2 right = Quaternion.Euler(0, 0, -25f) * forward;

        bool frontHit = Physics2D.Raycast(origin, forward, obstacleCheckDistance, obstacleMask);
        bool leftHit = Physics2D.Raycast(origin, left, obstacleCheckDistance, obstacleMask);
        bool rightHit = Physics2D.Raycast(origin, right, obstacleCheckDistance, obstacleMask);

        return frontHit && leftHit && rightHit;
    }

    // ================= ROTATION =================

    void RotateBodyToward(Vector2 target)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.fixedDeltaTime);
    }

    void RotateTurretToPlayer()
    {
        Vector3 direction = player.position - turret.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        turret.rotation = Quaternion.Euler(0, 0, angle);
    }

    // ================= VISION =================

    bool CanSeePlayer()
    {
        Vector2 origin = (Vector2)transform.position + (Vector2)transform.up * 0.5f;
        Vector2 direction = (player.position - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, detectionRange, visionMask);

        if (hit.collider != null)
            return hit.collider.CompareTag("Player");

        return false;
    }

    // ================= SHOOT =================

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.velocity = firePoint.up * bulletSpeed;

        if (muzzleFlash != null)
            Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);

        if (SFXManager.instance != null && shootSound != null)
            SFXManager.instance.PlaySFX(shootSound);
    }
}