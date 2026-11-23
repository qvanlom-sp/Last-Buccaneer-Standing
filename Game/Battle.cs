namespace Blackwake.Game;

public enum BattleResult { InProgress, Victory, Defeat }
public enum GateKind { Double, Split, Fire, Barrier }

public sealed class Gun(int slot, CannonKind kind, float health, float damageScale, float reloadScale)
{
    public int Slot { get; } = slot;
    public CannonKind Kind { get; } = kind;
    public float Health { get; set; } = health;
    public float MaxHealth { get; } = health;
    public float DamageScale { get; } = damageScale;
    public float ReloadScale { get; } = reloadScale;
    public float Cooldown { get; set; } = .28f + slot * .19f;
}

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
}

public sealed record Gate(float X, float Center, float Amplitude, float Phase, GateKind Kind)
{
    public float Y(float time) => Center + Amplitude * MathF.Sin(time * .86f + Phase);
}

public sealed class Treasure(int slot, int gold, float health)
{
    public int Slot { get; } = slot;
    public int Gold { get; } = gold;
    public float Health { get; set; } = health;
}

public sealed class Battle
{
    public const float PlayerX = 140;
    public const float EnemyX = 760;
    public const float MinY = 83;
    public const float MaxY = 337;
    public const float GateRadius = 27;
    private const float TargetRadius = 84;
    private static readonly float[] SlotOffsets = [-64, -32, 0, 32, 64];
    private readonly List<Shot> shots = [];
    private readonly float steeringSpeed;
    private float playerFire;
    private float enemyFire;
    private float nextGateNotice;
    private int nextShotId;

    public Battle(Campaign campaign)
    {
        Encounter = campaign.Here.Encounter;
        Hull = MaxHull = campaign.Hull;
        EnemyHull = Encounter.Hull;
        steeringSpeed = 175 + 40 * campaign.Level(UpgradeKind.Helmsman);
        var damage = 1 + .2f * campaign.Level(UpgradeKind.Powder);
        var reload = 1 - .12f * campaign.Level(UpgradeKind.GunCrew);
        for (var slot = 0; slot < SlotOffsets.Length; slot++)
        {
            if (campaign.Save.Layout[slot] is { } kind) PlayerGuns.Add(new(slot, kind, 26, damage, reload));
        }

        var rank = Encounter.Difficulty;
        EnemyGuns = Encounter.Kind switch
        {
            EncounterKind.Merchant => [new(2, CannonKind.Roundshot, 15, .5f, 3.4f)],
            EncounterKind.Fort => [EnemyGun(0, CannonKind.Heavy), EnemyGun(2, CannonKind.Roundshot), EnemyGun(4, CannonKind.Fire)],
            EncounterKind.Overlord when rank == 0 => [EnemyGun(0, CannonKind.Roundshot), EnemyGun(2, CannonKind.Heavy), EnemyGun(4, CannonKind.Fire)],
            EncounterKind.Overlord when rank == 1 => [EnemyGun(0, CannonKind.Fire), EnemyGun(1, CannonKind.Scatter), EnemyGun(3, CannonKind.Heavy), EnemyGun(4, CannonKind.Roundshot)],
            EncounterKind.Overlord => [EnemyGun(0, CannonKind.Heavy), EnemyGun(1, CannonKind.Fire), EnemyGun(2, CannonKind.Scatter), EnemyGun(4, CannonKind.Heavy)],
            _ => [EnemyGun(1, CannonKind.Roundshot), EnemyGun(3, rank == 0 ? CannonKind.Roundshot : CannonKind.Scatter)]
        };
        if (IsMerchant)
        {
            Chests = [new(0, 35 + 18 * rank, 19), new(2, 55 + 20 * rank, 25), new(4, 45 + 18 * rank, 19)];
        }

        Gun EnemyGun(int slot, CannonKind kind) =>
            new(slot, kind, 18 + 5 * rank, .70f + .15f * rank, 1.55f - .16f * rank);
    }

    public Encounter Encounter { get; }
    public bool IsMerchant => Encounter.Kind == EncounterKind.Merchant;
    public bool IsFort => Encounter.Kind == EncounterKind.Fort;
    public float Time { get; private set; }
    public float Duration => IsMerchant ? 23 : 65;
    public float PlayerY { get; private set; } = 210;
    public float EnemyY { get; private set; } = 210;
    public float TargetY { get; set; } = 210;
    public float Hull { get; private set; }
    public float MaxHull { get; }
    public float EnemyHull { get; private set; }
    public int Loot { get; private set; }
    public BattleResult Result { get; private set; }
    public string Notice { get; private set; } = "Broadsides underway";
    public List<Gun> PlayerGuns { get; } = [];
    public List<Gun> EnemyGuns { get; }
    public Treasure[] Chests { get; } = [];
    public IReadOnlyList<Shot> Shots => shots;
    public static readonly Gate[] Gates =
    [
        new(374, 92, 48, .2f, GateKind.Double),
        new(374, 303, 51, 2.3f, GateKind.Split),
        new(538, 202, 55, 1.2f, GateKind.Fire),
        new(538, 346, 35, 4.4f, GateKind.Barrier)
    ];

    public static float GunY(float center, int slot) => center + SlotOffsets[slot];
    public static float ChestY(float center, int slot) => center + SlotOffsets[slot] * .83f;
    public void Withdraw() => Result = BattleResult.Defeat;

    public void Step(float delta, int direction)
    {
        if (Result != BattleResult.InProgress) return;
        Time += delta;
        if (direction != 0) TargetY = Math.Clamp(PlayerY + direction * steeringSpeed * delta, MinY, MaxY);
        PlayerY = MoveTowards(PlayerY, Math.Clamp(TargetY, MinY, MaxY), steeringSpeed * delta);
        if (!IsFort)
        {
            var target = IsMerchant ? 210 + 70 * MathF.Sin(Time * .55f) :
                Math.Clamp(210 + .34f * (PlayerY - 210) + 71 * MathF.Sin(Time * .78f), MinY, MaxY);
            EnemyY = MoveTowards(EnemyY, target, (IsMerchant ? 66 : 91) * delta);
        }

        Fire(PlayerGuns, true, delta);
        Fire(EnemyGuns, false, delta);
        AdvanceShots(delta);
        Hull = Math.Max(0, Hull - Burn(ref playerFire, delta));
        EnemyHull = Math.Max(0, EnemyHull - Burn(ref enemyFire, delta));

        if (Hull <= 0) Result = BattleResult.Defeat;
        else if (EnemyHull <= 0 || IsMerchant && (Time >= Duration || Chests.All(chest => chest.Health <= 0)))
            Result = BattleResult.Victory;
        else if (Time >= Duration || PlayerGuns.All(gun => gun.Health <= 0) && !shots.Any(shot => shot.Speed > 0) && enemyFire <= 0)
            Result = BattleResult.Defeat;
    }

    private void Fire(List<Gun> guns, bool player, float delta)
    {
        foreach (var gun in guns)
        {
            if (gun.Health <= 0) continue;
            gun.Cooldown -= delta;
            if (gun.Cooldown > 0) continue;
            var spec = Campaign.Cannons[(int)gun.Kind];
            gun.Cooldown += spec.Reload * gun.ReloadScale;
            var x = player ? PlayerX : EnemyX;
            var y = GunY(player ? PlayerY : EnemyY, gun.Slot);
            var speed = spec.Speed * (player ? 1 : -.80f);
            var damage = spec.Damage * gun.DamageScale;
            AddShot(x, y, speed, 0, damage, gun.Kind == CannonKind.Fire, gun.Kind);
            if (gun.Kind != CannonKind.Scatter) continue;
            AddShot(x, y, speed, -27, damage, false, gun.Kind);
            AddShot(x, y, speed, 27, damage, false, gun.Kind);
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
            var outgoing = shot.Speed > 0;
            var remove = false;
            foreach (var gate in Gates)
            {
                if ((!outgoing && gate.Kind != GateKind.Barrier) ||
                    !Crosses(oldX, shot.X, gate.X) ||
                    Math.Abs(YAt(shot, oldX, oldY, gate.X) - gate.Y(Time)) > GateRadius) continue;
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
                        remove = true;
                        Announce(outgoing ? "Your shot hit the barrier" : "Barrier stopped enemy fire");
                        break;
                }
            }

            var impactX = outgoing ? EnemyX : PlayerX;
            if (!remove && Crosses(oldX, shot.X, impactX))
            {
                Hit(shot, YAt(shot, oldX, oldY, impactX), outgoing);
                remove = true;
            }
            if (remove || shot.Y is < 0 or > 420) shots.RemoveAt(index);
        }
    }

    private void Hit(Shot shot, float y, bool enemy)
    {
        var center = enemy ? EnemyY : PlayerY;
        if (Math.Abs(y - center) > TargetRadius) return;
        var guns = enemy ? EnemyGuns : PlayerGuns;
        foreach (var gun in guns)
        {
            if (gun.Health <= 0 || Math.Abs(y - GunY(center, gun.Slot)) > 11) continue;
            gun.Health -= shot.Damage;
            if (gun.Health <= 0) Announce(enemy ? "Enemy cannon disabled" : "Your cannon was disabled", true);
            return;
        }

        if (enemy)
        {
            foreach (var chest in Chests)
            {
                if (chest.Health <= 0 || Math.Abs(y - ChestY(center, chest.Slot)) > 13) continue;
                chest.Health -= shot.Damage;
                if (chest.Health <= 0)
                {
                    Loot += chest.Gold;
                    Announce($"+{chest.Gold} doubloons recovered", true);
                }
                return;
            }
            EnemyHull -= shot.Damage;
            if (shot.Burning) enemyFire = 2.6f;
        }
        else
        {
            Hull -= shot.Damage;
            if (shot.Burning) playerFire = 2.6f;
        }
    }

    private void AddShot(float x, float y, float speed, float drift, float damage, bool burning, CannonKind kind) =>
        shots.Add(new(++nextShotId, x, y, speed, drift, damage, burning, kind));

    private void Announce(string notice, bool urgent = false)
    {
        if (Time < nextGateNotice && !urgent) return;
        Notice = notice;
        nextGateNotice = Time + .75f;
    }

    private static bool Crosses(float start, float end, float x) => start < x && end >= x || start > x && end <= x;
    private static float YAt(Shot shot, float x, float y, float target) =>
        y + (shot.Y - y) * ((target - x) / (shot.X - x));
    private static float MoveTowards(float start, float target, float distance) =>
        start + Math.Clamp(target - start, -distance, distance);

    private static float Burn(ref float remaining, float delta)
    {
        var burn = Math.Min(remaining, delta);
        remaining -= burn;
        return burn * 4;
    }
}
