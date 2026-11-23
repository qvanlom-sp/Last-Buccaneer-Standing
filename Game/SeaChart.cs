namespace Blackwake.Game;

public enum EncounterKind { Water, Base, Merchant, Pirate, Fort, Overlord }
public sealed record Region(string Name, string Color);
public sealed record Encounter(EncounterKind Kind, string Name, int Hull, int Reward, int Difficulty);
public sealed record SeaTile(int Id, int Column, int Row, int Region, Encounter Encounter)
{
    public int X => 425 + (Column - Row) * 51;
    public int Y => 46 + (Column + Row) * 24;
}

public sealed class SeaChart
{
    public const int Columns = 9;
    public const int Rows = 7;
    public const int Home = 3 * Columns;
    public static readonly Region[] Regions =
    [
        new("Brasswater Reach", "#62d9c9"),
        new("The Gilded Shoals", "#e3ae64"),
        new("Widow's Current", "#b7a2ec")
    ];

    private static readonly (int Column, int Row)[][] Sites =
    [
        [(1, 3), (1, 1), (2, 4), (2, 2)],
        [(3, 4), (4, 1), (4, 5), (5, 3)],
        [(6, 2), (7, 5), (7, 1), (8, 3)]
    ];
    private static readonly Encounter Water = new(EncounterKind.Water, "Open water", 0, 0, 0);
    private static readonly Encounter HomePort = new(EncounterKind.Base, "Smuggler's Rest", 0, 0, 0);
    private static readonly string[] Overlords = ["Captain Cinder", "Admiral Voss", "The Widow Queen"];
    private static readonly string[] Forts = ["Saltwatch Fort", "Giltstone Battery", "Graveshore Keep"];
    private static readonly string[] Pirates = ["The Ragged Jackal", "The Brass Viper", "The Hollow Crown"];
    private static readonly string[] Merchants = ["The Copper Gull", "The Golden Ledger", "The Velvet Fortune"];

    public SeaChart(int seed)
    {
        var random = new Random(seed);
        var encounters = new Dictionary<int, Encounter> { [Home] = HomePort };
        for (var region = 0; region < Regions.Length; region++)
        {
            Encounter[] roster =
            [
                new(EncounterKind.Merchant, Merchants[region], 120 + region * 45, 0, region),
                new(EncounterKind.Pirate, Pirates[region], 96 + region * 54, 70 + region * 30, region),
                new(EncounterKind.Fort, Forts[region], 122 + region * 54, 105 + region * 35, region),
                new(EncounterKind.Overlord, Overlords[region], 142 + region * 68, 165 + region * 60, region)
            ];
            random.Shuffle(roster);
            for (var i = 0; i < roster.Length; i++)
            {
                var (column, row) = Sites[region][i];
                encounters.Add(row * Columns + column, roster[i]);
            }
        }

        Tiles = Enumerable.Range(0, Columns * Rows)
            .Select(id => new SeaTile(id, id % Columns, id / Columns, id % Columns / 3,
                encounters.GetValueOrDefault(id, Water)))
            .ToArray();
    }

    public SeaTile[] Tiles { get; }

    public IEnumerable<int> Neighbors(int id)
    {
        var tile = Tiles[id];
        if (tile.Column > 0) yield return id - 1;
        if (tile.Column < Columns - 1) yield return id + 1;
        if (tile.Row > 0) yield return id - Columns;
        if (tile.Row < Rows - 1) yield return id + Columns;
    }
}
