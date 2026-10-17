using System.Collections;
using UnityEngine;

// Un coffre du niveau qui contient une énigme. Il pulse tant qu'il n'est pas résolu.
public class RiddleObject : MonoBehaviour
{
    public int bankId;          // numéro de l'énigme dans RiddleBank
    public string question;
    public string[] answers;
    public bool solved;
    public Transform visual;    // enfant qui porte le sprite (c'est lui qu'on anime)
    public Sprite solvedSprite; // image du coffre ouvert (définie dans le prefab)

    float phase;
    bool popping;

    void Awake()
    {
        phase = Random.value * 6.28f;   // chaque bloc pulse à un rythme décalé
        if (visual == null) visual = transform.Find("Visuel");
    }

    // ANIMATION 1 : le bloc "respire" tant qu'il n'est pas résolu
    void Update()
    {
        if (visual == null || solved || popping) return;
        float s = 1f + 0.10f * Mathf.Sin(Time.time * 3f + phase);
        visual.localScale = new Vector3(s, s, 1f);
    }

    public bool IsCorrect(string playerAnswer)
    {
        string a = RiddleBank.Clean(playerAnswer);
        foreach (var good in answers)
            if (a == RiddleBank.Clean(good)) return true;
        return false;
    }

    public void MarkSolved(bool animate = true)
    {
        solved = true;
        var sr = visual.GetComponent<SpriteRenderer>();
        if (solvedSprite != null)
        {
            sr.sprite = solvedSprite;          // coffre ouvert
            sr.color = Color.white;
        }
        else
        {
            sr.color = new Color(0.3f, 0.85f, 0.4f);   // sans image : devient vert
        }
        if (animate) StartCoroutine(Pop());
        else visual.localScale = Vector3.one;
    }

    // ANIMATION 2 : petit effet "pop" quand l'énigme est résolue
    IEnumerator Pop()
    {
        popping = true;
        float t = 0f;
        const float duration = 0.4f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float s = 1f + 0.6f * Mathf.Sin(t / duration * Mathf.PI);
            visual.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        visual.localScale = Vector3.one;
        popping = false;
    }
}
