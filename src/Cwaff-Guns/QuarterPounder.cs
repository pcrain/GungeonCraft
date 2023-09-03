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
    public class QuarterPounder : AdvancedGunBehavior
    {
        public static string ItemName         = "Quarter Pounder";
        public static string SpriteName       = "quarter_pounder";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "Pay Per Pew";
        public static string LongDescription  = "(shoots money O:)";

        internal static tk2dSpriteAnimationClip _ProjSprite;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                        = 1.1f;
                gun.DefaultModule.angleVariance       = 15.0f;
                gun.quality                           = PickupObject.ItemQuality.D;
                gun.barrelOffset.transform.localPosition = new Vector3(1.8125f, 0.5f, 0f); // should match "Casing" in JSON file
                gun.SetAnimationFPS(gun.shootAnimation, 24);
                gun.SetAnimationFPS(gun.reloadAnimation, 16);

            var comp = gun.gameObject.AddComponent<QuarterPounder>();
                comp.SetFireAudio("fire_coin_sound");
                comp.SetReloadAudio("coin_gun_reload");

            _ProjSprite = AnimateBullet.CreateProjectileAnimation(
                new List<string> {
                    "coin-gun-projectile1",
                    "coin-gun-projectile2",
                    "coin-gun-projectile3",
                    "coin-gun-projectile4",
                    "coin-gun-projectile5",
                    "coin-gun-projectile6",
                    "coin-gun-projectile7",
                    "coin-gun-projectile8",
                    "coin-gun-projectile9",
                    "coin-gun-projectile10",
                }, 2, true, new IntVector2(9, 6),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.speed   = 44.0f;
                projectile.baseData.damage  = 20f;
                projectile.transform.parent = gun.barrelOffset;
                projectile.AddAnimation(_ProjSprite);
                projectile.SetAnimation(_ProjSprite);
                projectile.gameObject.AddComponent<MidasProjectile>();
        }

        public class MidasProjectile : MonoBehaviour
        {
            private void Start()
            {
                Projectile p = base.GetComponent<Projectile>();
                p.OnHitEnemy += this.OnHitEnemy;
            }

            private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool what)
            {
                // ETGModConsole.Log($"making a gold texture");
                Texture2D goldSprite = MakeSpriteGoldenTexture(enemy.sprite);

                // ETGModConsole.Log($"making a game object");
                GameObject g = UnityEngine.Object.Instantiate(new GameObject(), enemy.sprite.WorldBottomCenter, Quaternion.identity);
                    // ETGModConsole.Log($"making a gold collection");
                    tk2dSpriteCollectionData collection = SpriteBuilder.ConstructCollection(g, "goldcollection");
                    // ETGModConsole.Log($"making a gold sprite");
                    int spriteId = SpriteBuilder.AddSpriteToCollection(goldSprite, collection, "goldsprite");
                    // ETGModConsole.Log($"adding a gold sprite");
                    tk2dBaseSprite sprite = g.AddComponent<tk2dSprite>();
                        sprite.SetSprite(collection, spriteId);
                        sprite.usesOverrideMaterial = true;
                        // sprite.renderer.material.shader = ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader");
            }
        }

        public static Color _Gold = new Color(1f,1f,0f,1f);
        public static Texture2D MakeSpriteGoldenTexture(tk2dBaseSprite sprite)
        {
            Texture2D spriteTexture = sprite.CurrentSprite.DesheetTexture();
            Texture2D goldTexture = new Texture2D(spriteTexture.width, spriteTexture.height);
            for (int x = 0; x < spriteTexture.width; x++)
            {
                for (int y = 0; y < spriteTexture.height; y++)
                {
                    Color pixelColor = spriteTexture.GetPixel(x, y);
                    if (pixelColor.a > 0)
                        pixelColor = Color.Lerp(pixelColor, _Gold, 0.5f); // Blend opaque pixels
                    goldTexture.SetPixel(x, y, pixelColor);
                }
            }
            return goldTexture;
        }


        // protected override void OnPickedUpByPlayer(PlayerController player)
        // {
        //     base.OnPickedUpByPlayer(player);
        // }

        // protected override void OnPostDroppedByPlayer(PlayerController player)
        // {
        //     base.OnPostDroppedByPlayer(player);
        // }
    }
}
