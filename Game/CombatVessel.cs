namespace Blackwake.Game;

public abstract class CombatPart(float x, float lane, float health, float hitRadius)
{
    public float X { get; } = x;
    public float Lane { get; } = lane;
    public float MaxHealth { get; } = health;
    public float Health { get; internal set; } = health;
    public float HitRadius { get; } = hitRadius;
}

public sealed class Gun(float x, float lane, CannonKind kind, PirateCard? gunner, float cooldown)
    : CombatPart(x, lane, 26, 7)
{
    public CannonKind Kind { get; } = kind;
    public PirateCard? Gunner { get; } = gunner;
    public bool CanFire => Gunner is not null && Health > 0;
    public float Cooldown { get; internal set; } = cooldown;
}

public sealed class Barricade(float x, float lane) : CombatPart(x, lane, Shipyard.BarricadeHealth, 7);

public sealed class Treasure(float x, float lane, TreasureSpec spec) : CombatPart(x, lane, spec.Durability, 11)
{
    public int Gold { get; } = spec.Gold;
}

public sealed class CombatVessel
{
    private const float ColumnSpacing = 25;
    private readonly CombatPart[] parts;
    private float fireRemaining;

    public CombatVessel(ShipSpec ship, int hull, int speed, bool player, IEnumerable<DeckMount> mounts, Func<int, PirateCard> gunner)
    {
        Ship = ship;
        Hull = MaxHull = hull;
        Speed = speed;
        IsPlayer = player;
        Radius = (ship.Rows - 1) * 14 + (ship.Decks - 1) * 4.5f + 16;
        RailX = player ? 140 : 760;
        var inboard = RailX + (player ? -1 : 1) * (ship.Columns - 1) * ColumnSpacing;
        HullImpactX = inboard + (player ? -15 : 15);
        CenterX = (inboard + RailX) / 2;
        var guns = new List<Gun>();
        var walls = new List<Barricade>();
        var chests = new List<Treasure>();
        foreach (var mount in mounts)
        {
            var x = inboard + (player ? 1 : -1) * mount.Position.Column * ColumnSpacing;
            var lane = ship.Lane(mount.Position);
            if (mount.Barricade) walls.Add(new(x, lane));
            else if (mount.Treasure is { } cargo) chests.Add(new(x, lane, cargo));
            else if (mount.Cannon is { } kind)
            {
                var assigned = mount.GunnerId is { } id ? gunner(id) : null;
                guns.Add(new(x, lane, kind, assigned,
                    .28f + mount.Position.Row * .13f + mount.Position.Deck * .07f));
            }
        }
        Guns = guns.ToArray();
        Barricades = walls.ToArray();
        Chests = chests.ToArray();
        parts = walls.Cast<CombatPart>().Concat(guns).Concat(chests).ToArray();
    }

    public ShipSpec Ship { get; }
    public bool IsPlayer { get; }
    public float RailX { get; }
    public float XScale => .7f + .3f * (Ship.Columns - 2);
    public float Radius { get; }
    public float CenterX { get; }
    public float HullImpactX { get; }
    public float MinY => Radius + 6;
    public float MaxY => 420 - MinY;
    public float Y { get; private set; } = 210;
    public float Hull { get; private set; }
    public int MaxHull { get; }
    public int Speed { get; }
    public bool IsBurning => fireRemaining > 0;
    public IReadOnlyList<Gun> Guns { get; }
    public IReadOnlyList<Barricade> Barricades { get; }
    public IReadOnlyList<Treasure> Chests { get; }

    internal void Steer(float target, float distance) =>
        Y += Math.Clamp(Math.Clamp(target, MinY, MaxY) - Y, -distance, distance);

    internal CombatPart? FindImpact(Shot shot, float oldX, float oldY)
    {
        CombatPart? hit = null;
        var nearest = float.PositiveInfinity;
        var smallestOffset = float.PositiveInfinity;
        foreach (var part in parts)
        {
            if (part.Health <= 0 || !shot.Crosses(oldX, part.X)) continue;
            var distance = Math.Abs(part.X - oldX);
            if (distance > nearest) continue;
            var offset = Math.Abs(shot.HeightAt(oldX, oldY, part.X) - Y - part.Lane);
            if (offset > part.HitRadius || distance == nearest && offset >= smallestOffset) continue;
            hit = part;
            nearest = distance;
            smallestOffset = offset;
        }
        return hit;
    }

    internal void HitHull(float damage, bool ignite)
    {
        Hull = Math.Max(0, Hull - damage);
        if (ignite) fireRemaining = 2.6f;
    }

    internal void Burn(float delta)
    {
        if (!IsBurning) return;
        var burn = Math.Min(fireRemaining, delta);
        fireRemaining -= burn;
        HitHull(burn * 4, false);
    }
}
