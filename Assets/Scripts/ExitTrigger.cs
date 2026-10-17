using UnityEngine;

// Porte de sortie : verrouillée (fermée) puis ouverte quand les 3 énigmes sont résolues.
public class ExitTrigger : MonoBehaviour
{
    public Transform visual;
    public Sprite openSprite;   // image de la porte ouverte (définie dans le prefab)
    bool open;

    void Awake()
    {
        if (visual == null) visual = transform.Find("Visuel");
    }

    public void Open()
    {
        open = true;
        var sr = visual.GetComponent<SpriteRenderer>();
        if (openSprite != null) sr.sprite = openSprite;                    // porte ouverte
        else sr.color = new Color(0.3f, 0.9f, 0.4f);                       // sans image : verte
    }

    // ANIMATION 3 : sans image, la porte ouverte pulse pour attirer l'attention
    void Update()
    {
        if (!open || visual == null || openSprite != null) return;
        float s = 1f + 0.15f * Mathf.Sin(Time.time * 4f);
        visual.localScale = new Vector3(s, 1f, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null && GameManager.Instance != null)
            GameManager.Instance.TryExit();
    }
}
