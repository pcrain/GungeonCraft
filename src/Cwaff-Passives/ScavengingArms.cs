namespace CwaffingTheGungy;

public class ScavengingArms : CwaffPassive
{
    public static string ItemName         = "Scavenging Arms";
    public static string ShortDescription = "Loot Boxes";
    public static string LongDescription  = "Room decorations (crates, statues, etc.) have a chance of spawning a small ammo pickup when broken by running or rolling into them. Each pickup restores 10% of a single gun's ammo.";
    public static string Lore             = "Scavenging is a lost art in the age of shops, guaranteed chests, and random loot drops. After all, why would anyone risk their life drawing fire away from the miscellaneous objects littering the Gungeon on the off chance that some of it may be salvageable as a couple extra rounds of ammunition?\n\nHubris. The answer is hubris.";

    private const float _FIND_AMMO_CHANCE     = 0.05f;
    private const float _AMMO_PERCENT_TO_GAIN = 0.1f;

    internal static GameObject _SmallAmmoPickup;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ScavengingArms>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        _SmallAmmoPickup  = ItemHelper.Get(Items.PartialAmmo).gameObject.ClonePrefab();

        AmmoPickup ap = _SmallAmmoPickup.GetComponent<AmmoPickup>();
            ap.SpreadAmmoCurrentGunPercent       = _AMMO_PERCENT_TO_GAIN;
            ap.SpreadAmmoOtherGunsPercent        = 0.0f;

            // shenanigans for adding a new clip
            tk2dSpriteAnimation library              = ap.spriteAnimator.library;
            int oldLength                            = library.clips.Length;
            Array.Resize(ref library.clips, oldLength + 1);
            tk2dSpriteAnimationFrame referenceFrame  = library.clips[ap.spriteAnimator.defaultClipId].frames[0];
            tk2dSpriteAnimationClip boxClip          = VFX.Create("blue_ammobox_pickup", fps: 8).GetComponent<tk2dSpriteAnimator>().library.clips[0];
            library.clips[oldLength]                 = boxClip;
            ap.spriteAnimator.defaultClipId          = oldLength;
            ap.spriteAnimator.deferNextStartClip     = false;

            // fix the shaders and material properties to match those of the vanilla ammo boxes
            Material oldMat = referenceFrame.spriteCollection.spriteDefinitions[referenceFrame.spriteId].material;
            tk2dSpriteDefinition[] defs = boxClip.frames[0].spriteCollection.spriteDefinitions;
            foreach (tk2dSpriteAnimationFrame frame in boxClip.frames)
                defs[frame.spriteId].CopyMaterialProps(oldMat);

        // ap.minimapIcon.GetComponent<tk2dSprite>().SetSprite(VFX.Collection, clip.frames[0].spriteId);
        ap.minimapIcon = null; //TODO: nuking the minimap icon since i can't find the base game reference...put back later if i can make a good-looking new one
    }

    // NOTE: called by patch in CwaffPatches
    internal static void HandleCollisionWithMinorBreakable(SpeculativeRigidbody myRigidbody, SpeculativeRigidbody otherRigidBody)
    {
        if (UnityEngine.Random.value > _FIND_AMMO_CHANCE) //NOTE: called first because it's the fastest / most-likely failure point
            return; // unlucky :/
        if (otherRigidBody.GetComponent<PlayerController>() is not PlayerController player)
            return; // not broken by player
        if (!player.HasPassive<ScavengingArms>())
            return; // no scavenging arms
        if (player.CurrentGun.InfiniteAmmo || player.CurrentGun.LocalInfiniteAmmo || !player.CurrentGun.CanGainAmmo)
            return; // gun can't gain ammo

        LootEngine.SpawnItem(_SmallAmmoPickup, myRigidbody.UnitCenter, Vector2.zero, 0f, false);
        AkSoundEngine.PostEvent("knife_gun_hit", player.gameObject);
    }
}
