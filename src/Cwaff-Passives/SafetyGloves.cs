namespace CwaffingTheGungy;

public class SafetyGloves : CwaffPassive
{
    public static string ItemName         = "Safety Gloves";
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
        PickupObject item = Lazy.SetupPassive<SafetyGloves>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;

        //WARNING: reusing ammonomicon icon screws up bounding box in ammonomicon
        // _HandlingVFX = VFX.Create("safety_gloves_icon", 2, loops: true, anchor: Anchor.LowerCenter);
        _HandlingVFX = VFX.Create("safety_gloves_vfx", 2, loops: true, anchor: Anchor.LowerCenter);
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
        if (pc.GetAbsoluteParentRoom() is not RoomHandler room)
            return;

        Vector2 ppos         = pc.sprite.WorldCenter;
        float gunAngle       = pc.m_currentGunAngle;
        AIActor closestEnemy = null;
        float closestDist    = _REACH_SQR;
        foreach(AIActor enemy in room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All).EmptyIfNull())
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

        bool interacted = (pc.m_activeActions != null) && pc.m_activeActions.InteractAction.WasPressed;
        if (!interacted)
        {
            this._shouldClearVfx = false;
            Vector2 pos = (closestEnemy.sprite != null) ? closestEnemy.sprite.WorldTopCenter : closestEnemy.CenterPosition;
            if (!this._extantVfx)
                this._extantVfx = SpawnManager.SpawnVFX(_HandlingVFX);
            this._extantVfx.transform.position = pos.HoverAt(amplitude: 0.25f, frequency: 10f, offset: 0.5f);
            return;
        }

        this._extantVfx.SafeDestroy();
        this._extantVfx = null;

        FancyVFX.FromCurrentFrame(closestEnemy.sprite).ArcTowards(0.5f, pc.sprite, false, 0.1f, 1.0f);
        pc.gameObject.Play("safety_glove_grab_sound");
        closestEnemy.gameObject.Play("Play_ENM_Death");
        closestEnemy.EraseFromExistence(suppressDeathSounds: true);
        gun.GainAmmo(gun.AdjustedMaxAmmo / 10);
    }
}
