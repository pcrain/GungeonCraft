namespace CwaffingTheGungy;

public class Pincushion : AdvancedGunBehavior
{
    public static string ItemName         = "Pincushion";
    public static string SpriteName       = "pincushion";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private static ILHook _VeryFragileILHook;

    internal const int _SIMULTANEOUS_BULLETS = 2;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Pincushion>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.BEAM, reloadTime: 0.0f, ammo: 10000);
            gun.SetMuzzleVFX("muzzle_natascha", fps: 60, scale: 0.3f, anchor: Anchor.MiddleCenter);
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            gun.AddToSubShop(ModdedShopType.Rusty);

        Projectile p = gun.InitProjectile(clipSize: 1000 / _SIMULTANEOUS_BULLETS, cooldown: 0.0f, angleVariance: 30.0f, shootStyle: ShootStyle.Automatic,
          damage: 1.0f, speed: 200.0f, force: 0.0f, range: 999f, sprite: "needle", fps: 12, anchor: Anchor.MiddleLeft
          ).Attach<VeryFragileNeedle>(
          ).Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.1f;
            trail.StartColor = Color.gray;
            trail.BaseColor  = Color.gray;
            trail.EndColor   = Color.gray;
          });
        for (int i = 1; i < _SIMULTANEOUS_BULLETS; ++i)
            gun.RawSourceVolley.projectiles.Add(ProjectileModule.CreateClone(gun.RawSourceVolley.projectiles[0], inheritGuid: false, sourceIndex: i));

        p.pierceMinorBreakables = false;

        _VeryFragileILHook = new ILHook(
            typeof(MinorBreakable).GetMethod("OnPreCollision", BindingFlags.Instance | BindingFlags.NonPublic),
            VeryFragileIL
            );
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        AkSoundEngine.PostEvent("soul_kaliber_fire", gun.gameObject);
        // this.RecalculateGunStats();
    }

    private static void VeryFragileIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        // ILLabel successLabel = null;
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(0)))
            return;
        // cursor.FindNext(out ILCursor[] _, instr => instr.MatchBrfalse(out successLabel));
        // if (successLabel == null)
        //     return;

        ILLabel skipLabel = cursor.DefineLabel();
        cursor.Emit(OpCodes.Ldloc_0);
        cursor.Emit(OpCodes.Callvirt, typeof(Pincushion).GetMethod("IsProjectileFragile", BindingFlags.Static | BindingFlags.NonPublic));
        cursor.Emit(OpCodes.Brfalse, skipLabel);
        // cursor.Emit(OpCodes.Ldc_I4_1);
        // cursor.Emit(OpCodes.Call, typeof(PhysicsEngine).GetMethod("set_SkipCollision", BindingFlags.Static | BindingFlags.Public));
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(skipLabel);
    }

    private static bool IsProjectileFragile(Projectile possiblyNullProjectile)
    {
        return possiblyNullProjectile?.GetComponent<VeryFragileNeedle>();
    }
}


public class VeryFragileNeedle : MonoBehaviour
{
    // private Projectile _projectile;
    // private PlayerController _owner;
    // private void Start()
    // {
    //     this._projectile = base.GetComponent<Projectile>();
    //     this._owner = this._projectile.Owner as PlayerController;

    //     this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    //     // this._projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
    // }

    // private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    // {
    //     if (!other.GetComponent<AIActor>())
    //     {
    //         this._projectile.DieInAir();
    //         PhysicsEngine.SkipCollision = true;
    //     }
    // }

    // private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
    // {
    //     this._projectile.DieInAir();
    //     PhysicsEngine.SkipCollision = true;
    // }
}
