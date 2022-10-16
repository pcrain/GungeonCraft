using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class HandGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Hand Gun", "nnhandgun");
            Game.Items.Rename("outdated_gun_mods:hand_gun", "nn:hand_gun");
            gun.gameObject.AddComponent<HandGun>();
            gun.SetShortDescription("The Hand You've Been Dealt");
            gun.SetLongDescription("Brought to the Gungeon by infamous gambler Blast Eddie after the losing streak of his lifetime."+"\n\nThe characters depicted on the cards go back eons.");

            gun.SetupSprite(null, "nnhandgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(1.12f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(312);
            gun.ammo = 312;
            gun.gunClass = GunClass.SILLY;

            //------------------------------------------------------------HEARTS
            //Ace
            Projectile AceOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            AceOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(AceOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(AceOfHearts);
            AceOfHearts.baseData.damage *= 4f;
            AceOfHearts.baseData.range *= 3f;
            AceOfHearts.baseData.speed *= 0.5f;
            AceOfHearts.SetProjectileSpriteRight("aceofhearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(AceOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.ACE);
            //Queen
            Projectile QueenOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            QueenOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(QueenOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(QueenOfHearts);
            QueenOfHearts.baseData.damage *= 2.4f;
            QueenOfHearts.baseData.range *= 3f;
            QueenOfHearts.baseData.speed *= 0.5f;
            HomingModifier heartsHoming = QueenOfHearts.gameObject.GetOrAddComponent<HomingModifier>();
            heartsHoming.HomingRadius = 100;
            QueenOfHearts.SetProjectileSpriteRight("queenofhearts_projectile", 12, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(QueenOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.QUEEN);
            //King
            Projectile KingOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KingOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KingOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KingOfHearts);
            KingOfHearts.baseData.damage *= 2.6f;
            KingOfHearts.baseData.range *= 3f;
            KingOfHearts.baseData.speed *= 0.5f;
            PierceProjModifier heartsPiercing = KingOfHearts.gameObject.GetOrAddComponent<PierceProjModifier>();
            heartsPiercing.penetration += 5;
            BounceProjModifier heartsBouncing = KingOfHearts.gameObject.GetOrAddComponent<BounceProjModifier>();
            heartsBouncing.numberOfBounces += 5;
            KingOfHearts.SetProjectileSpriteRight("kingofhearts_projectile", 12, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(KingOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.KING);
            //Knave
            Projectile KnaveOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KnaveOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KnaveOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KnaveOfHearts);
            PlayerProjectileTeleportModifier teleport = KnaveOfHearts.gameObject.GetOrAddComponent<PlayerProjectileTeleportModifier>();
            KnaveOfHearts.baseData.damage *= 2.2f;
            KnaveOfHearts.baseData.range *= 4f;
            KnaveOfHearts.baseData.speed *= 0.5f;
            KnaveOfHearts.SetProjectileSpriteRight("knaveofhearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(KnaveOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.KNAVE);
            //Two of Hearts
            Projectile TwoOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TwoOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TwoOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TwoOfHearts);
            TwoOfHearts.baseData.damage *= 0.4f;
            TwoOfHearts.baseData.range *= 3f;
            TwoOfHearts.baseData.speed *= 0.5f;
            TwoOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(TwoOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Three of Hearts 
            Projectile ThreeOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            ThreeOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(ThreeOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(ThreeOfHearts);
            ThreeOfHearts.baseData.damage *= 0.6f;
            ThreeOfHearts.baseData.range *= 3f;
            ThreeOfHearts.baseData.speed *= 0.5f;
            ThreeOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(ThreeOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Four of Hearts
            Projectile FourOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FourOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FourOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FourOfHearts);
            FourOfHearts.baseData.damage *= 0.8f;
            FourOfHearts.baseData.range *= 3f;
            FourOfHearts.baseData.speed *= 0.5f;
            FourOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(FourOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Five of Hearts
            Projectile FiveOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FiveOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FiveOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FiveOfHearts);
            FiveOfHearts.baseData.damage *= 1f;
            FiveOfHearts.baseData.range *= 3f;
            FiveOfHearts.baseData.speed *= 0.5f;
            FiveOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(FiveOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Six of Hearts
            Projectile SixOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SixOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SixOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SixOfHearts);
            SixOfHearts.baseData.damage *= 1.2f;
            SixOfHearts.baseData.range *= 3f;
            SixOfHearts.baseData.speed *= 0.5f;
            SixOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(SixOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Seven of Hearts
            Projectile SevenOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SevenOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SevenOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SevenOfHearts);
            SevenOfHearts.baseData.damage *= 1.4f;
            SevenOfHearts.baseData.range *= 3f;
            SevenOfHearts.baseData.speed *= 0.5f;
            SevenOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(SevenOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Eight of Hearts
            Projectile EightOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            EightOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(EightOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(EightOfHearts);
            EightOfHearts.baseData.damage *= 1.6f;
            EightOfHearts.baseData.range *= 3f;
            EightOfHearts.baseData.speed *= 0.5f;
            EightOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(EightOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Nine of Hearts
            Projectile NineOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            NineOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(NineOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(NineOfHearts);
            NineOfHearts.baseData.damage *= 1.8f;
            NineOfHearts.baseData.range *= 3f;
            NineOfHearts.baseData.speed *= 0.5f;
            NineOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(NineOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);
            //Ten of Hearts
            Projectile TenOfHearts = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TenOfHearts.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TenOfHearts.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TenOfHearts);
            TenOfHearts.baseData.damage *= 2f;
            TenOfHearts.baseData.range *= 3f;
            TenOfHearts.baseData.speed *= 0.5f;
            TenOfHearts.SetProjectileSpriteRight("generichearts_projectile", 11, 13, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(TenOfHearts, HandGunCardBullet.CardSuit.HEARTS, HandGunCardBullet.CardValue.GENERIC);


            //------------------------------------------------------------DIAMONDS
            //Ace
            Projectile AceOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            AceOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(AceOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(AceOfDiamonds);
            AceOfDiamonds.baseData.damage *= 4f;
            AceOfDiamonds.baseData.range *= 3f;
            AceOfDiamonds.baseData.speed *= 0.5f;
            AceOfDiamonds.SetProjectileSpriteRight("aceofdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(AceOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.ACE);
            //Queen
            Projectile QueenOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            QueenOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(QueenOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(QueenOfDiamonds);
            QueenOfDiamonds.baseData.damage *= 2.4f;
            QueenOfDiamonds.baseData.range *= 3f;
            QueenOfDiamonds.baseData.speed *= 0.5f;
            HomingModifier diamondsHoming = QueenOfDiamonds.gameObject.GetOrAddComponent<HomingModifier>();
            diamondsHoming.HomingRadius = 100;
            QueenOfDiamonds.SetProjectileSpriteRight("queenofdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(QueenOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.QUEEN);
            //King
            Projectile KingOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KingOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KingOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KingOfDiamonds);
            KingOfDiamonds.baseData.damage *= 2.6f;
            KingOfDiamonds.baseData.range *= 3f;
            KingOfDiamonds.baseData.speed *= 0.5f;
            PierceProjModifier diamondsPiercing = KingOfDiamonds.gameObject.GetOrAddComponent<PierceProjModifier>();
            diamondsPiercing.penetration += 5;
            BounceProjModifier diamondsBouncing = KingOfDiamonds.gameObject.GetOrAddComponent<BounceProjModifier>();
            diamondsBouncing.numberOfBounces += 5;
            KingOfDiamonds.SetProjectileSpriteRight("kingofdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(KingOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.KING);
            //Knave
            Projectile KnaveOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KnaveOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KnaveOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KnaveOfDiamonds);
            PlayerProjectileTeleportModifier diamondsteleport = KnaveOfDiamonds.gameObject.GetOrAddComponent<PlayerProjectileTeleportModifier>();
            KnaveOfDiamonds.baseData.damage *= 2.2f;
            KnaveOfDiamonds.baseData.range *= 4f;
            KnaveOfDiamonds.baseData.speed *= 0.5f;
            KnaveOfDiamonds.SetProjectileSpriteRight("knaveofdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(KnaveOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.KNAVE);
            //Two of Diamonds
            Projectile TwoOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TwoOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TwoOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TwoOfDiamonds);
            TwoOfDiamonds.baseData.damage *= 0.4f;
            TwoOfDiamonds.baseData.range *= 3f;
            TwoOfDiamonds.baseData.speed *= 0.5f;
            TwoOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(TwoOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Three of Diamonds 
            Projectile ThreeOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            ThreeOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(ThreeOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(ThreeOfDiamonds);
            ThreeOfDiamonds.baseData.damage *= 0.6f;
            ThreeOfDiamonds.baseData.range *= 3f;
            ThreeOfDiamonds.baseData.speed *= 0.5f;
            ThreeOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(ThreeOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Four of Diamonds
            Projectile FourOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FourOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FourOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FourOfDiamonds);
            FourOfDiamonds.baseData.damage *= 0.8f;
            FourOfDiamonds.baseData.range *= 3f;
            FourOfDiamonds.baseData.speed *= 0.5f;
            FourOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(FourOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Five of Diamonds
            Projectile FiveOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FiveOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FiveOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FiveOfDiamonds);
            FiveOfDiamonds.baseData.damage *= 1f;
            FiveOfDiamonds.baseData.range *= 3f;
            FiveOfDiamonds.baseData.speed *= 0.5f;
            FiveOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(FiveOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Six of Diamonds
            Projectile SixOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SixOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SixOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SixOfDiamonds);
            SixOfDiamonds.baseData.damage *= 1.2f;
            SixOfDiamonds.baseData.range *= 3f;
            SixOfDiamonds.baseData.speed *= 0.5f;
            SixOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(SixOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Seven of Diamonds
            Projectile SevenOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SevenOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SevenOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SevenOfDiamonds);
            SevenOfDiamonds.baseData.damage *= 1.4f;
            SevenOfDiamonds.baseData.range *= 3f;
            SevenOfDiamonds.baseData.speed *= 0.5f;
            SevenOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(SevenOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Eight of Diamonds
            Projectile EightOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            EightOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(EightOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(EightOfDiamonds);
            EightOfDiamonds.baseData.damage *= 1.6f;
            EightOfDiamonds.baseData.range *= 3f;
            EightOfDiamonds.baseData.speed *= 0.5f;
            EightOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(EightOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Nine of Diamonds
            Projectile NineOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            NineOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(NineOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(NineOfDiamonds);
            NineOfDiamonds.baseData.damage *= 1.8f;
            NineOfDiamonds.baseData.range *= 3f;
            NineOfDiamonds.baseData.speed *= 0.5f;
            NineOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(NineOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);
            //Ten of Diamonds
            Projectile TenOfDiamonds = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TenOfDiamonds.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TenOfDiamonds.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TenOfDiamonds);
            TenOfDiamonds.baseData.damage *= 2f;
            TenOfDiamonds.baseData.range *= 3f;
            TenOfDiamonds.baseData.speed *= 0.5f;
            TenOfDiamonds.SetProjectileSpriteRight("genericdiamonds_projectile", 18, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 11, 7);
            DesignateType(TenOfDiamonds, HandGunCardBullet.CardSuit.DIAMONDS, HandGunCardBullet.CardValue.GENERIC);


            //------------------------------------------------------------SPADES
            //Ace
            Projectile AceOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            AceOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(AceOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(AceOfSpades);
            AceOfSpades.baseData.damage *= 8f;
            AceOfSpades.baseData.range *= 3f;
            AceOfSpades.baseData.speed *= 0.5f;
            AceOfSpades.ignoreDamageCaps = true;
            AceOfSpades.SetProjectileSpriteRight("aceofspades_projectile", 20, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 7);
            DesignateType(AceOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.ACE);
            //Queen
            Projectile QueenOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            QueenOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(QueenOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(QueenOfSpades);
            QueenOfSpades.baseData.damage *= 2.4f;
            QueenOfSpades.baseData.range *= 3f;
            QueenOfSpades.baseData.speed *= 0.5f;
            HomingModifier spadesHoming = QueenOfSpades.gameObject.GetOrAddComponent<HomingModifier>();
            spadesHoming.HomingRadius = 100;
            QueenOfSpades.SetProjectileSpriteRight("queenofspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(QueenOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.QUEEN);
            //King
            Projectile KingOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KingOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KingOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KingOfSpades);
            KingOfSpades.baseData.damage *= 2.6f;
            KingOfSpades.baseData.range *= 3f;
            KingOfSpades.baseData.speed *= 0.5f;
            PierceProjModifier spadesPiercing = KingOfSpades.gameObject.GetOrAddComponent<PierceProjModifier>();
            spadesPiercing.penetration += 5;
            BounceProjModifier spadesBouncing = KingOfSpades.gameObject.GetOrAddComponent<BounceProjModifier>();
            spadesBouncing.numberOfBounces += 5;
            KingOfSpades.SetProjectileSpriteRight("kingofspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(KingOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.KING);
            //Knave
            Projectile KnaveOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KnaveOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KnaveOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KnaveOfSpades);
            PlayerProjectileTeleportModifier spadesteleport = KnaveOfSpades.gameObject.GetOrAddComponent<PlayerProjectileTeleportModifier>();
            KnaveOfSpades.baseData.damage *= 2.2f;
            KnaveOfSpades.baseData.range *= 4f;
            KnaveOfSpades.baseData.speed *= 0.5f;
            KnaveOfSpades.SetProjectileSpriteRight("knaveofspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(KnaveOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.KNAVE);
            //Two of Spades
            Projectile TwoOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TwoOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TwoOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TwoOfSpades);
            TwoOfSpades.baseData.damage *= 0.4f;
            TwoOfSpades.baseData.range *= 3f;
            TwoOfSpades.baseData.speed *= 0.5f;
            TwoOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(TwoOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Three of Spades 
            Projectile ThreeOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            ThreeOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(ThreeOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(ThreeOfSpades);
            ThreeOfSpades.baseData.damage *= 0.6f;
            ThreeOfSpades.baseData.range *= 3f;
            ThreeOfSpades.baseData.speed *= 0.5f;
            ThreeOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(ThreeOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Four of Spades
            Projectile FourOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FourOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FourOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FourOfSpades);
            FourOfSpades.baseData.damage *= 0.8f;
            FourOfSpades.baseData.range *= 3f;
            FourOfSpades.baseData.speed *= 0.5f;
            FourOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(FourOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Five of Spades
            Projectile FiveOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FiveOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FiveOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FiveOfSpades);
            FiveOfSpades.baseData.damage *= 1f;
            FiveOfSpades.baseData.range *= 3f;
            FiveOfSpades.baseData.speed *= 0.5f;
            FiveOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(FiveOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Six of Spades
            Projectile SixOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SixOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SixOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SixOfSpades);
            SixOfSpades.baseData.damage *= 1.2f;
            SixOfSpades.baseData.range *= 3f;
            SixOfSpades.baseData.speed *= 0.5f;
            SixOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(SixOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Seven of Spades
            Projectile SevenOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SevenOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SevenOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SevenOfSpades);
            SevenOfSpades.baseData.damage *= 1.4f;
            SevenOfSpades.baseData.range *= 3f;
            SevenOfSpades.baseData.speed *= 0.5f;
            SevenOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(SevenOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Eight of Spades
            Projectile EightOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            EightOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(EightOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(EightOfSpades);
            EightOfSpades.baseData.damage *= 1.6f;
            EightOfSpades.baseData.range *= 3f;
            EightOfSpades.baseData.speed *= 0.5f;
            EightOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(EightOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Nine of Spades
            Projectile NineOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            NineOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(NineOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(NineOfSpades);
            NineOfSpades.baseData.damage *= 1.8f;
            NineOfSpades.baseData.range *= 3f;
            NineOfSpades.baseData.speed *= 0.5f;
            NineOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(NineOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);
            //Ten of Spades
            Projectile TenOfSpades = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TenOfSpades.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TenOfSpades.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TenOfSpades);
            TenOfSpades.baseData.damage *= 2f;
            TenOfSpades.baseData.range *= 3f;
            TenOfSpades.baseData.speed *= 0.5f;
            TenOfSpades.SetProjectileSpriteRight("genericspades_projectile", 14, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
            DesignateType(TenOfSpades, HandGunCardBullet.CardSuit.SPADES, HandGunCardBullet.CardValue.GENERIC);


            //------------------------------------------------------------CLUBS
            //Ace
            Projectile AceOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            AceOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(AceOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(AceOfClubs);
            AceOfClubs.baseData.damage *= 4f;
            AceOfClubs.baseData.range *= 3f;
            AceOfClubs.baseData.speed *= 0.5f;
            AceOfClubs.SetProjectileSpriteRight("aceofclubs_projectile", 15, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(AceOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.ACE);
            //Queen
            Projectile QueenOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            QueenOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(QueenOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(QueenOfClubs);
            QueenOfClubs.baseData.damage *= 2.4f;
            QueenOfClubs.baseData.range *= 3f;
            QueenOfClubs.baseData.speed *= 0.5f;
            HomingModifier clubsHoming = QueenOfClubs.gameObject.GetOrAddComponent<HomingModifier>();
            clubsHoming.HomingRadius = 100;
            QueenOfClubs.SetProjectileSpriteRight("queenofclubs_projectile", 15, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(QueenOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.QUEEN);
            //King
            Projectile KingOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KingOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KingOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KingOfClubs);
            KingOfClubs.baseData.damage *= 2.6f;
            KingOfClubs.baseData.range *= 3f;
            KingOfClubs.baseData.speed *= 0.5f;
            PierceProjModifier clubsPiercing = KingOfClubs.gameObject.GetOrAddComponent<PierceProjModifier>();
            clubsPiercing.penetration += 5;
            BounceProjModifier clubsBouncing = KingOfClubs.gameObject.GetOrAddComponent<BounceProjModifier>();
            clubsBouncing.numberOfBounces += 5;
            KingOfClubs.SetProjectileSpriteRight("kingofclubs_projectile", 15, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(KingOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.KING);
            //Knave
            Projectile KnaveOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            KnaveOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(KnaveOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(KnaveOfClubs);
            PlayerProjectileTeleportModifier clubsteleport = KnaveOfClubs.gameObject.GetOrAddComponent<PlayerProjectileTeleportModifier>();
            KnaveOfClubs.baseData.damage *= 2.2f;
            KnaveOfClubs.baseData.range *= 4f;
            KnaveOfClubs.baseData.speed *= 0.5f;
            KnaveOfClubs.SetProjectileSpriteRight("knaveofclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(KnaveOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.KNAVE);
            //Two of Clubs
            Projectile TwoOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TwoOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TwoOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TwoOfClubs);
            TwoOfClubs.baseData.damage *= 0.4f;
            TwoOfClubs.baseData.range *= 3f;
            TwoOfClubs.baseData.speed *= 0.5f;
            TwoOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(TwoOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Three of Clubs 
            Projectile ThreeOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            ThreeOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(ThreeOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(ThreeOfClubs);
            ThreeOfClubs.baseData.damage *= 0.6f;
            ThreeOfClubs.baseData.range *= 3f;
            ThreeOfClubs.baseData.speed *= 0.5f;
            ThreeOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(ThreeOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Four of Clubs
            Projectile FourOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FourOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FourOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FourOfClubs);
            FourOfClubs.baseData.damage *= 0.8f;
            FourOfClubs.baseData.range *= 3f;
            FourOfClubs.baseData.speed *= 0.5f;
            FourOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(FourOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Five of Clubs
            Projectile FiveOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            FiveOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(FiveOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(FiveOfClubs);
            FiveOfClubs.baseData.damage *= 1f;
            FiveOfClubs.baseData.range *= 3f;
            FiveOfClubs.baseData.speed *= 0.5f;
            FiveOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(FiveOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Six of Clubs
            Projectile SixOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SixOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SixOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SixOfClubs);
            SixOfClubs.baseData.damage *= 1.2f;
            SixOfClubs.baseData.range *= 3f;
            SixOfClubs.baseData.speed *= 0.5f;
            SixOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(SixOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Seven of Clubs
            Projectile SevenOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            SevenOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(SevenOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(SevenOfClubs);
            SevenOfClubs.baseData.damage *= 1.4f;
            SevenOfClubs.baseData.range *= 3f;
            SevenOfClubs.baseData.speed *= 0.5f;
            SevenOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(SevenOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Eight of Clubs
            Projectile EightOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            EightOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(EightOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(EightOfClubs);
            EightOfClubs.baseData.damage *= 1.6f;
            EightOfClubs.baseData.range *= 3f;
            EightOfClubs.baseData.speed *= 0.5f;
            EightOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(EightOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Nine of Clubs
            Projectile NineOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            NineOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(NineOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(NineOfClubs);
            NineOfClubs.baseData.damage *= 1.8f;
            NineOfClubs.baseData.range *= 3f;
            NineOfClubs.baseData.speed *= 0.5f;
            NineOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(NineOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);
            //Ten of Clubs
            Projectile TenOfClubs = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            TenOfClubs.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(TenOfClubs.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(TenOfClubs);
            TenOfClubs.baseData.damage *= 2f;
            TenOfClubs.baseData.range *= 3f;
            TenOfClubs.baseData.speed *= 0.5f;
            TenOfClubs.SetProjectileSpriteRight("genericclubs_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 9);
            DesignateType(TenOfClubs, HandGunCardBullet.CardSuit.CLUBS, HandGunCardBullet.CardValue.GENERIC);

            //HEARTS
            gun.DefaultModule.projectiles[0] = AceOfHearts; //0
            gun.DefaultModule.projectiles.Add(TwoOfHearts); //1
            gun.DefaultModule.projectiles.Add(ThreeOfHearts); //2
            gun.DefaultModule.projectiles.Add(FourOfHearts); //3
            gun.DefaultModule.projectiles.Add(FiveOfHearts); //4
            gun.DefaultModule.projectiles.Add(SixOfHearts); //5
            gun.DefaultModule.projectiles.Add(SevenOfHearts); //6 
            gun.DefaultModule.projectiles.Add(EightOfHearts); //7 
            gun.DefaultModule.projectiles.Add(NineOfHearts); //8
            gun.DefaultModule.projectiles.Add(TenOfHearts); //9
            gun.DefaultModule.projectiles.Add(KnaveOfHearts); //10 
            gun.DefaultModule.projectiles.Add(QueenOfHearts); //11
            gun.DefaultModule.projectiles.Add(KingOfHearts); //12
            //DIAMONDS
            gun.DefaultModule.projectiles.Add(AceOfDiamonds); //13
            gun.DefaultModule.projectiles.Add(TwoOfDiamonds); //14
            gun.DefaultModule.projectiles.Add(ThreeOfDiamonds); //15 
            gun.DefaultModule.projectiles.Add(FourOfDiamonds); //16
            gun.DefaultModule.projectiles.Add(FiveOfDiamonds); //17
            gun.DefaultModule.projectiles.Add(SixOfDiamonds); //18
            gun.DefaultModule.projectiles.Add(SevenOfDiamonds); //19
            gun.DefaultModule.projectiles.Add(EightOfDiamonds); //20
            gun.DefaultModule.projectiles.Add(NineOfDiamonds); //21
            gun.DefaultModule.projectiles.Add(TenOfDiamonds); //22 
            gun.DefaultModule.projectiles.Add(KnaveOfDiamonds); //23 
            gun.DefaultModule.projectiles.Add(QueenOfDiamonds);//24 
            gun.DefaultModule.projectiles.Add(KingOfDiamonds); //25
            //SPADES
            gun.DefaultModule.projectiles.Add(AceOfSpades); //26
            gun.DefaultModule.projectiles.Add(TwoOfSpades); //27 
            gun.DefaultModule.projectiles.Add(ThreeOfSpades); //28
            gun.DefaultModule.projectiles.Add(FourOfSpades); //29
            gun.DefaultModule.projectiles.Add(FiveOfSpades); //30
            gun.DefaultModule.projectiles.Add(SixOfSpades); //31
            gun.DefaultModule.projectiles.Add(SevenOfSpades); //32 
            gun.DefaultModule.projectiles.Add(EightOfSpades); //33
            gun.DefaultModule.projectiles.Add(NineOfSpades); //34
            gun.DefaultModule.projectiles.Add(TenOfSpades); //35
            gun.DefaultModule.projectiles.Add(KnaveOfSpades); //36
            gun.DefaultModule.projectiles.Add(QueenOfSpades); //37
            gun.DefaultModule.projectiles.Add(KingOfSpades); //38
            //CLUBS
            gun.DefaultModule.projectiles.Add(AceOfClubs); //39
            gun.DefaultModule.projectiles.Add(TwoOfClubs); //40
            gun.DefaultModule.projectiles.Add(ThreeOfClubs); //41
            gun.DefaultModule.projectiles.Add(FourOfClubs); //42
            gun.DefaultModule.projectiles.Add(FiveOfClubs); //43
            gun.DefaultModule.projectiles.Add(SixOfClubs); //44
            gun.DefaultModule.projectiles.Add(SevenOfClubs); //45
            gun.DefaultModule.projectiles.Add(EightOfClubs); //46 
            gun.DefaultModule.projectiles.Add(NineOfClubs); //47 
            gun.DefaultModule.projectiles.Add(TenOfClubs); //48 
            gun.DefaultModule.projectiles.Add(KnaveOfClubs); //49
            gun.DefaultModule.projectiles.Add(QueenOfClubs); //50
            gun.DefaultModule.projectiles.Add(KingOfClubs); //51

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Hand Gun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/handgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/handgun_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            HandGunID = gun.PickupObjectId;
        }
        public static int HandGunID;
        public static void DesignateType(Projectile projectile, HandGunCardBullet.CardSuit Suit, HandGunCardBullet.CardValue Value)
        {
            HandGunCardBullet card = projectile.gameObject.GetOrAddComponent<HandGunCardBullet>();
            card.Suit = Suit;
            card.Value = Value;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            HandGunCardBullet cardness = projectile.gameObject.GetComponent<HandGunCardBullet>();
            if (projectile.Owner is PlayerController && cardness != null)
            {
                PlayerController projOwner = projectile.Owner as PlayerController;
                if (projOwner.PlayerHasActiveSynergy("Suicide King"))
                {
                    projectile.baseData.damage *= 2;
                    ProjectileInstakillBehaviour instakill2 = projectile.gameObject.GetOrAddComponent<ProjectileInstakillBehaviour>();
                    instakill2.tagsToKill.Add("royalty");
                    if (cardness.Suit == HandGunCardBullet.CardSuit.HEARTS && cardness.Value == HandGunCardBullet.CardValue.KING)
                    {
                        projectile.baseData.damage *= 2;
                        projectile.ignoreDamageCaps = true;
                    }
                }
                if (projOwner.PlayerHasActiveSynergy("Girls Best Friend"))
                {
                    if (cardness.Suit == HandGunCardBullet.CardSuit.DIAMONDS)
                    {
                        projectile.baseData.damage *= 2;
                        projectile.baseData.damage *= ((0.003f * projOwner.carriedConsumables.Currency) + 1);
                    }
                }
                if (projOwner.PlayerHasActiveSynergy("Have A Heart"))
                {
                    if (cardness.Suit == HandGunCardBullet.CardSuit.HEARTS)
                    {
                        projectile.baseData.damage *= 2;
                        if (!projOwner.ForceZeroHealthState) projectile.baseData.damage *= ((0.025f * projOwner.healthHaver.GetCurrentHealth()) + 1);
                        else projectile.baseData.damage *= ((0.025f * projOwner.healthHaver.Armor) + 1);
                    }
                }
                if (projOwner.PlayerHasActiveSynergy("Dig Deep"))
                {
                    if (cardness.Suit == HandGunCardBullet.CardSuit.SPADES)
                    {
                        projectile.baseData.damage *= 2;
                        projectile.baseData.damage *= ((0.07f * projOwner.carriedConsumables.KeyBullets) + 1);
                    }
                }
                if (projOwner.PlayerHasActiveSynergy("Going Clubbing"))
                {
                    if (cardness.Suit == HandGunCardBullet.CardSuit.CLUBS)
                    {
                        projectile.baseData.damage *= 2;
                        projectile.baseData.damage *= ((0.07f * projOwner.Blanks) + 1);
                    }
                }
            }
            base.PostProcessProjectile(projectile);
        }
        private void modifyDamage(HealthHaver player, HealthHaver.ModifyDamageEventArgs data)
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController && gun.CurrentOwner.healthHaver == player)
            {
                if ((gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("Suicide King") && gun.CurrentOwner.CurrentGun == gun)
                {
                    data.ModifiedDamage = (data.InitialDamage * 3);
                }
            }
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.healthHaver.ModifyDamage += this.modifyDamage;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.healthHaver.ModifyDamage -= this.modifyDamage;
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                (gun.CurrentOwner as PlayerController).healthHaver.ModifyDamage -= this.modifyDamage;
            }
            base.OnDestroy();
        }

        public override void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        {
            if (gun.ClipShotsRemaining == gun.ClipCapacity && gun.CurrentAmmo > 15)
            {
                if (player.PlayerHasActiveSynergy("Royal Flush"))
                {
                    int suitToFire = UnityEngine.Random.Range(1, 5);
                    List<int> ProjectilesToFire = new List<int>();
                    switch (suitToFire)
                    {
                        case 1:
                            ProjectilesToFire.AddRange(new List<int>() { 0, 9, 10, 11, 12 });
                            break;
                        case 2:
                            ProjectilesToFire.AddRange(new List<int>() { 13, 22, 23, 24, 25 });
                            break;
                        case 3:
                            ProjectilesToFire.AddRange(new List<int>() { 26, 35, 36, 37, 38 });
                            break;
                        case 4:
                            ProjectilesToFire.AddRange(new List<int>() { 39, 48, 49, 50, 51 });
                            break;
                    }
                    if (ProjectilesToFire.Count > 0)
                    {
                        gun.CurrentAmmo -= 15;
                        foreach (int proj in ProjectilesToFire)
                        {
                            GameObject spawnedProj = ProjSpawnHelper.SpawnProjectileTowardsPoint(gun.DefaultModule.projectiles[proj].gameObject, player.CenterPosition, player.unadjustedAimPoint, 0, 40, player);
                            Projectile component = spawnedProj.GetComponent<Projectile>();
                            if (component != null)
                            {
                                component.Owner = player;
                                component.Shooter = player.specRigidbody;
                                component.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
                                component.BossDamageMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                                component.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                                component.AdditionalScaleMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                                component.baseData.range *= player.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                                component.baseData.force *= player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                player.DoPostProcessProjectile(component);
                            }
                        }
                    }
                }
            }
            base.OnReloadPressedSafe(player, gun, manualReload);
        }
        public HandGun()
        {

        }
    }
    public class HandGunCardBullet : MonoBehaviour
    {
        public HandGunCardBullet()
        {
        }
        public enum CardValue
        {
            ACE,
            QUEEN,
            KING,
            KNAVE,
            GENERIC
        }
        public enum CardSuit
        {
            HEARTS,
            DIAMONDS,
            SPADES,
            CLUBS,
            OTHER,
        }
        public CardSuit Suit;
        public CardValue Value;
    }
}