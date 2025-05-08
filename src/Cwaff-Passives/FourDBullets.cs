namespace CwaffingTheGungy;

public class FourDBullets : CwaffPassive
{
    public static string ItemName         = "4D Bullets";
    public static string ShortDescription = "No-Clipping Clips"; //"Thinking Outside the Tesseract";
    public static string LongDescription  = "Bullets can phase through the inner walls of a room, but lose 1/3 of their power after phasing.";
    public static string Lore             = "Wall hacks are almost universally despised for the completely one-sided advantage they give to those who possess them. Although most Gungeoneers aren't concerned for the Gundead's feelings, wall hacks also unfortunately don't exist in the real world. Historical attempts to create guns and gadgets that emulate wall hacks have generally ended in the loss of lives and, in some extreme cases, the loss of some pretty nice guns. The latest generation of technology has at least partially succeeded in emulating wall hacks by augmenting guns to shoot bullets into the 4th dimension. However, due to most Gungeoneers' inability to see said 4th dimension, their bullets tend to ricochet off walls in 4D space, losing a lot of their oomph in the process.";

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<FourDBullets>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.A;
        item.AddToShop(ModdedShopType.TimeTrader);
        item.AddToShop(ModdedShopType.Doug);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessProjectile += this.PostProcessProjectile;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.PostProcessProjectile -= this.PostProcessProjectile;
    }

    private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
    {
        if (this.Owner)
            proj.gameObject.AddComponent<PhaseThroughInnerWallsBehavior>();
    }
}

public class PhaseThroughInnerWallsBehavior : MonoBehaviour
{
    private const float _PHASE_DAMAGE_SCALING = 0.667f;
    private const float _ROOM_BORDER_WIDTH = 1f; // number of cell lengths that make up each room's border
    private const float _LENIENCE = 0.5f; // prevents certain projectiles that leave debris from getting stuck in the wall
    private const float _INSET = _ROOM_BORDER_WIDTH + _LENIENCE;

    private Projectile _projectile;
    private PlayerController _owner;
    private RoomHandler _startingRoom;

    private bool _phased = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._startingRoom = this._projectile.transform.position.GetAbsoluteRoom();
        this._projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
    }

    private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
    {
        if (this._projectile.transform.position.GetAbsoluteRoom() != this._startingRoom)
            return; // left our original room
        if (!this._startingRoom.GetBoundingRect().Inset(_INSET).Contains(this._projectile.transform.position))
            return; // outside the room

        PhysicsEngine.SkipCollision = true;
        if (this._phased)
            return;

        this._phased = true;
        if (this._owner && !this._owner.HasSynergy(Synergy.PROJECTING_MUCH))
            this._projectile.baseData.damage *= _PHASE_DAMAGE_SCALING;

        if (this._projectile.sprite is tk2dBaseSprite sprite)
        {
            sprite.usesOverrideMaterial = true;
            sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");
        }
        base.gameObject.PlayUnique("phase_through_wall_sound");
    }
}
