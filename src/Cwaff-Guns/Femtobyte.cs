namespace CwaffingTheGungy;

using static Femtobyte.HoldType;
using static Femtobyte.HoldSize;

/* trap prefabs that might be of interest
    - trap_spinning_log_vertical_resizable.prefab
    - trap_spike_gungeon_2x2.prefab
*/

public class Femtobyte : CwaffGun
{
    public static string ItemName         = "Femtobyte";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int _MAX_SLOTS = 8;

    public enum HoldType { EMPTY, TABLE, BARREL, TRAP, CHEST, ENEMY, PICKUP }
    public enum HoldSize { SMALL, MEDIUM, LARGE, HUGE }

    public class DigitizedObject
    {
        private HoldType   _type          = EMPTY;
        private GameObject _prefab        = null;
        private List<int>  _chestContents = null;
        private string     _enemyGuid     = null;
        private int        _pickupID      = -1;
        private int        _slotSpan      = -1;
    }

    public List<DigitizedObject> _digitizedObjects = new();
    private int _currentSlot = 0;

    public bool TryToDigitize(SpeculativeRigidbody body)
    {
        if (body.GetComponent<AIActor>() is AIActor enemy)
        {
            CwaffShaders.Digitize(enemy.sprite);
            enemy.EraseFromExistenceWithRewards();
            return true;
        }
        if (body.GetComponent<PickupObject>() is PickupObject pickup)
        {
            CwaffShaders.Digitize(pickup.sprite);
            UnityEngine.Object.Destroy(pickup.gameObject);
            return true;
        }
        if (body.GetComponent<Chest>() is Chest chest)
        {
            CwaffShaders.Digitize(chest.sprite);
            UnityEngine.Object.Destroy(chest.gameObject);
            return true;
        }
        if (body.GetComponent<KickableObject>() is KickableObject barrel)
        {
            CwaffShaders.Digitize(barrel.sprite);
            UnityEngine.Object.Destroy(barrel.gameObject);
            return true;
        }
        if (body.gameObject.transform.parent is Transform tp && tp.gameObject.GetComponent<FlippableCover>() is FlippableCover table)
        {
            CwaffShaders.Digitize(table.sprite);
            UnityEngine.Object.Destroy(table.gameObject);
            return true;
        }
        return false;
    }

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Femtobyte>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 1.1f, ammo: 9999, canGainAmmo: false,
                shootFps: 24, reloadFps: 16,  fireAudio: "fire_coin_sound", reloadAudio: "coin_gun_reload", banFromBlessedRuns: true);

        gun.InitProjectile(GunData.New(clipSize: 10, angleVariance: 15.0f, shootStyle: ShootStyle.SemiAutomatic, damage: 20.0f, speed: 44.0f,
          sprite: "femtobyte_projectile", fps: 2, anchor: Anchor.MiddleCenter)).Attach<FemtobyteProjectile>();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.gameObject.GetComponent<FemtobyteProjectile>() is not FemtobyteProjectile fp)
            return;
        fp.Setup(this);
    }
}

public class FemtobyteProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Femtobyte _femtobyte;

    public void Setup(Femtobyte femtobyte)
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._femtobyte = femtobyte;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody body, PixelCollider myCollider, SpeculativeRigidbody otherBody, PixelCollider otherCollider)
    {
        if (!this._owner || !this._projectile || !this._femtobyte || !otherBody)
            return;
        if (!this._femtobyte.TryToDigitize(otherBody))
            return;
        this._projectile.DieInAir(false, false, false, false);
        PhysicsEngine.SkipCollision = true;
    }

    private void Update()
    {
        if (!this._owner || !this._projectile || !this._femtobyte)
            return;
        IPlayerInteractable nearestIxable = this._owner.CurrentRoom.GetNearestInteractable(this._projectile.SafeCenter, 1f, this._owner);
        if (nearestIxable is not PickupObject pickup || pickup.IsBeingEyedByRat)
            return;
        if (pickup.GetComponent<SpeculativeRigidbody>() is not SpeculativeRigidbody body)
            return;
        if (!this._femtobyte.TryToDigitize(body))
            return;
        this._projectile.DieInAir(false, false, false, false);
    }
}


public class ProjectileBehavior : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
    }

    private void Update()
    {
      // enter update code here
    }

    private void OnDestroy()
    {
      // enter destroy code here
    }
}
