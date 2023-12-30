namespace CwaffingTheGungy;

public class SafetyGloves : PassiveItem
{
    public static string ItemName         = "Safety Gloves";
    public static string SpritePath       = "safety_gloves_icon";
    public static string ShortDescription = "Handling with Care";
    public static string LongDescription  = "Pressing interact near most Bullet Kin variants instantly defeats them and restores 10% ammo to the current gun.";
    public static string Lore             = "Bullet Kin, despite being animated by the magic of the Gungeon, are still just ammunition at the end of the day, and the only thing preventing them from being used as such is the difficulty of getting near them and grabbing hold of them. A sturdy enough glove will solve exactly one of those issues, so by extension, two such gloves should solve both of those issues.";

    private const float _REACH     = 3.0f;
    private const float _REACH_SQR = _REACH * _REACH;

    private static GameObject _HandlingVFX;

    private GameObject _extantVfx = null;
    private bool _shouldClearVfx = false;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<SafetyGloves>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;

        _HandlingVFX = VFX.Create("safety_gloves_icon", 2, loops: true, anchor: Anchor.LowerCenter);
    }

    public override void Update()
    {
        base.Update();

        if (this._extantVfx != null && this._shouldClearVfx)
        {
            UnityEngine.Object.Destroy(this._extantVfx);
            this._extantVfx = null;
        }
        this._shouldClearVfx = true;

        if (this.Owner is not PlayerController pc)
            return;
        if (pc.IsPetting || pc.IsDodgeRolling || pc.m_handlingQueuedAnimation)
            return;
        if (pc.CurrentGun is not Gun gun)
            return;
        if (!gun.CanGainAmmo || gun.InfiniteAmmo || gun.LocalInfiniteAmmo || gun.CurrentAmmo == gun.AdjustedMaxAmmo)
            return;

        Vector2 ppos         = pc.sprite.WorldCenter;
        float gunAngle       = pc.m_currentGunAngle;
        AIActor closestEnemy = null;
        float closestDist    = _REACH_SQR;
        foreach(AIActor enemy in pc.GetAbsoluteParentRoom()?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All).EmptyIfNull())
        {
            if (!enemy || !Enemies.BulletKinVariants.Contains(enemy.EnemyGuid))
                continue; // enemy is not one we should be targeting

            Vector2 delta = enemy.CenterPosition - ppos;
            if (!delta.IsNearAngle(gunAngle, 90f))
                continue; // enemy is not within our vision range

            float dist = delta.sqrMagnitude;
            if (dist >= closestDist)
                continue; // enemy is not the closest enemy we've encountered

            closestEnemy = enemy;
            closestDist  = dist;
        }

        if (!closestEnemy)
            return;

        if (!(pc.m_activeActions?.InteractAction?.WasPressed ?? false))
        {
            this._shouldClearVfx = false;
            this._extantVfx ??= SpawnManager.SpawnVFX(_HandlingVFX, closestEnemy.sprite.WorldTopCenter + new Vector2(0f, 0.5f), Quaternion.identity);
            this._extantVfx.transform.position = closestEnemy.sprite.WorldTopCenter
                + new Vector2(0f, 0.5f + 0.25f * Mathf.Sin(10f * BraveTime.ScaledTimeSinceStartup));
            return;
        }

        if (this._extantVfx != null)
        {
            UnityEngine.Object.Destroy(this._extantVfx);
            this._extantVfx = null;
        }

        FancyVFX.FromCurrentFrame(closestEnemy.sprite).ArcTowards(0.5f, pc.sprite, false, 0.1f, 1.0f);
        AkSoundEngine.PostEvent("safety_glove_grab_sound", pc.gameObject);
        AkSoundEngine.PostEvent("Play_ENM_Death", closestEnemy.gameObject);
        closestEnemy.EraseFromExistence(suppressDeathSounds: true);
        gun.GainAmmo(gun.AdjustedMaxAmmo / 10);
    }
}
