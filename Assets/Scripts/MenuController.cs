using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu principal : Jouer / Instructions / Quitter
public class MenuController : MonoBehaviour
{
    GameObject mainPanel;
    GameObject instructionsPanel;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.Get().StartMusic();
        Color bg = new Color(0.08f, 0.09f, 0.14f);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
        }

        var canvas = UIHelper.CreateCanvas();

        // --- Panneau principal ---
        var main = UIHelper.CreateFullScreenPanel(canvas.transform, "MainPanel", bg);
        mainPanel = main.gameObject;

        UIHelper.CreateText(main, "LE MANOIR DES ÉNIGMES", 110, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(1700, 160), new Color(1f, 0.85f, 0.3f));
        UIHelper.CreateText(main, "Résolvez 3 énigmes pour vous échapper !", 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1500, 70), Color.white);

        UIHelper.CreateButton(main, "JOUER", new Vector2(520, 100), new Vector2(0, 20), Jouer);
        UIHelper.CreateButton(main, "INSTRUCTIONS", new Vector2(520, 100), new Vector2(0, -110), AfficherInstructions);
        UIHelper.CreateButton(main, "QUITTER", new Vector2(520, 100), new Vector2(0, -240), Quitter,
            new Color(0.7f, 0.25f, 0.25f));

        // --- Panneau instructions ---
        var instr = UIHelper.CreateFullScreenPanel(canvas.transform, "InstructionsPanel", bg);
        instructionsPanel = instr.gameObject;

        UIHelper.CreateText(instr, "INSTRUCTIONS", 90, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0, 360), new Vector2(1500, 120), new Color(1f, 0.85f, 0.3f));

        string txt =
            "Vous êtes enfermé dans le manoir !\n\n" +
            "• Déplacement : flèches, ZQSD ou WASD\n" +
            "• Approchez-vous d'un bloc JAUNE et appuyez sur E pour lire son énigme\n" +
            "• Tapez la réponse puis validez avec Entrée ou le bouton Valider\n" +
            "• Les énigmes changent à chaque partie !\n" +
            "• Une mauvaise réponse vous fait perdre 5 secondes\n" +
            "• Un checkpoint est enregistré à chaque énigme résolue\n" +
            "• Quand les 3 énigmes sont résolues, la porte de droite devient VERTE : traversez-la !\n" +
            "• Vous avez 1 minute seulement ! Échap = pause (reprendre, menu ou quitter)\n" +
            "• M = couper ou remettre le son";
        UIHelper.CreateText(instr, txt, 38, TextAnchor.UpperLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1500, 600), Color.white);

        UIHelper.CreateButton(instr, "RETOUR", new Vector2(420, 90), new Vector2(0, -400), Retour);
        instructionsPanel.SetActive(false);
    }

    void Jouer()
    {
        Checkpoint.Clear();
        SceneManager.LoadScene("Level1");
    }

    void AfficherInstructions()
    {
        mainPanel.SetActive(false);
        instructionsPanel.SetActive(true);
    }

    void Retour()
    {
        instructionsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    void Quitter()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
