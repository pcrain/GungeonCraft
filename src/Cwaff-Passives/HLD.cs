using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

using Gungeon;

namespace CwaffingTheGungy
{
    public class HLD : PassiveItem
    {
        public static string passiveName      = "HLD";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/yellow_bandana_icon";
        public static string shortDescription = "Hyper Light Dasher";
        public static string longDescription  = "(Pyoooom)";

        private bool dodgeButtonHeld = false;
        private bool isDashing = false;
        private PlayerController owner = null;

        private static GameObject LinkVFXPrefab;

        public static void Init()
        {
            PickupObject item = Lazy.SetupItem<HLD>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality      = PickupObject.ItemQuality.A;

            LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            FakePrefab.MarkAsFakePrefab(LinkVFXPrefab);
            UnityEngine.Object.DontDestroyOnLoad(LinkVFXPrefab);
        }

        private IEnumerator DoDash(PlayerController player, float dashspeed, float dashtime)
        {
            Vector2 vel = dashspeed * player.m_lastNonzeroCommandedDirection.normalized;
            string anim = (Mathf.Abs(vel.y) > Mathf.Abs(vel.x)) ? (vel.y > 0 ? "slide_up" : "slide_down") : "slide_right";
            bool hasAnim = player.spriteAnimator.GetClipByName(anim) != null;

            AkSoundEngine.PostEvent("teledash", player.gameObject);
            player.SetInputOverride("hld");
            player.SetIsFlying(true, "hld");

            DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
            for (int i = 0; i < 16; ++i)
            {
                float dir = UnityEngine.Random.Range(0.0f,360.0f);
                float rot = UnityEngine.Random.Range(0.0f,360.0f);
                float mag = UnityEngine.Random.Range(0.3f,1.25f);
                SpawnManager.SpawnVFX(
                    dusts.rollLandDustup,
                    player.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                    Quaternion.Euler(0f, 0f, rot));
            }

            bool interrupted = false;
            for (float timer = 0.0f; timer < dashtime; )
            {
                player.PlayerAfterImage();
                timer += BraveTime.DeltaTime;
                player.specRigidbody.Velocity = vel;
                GameManager.Instance.Dungeon.dungeonDustups.InstantiateLandDustup(player.sprite.WorldCenter);
                // if (hasAnim && !player.spriteAnimator.IsPlaying(anim))
                //     player.spriteAnimator.Play(anim);  //TODO: the sliding animation itself causes the player to be invincible??? (QueryGroundedFrame())
                yield return null;
                if (player.IsFalling)
                {
                    interrupted = true;
                    break;
                }
            }
            if (!interrupted)
            {
                player.PlayerAfterImage();
                for (int i = 0; i < 8; ++i)
                {
                    float dir = UnityEngine.Random.Range(0.0f,360.0f);
                    float rot = UnityEngine.Random.Range(0.0f,360.0f);
                    float mag = UnityEngine.Random.Range(0.3f,1.0f);
                    SpawnManager.SpawnVFX(
                        dusts.rollLandDustup,
                        player.sprite.WorldCenter + Lazy.AngleToVector(dir, mag),
                        Quaternion.Euler(0f, 0f, rot));
                }
            }
            player.spriteAnimator.Stop();
            player.SetIsFlying(false, "hld");
            player.ClearInputOverride("hld");
            this.isDashing = false;
        }

        private void StartDash(PlayerController player)
        {
            const float DASH_SPEED = 50.0f; // Speed of our dash
            const float DASH_TIME = 0.1f; // Time we spend dashing
            const float DISOWN_TIME = DASH_TIME+0.05f; // Amount of time after our dash starts before lightning is no longer connected
            const float FADE_TIME = 0.5f; // Amount of time lightning persists after being disowned

            this.isDashing = true;
            Projectile p = SpawnManager.SpawnProjectile(
              TestLightning.defaultProjectile.gameObject,
              player.sprite.WorldCenter,
              Quaternion.Euler(0f, 0f, player.m_currentGunAngle),
              true).GetComponent<Projectile>();
                p.Owner = player;
                p.Shooter = player.specRigidbody;

                p.gameObject.AddComponent<FakeProjectileComponent>();
                p.gameObject.AddComponent<Expiration>().expirationTimer = DISOWN_TIME+FADE_TIME;

                OwnerConnectLightningModifier oclm = p.gameObject.AddComponent<OwnerConnectLightningModifier>();
                    oclm.linkPrefab = LinkVFXPrefab;
                    oclm.disownTimer = DISOWN_TIME;
                    oclm.fadeTimer = FADE_TIME;
                    oclm.MakeGlowy();

            player.StartCoroutine(DoDash(player, DASH_SPEED, DASH_TIME));
        }

        public override void Update()
        {
            if (!this.owner)
                return;

            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(this.owner.PlayerIDX);
            if (instanceForPlayer.ActiveActions.DodgeRollAction.IsPressed)
            {
                instanceForPlayer.ConsumeButtonDown(GungeonActions.GungeonActionType.DodgeRoll);
                // if (!(this.owner.IsDodgeRolling || this.owner.IsFalling || this.owner.IsInputOverridden || this.dodgeButtonHeld || this.isDashing))
                if (this.owner.AcceptingNonMotionInput && !(this.owner.IsDodgeRolling || this.dodgeButtonHeld || this.isDashing))
                {
                    this.dodgeButtonHeld = true;
                    StartDash(this.owner);
                }
            }
            else
                this.dodgeButtonHeld = false;
        }

        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            // TODO: uncomment this later if we want to be invulnerable while dashing
            // if(!isDashing)
            //     return;
            // Projectile component = otherRigidbody.GetComponent<Projectile>();
            // if (component != null && !(component.Owner is PlayerController))
            // {
            //     PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
            //     PhysicsEngine.SkipCollision = true;
            // }
        }

        public override void Pickup(PlayerController player)
        {
            this.owner = player;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            this.owner = null;
            isDashing = false;
            return base.Drop(player);
        }

        public override void OnDestroy()
        {
            isDashing = false;
            base.OnDestroy();
        }
    }
}

