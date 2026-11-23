using System.Text.Json.Serialization;

namespace Blackwake.Game;

public enum CannonKind { Roundshot, Heavy, Scatter, Fire }
public enum UpgradeKind { Hull, Powder, GunCrew, Helmsman }

public sealed record CannonSpec(string Name, string Description, int Cost, float Damage, float Reload, float Speed);
public sealed record UpgradeSpec(string Name, string Description, int Cost);

public sealed class CampaignSave
{
    public int Seed { get; set; } = Random.Shared.Next();
    public int Day { get; set; } = 1;
    public int Moves { get; set; } = Campaign.DailyMoves;
    public int Gold { get; set; } = 145;
    public int Position { get; set; } = SeaChart.Home;
    public int BaseCamp { get; set; } = SeaChart.Home;
    public CannonKind?[] Layout { get; set; } = [null, CannonKind.Roundshot, CannonKind.Roundshot, null, null];
    public int[] Stock { get; set; } = [2, 0, 0, 0];
    public int[] Upgrades { get; set; } = [0, 0, 0, 0];
    public HashSet<int> Revealed { get; set; } = [];
    public HashSet<int> Cleared { get; set; } = [];
    public BattleReport? Report { get; set; }
}

[JsonSerializable(typeof(CampaignSave))]
internal partial class SaveJson : JsonSerializerContext;

public sealed record BattleReport(string Title, string Detail);

public sealed class Campaign
{
    public const int DailyMoves = 7;
    public const int MaxUpgrade = 3;
    public static readonly CannonSpec[] Cannons =
    [
        new("Roundshot", "Balanced damage and reload", 30, 11, 1.12f, 310),
        new("Heavy bomb", "Slow, hard-hitting shot", 72, 25, 1.9f, 228),
        new("Grapeshot", "Three pellets in a spread", 59, 5, 1.25f, 330),
        new("Fire cannon", "Ignites the hull on impact", 86, 13, 1.42f, 285)
    ];
    public static readonly UpgradeSpec[] Improvements =
    [
        new("Reinforced hull", "+30 maximum hull per level", 60),
        new("Powder stores", "+20% shot damage per level", 65),
        new("Gun crew", "12% shorter reload per level", 55),
        new("Helmsman", "+40 steering speed per level", 45)
    ];

    public Campaign(CampaignSave? save = null)
    {
        Save = save ?? new();
        Chart = new(Save.Seed);
        Reveal();
    }

    public CampaignSave Save { get; }
    public SeaChart Chart { get; }
    public SeaTile Here => Chart.Tiles[Save.Position];
    public bool IsThreat(SeaTile tile) => tile.Encounter.Kind >= EncounterKind.Merchant && !Save.Cleared.Contains(tile.Id);
    public bool Controls(int region) => Save.Cleared.Any(id =>
        Chart.Tiles[id].Region == region && Chart.Tiles[id].Encounter.Kind == EncounterKind.Overlord);
    public int Conquered => Enumerable.Range(0, SeaChart.Regions.Length).Count(Controls);
    public int Hull => 130 + 30 * Level(UpgradeKind.Hull);
    public int Level(UpgradeKind upgrade) => Save.Upgrades[(int)upgrade];
    public int UpgradeCost(UpgradeKind upgrade) => Improvements[(int)upgrade].Cost + Level(upgrade) * 40;
    public int Available(CannonKind cannon) => Save.Stock[(int)cannon] - Save.Layout.Count(item => item == cannon);
    public bool HasCannon => Save.Layout.Any(item => item.HasValue);

    public bool CanSail(int destination) => Save.Moves > 0 && Chart.Neighbors(Save.Position).Contains(destination);

    public bool Sail(int destination)
    {
        if (!CanSail(destination)) return false;
        Save.Position = destination;
        Save.Moves--;
        Save.Report = null;
        Reveal();
        if (Here.Encounter.Kind == EncounterKind.Base ||
            Here.Encounter.Kind == EncounterKind.Fort && Save.Cleared.Contains(destination))
        {
            Save.BaseCamp = destination;
        }
        return true;
    }

    public void NextDay()
    {
        Save.Day++;
        Save.Moves = DailyMoves;
    }

    public bool Buy(CannonKind cannon)
    {
        var cost = Cannons[(int)cannon].Cost;
        if (Save.Gold < cost) return false;
        Save.Gold -= cost;
        Save.Stock[(int)cannon]++;
        var slot = Array.IndexOf(Save.Layout, null);
        if (slot >= 0) Save.Layout[slot] = cannon;
        return true;
    }

    public bool Fit(int slot, CannonKind? cannon)
    {
        if (cannon is { } selected && Save.Layout[slot] != cannon && Available(selected) <= 0) return false;
        Save.Layout[slot] = cannon;
        return true;
    }

    public bool Upgrade(UpgradeKind upgrade)
    {
        var cost = UpgradeCost(upgrade);
        if (Save.Gold < cost || Level(upgrade) >= MaxUpgrade) return false;
        Save.Gold -= cost;
        Save.Upgrades[(int)upgrade]++;
        return true;
    }

    public void Resolve(Battle battle)
    {
        if (battle.Result == BattleResult.InProgress || battle.Encounter != Here.Encounter) return;

        if (battle.Result == BattleResult.Defeat)
        {
            var loss = (Save.Gold + 4) / 5;
            // Merchant loot already recovered stays aboard, even if the raid ends in defeat.
            Save.Gold += battle.Loot - loss;
            Save.Position = Save.BaseCamp;
            Save.Report = new("Driven back to port",
                $"Lost {loss} doubloons and returned to {Here.Encounter.Name}."
                + (battle.Loot > 0 ? $" Recovered {battle.Loot} from the merchant." : ""));
            return;
        }

        var target = Here.Encounter;
        Save.Gold += target.Reward + battle.Loot;
        Save.Cleared.Add(Here.Id);
        if (target.Kind == EncounterKind.Fort) Save.BaseCamp = Here.Id;
        Save.Report = target.Kind switch
        {
            EncounterKind.Merchant => new("Raid complete", $"Recovered {battle.Loot} doubloons from {target.Name}."),
            EncounterKind.Fort => new("Island captured", $"+{target.Reward} doubloons. {target.Name} is your new basecamp."),
            EncounterKind.Overlord => new("Region conquered", $"{SeaChart.Regions[Here.Region].Name} flies your flag. +{target.Reward} doubloons."),
            _ => new("Rival defeated", $"{target.Name} struck its colors. +{target.Reward} doubloons.")
        };
    }

    private void Reveal()
    {
        Save.Revealed.Add(Save.Position);
        foreach (var neighbor in Chart.Neighbors(Save.Position)) Save.Revealed.Add(neighbor);
    }
}
