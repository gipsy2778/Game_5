using UnityEngine;

public class PlayerTank : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 200f;
    public Transform turret;
    public bool canStrafe = false;

    [Header("Engine Sound")]
    public AudioSource engineAudio;
    public float minPitch = 0.8f;
    public float maxPitch = 1.5f;
    public float minVolume = 0.1f;
    public float maxVolume = 0.6f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (engineAudio != null)
        {
            engineAudio.loop = true;
            engineAudio.Play();
        }
    }

    void Update()
    {
        if (Time.timeScale == 0) return;
        // Rotasi turret mengikuti mouse
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - turret.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        turret.rotation = Quaternion.Euler(0, 0, angle);
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0) return;
        float moveInput = Input.GetAxis("Vertical");
        float rotateInput = Input.GetAxis("Horizontal");

        if (canStrafe)
        {
            rb.velocity = transform.up * moveInput * moveSpeed +
                          transform.right * rotateInput * moveSpeed;
        }
        else
        {
            float newRotation = rb.rotation - rotateInput * rotateSpeed * Time.fixedDeltaTime;
            rb.rotation = newRotation;
            rb.velocity = transform.up * moveInput * moveSpeed;
        }

        UpdateEngineSound();
    }

    void UpdateEngineSound()
    {
        if (engineAudio == null) return;

        float speed = rb.velocity.magnitude;

        // 🔥 ambil kecepatan rotasi
        float rotationSpeed = Mathf.Abs(Input.GetAxis("Horizontal"));

        // gabungkan gerak maju + rotasi
        float movementFactor = Mathf.Clamp01((speed / moveSpeed) + rotationSpeed * 0.5f);

        // volume
        engineAudio.volume = Mathf.Lerp(minVolume, maxVolume, movementFactor);

        // pitch
        engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, movementFactor);

        // kalau benar-benar diam
        if (movementFactor < 0.05f)
        {
            engineAudio.volume = 0f;
        }
    }
}