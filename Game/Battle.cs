namespace Blackwake.Game;

public enum BattleResult { InProgress, Victory, Defeat }
public enum GateKind { Double, Split, Fire, Barrier }

public sealed class Shot(int id, float x, float y, float speed, float drift, float damage, bool burning, CannonKind kind)
{
    public int Id { get; } = id;
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public float Speed { get; } = speed;
    public float Drift { get; } = drift;
    public float Damage { get; set; } = damage;
    public bool Burning { get; set; } = burning;
    public CannonKind Kind { get; } = kind;
    public bool FromPlayer => Speed > 0;

    internal bool Crosses(float start, float target) => FromPlayer ? start < target && X >= target : start > target && X <= target;
    internal float HeightAt(float oldX, float oldY, float target) =>
        oldY + (Y - oldY) * ((target - oldX) / (X - oldX));
}

public sealed record Gate(float X, float Center, float Amplitude, float Phase, GateKind Kind)
{
    public float Y(float time) => Center + Amplitude * MathF.Sin(time * .86f + Phase);
}

public sealed class Battle
{
    public const float GateRadius = 27;
    private readonly List<Shot> shots = [];
    private readonly Random random;
    private float nextGateNotice;
    private int nextShotId;

    public Battle(Campaign campaign, Random? random = null)
    {
        this.random = random ?? Random.Shared;
        Encounter = campaign.Here.Encounter;
        Player = new(campaign.Ship, campaign.Hull, campaign.Speed, true,
            campaign.Loadout.Mounts, Shipyard.GetPirate);
        var enemy = EnemyFleet.For(Encounter);
        var ship = Shipyard.GetShip(enemy.Ship);
        Enemy = new(ship, (int)MathF.Round(ship.Hull * enemy.HullScale), ship.Speed, false,
            enemy.Mounts, enemy.GetGunner);
    }

    public Encounter Encounter { get; }
    public CombatVessel Player { get; }
    public CombatVessel Enemy { get; }
    public bool IsMerchant => Encounter.Kind == EncounterKind.Merchant;
    public bool IsFort => Encounter.Kind == EncounterKind.Fort;
    public float Time { get; private set; }
    public float Duration => IsMerchant ? 26 : 65;
    public float TargetY { get; set; } = 210;
    public int Loot { get; private set; }
    public BattleResult Result { get; private set; }
    public string Notice { get; private set; } = "Broadsides underway";
    public IReadOnlyList<Shot> Shots => shots;
    public static readonly Gate[] Gates =
    [
        new(374, 92, 48, .2f, GateKind.Double),
        new(374, 303, 51, 2.3f, GateKind.Split),
        new(538, 202, 55, 1.2f, GateKind.Fire),
        new(538, 346, 35, 4.4f, GateKind.Barrier)
    ];

    public void Withdraw() => Result = BattleResult.Defeat;

    public void Step(float delta, int direction)
    {
        if (Result != BattleResult.InProgress) return;
        Time += delta;
        var distance = Player.Speed * delta;
        if (direction != 0) TargetY = Math.Clamp(Player.Y + direction * distance, Player.MinY, Player.MaxY);
        Player.Steer(TargetY, distance);
        if (!IsFort)
        {
            var target = IsMerchant ? 210 + 56 * MathF.Sin(Time * .53f) :
                210 + .34f * (Player.Y - 210) + 71 * MathF.Sin(Time * .78f);
            Enemy.Steer(target, Enemy.Speed * (IsMerchant ? .34f : .48f) * delta);
        }

        Fire(Player, delta);
        Fire(Enemy, delta);
        AdvanceShots(delta);
        Player.Burn(delta);
        Enemy.Burn(delta);

        if (Player.Hull <= 0) Result = BattleResult.Defeat;
        else if (Enemy.Hull <= 0 || IsMerchant && (Time >= Duration || Enemy.Chests.All(chest => chest.Health <= 0)))
            Result = BattleResult.Victory;
        else if (Time >= Duration || !IsMerchant && Player.Guns.All(gun => !gun.CanFire) &&
            !shots.Any(shot => shot.FromPlayer) && !Enemy.IsBurning)
            Result = BattleResult.Defeat;
    }

    private void Fire(CombatVessel vessel, float delta)
    {
        foreach (var gun in vessel.Guns)
        {
            if (gun.Health <= 0 || gun.Gunner is not { } gunner) continue;
            gun.Cooldown -= delta;
            if (gun.Cooldown > 0) continue;
            var spec = Campaign.Cannons[(int)gun.Kind];
            gun.Cooldown += spec.Reload * gunner.ReloadScale;
            var speed = vessel.IsPlayer ? spec.Speed : -spec.Speed;
            var aim = gunner.AimSpread > 0 ? (random.NextSingle() * 2 - 1) * gunner.AimSpread * spec.Speed : 0;
            var y = vessel.Y + gun.Lane;
            AddShot(gun.X, y, speed, aim, spec.Damage, gun.Kind == CannonKind.Fire, gun.Kind);
            if (gun.Kind != CannonKind.Scatter) continue;
            AddShot(gun.X, y, speed, aim - 27, spec.Damage, false, gun.Kind);
            AddShot(gun.X, y, speed, aim + 27, spec.Damage, false, gun.Kind);
        }
    }

    private void AdvanceShots(float delta)
    {
        // New split shots join on the next step, after crossing the gate that created them.
        for (var index = shots.Count - 1; index >= 0; index--)
        {
            var shot = shots[index];
            var oldX = shot.X;
            var oldY = shot.Y;
            shot.X += shot.Speed * delta;
            shot.Y += shot.Drift * delta;
            var target = shot.FromPlayer ? Enemy : Player;
            if (ApplyGates(shot, oldX, oldY) || HitVessel(target, shot, oldX, oldY) || shot.Y is < 0 or > 420)
                shots.RemoveAt(index);
        }
    }

    private bool ApplyGates(Shot shot, float oldX, float oldY)
    {
        if (Math.Max(oldX, shot.X) < Gates[0].X || Math.Min(oldX, shot.X) > Gates[^1].X) return false;
        foreach (var gate in Gates)
        {
            if ((!shot.FromPlayer && gate.Kind != GateKind.Barrier) || !shot.Crosses(oldX, gate.X) ||
                Math.Abs(shot.HeightAt(oldX, oldY, gate.X) - gate.Y(Time)) > GateRadius) continue;
            switch (gate.Kind)
            {
                case GateKind.Double:
                    shot.Damage *= 2;
                    Announce("Double damage");
                    break;
                case GateKind.Split:
                    AddShot(shot.X, shot.Y, shot.Speed, shot.Drift + 21, shot.Damage, shot.Burning, shot.Kind);
                    Announce("Cannonball split");
                    break;
                case GateKind.Fire:
                    shot.Burning = true;
                    Announce("Cannonball ignited");
                    break;
                case GateKind.Barrier:
                    Announce(shot.FromPlayer ? "Your shot hit the floating barrier" : "Floating barrier stopped enemy fire");
                    return true;
            }
        }
        return false;
    }

    private bool HitVessel(CombatVessel target, Shot shot, float oldX, float oldY)
    {
        if (shot.FromPlayer ? shot.X < target.RailX : shot.X > target.RailX) return false;
        if (target.FindImpact(shot, oldX, oldY) is { } part)
        {
            part.Health = Math.Max(0, part.Health - shot.Damage);
            switch (part)
            {
                case Gun when part.Health <= 0:
                    Announce(target.IsPlayer ? "Your cannon was disabled" : "Enemy cannon disabled", true);
                    break;
                case Barricade:
                    Announce(part.Health <= 0
                        ? (target.IsPlayer ? "Your barricade was destroyed" : "Enemy barricade destroyed")
                        : (target.IsPlayer ? "Your barricade blocked enemy fire" : "Enemy barricade blocked your shot"), true);
                    break;
                case Treasure cargo when cargo.Health <= 0:
                    Loot += cargo.Gold;
                    Announce($"+{cargo.Gold} doubloons recovered", true);
                    break;
            }
            return true;
        }

        if (!shot.Crosses(oldX, target.HullImpactX)) return false;
        if (Math.Abs(shot.HeightAt(oldX, oldY, target.HullImpactX) - target.Y) <= target.Radius)
            target.HitHull(shot.Damage, shot.Burning);
        return true;
    }

    private void AddShot(float x, float y, float speed, float drift, float damage, bool burning, CannonKind kind) =>
        shots.Add(new(++nextShotId, x, y, speed, drift, damage, burning, kind));

    private void Announce(string notice, bool urgent = false)
    {
        if (Time < nextGateNotice && !urgent) return;
        Notice = notice;
        nextGateNotice = Time + .75f;
    }
}
