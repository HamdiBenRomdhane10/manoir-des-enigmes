using UnityEngine;

// Déplacement du joueur : flèches, WASD ou ZQSD (clavier AZERTY ou QWERTY).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public bool canMove = true;
    public Transform visual;    // enfant qui porte le sprite (c'est lui qu'on anime)

    Rigidbody2D rb;
    Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

    // ANIMATION 4 : le joueur s'écrase / s'étire quand il marche, et revient à la normale à l'arrêt
    void Animate()
    {
        if (visual == null) return;
        if (input.sqrMagnitude > 0.01f)
        {
            float s = Mathf.Sin(Time.time * 14f) * 0.08f;
            visual.localScale = new Vector3(1f + s, 1f - s, 1f);
        }
        else
        {
            visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one, 10f * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        rb.velocity = input * speed;
    }
}
