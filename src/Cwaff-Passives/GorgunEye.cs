namespace CwaffingTheGungy;

public class GorgunEye : CwaffPassive
{
    public static string ItemName         = "Gorgun Eye";
    public static string ShortDescription = "Staredown";
    public static string LongDescription  = "Stuns enemies while looking directly at them.";
    public static string Lore             = "Contrary to popular belief, Gorgun eyes have no intrinsic petrification properties. Gungeon archaeologists have discovered that Gorgun shamans traditionally imbue the eyes of their peers with petrification magic during infancy. More recently, it appears that Gorguns have begun to prefer enchanting synthetic eyes and disposable contact lenses, presumably after collectively realizing that petrifying everyone you meet was mildly inconvenient during social gatherings.";

    private const float _CONE_RADIUS      = 10f;
    private const float _STUN_LINGER_TIME = 0.1f;
    private const string _EFFECT_NAME     = "Gorgun Eyed";

    private static readonly Color _StoneColor        = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    private static GameActorHealthEffect _GorgunTint = null;

    private List<AIActor> _afflictedEnemies = new();
    private List<AIActor> _afflictedEnemiesAlt = new();

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<GorgunEye>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        item.AddToShop(ModdedShopType.Handy);

        _GorgunTint = new GameActorHealthEffect()
        {
            AppliesTint              = true,
            TintColor                = _StoneColor,
            AppliesDeathTint         = true,
            DeathTintColor           = _StoneColor,
            AffectsEnemies           = true,
            DamagePerSecondToEnemies = 0f,
            duration                 = 10000000,
            effectIdentifier         = _EFFECT_NAME,
        };
    }

    public override void Update()
    {
        base.Update();
        if (this.Owner is not PlayerController player)
            return;

        bool pierces = player.HasSynergy(Synergy.PIERCING_GAZE);
        bool blankStare = player.HasSynergy(Synergy.BLANK_STARE);

        this._afflictedEnemiesAlt.Clear();
        Vector2 ppos         = player.CenterPosition;
        float gunAngle       = player.m_currentGunAngle;
        AIActor closestEnemy = null;
        float closestDist    = 999999f;
        foreach(AIActor enemy in player.CurrentRoom.SafeGetEnemiesInRoom())
        {
            if (!enemy || !enemy.IsHostileAndNotABoss(canBeNeutral: true))
                continue; // enemy is not one we should be targeting
            if (!enemy.behaviorSpeculator || enemy.behaviorSpeculator.ImmuneToStun)
                continue; // enemy cannot be stunned

            Vector2 epos  = enemy.CenterPosition;
            Vector2 delta = epos - ppos;
            if (!delta.IsNearAngle(gunAngle, _CONE_RADIUS))
                continue; // enemy is not within our vision range

            if (pierces)
            {
                this._afflictedEnemiesAlt.Add(enemy);
                continue;
            }

            float dist = delta.sqrMagnitude;
            if (dist >= closestDist)
                continue; // enemy is not the closest enemy we've encountered

            // Make sure we're not going through walls
            Vector2 target = Raycast.ToNearestWallOrObject(ppos, gunAngle, minDistance: 0);
            if ((target-ppos).sqrMagnitude < dist)
                continue; // wall obstructs our view of the enemy

            closestEnemy = enemy;
            closestDist  = dist;
        }

        if (!pierces && closestEnemy)
            this._afflictedEnemiesAlt.Add(closestEnemy);

        foreach (AIActor enemy in this._afflictedEnemiesAlt)
        {
            enemy.behaviorSpeculator.Stun(_STUN_LINGER_TIME, createVFX: false);
            if (this._afflictedEnemies.Contains(enemy))
                continue;
            enemy.ApplyEffect(_GorgunTint);
            enemy.gameObject.PlayUnique("gorgun_eye_activate");
        }

        foreach (AIActor enemy in this._afflictedEnemies)
        {
            if (!enemy || this._afflictedEnemiesAlt.Contains(enemy))
                continue;
            enemy.RemoveEffect(_GorgunTint);
            if (blankStare && enemy.behaviorSpeculator is BehaviorSpeculator bs && !bs.ImmuneToStun)
                bs.Stun(1f);
        }

        BraveUtility.Swap(ref this._afflictedEnemies, ref this._afflictedEnemiesAlt);
    }
}
