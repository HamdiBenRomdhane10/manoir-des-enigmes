using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gère tout le Level 1 : construction du niveau, énigmes aléatoires, timer,
// checkpoint, Win / GameOver.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    const int RiddleCount = 3;

    // Réglages (constantes : on les change ici dans le code)
    const float TimeLimit = 90f;       // durée d'une nouvelle partie : 1 minute
    const float ResumeTime = 30f;      // temps donné quand on reprend au checkpoint
    const float WrongPenalty = 5f;     // pénalité par mauvaise réponse (secondes)

    Sprite squareSprite;
    Camera cam;
    PlayerController player;
    ExitTrigger exitDoor;
    readonly List<RiddleObject> riddles = new List<RiddleObject>();

    // Positions fixes des 3 blocs d'énigme et du départ
    readonly Vector2[] riddlePositions =
    {
        new Vector2(-5.5f, 2.5f), new Vector2(0f, 2.5f), new Vector2(5.5f, -2.5f)
    };
    readonly Vector2 startPos = new Vector2(-6.5f, -2.5f);

    int[] ids;                 // numéros des 3 énigmes tirées dans RiddleBank
    int solved = 0;
    float timeLeft;
    bool playing = true;
    bool panelOpen = false;
    bool paused = false;
    RiddleObject current;
    Coroutine messageRoutine;

    // UI
    Text timerText, progressText, hintText, messageText;
    GameObject riddlePanel, endPanel, pausePanel;
    Text riddleQuestion, riddleFeedback, endTitle, endSubtitle;
    InputField answerField;
    Button resumeButton, replayButton, menuButton;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        squareSprite = MakeSquareSprite();

        // Reprise au checkpoint, ou nouvelle partie avec des énigmes tirées au hasard ?
        bool resuming = Checkpoint.resume && Checkpoint.hasCheckpoint;
        Checkpoint.resume = false;
        if (resuming)
        {
            ids = Checkpoint.riddleIds;
        }
        else
        {
            Checkpoint.Clear();
            ids = RiddleBank.PickRandom(RiddleCount);
        }
        timeLeft = resuming ? ResumeTime : TimeLimit;

        SetupCamera();
        BuildWorld(resuming);
        BuildUI();
        UpdateProgress();
        if (resuming) ShowMessage("Checkpoint chargé ! Il vous reste " + ResumeTime + " secondes.");
    }

    // ---------- Construction du monde ----------

    Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(4, 4);
        var cols = new Color[16];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        // 4 pixels = 1 unité => le sprite fait 1x1 unité (on le met à l'échelle ensuite)
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        cam.transform.position = new Vector3(0, 0, -10);
    }

    // Boîte simple : sprite coloré (+ collider si demandé). Sert pour le sol et les murs.
    GameObject MakeBox(string name, Vector2 pos, Vector2 scale, Color color, bool collider, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = color;
        sr.sortingOrder = order;
        if (collider) go.AddComponent<BoxCollider2D>();
        return go;
    }

    // Crée un enfant "Visuel" qui porte le sprite : on peut l'animer
    // sans déformer le collider du parent.
    Transform MakeVisual(Transform parent, Color color, int order)
    {
        var v = new GameObject("Visuel");
        v.transform.SetParent(parent, false);
        var sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = color;
        sr.sortingOrder = order;
        return v.transform;
    }

    void BuildWorld(bool resuming)
    {
        Color wallColor = new Color(0.45f, 0.35f, 0.3f);

        // Sol
        MakeBox("Sol", Vector2.zero, new Vector2(16, 8), new Color(0.18f, 0.2f, 0.28f), false, -10);

        // Murs extérieurs
        MakeBox("MurHaut", new Vector2(0, 4.25f), new Vector2(17, 0.5f), wallColor, true, 0);
        MakeBox("MurBas", new Vector2(0, -4.25f), new Vector2(17, 0.5f), wallColor, true, 0);
        MakeBox("MurGauche", new Vector2(-8.25f, 0), new Vector2(0.5f, 9), wallColor, true, 0);
        MakeBox("MurDroit", new Vector2(8.25f, 0), new Vector2(0.5f, 9), wallColor, true, 0);

        // Murs intérieurs (forment un parcours en "S")
        MakeBox("MurInterieurA", new Vector2(-2.5f, 2f), new Vector2(0.5f, 4), wallColor, true, 0);
        MakeBox("MurInterieurB", new Vector2(2.5f, -2f), new Vector2(0.5f, 4), wallColor, true, 0);

        // Les 3 énigmes (tirées au hasard, ou rechargées depuis le checkpoint)
        for (int i = 0; i < RiddleCount; i++)
        {
            var r = CreateRiddle("Enigme" + (i + 1), riddlePositions[i], ids[i]);
            if (resuming && Checkpoint.solved != null && Checkpoint.solved[i])
            {
                r.MarkSolved(false);
                solved++;
            }
        }

        // Porte de sortie (rouge = verrouillée)
        var exit = new GameObject("PorteSortie");
        exit.transform.position = new Vector2(7.6f, 2.5f);
        exit.transform.localScale = new Vector3(0.6f, 1.8f, 1f);
        var col = exit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        exitDoor = exit.AddComponent<ExitTrigger>();
        exitDoor.visual = MakeVisual(exit.transform, new Color(0.85f, 0.2f, 0.2f), 1);
        if (solved >= RiddleCount) exitDoor.Open();

        // Joueur (au départ, ou à la position du checkpoint)
        Vector2 pos = resuming ? Checkpoint.position : startPos;
        var p = new GameObject("Joueur");
        p.transform.position = pos;
        p.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        p.AddComponent<BoxCollider2D>();
        var rb = p.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        player = p.AddComponent<PlayerController>();
        player.visual = MakeVisual(p.transform, new Color(0.3f, 0.8f, 1f), 5);
    }

    RiddleObject CreateRiddle(string name, Vector2 pos, int bankId)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        go.AddComponent<BoxCollider2D>();
        var r = go.AddComponent<RiddleObject>();
        r.bankId = bankId;
        r.question = RiddleBank.All[bankId].question;
        r.answers = RiddleBank.All[bankId].answers;
        r.visual = MakeVisual(go.transform, new Color(1f, 0.85f, 0.2f), 1);
        riddles.Add(r);
        return r;
    }

    // ---------- Interface ----------

    void BuildUI()
    {
        var canvas = UIHelper.CreateCanvas();
        Transform c = canvas.transform;

        timerText = UIHelper.CreateText(c, "", 48, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(30, -25), new Vector2(700, 70), Color.white);
        progressText = UIHelper.CreateText(c, "", 48, TextAnchor.UpperRight,
            new Vector2(1, 1), new Vector2(-30, -25), new Vector2(700, 70), Color.white);
        hintText = UIHelper.CreateText(c, "Appuyez sur E pour examiner", 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1200, 70), new Color(1f, 0.9f, 0.3f));
        hintText.gameObject.SetActive(false);
        messageText = UIHelper.CreateText(c, "", 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(1500, 70), new Color(1f, 0.7f, 0.3f));

        // Bouton PAUSE toujours visible en haut au centre (Échap fait la même chose)
        UIHelper.CreateButton(c, "PAUSE", new Vector2(260, 70), new Vector2(0, -25), TogglePause,
            new Color(0.3f, 0.3f, 0.4f), new Vector2(0.5f, 1f));

        // --- Panneau d'énigme ---
        var overlay = UIHelper.CreateFullScreenPanel(c, "RiddleOverlay", new Color(0, 0, 0, 0.7f));
        riddlePanel = overlay.gameObject;
        var box = UIHelper.CreatePanel(overlay, "Box", new Color(0.15f, 0.17f, 0.25f), new Vector2(1200, 640));

        riddleQuestion = UIHelper.CreateText(box, "", 46, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1100, 260), Color.white);
        answerField = UIHelper.CreateInputField(box, new Vector2(800, 80), new Vector2(0, -40));
        answerField.onEndEdit.AddListener(OnEndEdit);
        UIHelper.CreateButton(box, "VALIDER", new Vector2(360, 80), new Vector2(-220, -170), SubmitAnswer);
        UIHelper.CreateButton(box, "FERMER", new Vector2(360, 80), new Vector2(220, -170), ClosePanel,
            new Color(0.5f, 0.5f, 0.55f));
        riddleFeedback = UIHelper.CreateText(box, "", 36, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(1100, 60), new Color(1f, 0.4f, 0.4f));
        riddlePanel.SetActive(false);

        // --- Panneau de fin (Win / GameOver) ---
        var end = UIHelper.CreateFullScreenPanel(c, "EndOverlay", new Color(0, 0, 0, 0.85f));
        endPanel = end.gameObject;
        endTitle = UIHelper.CreateText(end, "", 110, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1500, 160), Color.white);
        endSubtitle = UIHelper.CreateText(end, "", 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(1500, 80), Color.white);

        resumeButton = UIHelper.CreateButton(end, "REPRENDRE AU CHECKPOINT", new Vector2(760, 100),
            new Vector2(0, -20), ResumeFromCheckpoint, new Color(0.25f, 0.65f, 0.4f));
        replayButton = UIHelper.CreateButton(end, "REJOUER (NOUVELLES ÉNIGMES)", new Vector2(760, 100),
            new Vector2(0, -140), Replay);
        menuButton = UIHelper.CreateButton(end, "MENU PRINCIPAL", new Vector2(760, 100),
            new Vector2(0, -260), BackToMenu, new Color(0.5f, 0.5f, 0.55f));
        endPanel.SetActive(false);

        // --- Panneau de pause (Reprendre / Menu / Quitter) ---
        var pause = UIHelper.CreateFullScreenPanel(c, "PauseOverlay", new Color(0, 0, 0, 0.85f));
        pausePanel = pause.gameObject;
        UIHelper.CreateText(pause, "PAUSE", 110, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1500, 160), Color.white);
        UIHelper.CreateButton(pause, "REPRENDRE", new Vector2(620, 100), new Vector2(0, 60), TogglePause);
        UIHelper.CreateButton(pause, "MENU PRINCIPAL", new Vector2(620, 100), new Vector2(0, -60), BackToMenu,
            new Color(0.5f, 0.5f, 0.55f));
        UIHelper.CreateButton(pause, "QUITTER LE JEU", new Vector2(620, 100), new Vector2(0, -180), QuitGame,
            new Color(0.7f, 0.25f, 0.25f));
        pausePanel.SetActive(false);
    }

    // ---------- Boucle de jeu ----------

    void Update()
    {
        // Caméra responsive : on s'assure que toute la salle reste visible
        if (cam != null) cam.orthographicSize = Mathf.Max(5f, 8.8f / cam.aspect);

        if (!playing) return;

        // En pause : seule la touche Échap fait reprendre (le temps est figé)
        if (paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateTimer();
            EndGame(false);
            return;
        }
        UpdateTimer();

        if (panelOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ClosePanel();
            return;
        }

        // Échap en jeu : ouvre le menu pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
            return;
        }

        // Énigme la plus proche non résolue
        RiddleObject near = null;
        float best = 1.6f;
        foreach (var r in riddles)
        {
            if (r.solved) continue;
            float d = Vector2.Distance(player.transform.position, r.transform.position);
            if (d < best) { best = d; near = r; }
        }
        hintText.gameObject.SetActive(near != null);
        if (near != null && Input.GetKeyDown(KeyCode.E)) OpenPanel(near);
    }

    void UpdateTimer()
    {
        timerText.text = "Temps : " + Mathf.CeilToInt(timeLeft) + " s";
        timerText.color = timeLeft <= 15f ? new Color(1f, 0.4f, 0.4f) : Color.white;
    }

    void UpdateProgress()
    {
        progressText.text = "Énigmes : " + solved + " / " + RiddleCount;
    }

    // ---------- Énigmes ----------

    void OpenPanel(RiddleObject r)
    {
        current = r;
        panelOpen = true;
        player.canMove = false;
        hintText.gameObject.SetActive(false);
        riddleQuestion.text = r.question;
        riddleFeedback.text = "";
        riddlePanel.SetActive(true);
        StartCoroutine(FocusField());
    }

    IEnumerator FocusField()
    {
        yield return null; // on attend 1 frame pour que le "E" ne s'écrive pas dans le champ
        answerField.text = "";
        answerField.ActivateInputField();
        answerField.Select();
    }

    void OnEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SubmitAnswer();
    }

    public void SubmitAnswer()
    {
        if (!panelOpen || current == null || !playing) return;

        if (current.IsCorrect(answerField.text))
        {
            current.MarkSolved();
            solved++;
            UpdateProgress();

            // CHECKPOINT : on mémorise la progression et la position du joueur
            Checkpoint.Save(ids, GetSolvedFlags(), player.transform.position);

            ClosePanel();
            if (solved >= RiddleCount)
            {
                exitDoor.Open();
                ShowMessage("Toutes les énigmes sont résolues ! Courez vers la porte !");
            }
            else
            {
                ShowMessage("Bonne réponse ! Checkpoint enregistré.");
            }
        }
        else
        {
            timeLeft -= WrongPenalty;
            riddleFeedback.text = "Mauvaise réponse ! -" + WrongPenalty + " secondes";
            answerField.text = "";
            answerField.ActivateInputField();
        }
    }

    bool[] GetSolvedFlags()
    {
        var flags = new bool[riddles.Count];
        for (int i = 0; i < riddles.Count; i++) flags[i] = riddles[i].solved;
        return flags;
    }

    public void ClosePanel()
    {
        panelOpen = false;
        current = null;
        riddlePanel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (playing) player.canMove = true;
    }

    // ---------- Sortie / Fin ----------

    public void TryExit()
    {
        if (!playing) return;
        if (solved >= RiddleCount) EndGame(true);
        else ShowMessage("Porte verrouillée : résolvez les " + RiddleCount + " énigmes d'abord !");
    }

    void EndGame(bool win)
    {
        if (!playing) return;
        ClosePanel();
        playing = false;
        player.canMove = false;
        hintText.gameObject.SetActive(false);
        if (win) Checkpoint.Clear();

        endPanel.SetActive(true);
        bool canResume = !win && Checkpoint.hasCheckpoint;
        resumeButton.gameObject.SetActive(canResume);
        // Si le bouton "Reprendre" est caché, on remonte les deux autres boutons
        ((RectTransform)replayButton.transform).anchoredPosition = new Vector2(0, canResume ? -140 : -20);
        ((RectTransform)menuButton.transform).anchoredPosition = new Vector2(0, canResume ? -260 : -140);

        if (win)
        {
            endTitle.text = "VICTOIRE !";
            endTitle.color = new Color(0.4f, 1f, 0.5f);
            endSubtitle.text = "Vous vous êtes échappé avec " + Mathf.CeilToInt(timeLeft) + " secondes restantes.";
        }
        else
        {
            endTitle.text = "GAME OVER";
            endTitle.color = new Color(1f, 0.4f, 0.4f);
            endSubtitle.text = "Le temps est écoulé... Vous restez prisonnier du manoir.";
        }
    }

    void ResumeFromCheckpoint()
    {
        Checkpoint.resume = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Replay()
    {
        Checkpoint.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------- Pause / Quitter ----------

    public void TogglePause()
    {
        if (!playing || panelOpen) return;
        paused = !paused;
        pausePanel.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;   // 0 = tout le jeu est figé
        player.canMove = !paused;
        if (paused) hintText.gameObject.SetActive(false);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void BackToMenu()
    {
        Time.timeScale = 1f;
        Checkpoint.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    void ShowMessage(string msg)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(msg));
    }

    IEnumerator MessageRoutine(string msg)
    {
        messageText.text = msg;
        yield return new WaitForSeconds(2.5f);
        messageText.text = "";
    }
}
