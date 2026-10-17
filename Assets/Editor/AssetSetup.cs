using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menu  Tools > Generer prefabs et animations
// 1. règle l'import des images (Assets/Art/Sprites) en "Sprite"
// 2. crée les animations (.anim) et l'Animator Controller du personnage
// 3. crée les Prefabs (Player, RiddleBlock, Door, Wall, Floor) dans Assets/Resources/Prefabs
public static class AssetSetup
{
    const string Art = "Assets/Art/Sprites/";
    const string PrefabDir = "Assets/Resources/Prefabs/";
    const string AnimDir = "Assets/Animations/";

    [MenuItem("Tools/Generer prefabs et animations")]
    public static void Generate()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Arrêtez le jeu", "Cliquez sur Play pour arrêter le jeu, puis relancez cet outil.", "OK");
            return;
        }

        AssetDatabase.Refresh();
        Directory.CreateDirectory("Assets/Resources/Prefabs");
        Directory.CreateDirectory("Assets/Animations");

        // ---------- 1. Import des images ----------
        SetupSprite("player_idle", 19, false);
        for (int i = 0; i < 4; i++) SetupSprite("player_walk_" + i, 19, false);
        SetupSprite("block_closed", 20, false);
        SetupSprite("block_solved", 20, false);
        SetupSprite("door_closed", 14, false);
        SetupSprite("door_open", 14, false);
        SetupSprite("wall_tile", 16, true);     // tuile répétée : mode Repeat + FullRect
        SetupSprite("floor_tile", 16, true);
        AssetDatabase.Refresh();

        var idle = LoadSprite("player_idle");
        var walk = new[] { LoadSprite("player_walk_0"), LoadSprite("player_walk_1"),
                           LoadSprite("player_walk_2"), LoadSprite("player_walk_3") };
        var blockClosed = LoadSprite("block_closed");
        var blockSolved = LoadSprite("block_solved");
        var doorClosed = LoadSprite("door_closed");
        var doorOpen = LoadSprite("door_open");
        var wallTile = LoadSprite("wall_tile");
        var floorTile = LoadSprite("floor_tile");

        if (idle == null || walk[0] == null || blockClosed == null || doorClosed == null || wallTile == null)
        {
            EditorUtility.DisplayDialog("Images introuvables",
                "Vérifiez que le dossier Assets/Art/Sprites contient les images (.png), attendez la fin de l'import puis relancez.", "OK");
            return;
        }

        // ---------- 2. Animations + Animator Controller ----------
        var idleClip = MakeClip("Player_Idle", new[] { idle }, 2f);
        var walkClip = MakeClip("Player_Walk", walk, 8f);
        var controller = MakeController(idleClip, walkClip);

        // ---------- 3. Prefabs ----------
        MakePlayerPrefab(idle, controller);
        MakeBlockPrefab(blockClosed, blockSolved);
        MakeDoorPrefab(doorClosed, doorOpen);
        MakeTiledPrefab("Wall", wallTile, 0);
        MakeTiledPrefab("Floor", floorTile, -10);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Terminé",
            "Prefabs créés dans Assets/Resources/Prefabs\nAnimations dans Assets/Animations\n\nOuvrez MainMenu et appuyez sur Play !", "OK");
    }

    // ---------- Import ----------
    static void SetupSprite(string name, float ppu, bool tile)
    {
        string path = Art + name + ".png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;                       // pixels nets
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteMeshType = tile ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();
    }

    static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
    }

    // ---------- Animation ----------
    // Crée un clip qui change l'image (Sprite) du SpriteRenderer de l'enfant "Visuel"
    static AnimationClip MakeClip(string name, Sprite[] frames, float fps)
    {
        string path = AnimDir + name + ".anim";
        AssetDatabase.DeleteAsset(path);

        var clip = new AnimationClip { frameRate = fps };
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "Visuel",
            propertyName = "m_Sprite"
        };

        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // Animator Controller : état Idle <-> Walk selon le paramètre "Speed"
    static AnimatorController MakeController(AnimationClip idleClip, AnimationClip walkClip)
    {
        string path = AnimDir + "Player.controller";
        AssetDatabase.DeleteAsset(path);

        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = c.layers[0].stateMachine;

        var sIdle = sm.AddState("Idle");
        sIdle.motion = idleClip;
        var sWalk = sm.AddState("Walk");
        sWalk.motion = walkClip;
        sm.defaultState = sIdle;

        var toWalk = sIdle.AddTransition(sWalk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = sWalk.AddTransition(sIdle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        return c;
    }

    // ---------- Prefabs ----------
    static GameObject MakeVisualChild(GameObject parent, Sprite sprite, int order)
    {
        var v = new GameObject("Visuel");
        v.transform.SetParent(parent.transform, false);
        var sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return v;
    }

    static void Save(GameObject go, string prefabName)
    {
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + prefabName + ".prefab");
        Object.DestroyImmediate(go);
    }

    static void MakePlayerPrefab(Sprite idle, AnimatorController controller)
    {
        var go = new GameObject("Player");
        var vis = MakeVisualChild(go, idle, 5);

        var col = go.AddComponent<BoxCollider2D>();      // collider aux pieds
        col.size = new Vector2(0.5f, 0.4f);
        col.offset = new Vector2(0f, -0.3f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        var pc = go.AddComponent<PlayerController>();
        pc.visual = vis.transform;
        Save(go, "Player");
    }

    static void MakeBlockPrefab(Sprite closed, Sprite solved)
    {
        var go = new GameObject("RiddleBlock");
        var vis = MakeVisualChild(go, closed, 1);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);
        var r = go.AddComponent<RiddleObject>();
        r.visual = vis.transform;
        r.solvedSprite = solved;
        Save(go, "RiddleBlock");
    }

    static void MakeDoorPrefab(Sprite closed, Sprite open)
    {
        var go = new GameObject("Door");
        var vis = MakeVisualChild(go, closed, 1);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 1.7f);
        col.isTrigger = true;
        var d = go.AddComponent<ExitTrigger>();
        d.visual = vis.transform;
        d.openSprite = open;
        Save(go, "Door");
    }

    // Mur ou sol : une image répétée (mode Tiled) sur la taille voulue
    static void MakeTiledPrefab(string name, Sprite tile, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = tile;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = Vector2.one;
        sr.sortingOrder = order;
        if (name == "Wall")
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }
        Save(go, name);
    }
}
