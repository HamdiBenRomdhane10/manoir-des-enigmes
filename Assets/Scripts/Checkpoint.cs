using UnityEngine;

// Mémoire du checkpoint. Les variables "static" survivent au rechargement d'une scène,
// ce qui permet de reprendre la partie après un Game Over.
public static class Checkpoint
{
    public static bool hasCheckpoint;   // un checkpoint existe-t-il ?
    public static bool resume;          // le joueur a-t-il demandé de reprendre ?
    public static int[] riddleIds;      // les énigmes tirées pour cette partie
    public static bool[] solved;        // lesquelles sont déjà résolues
    public static Vector2 position;     // position du joueur au moment du checkpoint

    public static void Save(int[] ids, bool[] solvedFlags, Vector2 pos)
    {
        riddleIds = (int[])ids.Clone();
        solved = (bool[])solvedFlags.Clone();
        position = pos;
        hasCheckpoint = true;
    }

    public static void Clear()
    {
        hasCheckpoint = false;
        resume = false;
        riddleIds = null;
        solved = null;
    }
}
