
using SaveAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class Lvl2Molotov : SpawnObjectPlayerItem
    {
        public static void Init()
        {
            string itemName = "Lvl. 2 Molotov";

            string resourceName = "NevernamedsItems/Resources/ThrowableActives/lvl2molotov_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Lvl2Molotov>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "BURN IT DOWN";
            string longDesc = "A souped up molotov, mixed together using organic, non-gmo, locally grown magical chemicals.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 800);
            item.consumable = false;

            item.objectToSpawn = BuildPrefab();

            item.tossForce = 12;
            item.canBounce = false;

            item.IsCigarettes = false;
            item.RequireEnemiesInRoom = false;

            item.SpawnRadialCopies = false;
            item.RadialCopiesToSpawn = 0;

            item.AudioEvent = null;
            item.IsKageBunshinItem = false;

            item.quality = PickupObject.ItemQuality.D;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_CONVICT, true);
        }


        public static GameObject BuildPrefab()
        {

            var bomb = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_spin_001.png", new GameObject("Lvl2Molotov"));
            bomb.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bomb);

            var animator = bomb.AddComponent<tk2dSpriteAnimator>();
            var collection = (PickupObjectDatabase.GetById(108) as SpawnObjectPlayerItem).objectToSpawn.GetComponent<tk2dSpriteAnimator>().Library.clips[0].frames[0].spriteCollection;

            //DEPLOYMENT
            var deployAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_spin_004.png", collection),
            }, "lvl2mol_throw", tk2dSpriteAnimationClip.WrapMode.Once);
            deployAnimation.fps = 12;
            foreach (var frame in deployAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            var explodeAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {

                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_burst_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_burst_002.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_burst_003.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_burst_004.png", collection),

            }, "lvl2mol_burst", tk2dSpriteAnimationClip.WrapMode.Once);
            explodeAnimation.fps = 16;
            foreach (var frame in explodeAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            var armedAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_spin_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_spin_002.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_spin_003.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/lvl2molotov_spin_004.png", collection),

            }, "lvl2mol_default", tk2dSpriteAnimationClip.WrapMode.LoopSection);
            armedAnimation.fps = 10.0f;
            armedAnimation.loopStart = 0;
            foreach (var frame in armedAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            CustomThrowableObject throwable = new CustomThrowableObject
            {
                doEffectOnHitGround = true,
                OnThrownAnimation = "lvl2mol_throw",
                OnHitGroundAnimation = "lvl2mol_burst",
                DefaultAnim = "lvl2mol_default",
                destroyOnHitGround = false,
                thrownSoundEffect = "Play_OBJ_item_throw_01",
                effectSoundEffect = "Play_OBJ_glassbottle_shatter_01",
            };
            bomb.AddComponent<CustomThrowableObject>(throwable);
            bomb.AddComponent<LvL2MolotovEffect>();
            return bomb;
        }
        public class LvL2MolotovEffect : CustomThrowableEffectDoer
        {
            public override void OnEffect(GameObject obj)
            {
                DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.GreenFireDef);
                goop.TimedAddGoopCircle(obj.transform.position, 4, 0.75f, true);
                StartCoroutine(Kill(obj));
            }
            private IEnumerator Kill(GameObject obj)
            {
                yield return new WaitForSeconds(0.25f);
                UnityEngine.Object.Destroy(obj);
                yield break;
            }
        }

    }
}