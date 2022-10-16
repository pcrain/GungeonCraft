using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class InfantryGrenade : SpawnObjectPlayerItem
    {
        public static void Init()
        {
            string itemName = "Infantry Grenade";

            string resourceName = "NevernamedsItems/Resources/ThrowableActives/infantrygrenade_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<InfantryGrenade>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Cheap, but Efficient";
            string longDesc = "A paltry explosive device carried by infantry soldiers from a far off land."+"\n\nHas a weak blast, but can be slung multiple times in quick succession.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 50);
            item.consumable = false;

            item.objectToSpawn = BuildPrefab();

            item.tossForce = 7;
            item.canBounce = true;

            item.IsCigarettes = false;
            item.RequireEnemiesInRoom = false;

            item.SpawnRadialCopies = false;
            item.RadialCopiesToSpawn = 0;

            item.AudioEvent = "Play_OBJ_bomb_fuse_01";
            item.IsKageBunshinItem = false;

            item.quality = PickupObject.ItemQuality.D;
            InfantryGrenadeID = item.PickupObjectId;
        }
        public static int InfantryGrenadeID;

        public static GameObject BuildPrefab()
        {

            var bomb = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/ThrowableActives/infantrygrenade_throw_001.png", new GameObject("InfantryGrenade"));
            bomb.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bomb);

            var animator = bomb.AddComponent<tk2dSpriteAnimator>();
            var collection = (PickupObjectDatabase.GetById(108) as SpawnObjectPlayerItem).objectToSpawn.GetComponent<tk2dSpriteAnimator>().Library.clips[0].frames[0].spriteCollection;

            var deployAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/infantrygrenade_throw_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/infantrygrenade_throw_002.png", collection),

            }, "infantrygrenade_throw", tk2dSpriteAnimationClip.WrapMode.Once);
            deployAnimation.fps = 12;
            foreach (var frame in deployAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            var explodeAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {

                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/infantrygrenade_explode_001.png", collection),

            }, "infantrygrenade_explode", tk2dSpriteAnimationClip.WrapMode.Once);
            explodeAnimation.fps = 30;
            foreach (var frame in explodeAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            var armedAnimation = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/infantrygrenade_throw_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/infantrygrenade_throw_002.png", collection),

            }, "infantrygrenade_primed", tk2dSpriteAnimationClip.WrapMode.LoopSection);
            armedAnimation.fps = 10.0f;
            armedAnimation.loopStart = 0;
            foreach (var frame in armedAnimation.frames)
            {
                frame.eventLerpEmissiveTime = 0.5f;
                frame.eventLerpEmissivePower = 30f;
            }

            var audioListener = bomb.AddComponent<AudioAnimatorListener>();
            audioListener.animationAudioEvents = new ActorAudioEvent[]
            {
                new ActorAudioEvent
                {
                    eventName = "Play_OBJ_mine_beep_01",
                    eventTag = "beep"
                }
            };

            ProximityMine proximityMine = new ProximityMine
            {
                explosionData = new ExplosionData
                {
                    useDefaultExplosion = false,
                    doDamage = true,
                    forceUseThisRadius = false,
                    damageRadius = 3f,
                    damageToPlayer = 0,
                    damage = 30,
                    breakSecretWalls = false,
                    secretWallsRadius = 3.5f,
                    forcePreventSecretWallDamage = false,
                    doDestroyProjectiles = true,
                    doForce = true,
                    pushRadius = 3,
                    force = 25,
                    debrisForce = 12.5f,
                    preventPlayerForce = false,
                    explosionDelay = 0.1f,
                    usesComprehensiveDelay = false,
                    comprehensiveDelay = 0,
                    playDefaultSFX = false,

                    doScreenShake = true,
                    ss = new ScreenShakeSettings
                    {
                        magnitude = 1,
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
                    effect = StaticExplosionDatas.explosiveRoundsExplosion.effect,
                    freezeEffect = null,
                },
                explosionStyle = ProximityMine.ExplosiveStyle.TIMED,
                detectRadius = 3.5f,
                explosionDelay = 1f,
                usesCustomExplosionDelay = false,
                customExplosionDelay = 0.1f,
                deployAnimName = "infantrygrenade_throw",
                explodeAnimName = "infantrygrenade_explode",
                idleAnimName = "infantrygrenade_primed",



                MovesTowardEnemies = false,
                HomingTriggeredOnSynergy = false,
                HomingDelay = 3.25f,
                HomingRadius = 10,
                HomingSpeed = 4,

            };

            var boom = bomb.AddComponent<ProximityMine>(proximityMine);


            return bomb;
        }
    }
}