using UnityEngine;

// Porte de sortie : verrouillée (rouge) puis ouverte (verte et animée).
public class ExitTrigger : MonoBehaviour
{
    public Transform visual;
    bool open;

    public void Open()
    {
        open = true;
        visual.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.9f, 0.4f);
    }

    // ANIMATION 3 : la porte ouverte pulse pour attirer l'attention
    void Update()
    {
        if (!open || visual == null) return;
        float s = 1f + 0.15f * Mathf.Sin(Time.time * 4f);
        visual.localScale = new Vector3(s, 1f, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null && GameManager.Instance != null)
            GameManager.Instance.TryExit();
    }
}
