using static Blackwake.Game.CannonKind;
using static Blackwake.Game.ShipKind;

namespace Blackwake.Game;

internal sealed record EnemyBlueprint(ShipKind Ship, float HullScale, DeckMount[] Mounts, PirateCard[] Crew)
{
    public PirateCard GetGunner(int id) => Crew[id];
}

internal static class EnemyFleet
{
    // Coordinates are column (inboard to outboard), row (bow to stern), and deck (bottom up).
    private static readonly EnemyBlueprint[] Merchants =
    [
        Build(Sloop, .78f,
            Gun(1, 1, 0, Roundshot, "Finch", 18, 25),
            Chest(1, 0, 0, 55), Chest(0, 1, 0, 70), Chest(1, 2, 0, 45)),
        Build(Brigantine, .77f,
            Gun(0, 2, 1, Roundshot, "Gill", 29, 33), Wall(1, 2, 1),
            Chest(1, 0, 0, 60), Chest(0, 1, 0, 85), Chest(1, 2, 0, 70), Chest(1, 3, 1, 55)),
        Build(Frigate, .75f,
            Gun(1, 2, 0, Roundshot, "Nash", 37, 43), Wall(2, 2, 0),
            Chest(2, 0, 1, 65), Chest(1, 1, 0, 75), Chest(0, 2, 0, 115, 27),
            Chest(2, 3, 1, 80), Chest(2, 4, 0, 70))
    ];

    private static readonly EnemyBlueprint[] Pirates =
    [
        Build(Sloop, .8f,
            Gun(1, 0, 0, Roundshot, "Rusk", 44, 53), Gun(0, 2, 0, Roundshot, "Kit", 49, 47),
            Wall(1, 2, 0)),
        Build(Brigantine, .92f,
            Gun(1, 0, 1, Fire, "Holt", 68, 65), Gun(1, 2, 0, Scatter, "Tess", 64, 61),
            Gun(0, 3, 1, Roundshot, "Penn", 59, 70), Wall(1, 3, 1)),
        Build(Frigate, .94f,
            Gun(1, 0, 1, Heavy, "Grimm", 74, 78), Gun(2, 1, 0, Scatter, "Pike", 79, 72),
            Gun(0, 3, 1, Fire, "Shaw", 76, 84), Gun(2, 4, 0, Roundshot, "Vail", 83, 79),
            Wall(2, 0, 1), Wall(2, 3, 1))
    ];

    private static readonly EnemyBlueprint[] Forts =
    [
        Build(Brigantine, .84f,
            Gun(0, 1, 0, Heavy, "Cole", 37, 45), Gun(0, 2, 1, Roundshot, "Ash", 44, 53),
            Wall(1, 1, 0), Wall(1, 2, 1), Wall(1, 3, 0)),
        Build(Frigate, .94f,
            Gun(0, 1, 1, Heavy, "Fenn", 54, 62), Gun(1, 3, 0, Fire, "Tait", 57, 65),
            Wall(2, 1, 1), Wall(2, 3, 0), Wall(1, 1, 1), Wall(2, 2, 0)),
        Build(Galleon, .96f,
            Gun(0, 1, 2, Heavy, "Slate", 69, 77), Gun(1, 3, 1, Heavy, "Hall", 72, 80),
            Gun(0, 4, 0, Roundshot, "Stone", 77, 76),
            Wall(2, 1, 2), Wall(2, 3, 1), Wall(2, 4, 0), Wall(1, 1, 2), Wall(1, 4, 0), Wall(2, 2, 1))
    ];

    private static readonly EnemyBlueprint[] Overlords =
    [
        Build(Brigantine, .88f,
            Gun(1, 0, 0, Fire, "Venn", 51, 59), Gun(0, 2, 1, Heavy, "Redd", 58, 54),
            Gun(1, 3, 1, Roundshot, "Reed", 56, 62), Wall(1, 2, 1)),
        Build(Frigate, .95f,
            Gun(1, 0, 1, Fire, "Sparks", 69, 74), Gun(2, 1, 0, Scatter, "Mott", 73, 71),
            Gun(0, 2, 0, Heavy, "Crowe", 66, 79), Gun(2, 4, 1, Roundshot, "Vale", 74, 77),
            Wall(2, 0, 1), Wall(2, 2, 0), Wall(1, 2, 0)),
        Build(Galleon, 1.03f,
            Gun(1, 0, 2, Fire, "Thorn", 86, 89), Gun(0, 1, 0, Heavy, "Gore", 85, 93),
            Gun(2, 2, 1, Scatter, "Wren", 90, 87), Gun(1, 3, 2, Fire, "Sable", 94, 88),
            Gun(2, 5, 1, Heavy, "Draik", 89, 96),
            Wall(2, 0, 2), Wall(2, 1, 0), Wall(1, 1, 0), Wall(2, 3, 2))
    ];

    public static EnemyBlueprint For(Encounter encounter) => encounter.Kind switch
    {
        EncounterKind.Merchant => Merchants[encounter.Difficulty],
        EncounterKind.Pirate => Pirates[encounter.Difficulty],
        EncounterKind.Fort => Forts[encounter.Difficulty],
        EncounterKind.Overlord => Overlords[encounter.Difficulty],
        _ => throw new ArgumentException("Only hostile encounters have battle layouts.", nameof(encounter))
    };

    private static EnemyBlueprint Build(ShipKind ship, float hullScale, params Station[] stations)
    {
        var crew = new List<PirateCard>();
        var mounts = new DeckMount[stations.Length];
        for (var index = 0; index < stations.Length; index++)
        {
            var (mount, gunner) = stations[index];
            if (gunner is not null)
            {
                mount = mount with { GunnerId = crew.Count };
                crew.Add(gunner with { Id = crew.Count });
            }
            mounts[index] = mount;
        }
        return new(ship, hullScale, mounts, crew.ToArray());
    }

    private sealed record Station(DeckMount Mount, PirateCard? Gunner = null);
    private static Station Gun(int column, int row, int deck, CannonKind kind, string name, int swiftness, int accuracy) =>
        new(new(new(column, row, deck), kind), new(0, name, "", swiftness, accuracy, 0));
    private static Station Wall(int column, int row, int deck) =>
        new(new(new(column, row, deck), null, Barricade: true));
    private static Station Chest(int column, int row, int deck, int gold, int durability = 20) =>
        new(new(new(column, row, deck), null, Treasure: new(gold, durability)));
}
