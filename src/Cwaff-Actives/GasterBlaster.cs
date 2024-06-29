namespace CwaffingTheGungy;

public class GasterBlaster : CwaffActive
{
    public static string ItemName         = "Gaster Blaster";
    public static string ShortDescription = "Use Your Best Attack First";
    public static string LongDescription  = "Deploys a blaster behind you that fires an extremely powerful beam after a short delay.";
    public static string Lore             = "Given to Mr. Gundertale by \"a dear old friend\" and passed along to you, this blaster operates using a uniquely powerful combination of science, magic, and friendship. It shows surprisingly little wear, and the initials \"WDG\" can still clearly be seen engraved between the eyes. Holding it in your hands fills with you with a positive feeling you find difficult to determine at the moment.";

    internal static Projectile _GasterBlast;
    internal static GameObject _GasterBlaster;

    public static int ID;

    private bool _anyGunFiredInRoom = false;
    private PlayerController _owner = null;

    public static void Init()
    {
        PlayerItem item   = Lazy.SetupActive<GasterBlaster>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.SPECIAL;
        item.consumable   = false;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Damage, 100f);

        _GasterBlaster = VFX.Create("gaster_blaster_vfx", 2, loops: true, anchor: Anchor.MiddleCenter);

        _GasterBlast = Items.MarineSidearm.CloneProjectile(GunData.New(damage: 700.0f, speed: 150.0f, force: 70.0f, range: 200.0f));
        _GasterBlast.ignoreDamageCaps        = false;
        _GasterBlast.PenetratesInternalWalls = true;
        _GasterBlast.pierceMinorBreakables   = true;

        BasicBeamController beamComp = _GasterBlast.SetupBeamSprites(
          spriteName: "gaster_beam", fps: 60, dims: new Vector2(35, 39), impactDims: new Vector2(36, 36), impactFps: 16);
            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.ContinueBeamArtToWall = false;
            beamComp.PenetratesCover       = true;
            beamComp.penetration           = 1000;

        PierceProjModifier pierce = _GasterBlast.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration          = 100;
            pierce.penetratesBreakables = true;

        ID = item.PickupObjectId;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this._owner = player;
    }

    public override void OnPreDrop(PlayerController player)
    {
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return user.IsInCombat && base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
        if (user != this._owner)
            return;
        user.StartCoroutine(BlasterWithGaster());
    }

    private IEnumerator BlasterWithGaster()
    {
        const float SWING_RADIUS = 16f;

        float angle = this._owner.m_currentGunAngle;
        // Vector2 targetPos = this._owner.CenterPosition - BraveMathCollege.DegreesToVector(angle, 4f);
        Vector2 targetPos = this._owner.CenterPosition.ToNearestWallOrObject((180f + angle).Clamp180(), 0f);
        Vector2 delta = targetPos - this._owner.CenterPosition;
        targetPos = this._owner.CenterPosition + (delta.magnitude - 1f).Clamp(0f, 2f) * delta.normalized;

        GameObject blaster = SpawnManager.SpawnVFX(_GasterBlaster, this._owner.CenterPosition.ToVector3ZUp(10f), Quaternion.identity);
        RotateIntoPositionBehavior rotcomp = blaster.AddComponent<RotateIntoPositionBehavior>();
            rotcomp.m_radius       = SWING_RADIUS;
            rotcomp.m_fulcrum      = targetPos + BraveMathCollege.DegreesToVector(angle, SWING_RADIUS);
            rotcomp.m_start_angle  = angle;
            rotcomp.m_end_angle    = (180f + angle).Clamp180();
            rotcomp.m_rotate_time  = 0.5f;
            rotcomp.Setup();
        blaster.PlayUnique("gaster_blaster_sound_effect");
        yield return new WaitForSeconds(0.75f);

        BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(
            _GasterBlast, this._owner, /*blaster*/ null, // TODO: not sure why blaster doesn't work here
            blaster.GetComponent<tk2dSprite>().WorldCenter + rotcomp.m_start_angle.ToVector(0.33f), rotcomp.m_start_angle, 0.75f, true, true);
        beam.sprite.gameObject.SetGlowiness(300f); // TODO: not sure why this doesn't actually seem to have any effect
        blaster.ExpireIn(1.25f, fadeFor: 0.25f);
    }
}
