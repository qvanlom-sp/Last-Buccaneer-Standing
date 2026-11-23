namespace Blackwake.Game;

public enum ShipKind { Sloop, Brigantine, Frigate, Galleon }

public sealed record ShipSpec(ShipKind Kind, string Name, string Class, int Columns, int Rows, int Decks,
    int Hull, int Speed, int Moves, int Cost)
{
    public bool Contains(DeckPosition cell) =>
        cell.Column >= 0 && cell.Column < Columns &&
        cell.Row >= 0 && cell.Row < Rows &&
        cell.Deck >= 0 && cell.Deck < Decks;

    public float Lane(DeckPosition cell) =>
        (cell.Row - (Rows - 1) * .5f) * 28 + (cell.Deck - (Decks - 1) * .5f) * 9;
}

public readonly record struct DeckPosition(int Column, int Row, int Deck);
public sealed record TreasureSpec(int Gold, int Durability);
public sealed record DeckMount(DeckPosition Position, CannonKind? Cannon, bool Barricade = false,
    int? GunnerId = null, TreasureSpec? Treasure = null);

public sealed class Loadout
{
    public ShipKind Ship { get; set; }
    public List<ShipKind> Fleet { get; set; } = [ShipKind.Sloop];
    public List<int> Crew { get; set; } = [1, 2];
    public List<DeckMount> Mounts { get; set; } = [];
    public int Barricades { get; set; }
}

public sealed record PirateCard(int Id, string Name, string Bio, int Swiftness, int Accuracy, int Cost)
{
    public decimal Overall => (Swiftness + Accuracy) / 2m;
    public float ReloadScale => 1.38f - .68f * Swiftness / 100;
    public float AimSpread => .105f * (1 - Accuracy / 100f);
}

public sealed record BasecampMarket(ShipKind Ship, PirateCard[] Recruits);

public static class Shipyard
{
    public const int BarricadeCost = 34;
    public const int BarricadeHealth = 42;

    private static readonly ShipSpec[] Ships =
    [
        new(ShipKind.Sloop, "The Cutlass", "Sloop", 2, 3, 1, 130, 190, 7, 0),
        new(ShipKind.Brigantine, "The Brazen Wake", "Brigantine", 2, 4, 2, 180, 165, 6, 175),
        new(ShipKind.Frigate, "The Gilded Tempest", "Frigate", 3, 5, 2, 240, 145, 5, 315),
        new(ShipKind.Galleon, "The Widow's Due", "Galleon", 3, 6, 3, 330, 122, 4, 465)
    ];

    private static readonly PirateCard[] Starters =
    [
        new(1, "Nell Flint", "A dockside gunner with a quick hand on the ramrod.", 58, 62, 0),
        new(2, "Tom Rook", "A former lookout who waits for a clean shot.", 49, 68, 0)
    ];

    private static readonly BasecampMarket[] Markets =
    [
        new(ShipKind.Sloop,
        [
            new(10, "Pip Quick", "A runner with fast hands and a restless aim.", 67, 46, 32),
            new(11, "Mara Keel", "An apprentice gunner with a patient eye.", 49, 72, 41),
            new(12, "Ivo Tar", "A harbor guard who can work either end of a gun.", 59, 60, 38)
        ]),
        new(ShipKind.Brigantine,
        [
            new(20, "Ada Wick", "The fort's powder runner can reload in the dark.", 80, 61, 56),
            new(21, "Silas Vane", "A battery gunner trained to pick out gunports.", 60, 83, 66),
            new(22, "Bess Crane", "A veteran of the Saltwatch battery.", 71, 74, 62)
        ]),
        new(ShipKind.Frigate,
        [
            new(30, "Orla Fuse", "The quickest gun captain in the Shoals.", 92, 71, 84),
            new(31, "Jory Glass", "A former marksman who can split a mast stay.", 69, 94, 92),
            new(32, "Cora Bell", "A privateer who drills her broadside every dawn.", 83, 86, 96),
            new(33, "Rafe Hawke", "A siege gunner who spent years on a rolling deck.", 79, 82, 88)
        ]),
        new(ShipKind.Galleon,
        [
            new(40, "Sable Knox", "A boarding veteran who keeps firing under pressure.", 100, 84, 116),
            new(41, "Anne Blackglass", "The last marksman of the Widow's guard.", 81, 100, 120),
            new(42, "Dax Storm", "An old gun captain who times shots between swells.", 94, 96, 132),
            new(43, "Ren Crow", "A master of the heavy guns at Graveshore Keep.", 92, 91, 124)
        ])
    ];

    private static readonly Dictionary<int, PirateCard> Pirates = Starters
        .Concat(Markets.SelectMany(market => market.Recruits)).ToDictionary(card => card.Id);

    public static ShipSpec GetShip(ShipKind kind) => Ships[(int)kind];
    public static PirateCard GetPirate(int id) => Pirates[id];
    public static BasecampMarket GetMarket(SeaTile port) =>
        Markets[port.Encounter.Kind == EncounterKind.Base ? 0 : port.Region + 1];
}
