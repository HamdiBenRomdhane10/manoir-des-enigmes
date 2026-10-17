using UnityEngine;

// Déplacement du joueur : flèches, WASD ou ZQSD (clavier AZERTY ou QWERTY).
// Le joueur est un personnage qui change d'image quand il marche.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public bool canMove = true;
    public Transform visual;        // enfant qui porte le sprite (c'est lui qu'on anime)
    public Sprite idleSprite;       // image à l'arrêt
    public Sprite[] walkSprites;    // images de la marche

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer sr;
    Vector2 input;
    float walkTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (visual == null) visual = transform.Find("Visuel");
    }

    void Update()
    {
        if (canMove)
        {
            float x = 0f, y = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W)) y += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y -= 1f;
            input = new Vector2(x, y).normalized;
        }
        else
        {
            input = Vector2.zero;
        }

        Animate();
    }

    // ANIMATION du personnage : on change d'image 8 fois par seconde quand il marche
    // (animation "image par image"), avec un petit rebond. À l'arrêt : image fixe.
    void Animate()
    {
        if (visual == null) return;
        if (sr == null) sr = visual.GetComponent<SpriteRenderer>();

        // ANIMATOR : si le prefab a un Animator Controller, c'est lui qui change les images.
        // Le script lui envoie seulement la vitesse (paramètre "Speed").
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("Speed", input.magnitude);
            if (Mathf.Abs(input.x) > 0.01f) sr.flipX = input.x < 0f;   // regarde à gauche / à droite
            return;
        }

        // Sécurité : sans images de personnage, on garde l'ancien effet "écrasement"
        if (idleSprite == null || walkSprites == null || walkSprites.Length == 0)
        {
            if (input.sqrMagnitude > 0.01f)
            {
                float s = Mathf.Sin(Time.time * 14f) * 0.08f;
                visual.localScale = new Vector3(1f + s, 1f - s, 1f);
            }
            else
            {
                visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one, 10f * Time.deltaTime);
            }
            return;
        }

        if (input.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(input.x) > 0.01f) sr.flipX = input.x < 0f;   // regarde à gauche / à droite
            walkTimer += Time.deltaTime * 8f;                           // 8 images par seconde
            sr.sprite = walkSprites[(int)walkTimer % walkSprites.Length];
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.05f, 0f);
        }
        else
        {
            walkTimer = 0f;
            sr.sprite = idleSprite;
            visual.localPosition = Vector3.Lerp(visual.localPosition, Vector3.zero, 10f * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        rb.velocity = input * speed;
    }
}
