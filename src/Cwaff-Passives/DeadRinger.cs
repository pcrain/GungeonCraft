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
    /* TODO:
        - Look into logic in ConsumableStealthItem
    */
    public class DeadRinger : PassiveItem
    {
        public static string ItemName         = "Dead Ringer";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/dead_ringer_icon";
        public static string ShortDescription = "Tactical Defeat";
        public static string LongDescription  = "Feigh death and become stealthed upon taking damage. Shooting while stealthed deals 10x damage and removes stealth.\n\nDeveloped by the French government for use by their elite secret agents in case of their inevitable failure, this marvelous gadget takes making lemonade out of lemons to the next level.";

        internal const float _DEAD_RINGER_DAMAGE_MULT = 10.0f;
        internal static GameObject _CorpsePrefab;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<DeadRinger>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.A;
            _CorpsePrefab     = BraveResources.Load("Global Prefabs/PlayerCorpse") as GameObject;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReceivedDamage += this.OnReceivedDamage;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnReceivedDamage -= this.OnReceivedDamage;
            return base.Drop(player);
        }

        private void OnReceivedDamage(PlayerController player)
        {
            if (this.Owner != player || player.IsStealthed)
                return;
            FeignDeath();
            BecomeInvisible();
        }

        public override void Update()
        {
            base.Update();
            if (!this.Owner.IsStealthed)
                return;
        }

        private void FeignDeath()
        {
            AkSoundEngine.PostEvent("spy_uncloak_feigndeath", this.Owner.gameObject);
            StartCoroutine(AnimateTheCorpse(this.Owner));
        }

        private IEnumerator AnimateTheCorpse(PlayerController pc)
        {
            tk2dBaseSprite deathSprite = pc.sprite;
            Vector3 deathScale         = pc.sprite.scale;
            Vector3 deathPosition      = pc.sprite.transform.position;

            GameObject corpse          = SpawnManager.SpawnDebris(_CorpsePrefab, pc.transform.position, Quaternion.identity);
            tk2dSprite corpseSprite    = corpse.GetComponent<tk2dSprite>();
            corpseSprite.SetSprite(pc.sprite.Collection, pc.sprite.spriteId);
            tk2dSpriteAnimator animator = corpseSprite.gameObject.GetOrAddComponent<tk2dSpriteAnimator>();
            string feignDeathAnimation = ((!pc.UseArmorlessAnim) ? "death_coop" : "death_coop_armorless");
            // string feignDeathAnimation = "spinfall";
            tk2dSpriteAnimationClip deathClip = pc.spriteAnimator.GetClipByName(feignDeathAnimation);

            animator.Play(deathClip, clipStartTime: 0f, overrideFps: 8f, skipEvents: true);
            yield return null;
            while (animator.IsPlaying(deathClip))
                yield return null;
            corpseSprite.scale = deathScale;
            corpse.transform.position = deathPosition;
            corpseSprite.HeightOffGround = -3.5f;
            corpseSprite.UpdateZDepth();
        }

        // copied and simplified from DoEffect() of CardboardBoxItem.cs
        private void BecomeInvisible()
        {
            this.Owner.CurrentGun?.CeaseAttack(false);
            this.Owner.OnDidUnstealthyAction += BreakStealth;
            this.Owner.PostProcessProjectile += SneakAttackProcessor;
            // if (!CanAnyBossOrNPCSee(this.Owner)) // don't need this check, we can feign death in front of them
            this.Owner.SetIsStealthed(true, "DeadRinger");
            this.Owner.SetCapableOfStealing(true, "DeadRinger");

            // Apply a shadowy shader
            foreach (Material m in this.Owner.SetOverrideShader(ShaderCache.Acquire("Brave/Internal/HighPriestAfterImage")))
            {
                m.SetFloat("_EmissivePower", 0f);
                m.SetFloat("_Opacity", 0.5f);
                m.SetColor("_DashColor", Color.gray);
            }

            DoSmokeAroundPlayer(8);
        }

        private void BreakStealth(PlayerController pc)
        {
            if (this.Owner != pc)
                return;
            this.Owner.ClearOverrideShader();
            this.Owner.OnDidUnstealthyAction -= BreakStealth;
            this.Owner.PostProcessProjectile -= SneakAttackProcessor;
            this.Owner.SetIsStealthed(false, "DeadRinger");
            this.Owner.SetCapableOfStealing(false, "DeadRinger");
            AkSoundEngine.PostEvent("medigun_heal_detach", this.Owner.gameObject);
            DoSmokeAroundPlayer(8);
        }

        private void SneakAttackProcessor(Projectile proj, float _)
        {
            if (this.Owner?.IsStealthed ?? false)
                proj.baseData.damage *= _DEAD_RINGER_DAMAGE_MULT;
        }

        private void DoSmokeAroundPlayer(int amount)
        {
            GameObject smokePrefab = ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof") as GameObject;
            Vector2 ppos = this.Owner.sprite.WorldCenter;
            for (int i = 0; i < amount; ++i)
            {
                GameObject smoke = UnityEngine.Object.Instantiate(smokePrefab);
                tk2dBaseSprite sprite = smoke.GetComponent<tk2dBaseSprite>();
                sprite.PlaceAtPositionByAnchor((ppos + Lazy.RandomVector(
                    UnityEngine.Random.Range(0f,0.5f))).ToVector3ZisY(), tk2dBaseSprite.Anchor.MiddleCenter);
                sprite.transform.position = sprite.transform.position.Quantize(0.0625f);
            }
        }
    }
}
