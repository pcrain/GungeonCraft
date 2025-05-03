namespace CwaffingTheGungy;

public class PogoGun : CwaffGun
{
    public static string ItemName         = "Pogo Gun";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private static readonly Vector3 _PogoLeftHandOffset = new Vector3(-3/16f, 17/16f, 0f);
    private static readonly Vector3 _PogoRightHandOffset = new Vector3(4/16f, 17/16f, 0f);

    private PogoStick  _pogoItem  = null;
    private tk2dSprite _leftHand  = null;
    private tk2dSprite _rightHand = null;
    private bool       _wasOnPogo = false;
    private bool       _onPogo    => this._pogoItem && this._pogoItem._active;

    public static void Init()
    {
        Lazy.SetupGun<PogoGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.EXCLUDED, gunClass: GunClass.RIFLE, reloadTime: 1.5f, ammo: 1000, infiniteAmmo: true, shootFps: 30,
            muzzleFrom: Items.Mailbox, fireAudio: "pogo_gun_shoot_sound", smoothReload: 0.1f)
          .SetReloadAudio("rogo_dodge_sound", 3, 6, 9)
          .SetReloadAudio("pogo_gun_reload_sound", 18)
          .InitProjectile(GunData.New(baseProjectile: Items.Ak47.Projectile(), sprite: null, clipSize: 12, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 4.5f, speed: 30f, range: 18f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound"));
    }

    private void CheckPogoActive()
    {
        if (this._pogoItem)
        {
            if (this._pogoItem._owner == this.PlayerOwner)
                return;
            this._pogoItem = null;
        }
        if (this.PlayerOwner is not PlayerController pc)
            return;
        if (pc.GetActive<PogoStick>() is not PogoStick pogo)
            return;
        this._pogoItem = pogo;
    }

    private void EnableRenderers(PlayerController pc = null)
    {
        if (!pc)
            pc = this.PlayerOwner;
        pc.ToggleGunRenderers(true, ItemName);
        pc.ToggleHandRenderers(true, ItemName);
        if (this._rightHand)
            this._rightHand.renderer.enabled = false;
        if (this._leftHand)
            this._leftHand.renderer.enabled = false;
    }

    private void DisableRenderers(PlayerController pc = null)
    {
        if (!pc)
            pc = this.PlayerOwner;
        pc.ToggleGunRenderers(false, ItemName);
        pc.ToggleHandRenderers(false, ItemName);
        if (!this._rightHand && pc.primaryHand)
        {
            this._rightHand = new GameObject().AddComponent<tk2dSprite>();
            this._rightHand.SetSprite(pc.primaryHand.sprite.collection, pc.primaryHand.sprite.spriteId);
        }
        if (!this._leftHand && pc.primaryHand)
        {
            this._leftHand = new GameObject().AddComponent<tk2dSprite>();
            this._leftHand.SetSprite(pc.primaryHand.sprite.collection, pc.primaryHand.sprite.spriteId);
        }

        tk2dSprite pogoSprite = this._pogoItem ? this._pogoItem._attachedPogoSprite : null;
        if (pogoSprite)
        {
            if (this._rightHand)
            {
                this._rightHand.renderer.enabled = true;
                this._rightHand.transform.parent = pogoSprite.transform;
                this._rightHand.transform.position = pogoSprite.transform.position + _PogoRightHandOffset;
                this._rightHand.HeightOffGround = 2f;
                this._rightHand.UpdateZDepth();
            }
            if (this._leftHand)
            {
                this._leftHand.renderer.enabled = true;
                this._leftHand.transform.parent = pogoSprite.transform;
                this._leftHand.transform.position = pogoSprite.transform.position + _PogoLeftHandOffset;
                this._leftHand.HeightOffGround = 2f;
                this._leftHand.UpdateZDepth();
            }
        }
        else
        {
            if (this._rightHand)
                this._rightHand.renderer.enabled = false;
            if (this._leftHand)
                this._leftHand.renderer.enabled = false;
        }
    }

    private void OnTriedToInitiateAttack(PlayerController player)
    {
        if (this && player.CurrentGun == this.gun && this._onPogo)
          player.SuppressThisClick = true; // can't be fired while riding Pogo Stick
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        //
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        this._wasOnPogo = false;
        if (this.PlayerOwner is PlayerController player)
        {
            player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
            player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
        }
        Update();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        EnableRenderers();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        this._wasOnPogo = false;
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
        Update();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        EnableRenderers(player);
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        EnableRenderers();
        if (this.PlayerOwner)
            this.PlayerOwner.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        if (this._rightHand)
        {
            this._rightHand.gameObject.transform.parent = null;
            UnityEngine.Object.Destroy(this._rightHand.gameObject);
        }
        if (this._leftHand)
        {
            this._leftHand.gameObject.transform.parent = null;
            UnityEngine.Object.Destroy(this._leftHand.gameObject);
        }
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        CheckPogoActive();
        bool onPogo = this._onPogo;
        if (onPogo == this._wasOnPogo)
            return;

        this._wasOnPogo = onPogo;
        if (onPogo)
            DisableRenderers();
        else
            EnableRenderers();
    }
}
