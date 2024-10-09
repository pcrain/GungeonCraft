namespace CwaffingTheGungy;

/* TODO:
    - red barrel, "Exposive Barrel"
    - red drum, "Exposive Drum"
    - blue drum, "Water Drum"
    - purple drum, "Oil Drum"
    - yellow drum, "Poison Drum"
*/

public class Overflow : CwaffGun
{
    public static string ItemName         = "Overflow";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const float _FILL_RATE = 0.04f; // time per unit of ammo refilled while attached to a barrel

    internal static tk2dSpriteAnimationClip _Hose = null;

    private KickableObject _attachedBarrel = null;
    private CwaffBezierMesh _cable = null;
    private float _fillTimer = 0.0f;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Overflow>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 100, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound",
                rampUpFireRate: true);

        gun.InitProjectile(GunData.New(sprite: null, clipSize: -1, cooldown: 0.05f, shootStyle: ShootStyle.Automatic,
            damage: 9.0f, speed: 25f, range: 18f, force: 12f));

        _Hose = VFX.Create("overflow_hose").DefaultAnimation();
    }

    private void ConnectBarrel(KickableObject barrel)
    {
        DisconnectBarrel();
        this._fillTimer = 0.0f;
        this._attachedBarrel = barrel;
        this._cable = CwaffBezierMesh.Create(_Hose, this._attachedBarrel.sprite.WorldCenter, this.gun.PrimaryHandAttachPoint.position);
        this._cable.gameObject.SetLayerRecursively(LayerMask.NameToLayer("BG_Critical"));
    }

    private void DisconnectBarrel()
    {
        if (this._cable)
            UnityEngine.Object.Destroy(this._cable.gameObject);
        this._cable = null;
        this._attachedBarrel = null;
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        DisconnectBarrel();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DisconnectBarrel();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        DisconnectBarrel();
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        if (!this._attachedBarrel || !this._attachedBarrel.isActiveAndEnabled)
            DisconnectBarrel();
        if (this._cable)
        {
            this._cable.startPos = this._attachedBarrel.sprite.WorldCenter;
            this._cable.endPos = this.gun.PrimaryHandAttachPoint.position;
            if ((this._fillTimer += BraveTime.DeltaTime) > _FILL_RATE)
            {
                this._fillTimer -= _FILL_RATE;
                if (this.gun.CurrentAmmo < this.gun.AdjustedMaxAmmo)
                    this.gun.GainAmmo(1);
            }
        }
        else
            this._fillTimer = 0.0f;
    }

    //NOTE: Only works if GainsRateOfFireAsContinueAttack is true (i.e., rampUpFireRate: true is set in attributes)
    public override float GetDynamicFireRate() => Mathf.Clamp((float)this.gun.CurrentAmmo / (float)this.gun.AdjustedMaxAmmo, 0.1f, 1.0f);

    [HarmonyPatch(typeof(KickableObject), nameof(KickableObject.Interact))]
    private class KickableObjectInteractPatch
    {
        static bool Prefix(KickableObject __instance, PlayerController player)
        {
            if (player.CurrentGun is Gun gun && gun.GetComponent<Overflow>() is Overflow overflow)
            {
                overflow.ConnectBarrel(__instance);
                return false;    // skip the original method
            }
            return true;
        }
    }
}
