namespace SparklesReborn;

/// <summary>
/// Static storyboard/slideshow placeholders.  These are intentionally data, not hard-coded renderer logic, so the same
/// cutscene beats can later be replaced by video files without changing campaign progression.
/// </summary>
public static class StoryBook
{
    public static IReadOnlyList<StorySlide> Intro { get; } = new[]
    {
        new StorySlide("A PERFECTLY NORMAL MORNING", "Sparkles was enjoying a peaceful pasture, violating no ordinances more serious than those concerning glitter runoff.", "Several witnesses would later dispute the word 'peaceful'.", Biome.Pasture),
        new StorySlide("THEN NIGHT MARE HAPPENED", "Night Mare rolled across Shinnyland with an army of malicious toys, unionized squirrels, undead office supplies and people who take mall swords much too seriously.", "Property values responded immediately.", Biome.Nightmare),
        new StorySlide("THE PLAN", "Run. Jump. Joust. Stomp. Consume fairies under controlled laboratory conditions. Expel rainbows under less controlled conditions. Reach Night Mare.", "This plan received no peer review.", Biome.Night),
        new StorySlide("SPARKLES: TRFU", "The Rainbow Farting Unicorn is now cleared for launch.", "Press anything responsible-looking to continue.", Biome.Pasture)
    };

    public static IReadOnlyList<StorySlide> Ending { get; } = new[]
    {
        new StorySlide("NIGHT MARE: DE-MARED", "The final rainbow shockwave rolls across Shinnyland. Night Mare's reality-editing operation loses both funding and several walls.", "The walls were insured. Reality was not.", Biome.Nightmare),
        new StorySlide("PEACE RETURNS", "Teddy bears become merely judgmental. The squirrels renegotiate. Count Spatula opens a cookware outlet with deeply suspicious evening hours.", "Ed receives a participation ribbon.", Biome.Pasture),
        new StorySlide("THE HERO", "Sparkles returns to the pasture, faces the sunset, and resolves to use tremendous digestive power only for good.", "This resolution lasts approximately eleven seconds.", Biome.Pasture),
        new StorySlide("THE END?", "Secret codes remain. The map does not acknowledge them. The map is a coward.", "Write them down. Share them. Blame the generator.", Biome.Night)
    };

    public static IReadOnlyList<StorySlide> BossIntro(EnemyKind boss)
    {
        return boss switch
        {
            EnemyKind.Ed => One("ED", "A slime has been promoted beyond its competence.", "Ed appears surprised by the health bar.", Biome.Marsh),
            EnemyKind.OldGnarley => One("OLD GNARLEY", "An ancient tree has chosen violence, artillery and extremely poor pruning etiquette.", "Root cause analysis is about to become literal.", Biome.Forest),
            EnemyKind.Spookers => One("SPOOKERS", "The local ghost has unionized with itself and demands one entire crypt.", "Boo, et cetera.", Biome.Crypt),
            EnemyKind.Webbey => One("WEBBEY", "Eight legs. Several hundred meters of web. Zero respect for personal bandwidth.", "Please clear browser history after combat.", Biome.Cave),
            EnemyKind.Mina => One("MINA", "Mina keeps changing shape. None of the shapes are interested in conflict resolution.", "Keep limbs inside reality.", Biome.Ice),
            EnemyKind.CountSpatula => One("COUNT SPATULA", "Lord of the Non-Stick Night, terror of cookware aisles and deeply committed vampire chef.", "The arena has been preheated.", Biome.Fortress),
            EnemyKind.Rip => One("RIP 'THE PIECE MAKER'", "He misunderstood 'peacemaker' years ago and has refused every correction since.", "Expect pieces.", Biome.Weald),
            EnemyKind.NightMare => One("NIGHT MARE", "The horse at the end of all reasonable design documents.", "Reality warranty expires now.", Biome.Nightmare),
            _ => Array.Empty<StorySlide>()
        };
    }

    private static IReadOnlyList<StorySlide> One(string title, string body, string caption, Biome biome)
        => new[] { new StorySlide(title, body, caption, biome) };
}
