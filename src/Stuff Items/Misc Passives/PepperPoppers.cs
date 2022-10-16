using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class PepperPoppers : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Pepper Poppers";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/pepperpoppers_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PepperPoppers>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Popping Off";
            string longDesc = "Launches young Gungeon Peppers." + "\n\nA spicy dish of stuffed Gungeon Peppers, and an important part of the Gungeon Pepper life cycle.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            genericPopper = BuildPrefab();

            PopperProjectile = (PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0].InstantiateAndFakeprefab();
            PopperProjectile.baseData.damage = 5f;
            PopperProjectile.baseData.speed *= 0.6f;
            PopperProjectile.SetProjectileSpriteRight("popperproj_1", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 16, 7);
            SpawnObjectBehaviour spawnpopper = PopperProjectile.gameObject.AddComponent<SpawnObjectBehaviour>();
            spawnpopper.objectToSpawn = genericPopper;

            PopperProjectile.AnimateProjectile(new List<string> {
                "popperproj_1",
                "popperproj_2",
                "popperproj_3",
                "popperproj_4",
            }, 12, true, AnimateBullet.ConstructListOfSameValues(new IntVector2(16, 7), 4), AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), AnimateBullet.ConstructListOfSameValues(true, 4), AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));
        }
        public static Projectile PopperProjectile;
        public static GameObject genericPopper;
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            StartCoroutine(DelayedPep(sourceProjectile, effectChanceScalar));
        }
        private IEnumerator DelayedPep(Projectile sourceProjectile, float effectChanceScalar)
        {
            yield return null;
            float chance = 0.1f;
            if (sourceProjectile && sourceProjectile.ProjectilePlayerOwner())
            {
                if (sourceProjectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Pepper X")) chance = 0.15f;
             chance *= effectChanceScalar;
                if (UnityEngine.Random.value <= chance)
                {
                    Projectile instancePopper = PopperProjectile.InstantiateAndFireInDirection(sourceProjectile.transform.position, sourceProjectile.Direction.ToAngle(), 5, sourceProjectile.ProjectilePlayerOwner()).GetComponent<Projectile>();
                    instancePopper.Owner = sourceProjectile.ProjectilePlayerOwner();
                    instancePopper.Shooter = sourceProjectile.ProjectilePlayerOwner().specRigidbody;
                    if (sourceProjectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Pepper X")) Owner.DoPostProcessProjectile(instancePopper);
                }
            }
            yield break;
        }
        private void PostProcessBeamChanceTick(BeamController beam)
        {
            if (beam && beam.projectile)
            {
                if (UnityEngine.Random.value <= 0.1f)
                {
                    if (beam.projectile.ProjectilePlayerOwner())
                    {
                        Projectile instancePopper = PopperProjectile.InstantiateAndFireInDirection(beam.Origin, beam.Direction.ToAngle(), 5, beam.projectile.ProjectilePlayerOwner()).GetComponent<Projectile>();
                        instancePopper.Owner = beam.projectile.ProjectilePlayerOwner();
                        instancePopper.Shooter = beam.projectile.ProjectilePlayerOwner().specRigidbody;
                    }
                }
            }
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessBeamChanceTick += PostProcessBeamChanceTick;
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void DisableEffect(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeamChanceTick -= PostProcessBeamChanceTick;
            base.DisableEffect(player);
        }
        public static GameObject BuildPrefab()
        {
            var popper = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_idle_001.png", new GameObject("Popper"));
            popper.SetActive(false);
            FakePrefab.MarkAsFakePrefab(popper);

            var animator = popper.AddComponent<tk2dSpriteAnimator>();
            var collection = (PickupObjectDatabase.GetById(108) as SpawnObjectPlayerItem).objectToSpawn.GetComponent<tk2dSpriteAnimator>().Library.clips[0].frames[0].spriteCollection;

            var popperDeactivate = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_002.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_003.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_004.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_005.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_006.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_007.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_008.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_deactivate_009.png", collection),

            }, "popper_deactivate", tk2dSpriteAnimationClip.WrapMode.LoopSection);
            popperDeactivate.fps = 12;
            popperDeactivate.loopStart = 8;

            var popperIdle = SpriteBuilder.AddAnimation(animator, collection, new List<int>
            {
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_idle_001.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_idle_002.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_idle_003.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_idle_004.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_idle_005.png", collection),
                SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/ThrowableActives/PepperPopper/popper_idle_006.png", collection),

            }, "popper_idle", tk2dSpriteAnimationClip.WrapMode.Loop);
            popperIdle.fps = 12f;
            animator.DefaultClipId = animator.GetClipIdByName("popper_idle");
            animator.playAutomatically = true;

            popper.AddComponent<PopperController>();

            return popper;
        }
    }
    public class PopperController : GameObjectDamageAura
    {
        public PopperController()
        {
            this.secondsTillDeath = 20;
        }
        public override void Start()
        {
            isDead = false;
            timer = secondsTillDeath;
            if (base.GetComponent<SpeculativeRigidbody>()) body = base.GetComponent<SpeculativeRigidbody>();
            if (base.GetComponent<tk2dSprite>()) sprite = base.GetComponent<tk2dSprite>();
            if (base.GetComponent<tk2dSpriteAnimator>()) animator = base.GetComponent<tk2dSpriteAnimator>();
            base.Start();
        }
        private void FixedUpdate()
        {
            if (base.gameObject != null && !isDead)
            {
                if (CenterPosition.GetAbsoluteRoom() == null || GameManager.Instance.PrimaryPlayer.CurrentRoom == null) { KeelOver(); }
                else if (GameManager.Instance.PrimaryPlayer != null && GameManager.Instance.PrimaryPlayer.CurrentRoom != null && CenterPosition.GetAbsoluteRoom() != null)
                {
                    if (GameManager.Instance.PrimaryPlayer.CurrentRoom != CenterPosition.GetAbsoluteRoom()) { KeelOver(); }
                }
                if (timer > 0) { timer -= BraveTime.DeltaTime; }
                if (timer <= 0 ) { KeelOver(); }
            }
        }
        public void KeelOver()
        {
            if (isDead) return;
            isDead = true;
            if (animator) animator.Play("popper_deactivate");
            base.damageAuraActivated = false;
            if (base.gameObject.GetComponent<DebrisObject>()) base.gameObject.GetComponent<DebrisObject>().Priority = EphemeralObject.EphemeralPriority.Minor;
        }
        public override void TickedOnEnemy(AIActor enemy)
        {
            if (GameManager.Instance.AnyPlayerHasActiveSynergy("Pickled Poppers")) enemy.ApplyEffect(StaticStatusEffects.irradiatedLeadEffect);
            base.TickedOnEnemy(enemy);
        }
        private Vector2 CenterPosition
        {
            get
            {
                if (body) return body.UnitCenter;
                if (sprite) return sprite.WorldCenter;
                return transform.position;
            }
        }
        private SpeculativeRigidbody body;
        private tk2dSprite sprite;
        private tk2dSpriteAnimator animator;

        public float secondsTillDeath;
        private float timer;
        private bool isDead;
    }


}