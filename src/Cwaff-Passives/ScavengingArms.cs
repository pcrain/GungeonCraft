namespace CwaffingTheGungy;

public class ScavengingArms : PassiveItem
{
    public static string ItemName         = "Scavenging Arms";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _FIND_AMMO_CHANCE     = 0.05f;
    private const float _AMMO_PERCENT_TO_GAIN = 0.1f;

    private static GameObject _SmallAmmoPickup;
    private static int _ScavengingArmsId;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<ScavengingArms>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        _ScavengingArmsId   = item.PickupObjectId;
        tk2dSpriteAnimationClip clip = VFX.Create("blue_ammobox_pickup", fps: 8).GetComponent<tk2dSpriteAnimator>().library.clips[0];
        _SmallAmmoPickup = ItemHelper.Get(Items.PartialAmmo).gameObject.ClonePrefab();
        AmmoPickup ap = _SmallAmmoPickup.GetComponent<AmmoPickup>();
        ap.SpreadAmmoCurrentGunPercent = _AMMO_PERCENT_TO_GAIN;
        ap.SpreadAmmoOtherGunsPercent = 0.0f;

        ap.spriteAnimator.library.clips[0] = clip;
        ap.spriteAnimator.defaultClipId = 0;
        ap.spriteAnimator.deferNextStartClip = false;

        // ap.minimapIcon.GetComponent<tk2dSprite>().SetSprite(VFX.Collection, clip.frames[0].spriteId);
        ap.minimapIcon = null; //TODO: nuking the minimap icon since i can't find the base game reference...put back later if i can make a good-looking new one
    }

    // NOTE: called by patch in CwaffPatches
    private static void HandleCollisionWithMinorBreakable(SpeculativeRigidbody myRigidbody, SpeculativeRigidbody otherRigidBody)
    {
        if (UnityEngine.Random.value > _FIND_AMMO_CHANCE) //NOTE: called first because it's the fastest / most-likely failure point
            return; // unlucky :/
        if (otherRigidBody.GetComponent<PlayerController>() is not PlayerController player)
            return; // not broken by player
        if (!player.HasPassiveItem(_ScavengingArmsId))
            return; // no scavenging arms
        if (player.CurrentGun.InfiniteAmmo || player.CurrentGun.LocalInfiniteAmmo || !player.CurrentGun.CanGainAmmo)
            return; // gun can't gain ammo

        LootEngine.SpawnItem(_SmallAmmoPickup, myRigidbody.UnitCenter, Vector2.zero, 0f, false);
        AkSoundEngine.PostEvent("knife_gun_hit", player.gameObject);
    }
}
