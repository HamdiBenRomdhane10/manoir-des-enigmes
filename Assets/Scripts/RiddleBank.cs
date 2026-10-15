using System.Collections.Generic;
using UnityEngine;

// Une énigme : une question + les réponses acceptées.
public class RiddleData
{
    public string question;
    public string[] answers;

    public RiddleData(string question, params string[] answers)
    {
        this.question = question;
        this.answers = answers;
    }
}

// Banque de questions. À chaque partie, 3 énigmes sont tirées au hasard,
// différentes de celles de la partie précédente.
public static class RiddleBank
{
    public static readonly List<RiddleData> All = new List<RiddleData>
    {
        new RiddleData("Plus on en prend, plus on en laisse derrière soi.\nQue suis-je ?", "pas"),
        new RiddleData("Je n'ai pas de voix, et pourtant je réponds quand on me parle.\nQue suis-je ?", "echo"),
        new RiddleData("Quel est le nombre suivant ?\n2 - 4 - 8 - 16 - ...", "32", "trente-deux", "trente deux"),
        new RiddleData("Je suis grand quand je suis jeune, et petit quand je suis vieux.\nQue suis-je ?", "bougie"),
        new RiddleData("Qu'est-ce qui a des touches mais n'ouvre aucune porte ?", "piano", "clavier"),
        new RiddleData("Plus il y en a, moins on voit.\nQue suis-je ?", "obscurite", "noir", "tenebres"),
        new RiddleData("Combien font 7 x 8 ?", "56", "cinquante-six", "cinquante six"),
        new RiddleData("Quel est le nombre suivant ?\n1 - 1 - 2 - 3 - 5 - 8 - ...", "13", "treize"),
        new RiddleData("Qu'est-ce qui monte et descend sans jamais bouger ?", "escalier", "escaliers"),
        new RiddleData("J'ai des villes sans maisons, des forêts sans arbres et de l'eau sans poissons.\nQue suis-je ?", "carte"),
        new RiddleData("Combien font 15 + 27 ?", "42", "quarante-deux", "quarante deux"),
        new RiddleData("Je suis toujours devant toi, mais tu ne peux jamais me voir.\nQue suis-je ?", "futur", "avenir")
    };

    static int[] lastPlayed = new int[0];

    // Tire "count" énigmes différentes, en évitant celles de la partie précédente.
    public static int[] PickRandom(int count)
    {
        var pool = new List<int>();
        for (int i = 0; i < All.Count; i++)
            if (System.Array.IndexOf(lastPlayed, i) < 0) pool.Add(i);

        if (pool.Count < count)   // sécurité : pas assez de questions "neuves"
        {
            pool.Clear();
            for (int i = 0; i < All.Count; i++) pool.Add(i);
        }

        var result = new int[count];
        for (int k = 0; k < count; k++)
        {
            int r = Random.Range(0, pool.Count);
            result[k] = pool[r];
            pool.RemoveAt(r);
        }
        lastPlayed = result;
        return result;
    }

    // Nettoie une réponse : minuscules, sans accents, sans ponctuation, sans article.
    public static string Clean(string s)
    {
        s = s.Trim().ToLower().Replace('’', '\'');
        s = s.Replace('é', 'e').Replace('è', 'e').Replace('ê', 'e').Replace('ë', 'e')
             .Replace('à', 'a').Replace('â', 'a').Replace('î', 'i').Replace('ï', 'i')
             .Replace('ô', 'o').Replace('ù', 'u').Replace('û', 'u').Replace('ç', 'c');
        s = s.Trim(new char[] { ' ', '.', '!', '?' });

        string[] articles = { "l'", "le ", "la ", "les ", "un ", "une ", "des ", "du " };
        foreach (var art in articles)
        {
            if (s.StartsWith(art)) { s = s.Substring(art.Length); break; }
        }
        return s.Trim();
    }
}
