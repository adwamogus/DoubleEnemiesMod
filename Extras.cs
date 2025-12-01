public static class StringLists
{
    public static readonly string[] Blacklist = new string[]
    {
        "Crystal Drifter",
        "Zap Core Enemy",
        "Music Box Bell",
        "Bone Goomba Bounce Fly",
    };
    public static readonly string[] BlacklistedScenes = new string[]
    {
        "Shellwood_18",
        "Shadow_18",
        "Dust_Chef",
        "Bone_18",
        "Bone_East_08_boss_beastfly",
    };
    public static readonly string[] SyncStates = new string[]
    {
        "Pause", // Lost Lace
        "Dormant",
        "Zoom Down", // Moorwing
        "Spear Spawn Pause", // Coral Tower Spawn
        "Burst Out",
        "Fly In",
        "Jump Away Air",
        "Burst Out?",
        "Spawn Antic",
        "Spawn",
        "BG Dance", // Skarrsinger
        "Challenge Pause",
        "Battle Roar Antic",
        "Battle Roar",
        "Battle Roar End",
        "Battle Dance",
        "Init", // Grandmother
        "Ready",
        "Wall", // Flint bug
        "Ambush Ready",
        "Pointing", // signis
        "Arena Start",
        "Roar",
    };
    public static readonly string[] ParentKeywords = new string[]
    {
        "Dancer Control",
        "Boss Scene",
        "Battle Scene",
        "Muckmen Control",
        "song_golem",
        "First Weaver",
        "Silk Boss",
        "Lace Boss2 New",
    };
    public static readonly string[] BossFilterKeywords = new string[]
    {
        "Dancer Control",
        "Boss Scene",
        "Splinter Queen",
        "song_golem",
        "Vampire Gnat",
        "First Weaver",
        "Silk Boss",
        "Lace Boss2 New",
        "Swamp Shaman",
        "Roachkeeper Chef",
        "Bone Flyer Giant",
        "Lost Lace Boss",
        "Coral Warrior Grey",
    };
    public static readonly string[] ArenaFilterKeywords = new string[]
    {
        "Battle Scene"
    };
}
public enum EnemyType
{
    Enemy,
    Arena,
    Boss
}