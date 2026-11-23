using System.Text.Json.Serialization;

namespace Blackwake.Game;

public enum CannonKind { Roundshot, Heavy, Scatter, Fire }
public enum UpgradeKind { Hull, Rigging }

public sealed record CannonSpec(string Name, string Description, int Cost, float Damage, float Reload, float Speed);
public sealed record UpgradeSpec(string Name, string Description, int Cost);

public sealed class CampaignSave
{
    public int Seed { get; set; } = Random.Shared.Next();
    public int Day { get; set; } = 1;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Moves { get; set; }
    public int SailedToday { get; set; }
    public int Gold { get; set; } = 145;
    public int Position { get; set; } = SeaChart.Home;
    public int BaseCamp { get; set; } = SeaChart.Home;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CannonKind?[]? Layout { get; set; }
    public Loadout? Loadout { get; set; }
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
        new("Reinforced hull", "+18% base hull per level", 60),
        new("Improved rigging", "+12% base steering and sailing speed per level", 45)
    ];

    public Campaign(CampaignSave? save = null)
    {
        Save = save ?? new();
        Chart = new(Save.Seed);
        Save.Loadout ??= MigrateLoadout();
        Reveal();
    }

    public CampaignSave Save { get; }
    public SeaChart Chart { get; }
    public SeaTile Here => Chart.Tiles[Save.Position];
    public Loadout Loadout => Save.Loadout!;
    public ShipSpec Ship => Shipyard.GetShip(Loadout.Ship);
    public bool AtBasecamp => Here.Encounter.Kind == EncounterKind.Base ||
        Here.Encounter.Kind == EncounterKind.Fort && Save.Cleared.Contains(Here.Id);
    public BasecampMarket? Market => AtBasecamp ? Shipyard.GetMarket(Here) : null;
    public bool IsThreat(SeaTile tile) => tile.Encounter.Kind >= EncounterKind.Merchant && !Save.Cleared.Contains(tile.Id);
    public bool Controls(int region) => Save.Cleared.Any(id =>
        Chart.Tiles[id].Region == region && Chart.Tiles[id].Encounter.Kind == EncounterKind.Overlord);
    public int Conquered => Enumerable.Range(0, SeaChart.Regions.Length).Count(Controls);
    public int Hull => (int)Math.Round(Ship.Hull * (1 + .18 * Level(UpgradeKind.Hull)));
    private double SpeedScale => 1 + .12 * Level(UpgradeKind.Rigging);
    public int Speed => (int)Math.Round(Ship.Speed * SpeedScale);
    public int DailyMoves => (int)Math.Round(Ship.Moves * SpeedScale, MidpointRounding.AwayFromZero);
    public int MovesLeft => Math.Max(0, DailyMoves - Save.SailedToday);
    public int Level(UpgradeKind upgrade) => Save.Upgrades[(int)upgrade];
    public int UpgradeCost(UpgradeKind upgrade) => Improvements[(int)upgrade].Cost + Level(upgrade) * 40;
    public int Available(CannonKind cannon) => Save.Stock[(int)cannon] - Loadout.Mounts.Count(mount => mount.Cannon == cannon);
    public int AvailableBarricades => Loadout.Barricades - Loadout.Mounts.Count(mount => mount.Barricade);
    public int MannedCannons => Loadout.Mounts.Count(mount => mount.GunnerId.HasValue);
    public bool HasMannedCannon => MannedCannons > 0;
    public DeckMount? MountAt(DeckPosition position) => Loadout.Mounts.Find(mount => mount.Position == position);

    public bool CanSail(int destination) => MovesLeft > 0 && Chart.Neighbors(Save.Position).Contains(destination);

    public bool Sail(int destination)
    {
        if (!CanSail(destination)) return false;
        Save.Position = destination;
        Save.SailedToday++;
        Save.Report = null;
        Reveal();
        if (AtBasecamp) Save.BaseCamp = destination;
        return true;
    }

    public void NextDay()
    {
        Save.Day++;
        Save.SailedToday = 0;
    }

    public bool Buy(CannonKind cannon)
    {
        if (!PayAtBasecamp(Cannons[(int)cannon].Cost)) return false;
        Save.Stock[(int)cannon]++;
        return true;
    }

    public bool BuyBarricade()
    {
        if (!PayAtBasecamp(Shipyard.BarricadeCost)) return false;
        Loadout.Barricades++;
        return true;
    }

    public bool Fit(DeckPosition position, CannonKind? cannon, bool barricade = false)
    {
        if (!Ship.Contains(position)) return false;
        var current = MountAt(position);
        if (barricade && current?.Barricade != true && AvailableBarricades <= 0 ||
            cannon is { } kind && current?.Cannon != kind && Available(kind) <= 0) return false;
        if (current is not null) Loadout.Mounts.Remove(current);
        if (barricade) Loadout.Mounts.Add(new(position, null, true));
        else if (cannon.HasValue) Loadout.Mounts.Add(new(position, cannon, GunnerId: current?.GunnerId));
        return true;
    }

    public bool MoveMount(DeckPosition source, DeckPosition destination)
    {
        if (source == destination || !Ship.Contains(source) || !Ship.Contains(destination)) return false;
        var mounts = Loadout.Mounts;
        var sourceIndex = mounts.FindIndex(mount => mount.Position == source);
        if (sourceIndex < 0) return false;
        var targetIndex = mounts.FindIndex(mount => mount.Position == destination);
        mounts[sourceIndex] = mounts[sourceIndex] with { Position = destination };
        if (targetIndex >= 0) mounts[targetIndex] = mounts[targetIndex] with { Position = source };
        return true;
    }

    public bool Assign(DeckPosition position, int? pirate)
    {
        if (MountAt(position) is not { Cannon: not null } mount ||
            pirate is { } id && !Loadout.Crew.Contains(id)) return false;
        for (var index = 0; index < Loadout.Mounts.Count; index++)
        {
            var item = Loadout.Mounts[index];
            if (item == mount) Loadout.Mounts[index] = item with { GunnerId = pirate };
            else if (pirate.HasValue && item.GunnerId == pirate) Loadout.Mounts[index] = item with { GunnerId = null };
        }
        return true;
    }

    public void UnassignAll()
    {
        for (var index = 0; index < Loadout.Mounts.Count; index++)
        {
            var mount = Loadout.Mounts[index];
            if (mount.GunnerId.HasValue) Loadout.Mounts[index] = mount with { GunnerId = null };
        }
    }

    public void ClearLayout() => Loadout.Mounts.Clear();

    public bool Hire(int pirate)
    {
        var recruit = Market?.Recruits.FirstOrDefault(card => card.Id == pirate);
        if (recruit is null || Loadout.Crew.Contains(pirate) || !PayAtBasecamp(recruit.Cost)) return false;
        Loadout.Crew.Add(pirate);
        return true;
    }

    public bool Commission(ShipKind ship)
    {
        if (Market?.Ship != ship || Loadout.Fleet.Contains(ship) || !PayAtBasecamp(Shipyard.GetShip(ship).Cost)) return false;
        Loadout.Fleet.Add(ship);
        return SwitchShip(ship);
    }

    public bool SwitchShip(ShipKind ship)
    {
        if (!AtBasecamp || !Loadout.Fleet.Contains(ship)) return false;
        Loadout.Ship = ship;
        Loadout.Mounts.RemoveAll(mount => !Ship.Contains(mount.Position));
        return true;
    }

    public bool Upgrade(UpgradeKind upgrade)
    {
        if (Level(upgrade) >= MaxUpgrade || !PayAtBasecamp(UpgradeCost(upgrade))) return false;
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

    private bool PayAtBasecamp(int cost)
    {
        if (!AtBasecamp || Save.Gold < cost) return false;
        Save.Gold -= cost;
        return true;
    }

    private Loadout MigrateLoadout()
    {
        var loadout = new Loadout();
        var legacyLayout = Save.Layout ?? [null, CannonKind.Roundshot, CannonKind.Roundshot, null, null];
        DeckPosition[] positions = [new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(1, 2, 0), new(0, 2, 0)];
        var nextGunner = 0;
        for (var slot = 0; slot < legacyLayout.Length && slot < positions.Length; slot++)
        {
            if (legacyLayout[slot] is not { } cannon) continue;
            int? gunner = nextGunner < loadout.Crew.Count ? loadout.Crew[nextGunner++] : null;
            loadout.Mounts.Add(new(positions[slot], cannon, GunnerId: gunner));
        }

        if (Save.Upgrades.Length >= 4)
        {
            // Retired upgrades are refunded at the original purchase prices.
            for (var level = 0; level < Save.Upgrades[1]; level++) Save.Gold += 65 + 40 * level;
            for (var level = 0; level < Save.Upgrades[2]; level++) Save.Gold += 55 + 40 * level;
            Save.Upgrades = [Save.Upgrades[0], Save.Upgrades[3]];
        }
        if (Save.Moves is { } remaining) Save.SailedToday = Math.Max(0, 7 - remaining);
        Save.Layout = null;
        Save.Moves = null;
        return loadout;
    }
}
