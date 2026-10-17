using UnityEngine;

// Dessine le personnage en "pixel art" PAR CODE (aucune image importée).
// Chaque caractère d'une ligne = un pixel :  . = transparent, H = cheveux, S = peau,
// E = yeux, M = bouche, B = t-shirt, P = pantalon, O = chaussures.
public static class CharacterSprites
{
    // Haut du corps (12 lignes de 12 pixels), commun à toutes les images
    static readonly string[] top =
    {
        "....HHHH....",
        "...HHHHHH...",
        "..HHHHHHHH..",
        "..HSSSSSSH..",
        "..SSESSESS..",
        "..SSSSSSSS..",
        "...SSMMSS...",
        "....BBBB....",
        "..BBBBBBBB..",
        ".SBBBBBBBBS.",
        ".SBBBBBBBBS.",
        "..BBBBBBBB.."
    };

    // Jambes (4 lignes) : à l'arrêt, et deux positions de marche
    static readonly string[] legsIdle =
    {
        "...PPPPPP...",
        "...PP..PP...",
        "...PP..PP...",
        "...OO..OO..."
    };
    static readonly string[] legsWalkA =
    {
        "...PPPPPP...",
        "..PP....PP..",
        "..PP....PP..",
        "..OO....OO.."
    };
    static readonly string[] legsWalkB =
    {
        "...PPPPPP...",
        "....PPPP....",
        "....PPPP....",
        "....OOOO...."
    };

    static Sprite idle;
    static Sprite[] walk;

    public static Sprite Idle
    {
        get { if (idle == null) Build(); return idle; }
    }

    public static Sprite[] Walk
    {
        get { if (walk == null || walk[0] == null) Build(); return walk; }
    }

    static void Build()
    {
        idle = MakeSprite(Join(top, legsIdle));
        // Cycle de marche : jambes écartées, jambes jointes, jambes serrées, jambes jointes
        walk = new Sprite[]
        {
            MakeSprite(Join(top, legsWalkA)),
            MakeSprite(Join(top, legsIdle)),
            MakeSprite(Join(top, legsWalkB)),
            MakeSprite(Join(top, legsIdle))
        };
    }

    static string[] Join(string[] a, string[] b)
    {
        var r = new string[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }

    static Sprite MakeSprite(string[] rows)
    {
        int h = rows.Length;
        int w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;   // pixels nets (pas de flou)
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int row = 0; row < h; row++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, h - 1 - row, ColorOf(rows[row][x]));   // la ligne 0 est en haut
        tex.Apply();

        // pivot à (0.5, 0.35) : l'origine du joueur est au niveau de ses hanches
        // 11.2 pixels par unité => personnage d'environ 1 unité de haut dans le jeu
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.35f), 11.2f);
    }

    static Color ColorOf(char c)
    {
        switch (c)
        {
            case 'H': return new Color(0.35f, 0.20f, 0.10f);   // cheveux
            case 'S': return new Color(0.96f, 0.76f, 0.60f);   // peau
            case 'E': return new Color(0.10f, 0.10f, 0.10f);   // yeux
            case 'M': return new Color(0.70f, 0.25f, 0.25f);   // bouche
            case 'B': return new Color(0.20f, 0.50f, 0.90f);   // t-shirt
            case 'P': return new Color(0.25f, 0.25f, 0.40f);   // pantalon
            case 'O': return new Color(0.15f, 0.10f, 0.10f);   // chaussures
            default:  return Color.clear;                      // transparent
        }
    }
}
