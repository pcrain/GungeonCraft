using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Dungeonator;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class TableTechNology : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Table Tech-Nology";
            string resourceName = "NevernamedsItems/Resources/tabletechguon_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<TableTechNology>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "";
            string longDesc = "";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.EXCLUDED;

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

            Projectile projectile = ProjectileUtility.SetupProjectile(86);
            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/redbeam_seg_001", new Vector2(18, 2), new Vector2(0, 8), BeamAnimPaths, 8,
                ImpactAnimPaths, 13, new Vector2(4, 4), new Vector2(7, 7));

            projectile.baseData.damage = 20;
            projectile.baseData.range *= 2;
            projectile.baseData.speed *= 4;
            projectile.gameObject.AddComponent<NoCollideBehaviour>();
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            beamComp.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp.interpolateStretchedBones = false;
            beamComp.penetration++;
            beamComp.PenetratesCover = true;
            lasa = projectile;
        }
        public static Projectile lasa;
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnTableFlipCompleted += this.DoLaser;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.OnTableFlipCompleted -= this.DoLaser;
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnTableFlipCompleted -= this.DoLaser;
            base.OnDestroy();
        }
        private void DoLaser(FlippableCover obj)
        {
            SpeculativeRigidbody rigidBody = obj.GetComponentInChildren<SpeculativeRigidbody>();
            
            PlayerController owner = base.Owner;
            Vector2 vector = DungeonData.GetIntVector2FromDirection(obj.DirectionFlipped).ToVector2();
            BeamController cont = BeamAPI.FreeFireBeamFromAnywhere(lasa, owner, obj.gameObject, rigidBody.UnitCenter,  vector.ToAngle(), 20, true);


            GameObject gameObject = SpawnManager.SpawnProjectile(lasa.gameObject, rigidBody.UnitCenter, Quaternion.identity, true);
            Projectile component = gameObject.GetComponent<Projectile>();
            component.Owner = owner;
            BeamController component2 = gameObject.GetComponent<BeamController>();

            component2.chargeDelay = 0f;
            component2.usesChargeDelay = false;
            component2.Owner = owner;
            component2.HitsPlayers = false;
            component2.HitsEnemies = true;
            component2.Direction = vector;
            component2.Origin = rigidBody.UnitCenter;
            GameManager.Instance.Dungeon.StartCoroutine(HandleFreeFiringBeam(component2, rigidBody, 20));
            
        }
        private IEnumerator HandleFreeFiringBeam(BeamController beam, SpeculativeRigidbody otherShooter, float duration)
        {
            float elapsed = 0f;
            yield return null;
            while (elapsed < duration)
            {
                Vector2 sourcePos;
                if (otherShooter == null) { break; }
                if (!otherShooter.GetComponentInParent<FlippableCover>()) break;
                if (otherShooter.GetComponentInParent<FlippableCover>().IsBroken) break;
                else sourcePos = otherShooter.UnitCenter;

                elapsed += BraveTime.DeltaTime;
                if (sourcePos != null)
                {
                    beam.Origin = sourcePos;
                    beam.LateUpdatePosition(sourcePos);
                }
                else { ETGModConsole.Log("SOURCEPOS WAS NULL IN BEAM FIRING HANDLER"); }
                yield return null;
            }
            beam.CeaseAttack();
            yield break;
        }
    }
}