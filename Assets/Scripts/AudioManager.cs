using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Gère tous les sons du jeu. Les sons sont GÉNÉRÉS PAR CODE (ondes sonores),
// donc aucun fichier audio n'est nécessaire. Touche M = couper / remettre le son.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    const int SampleRate = 44100;

    AudioSource sfx, music;
    AudioClip clickClip, correctClip, wrongClip, winClip, loseClip, musicClip;
    bool muted;

    // Crée le gestionnaire au premier appel (un seul pour tout le jeu)
    public static AudioManager Get()
    {
        if (Instance == null)
        {
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // survit au changement de scène (la musique continue)

        sfx = gameObject.AddComponent<AudioSource>();
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.volume = 0.15f;

        // Fréquences en Hz (La4 = 440 Hz)
        // Sons importés (Assets/Resources/Audio/*.wav) ; s'ils manquent, on les génère par code
        clickClip   = Load("click",   Tone(new[] { 880f }, 0.06f, false, 0.3f));
        correctClip = Load("correct", Tone(new[] { 660f, 880f }, 0.12f, false, 0.4f));
        wrongClip   = Load("wrong",   Tone(new[] { 150f }, 0.30f, true, 0.25f));
        winClip     = Load("win",     Tone(new[] { 523f, 659f, 784f, 1046f }, 0.18f, false, 0.4f));
        loseClip    = Load("lose",    Tone(new[] { 392f, 330f, 262f, 196f }, 0.25f, false, 0.4f));
        musicClip   = Load("music",   Tone(new[] { 220f, 330f, 392f, 330f, 262f, 330f, 440f, 392f,
                                                   220f, 330f, 392f, 494f, 440f, 392f, 330f, 262f }, 0.45f, false, 0.3f));
    }

    // Charge un son depuis Resources/Audio, ou garde la version générée par code
    AudioClip Load(string clipName, AudioClip fallback)
    {
        var c = Resources.Load<AudioClip>("Audio/" + clipName);
        return c != null ? c : fallback;
    }

    void Update()
    {
        // M coupe / remet le son (sauf quand le joueur tape dans le champ de réponse)
        var es = EventSystem.current;
        bool typing = es != null && es.currentSelectedGameObject != null
                      && es.currentSelectedGameObject.GetComponent<InputField>() != null;
        if (!typing && Input.GetKeyDown(KeyCode.M))
        {
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;
        }
    }

    // Fabrique un son : une suite de notes (sinus ou carré) avec un fondu pour éviter les "clics".
    AudioClip Tone(float[] freqs, float noteLength, bool square, float gain)
    {
        int perNote = (int)(SampleRate * noteLength);
        var data = new float[perNote * freqs.Length];
        int attack = (int)(SampleRate * 0.005f);   // montée rapide de 5 ms

        for (int n = 0; n < freqs.Length; n++)
        {
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / SampleRate;
                float phase = 2f * Mathf.PI * freqs[n] * t;
                float wave = square ? Mathf.Sign(Mathf.Sin(phase)) : Mathf.Sin(phase);
                float fadeOut = 1f - (float)i / perNote;
                float fadeIn = Mathf.Min(1f, (float)i / attack);
                data[n * perNote + i] = wave * fadeOut * fadeIn * gain;
            }
        }

        var clip = AudioClip.Create("tone", data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlayClick()   { sfx.PlayOneShot(clickClip, 0.6f); }
    public void PlayCorrect() { sfx.PlayOneShot(correctClip, 0.8f); }
    public void PlayWrong()   { sfx.PlayOneShot(wrongClip, 0.8f); }
    public void PlayWin()     { sfx.PlayOneShot(winClip, 0.9f); }
    public void PlayLose()    { sfx.PlayOneShot(loseClip, 0.9f); }

    public void StartMusic()
    {
        if (!music.isPlaying) { music.clip = musicClip; music.Play(); }
    }

    public void StopMusic()
    {
        music.Stop();
    }
}
