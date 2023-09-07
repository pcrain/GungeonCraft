using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class HolyWaterGun : AdvancedGunBehavior
    {
        public static string ItemName         = "Holy Water Gun";
        public static string SpriteName       = "holy_water_gun";
        public static string ProjectileName   = "10"; // water gun
        // public static string ProjectileName   = "86"; // marine sidearm
        public static string ShortDescription = "The Water, Gun, & Holy Soak";
        public static string LongDescription  = "(Deals some damage to jammed; killing a jammed enemy reduces curse by 1)";

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.MegaDouser) as Gun).gunSwitchGroup;
                gun.barrelOffset.transform.localPosition = new Vector3(1.9375f, 0.5f, 0f); // should match "Casing" in JSON file
                gun.muzzleFlashEffects.type              = VFXPoolType.None; // prevent visual glitch with non-rotating beam near gun muzzle
                gun.usesContinuousMuzzleFlash = false;
                gun.finalMuzzleFlashEffects.type         = VFXPoolType.None;

                gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
                gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
                gun.gunClass = GunClass.BEAM;

            var comp = gun.gameObject.AddComponent<HolyWaterGun>();

            // Dissect.CompareFieldsAndProperties<Projectile>(
            //     ((PickupObjectDatabase.GetById(10)) as Gun).DefaultModule.projectiles[0],
            //     ((PickupObjectDatabase.GetById(86)) as Gun).DefaultModule.projectiles[0]);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                // projectile.spriteAnimator.
                // projectile.gameObject.AddComponent<TestComp>();

            BasicBeamController beamComp = projectile.SetupBeamSprites(
              spriteName: "alphabeam", fps: 13, dims: new Vector2(15, 7), impactDims: new Vector2(7, 7));
                // beamComp.sprite.usesOverrideMaterial = true;
                // beamComp.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
                // beamComp.sprite.renderer.material.SetFloat("_EmissivePower", 100f);
                // beamComp.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
                // beamComp.sprite.renderer.material.SetColor("_EmissiveColor", ExtendedColours.paleYellow);
        }
    }

    public class TestComp : MonoBehaviour
    {
        private PlayerController _owner;
        private Projectile _proj;

        private void Start()
        {
            this._proj = base.GetComponent<Projectile>();
            this._owner = this._proj.Owner as PlayerController;
        }

        private void Update()
        {
            this._proj.specRigidbody.transform.rotation = (this._owner?.m_currentGunAngle ?? 0f).EulerZ();
        }
    }
}
