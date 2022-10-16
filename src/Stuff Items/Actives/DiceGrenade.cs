using Alexandria.ItemAPI;
using SaveAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class DiceGrenade : SpawnObjectPlayerItem
    {
        public static void Init()
        {
            string itemName = "Dice Grenade";

            string resourceName = "NevernamedsItems/Resources/ThrowableActives/dicegrenade_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<DiceGrenade>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Die, Fool!";
            string longDesc = "A rare collectors item, two explosions are never quite the same." + "\n\nDesigned for the assassination of tabletop roleplayers, this one was put up for resale online after a series of continually poor rolls.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 190);
            item.consumable = false;

            item.objectToSpawn = BuildPrefab();

            item.tossForce = 5;
            item.canBounce = true;

            item.IsCigarettes = false;
            item.RequireEnemiesInRoom = false;

            item.SpawnRadialCopies = false;
            item.RadialCopiesToSpawn = 0;

            item.AudioEvent = "Play_OBJ_bomb_fuse_01";
            item.IsKageBunshinItem = false;

            item.quality = PickupObject.ItemQuality.D;
        }


        public static GameObject BuildPrefab()
        {

            var bomb = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/ThrowableActives/dicegrenade_primed_001.png", new GameObject("DiceBomb"));
            bomb.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bomb);

            var animator = bomb.AddComponent<tk2dSpriteAnimator>();
            var collection = (PickupObjectDatabase.GetById(108) as SpawnObjectPlayerItem).objectToSpawn.GetComponent<tk2dSpriteAnimator>().Library.clips[0].frames[0].spriteCollection;

            //DEPLOYMENT
            var deployAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_002.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_003.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_004.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_005.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_006.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_007.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_008.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_009.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_throw_010.png", collection),
            }, "dicebomb_thrown", tk2dSpriteAnimationClip.WrapMode.Once);
            deployAnimation.fps = 20; //Dice bomb takes 1 second to explode, so I set this to double the amount of frames in the animation to make sure this animation takes half the time to play.
            foreach (var frame in deployAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            var explodeAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {

                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_explode_001.png", collection),

            }, "dicebomb_explode", tk2dSpriteAnimationClip.WrapMode.Once);
            explodeAnimation.fps = 16;
            foreach (var frame in explodeAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            var armedAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_primed_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_primed_002.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_primed_003.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_primed_004.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_primed_005.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/dicegrenade_primed_006.png", collection),

            }, "dicebomb_primed", tk2dSpriteAnimationClip.WrapMode.LoopSection);
            armedAnimation.fps = 12f;
            armedAnimation.loopStart = 4;
            foreach (var frame in armedAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            CustomThrowableObject throwable = new CustomThrowableObject
            {
                OnThrownAnimation = "dicebomb_thrown",
                DefaultAnim = "dicebomb_primed",
                OnEffectAnim = "dicebomb_explode",
                thrownSoundEffect = "Play_OBJ_item_throw_01",
                destroyOnHitGround = false,
                doEffectOnHitGround = false,
                doEffectAfterTime = true,
                timeTillEffect = 1f

            };
            bomb.AddComponent<CustomThrowableObject>(throwable);
            bomb.AddComponent<DiceGrenadeEffect>();
            return bomb;
        }
        public class DiceGrenadeEffect : CustomThrowableEffectDoer
        {
            public override void OnEffect(GameObject obj)
            {
                float radius = UnityEngine.Random.Range(2f, 5f);
                float force = UnityEngine.Random.Range(25f, 50f);
                ExplosionData explosionData = new ExplosionData
                {
                    useDefaultExplosion = false,
                    doDamage = true,
                    forceUseThisRadius = false,
                    damageRadius = radius,
                    damageToPlayer = 0,
                    damage = UnityEngine.Random.Range(10f, 50f),
                    breakSecretWalls = UnityEngine.Random.value <= 0.5f,
                    secretWallsRadius = radius,
                    forcePreventSecretWallDamage = false,
                    doDestroyProjectiles = true,
                    doForce = true,
                    pushRadius = UnityEngine.Random.Range(3f, 10f),
                    force = force,
                    debrisForce = force,
                    preventPlayerForce = false,
                    explosionDelay = 0f,
                    usesComprehensiveDelay = false,
                    comprehensiveDelay = 0,
                    playDefaultSFX = false,

                    doScreenShake = true,
                    ss = new ScreenShakeSettings
                    {
                        magnitude = UnityEngine.Random.Range(1f, 3f),
                        speed = 6.5f,
                        time = 0.22f,
                        falloff = 0,
                        direction = new Vector2(0, 0),
                        vibrationType = ScreenShakeSettings.VibrationType.Auto,
                        simpleVibrationStrength = Vibration.Strength.Medium,
                        simpleVibrationTime = Vibration.Time.Normal
                    },
                    doStickyFriction = true,
                    doExplosionRing = true,
                    isFreezeExplosion = false,
                    freezeRadius = 5,
                    IsChandelierExplosion = false,
                    rotateEffectToNormal = false,
                    ignoreList = new List<SpeculativeRigidbody>(),
                    overrideRangeIndicatorEffect = null,
                    effect = radius > 3.5f ? StaticExplosionDatas.genericLargeExplosion.effect : StaticExplosionDatas.explosiveRoundsExplosion.effect,
                    freezeEffect = null,
                };
                if (obj.GetComponent<CustomThrowableObject>().SpawningPlayer.PlayerHasActiveSynergy("Roll With Advantage"))
                {
                    DeadlyDeadlyGoopManager goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(BraveUtility.RandomElement(new List<GoopDefinition>() {
                        EasyGoopDefinitions.BlobulonGoopDef,
                        EasyGoopDefinitions.CharmGoopDef,
                        EasyGoopDefinitions.CheeseDef,
                        EasyGoopDefinitions.FireDef,
                        EasyGoopDefinitions.GreenFireDef,
                        EasyGoopDefinitions.HoneyGoop,
                        EasyGoopDefinitions.OilDef,
                        EasyGoopDefinitions.PitGoop,
                        EasyGoopDefinitions.PlagueGoop,
                        EasyGoopDefinitions.PlayerFriendlyWebGoop,
                        EasyGoopDefinitions.WaterGoop
                    }));
                    goop.TimedAddGoopCircle(obj.transform.position, 4, 0.75f, true);
                }
                Exploder.Explode(obj.GetComponent<tk2dSprite>().WorldCenter, explosionData, Vector2.zero);
                StartCoroutine(Kill(obj));
            }
            private IEnumerator Kill(GameObject obj)
            {
                yield return new WaitForSeconds(0.1f);
                UnityEngine.Object.Destroy(obj);
                yield break;
            }
        }

    }
}