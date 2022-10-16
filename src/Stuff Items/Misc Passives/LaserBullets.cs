using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class LaserBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Laser Bullets";
            string resourceName = "NevernamedsItems/Resources/laserbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<LaserBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Hybrid";
            string longDesc = "A beam and bullet crossbreed that defies nature, these bullets make your bullets shoot... beams!";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/redbeam_seg_001",
                "NevernamedsItems/Resources/BeamSprites/redbeam_seg_002",
                "NevernamedsItems/Resources/BeamSprites/redbeam_seg_003",
                "NevernamedsItems/Resources/BeamSprites/redbeam_seg_004"
            };
            List<string> ImpactAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/redbeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/redbeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/redbeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/redbeam_impact_004",
            };

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/redbeam_seg_001", new Vector2(18, 2), new Vector2(0, 8), BeamAnimPaths, 8,
                ImpactAnimPaths, 13, new Vector2(4, 4), new Vector2(7, 7));
            projectile.gameObject.SetActive(false);
            projectile.baseData.damage *= 4;
            projectile.baseData.range *= 2;
            projectile.baseData.speed *= 4;
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            beamComp.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp.interpolateStretchedBones = false;
            beamComp.ContinueBeamArtToWall = true;
            SimpleRedBeam = projectile;
        }
        public static Projectile SimpleRedBeam;
        private void PostProcess(Projectile bullet, float thing)
        {
            float procChance = 0.2f;
            procChance *= thing;
            if (UnityEngine.Random.value <= procChance && bullet.GetComponent<BeamBulletsBehaviour>() == null && bullet.GetComponent<BulletIsFromBeam>() == null)
            {
                BeamBulletsBehaviour beambullets = bullet.gameObject.AddComponent<BeamBulletsBehaviour>();
                if (UnityEngine.Random.value <= 0.5f) beambullets.firetype = BeamBulletsBehaviour.FireType.PLUS;
                else beambullets.firetype = BeamBulletsBehaviour.FireType.CROSS;
                if (Owner.PlayerHasActiveSynergy("new Vector2(x, y)")) beambullets.firetype = BeamBulletsBehaviour.FireType.STAR;
                beambullets.beamToFire = SimpleRedBeam;

                List<Projectile> ValidBeams = new List<Projectile>();
                if (Owner.PlayerHasActiveSynergy("Beam Me Up!"))
                {
                    if (UnityEngine.Random.value <= 0.2f)
                    {
                        for (int j = 0; j < Owner.inventory.AllGuns.Count; j++)
                        {
                            if (Owner.inventory.AllGuns[j] && !Owner.inventory.AllGuns[j].InfiniteAmmo)
                            {
                                ProjectileModule defaultModule = Owner.inventory.AllGuns[j].DefaultModule;
                                if (defaultModule.shootStyle == ProjectileModule.ShootStyle.Beam)
                                {
                                    ValidBeams.Add(defaultModule.GetCurrentProjectile());
                                }
                            }
                        }
                        beambullets.beamToFire = BraveUtility.RandomElement(ValidBeams);
                    }
                }

                bullet.baseData.speed *= 0.2f;
                bullet.UpdateSpeed();
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.PostProcess;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcess;

            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcess;
            }
            base.OnDestroy();
        }
    }

}