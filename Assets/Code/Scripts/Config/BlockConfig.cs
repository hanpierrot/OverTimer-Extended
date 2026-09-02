using UnityEngine;

/// <summary>One block type: how tough it is, what the pickaxe does to it, and its sprite.</summary>
[System.Serializable]
public class BlockTypeDefinition
{
    public string label = "Dirt";
    public int hp = 1;
    public int pickaxeDamage = 1;
    public Sprite sprite;
}

/// <summary>
/// Ordered block type definitions for the mining vein - index 0 is the first
/// block hit, index N-1 the last. A vein of length L walks blocks[0..L-1] in
/// order (fixed, not randomized), so a run always progresses dirt -> ... ->
/// ruby. Payout stays position-indexed in GameConfig.blockPayout (DESIGN.md
/// §5.2) - hp/pickaxeDamage here are a separate axis of difficulty.
/// </summary>
[CreateAssetMenu(fileName = "BlockConfig", menuName = "Overtime/Block Config")]
public class BlockConfig : ScriptableObject
{
    public BlockTypeDefinition[] blocks;
}
