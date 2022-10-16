using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Viscerifle : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Viscerifle", "viscerifle");
            Game.Items.Rename("outdated_gun_mods:viscerifle", "nn:viscerifle");
            gun.gameObject.AddComponent<Viscerifle>();
            gun.SetShortDescription("Bloody Stream");
            gun.SetLongDescription("Fires hearts and armour as ammunition." + "\n\nThis, by definition, is not a rifle. The name is purely marketing." + "\n\nFires nothing if fired while invincible.");

            gun.SetupSprite(null, "viscerifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;

            gun.DefaultModule.angleVariance = 0f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.CanGainAmmo = false;
            gun.barrelOffset.transform.localPosition = new Vector3(1.5f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(0);
            gun.gunClass = GunClass.SHITTY;


            //HEART BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 20f;
            projectile.pierceMinorBreakables = true;
            projectile.baseData.speed *= 1f;
            projectile.ignoreDamageCaps = true;
            projectile.baseData.range *= 10f;
            projectile.SetProjectileSpriteRight("viscerifle_heart_projectile", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 16, 7);
            GoopModifier blood = projectile.gameObject.AddComponent<GoopModifier>();
            blood.goopDefinition = EasyGoopDefinitions.BlobulonGoopDef;
            blood.SpawnGoopInFlight = true;
            blood.InFlightSpawnFrequency = 0.05f;
            blood.InFlightSpawnRadius = 1f;
            blood.SpawnGoopOnCollision = true;
            blood.CollisionSpawnRadius = 2f;

            //SHADE BULLETS
            shadeProjectile = ProjectileSetupUtility.MakeProjectile(56, 20, 35, 23);
            shadeProjectile.SetProjectileSpriteRight("shadeviscerifle_proj", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 16, 7);

            //ARMOUR
            armourProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            armourProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(armourProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(armourProjectile);
            armourProjectile.baseData.damage *= 24f;
            armourProjectile.ignoreDamageCaps = true;
            armourProjectile.baseData.speed *= 1f;
            armourProjectile.baseData.range *= 10;
            armourProjectile.pierceMinorBreakables = true;
            armourProjectile.SetProjectileSpriteRight("viscerifle_armour_projectile", 12, 8, false, tk2dBaseSprite.Anchor.MiddleCenter, 12, 8);
            armourProjectile.transform.parent = gun.barrelOffset;
            BlankProjModifier blankingArmor = armourProjectile.gameObject.GetOrAddComponent<BlankProjModifier>();
            blankingArmor.blankType = Alexandria.Misc.EasyBlankType.FULL;


            //CREST
            crestProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            crestProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(crestProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(crestProjectile);
            crestProjectile.ignoreDamageCaps = true;
            crestProjectile.baseData.damage *= 1000f;
            crestProjectile.baseData.speed *= 1f;
            crestProjectile.baseData.range *= 10;
            crestProjectile.pierceMinorBreakables = true;
            crestProjectile.SetProjectileSpriteRight("viscerifle_crest_projectile", 12, 9, false, tk2dBaseSprite.Anchor.MiddleCenter, 12, 9);
            crestProjectile.transform.parent = gun.barrelOffset;
            BlankProjModifier blankingCrest = crestProjectile.gameObject.GetOrAddComponent<BlankProjModifier>();
            blankingCrest.blankType = Alexandria.Misc.EasyBlankType.FULL;
            BounceProjModifier Bouncing = crestProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 1;

            //FUCKYOUPROJECTILE
            fuckyouprojectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            fuckyouprojectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(fuckyouprojectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(fuckyouprojectile);
            fuckyouprojectile.baseData.damage *= 0f;
            fuckyouprojectile.baseData.speed *= 0f;
            fuckyouprojectile.baseData.range *= 0;
            DieFuckYou fuckingdie = fuckyouprojectile.gameObject.GetOrAddComponent<DieFuckYou>();
            fuckyouprojectile.sprite.renderer.enabled = false;
            fuckyouprojectile.transform.parent = gun.barrelOffset;


            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Viscerifle";
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_VISCERIFLE, true);
            gun.AddItemToGooptonMetaShop(23);
        }
        public static Projectile armourProjectile;
        public static Projectile crestProjectile;
        public static Projectile fuckyouprojectile;
        public static Projectile shadeProjectile;
        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            PlayerController player = gun.CurrentOwner as PlayerController;
            PlayableCharacters characterIdentity = player.characterIdentity;
            switch (DeterminedUsedHealth(player))
            {
                case "heart":
                    return projectile;
                case "armour":
                    return armourProjectile;
                case "crest":
                    return crestProjectile;
                case "shade":
                    return shadeProjectile;
            }
            return projectile;
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            float saveChance = 0.2f;
            if (((player.healthHaver.GetCurrentHealth() == 0.5) && (player.healthHaver.Armor == 0)) || (player.HasPickupID(Gungeon.Game.Items["nn:meat_shield"].PickupObjectId) && (player.healthHaver.GetCurrentHealth() == 0) && (player.healthHaver.Armor == 1) && (!player.HasPickupID(Gungeon.Game.Items["old_crest"].PickupObjectId)))) saveChance = 0.5f;
            if (player.PlayerHasActiveSynergy("Danger Is My Middle Name") && (UnityEngine.Random.value <= saveChance))
            {
                return;
            }
            else
            {
                if (player.name != "PlayerShade(Clone)")
                {
                    switch (DeterminedUsedHealth(player))
                    {
                        case "heart":
                            player.healthHaver.ApplyHealing(-0.5f);
                            break;
                        case "armour":
                            player.healthHaver.Armor -= 1;
                            break;
                        case "crest":
                            player.RemovePassiveItem(305);
                            break;
                    }
                    if (player.healthHaver.GetCurrentHealth() == 0 && player.healthHaver.Armor == 0)
                    {
                        player.healthHaver.Die(Vector2.zero);
                    }
                }
                else if (player.name == "PlayerShade(Clone)" && player.HasPickupID(305))
                {
                    player.RemovePassiveItem(305);
                }
            }
            base.OnPostFired(player, gun);
        }
        private float currentHP, lastHP;
        private float currentArmour, lastArmour;
        private bool currentHasCrest, lastHasCrest;
        protected override void Update()
        {
            if (gun.GunPlayerOwner())
            {
                if (gun.GunPlayerOwner().CharacterUsesRandomGuns) gun.GunPlayerOwner().ChangeToRandomGun();

                currentHasCrest = (gun.CurrentOwner as PlayerController).HasPickupID(305);
                currentArmour = gun.CurrentOwner.healthHaver.Armor;
                currentHP = gun.CurrentOwner.healthHaver.GetCurrentHealth();
                if ((currentHP != lastHP) || (currentArmour != lastArmour) || (currentHasCrest != lastHasCrest))
                {
                    RecalculateClip(gun.CurrentOwner as PlayerController);
                }
                lastHP = currentHP;
                lastArmour = currentArmour;
                lastHasCrest = currentHasCrest;
                if ((gun.CurrentAmmo == 0) || (gun.DefaultModule.numberOfShotsInClip != gun.CurrentAmmo))
                {
                    RecalculateClip(gun.CurrentOwner as PlayerController);
                }
            }
            base.Update();
        }
        private void RecalculateClip(PlayerController gunOwner)
        {
            int healthInt = Convert.ToInt32(gunOwner.healthHaver.GetCurrentHealth() * 2);
            int armorInt = Convert.ToInt32(gunOwner.healthHaver.Armor);
            int total = healthInt + armorInt;
            if (gunOwner.HasPickupID(305)) total += 1;
            gun.CurrentAmmo = total;
            gun.DefaultModule.numberOfShotsInClip = total;
            gun.ClipShotsRemaining = total;
        }
        public Viscerifle()
        {

        }
        private string DeterminedUsedHealth(PlayerController player)
        {
            if (player.characterIdentity == OMITBChars.Shade)
            {
                return "shade";
            }

            if (player.HasPickupID(Gungeon.Game.Items["nn:meat_shield"].PickupObjectId))
            {
                if (player.healthHaver.GetCurrentHealth() > 0)
                {
                    return "heart";
                }
                else
                {
                    if (player.HasPickupID(305)) return "crest";
                    else return "armour";
                }
            }
            else
            {
                if (player.healthHaver.Armor > 0)
                {
                    if (player.HasPickupID(305)) return "crest";
                    else return "armour";
                }
                else
                {
                    return "heart";
                }
            }

        }
    }
    
    public class DieFuckYou : MonoBehaviour
    {
        public DieFuckYou()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.ForceDestruction();
        }
        private Projectile m_projectile;
    }
}
