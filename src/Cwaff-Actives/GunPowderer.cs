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
    class GunPowderer : PlayerItem
    {
        public static string ItemName         = "Gun Powderer";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/gun_powderer_icon";
        public static string ShortDescription = "Ground Up Guns";
        public static string LongDescription  = "(Converts nearest dropped gun to 1-5 spread ammo boxes depending on its remaining ammo percentage)";

        private const float _MAX_DIST = 5f;

        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<GunPowderer>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.A;
            item.consumable   = false;
            item.CanBeDropped = true;
            item.SetCooldownType(ItemBuilder.CooldownType.Timed, 2f);
        }

        public override void DoEffect(PlayerController user)
        {
            Gun nearestGun    = null;
            float nearestDist = _MAX_DIST;
            foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
            {
                if (!debris.IsPickupObject)
                    continue;
                if (debris.GetComponentInChildren<PickupObject>()?.GetComponent<Gun>() is not Gun gun)
                    continue;

                float gunDist = (gun.sprite.WorldCenter - user.sprite.WorldCenter).magnitude;
                if (gunDist >= nearestDist)
                    continue;

                nearestGun  = gun;
                nearestDist = gunDist;
            }
            if (nearestGun)
                ConvertGunToAmmo(nearestGun);
        }

        private void ConvertGunToAmmo(Gun gun)
        {
            float ammoPercent    = (float)gun.CurrentAmmo / (float)gun.AdjustedMaxAmmo;
            int ammoBoxesToSpawn = Mathf.Max(1, Mathf.FloorToInt(ammoPercent * 5f));

            Vector2 spawnCenter = gun.sprite.WorldCenter;
            Lazy.DoSmokeAt(spawnCenter);
            UnityEngine.Object.Destroy(gun.gameObject);

            for (int i = 1; i <= ammoBoxesToSpawn; ++i)
            {
                float angle = (float)i * (360f / ammoBoxesToSpawn);
                StartCoroutine(SpawnSomeAmmo(spawnCenter + angle.ToVector(1.5f), 0.25f * i));
            }
        }

        private IEnumerator SpawnSomeAmmo(Vector2 pos, float delay)
        {
            yield return new WaitForSeconds(delay);
            LootEngine.SpawnItem(ItemHelper.Get(Items.PartialAmmo).gameObject, pos, spawnDirection: Vector2.zero, force: 0f, doDefaultItemPoof: true);
            yield break;
        }
    }
}
