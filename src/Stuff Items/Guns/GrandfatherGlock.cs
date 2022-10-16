using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class GrandfatherGlock : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Grandfather Glock", "grandfatherglock");
            Game.Items.Rename("outdated_gun_mods:grandfather_glock", "nn:grandfather_glock");
            var behav = gun.gameObject.AddComponent<GrandfatherGlock>();
            behav.overrideNormalReloadAudio = "Play_BOSS_mineflayer_bellshot_01";
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("It's About Time");
            gun.SetLongDescription("Fires bullets in directions that correspond to it's hands in order to tell the current time."+"\n\nThe Gunslinger's clock was too large for the case," +
                "\nSo it sat ninety years on the floor." +
                "\nIt was longer by half than the old man himself," +
                "\nThough it weighed not a pennyweight more." +
                "\n\nIt was forged on the morn on the day that he was born," +
                "\nAnd was always his treasure and pride." +
                "\nBut it jammed, hard, never to shoot again," +
                "\nWhen the old man died.");

            gun.SetupSprite(null, "grandfatherglock_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(56) as Gun).gunSwitchGroup;
            gun.muzzleFlashEffects.type = VFXPoolType.None;


            gun.SetAnimationFPS(gun.shootAnimation, 12);

            for (int i = 0; i < 3; i++) 
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            }
            //Easy Variables to Change all the Modules
            float AllModCooldown = 0.35f;
            int AllModClipshots = 12;


            int i2 = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                if (i2 <= 0) //Hour hand
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleVariance = 0.1f;
                    mod.numberOfShotsInClip = AllModClipshots;

                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                    mod.projectiles[0] = projectile;
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.baseData.damage *= 5f;
                    projectile.baseData.speed *= 0.6f;
                    projectile.baseData.range *= 2f;
                    projectile.pierceMinorBreakables = true;
                    TimeBasedBulletAimer orAddComponent = projectile.gameObject.GetOrAddComponent<TimeBasedBulletAimer>();
                    orAddComponent.aimType = TimeBasedBulletAimer.ClockHandAimType.HOUR_HAND;
                    projectile.SetProjectileSpriteRight("grandfatherglock_hourhand_projectile", 19, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 18, 10);
                    i2++;
                }
                else if (i2 == 1) //Minute hand
                {
                    mod.ammoCost = 1;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleVariance = 0.1f;
                    mod.numberOfShotsInClip = AllModClipshots;

                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                    mod.projectiles[0] = projectile;
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.baseData.damage *= 3f;
                    projectile.baseData.speed *= 0.7f;
                    projectile.pierceMinorBreakables = true;
                    projectile.baseData.range *= 2f;
                    TimeBasedBulletAimer orAddComponent = projectile.gameObject.GetOrAddComponent<TimeBasedBulletAimer>();
                    orAddComponent.aimType = TimeBasedBulletAimer.ClockHandAimType.MINUTE_HAND;
                    projectile.SetProjectileSpriteRight("grandfatherglock_minutehand_projectile", 26, 9, true, tk2dBaseSprite.Anchor.MiddleCenter, 25, 8);
                    i2++;
                }
                else if (i2 >= 2) //Second hand
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleVariance = 0.1f;
                    mod.numberOfShotsInClip = AllModClipshots;

                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                    mod.projectiles[0] = projectile;
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.baseData.damage *= 1f;
                    projectile.pierceMinorBreakables = true;
                    projectile.baseData.speed *= 0.7f;
                    projectile.baseData.range *= 2f;
                    //TrailController trail = projectile.gameObject.GetOrAddComponent<TrailController>();
                    
                    TimeBasedBulletAimer orAddComponent = projectile.gameObject.GetOrAddComponent<TimeBasedBulletAimer>();
                    orAddComponent.aimType = TimeBasedBulletAimer.ClockHandAimType.SECOND_HAND;
                    projectile.SetProjectileSpriteRight("grandfatherglock_secondhand_projectile", 22, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 21, 4);
                    i2++;
                }
            }
            gun.reloadTime = 1.4f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(300);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Grandfather Glock Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/grandfatherglock_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/grandfatherglock_clipempty");

            gun.gunClass = GunClass.SILLY;
            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            GrandfatherGlockID = gun.PickupObjectId;
        }
        public static int GrandfatherGlockID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner is PlayerController)
            {
                PlayerController owner = projectile.Owner as PlayerController;
                if (owner != null)
                {
                    if (owner.PlayerHasActiveSynergy("Mechanical Hands"))
                    {
                        BounceProjModifier bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                        bouncing.bouncesTrackEnemies = true;
                        bouncing.numberOfBounces += 1;
                        bouncing.bounceTrackRadius = 100f;
                    }
                    if (owner.PlayerHasActiveSynergy("Six Sharp") || owner.PlayerHasActiveSynergy("Kellys Eye"))
                    {
                        TimeBasedBulletAimer bulletmod = projectile.GetComponent<TimeBasedBulletAimer>();
                        if (bulletmod != null)
                        {
                            if (bulletmod.aimType == TimeBasedBulletAimer.ClockHandAimType.HOUR_HAND)
                            {
                                int hour = DateTime.Now.Hour;
                                if (hour > 12) hour -= 12;
                                if (hour == 0) hour = 12;
                                if (owner.PlayerHasActiveSynergy("Six Sharp") && hour.ToString().Contains("6")) projectile.baseData.damage *= 3;
                                if (owner.PlayerHasActiveSynergy("Kellys Eye") && hour.ToString().Contains("1")) projectile.baseData.damage *= 3;
                            }
                            if (bulletmod.aimType == TimeBasedBulletAimer.ClockHandAimType.MINUTE_HAND)
                            {
                                int minute = DateTime.Now.Minute;
                                if (owner.PlayerHasActiveSynergy("Kellys Eye") && minute.ToString().Contains("1")) projectile.baseData.damage *= 3;
                                if (owner.PlayerHasActiveSynergy("Six Sharp") && minute.ToString().Contains("6")) projectile.baseData.damage *= 3;
                            }
                            if (bulletmod.aimType == TimeBasedBulletAimer.ClockHandAimType.SECOND_HAND)
                            {
                                int second = DateTime.Now.Second;
                                if (owner.PlayerHasActiveSynergy("Kellys Eye") && second.ToString().Contains("1")) projectile.baseData.damage *= 3;
                                if (owner.PlayerHasActiveSynergy("Six Sharp") && second.ToString().Contains("6")) projectile.baseData.damage *= 3;
                            }
                        }
                    }
                }
            }
                            base.PostProcessProjectile(projectile);
        }
        public GrandfatherGlock()
        {

        }
        int currentHour;
        int lastHour;
        protected override void Update()
        {
            currentHour = DateTime.Now.Hour;
            if (currentHour != lastHour)
            {
                lastHour = currentHour;
                GameManager.Instance.StartCoroutine(this.DoChimes());
            }
            base.Update();
        }
        private IEnumerator DoChimes()
        {
            for (int i = 0; i < 3; i++)
            {
                AkSoundEngine.PostEvent("Play_BOSS_mineflayer_bellshot_01", gameObject);
                yield return new WaitForSeconds(1f);
            }
            yield break;

        }
    }
   
    public class TimeBasedBulletAimer : MonoBehaviour
    {
        public TimeBasedBulletAimer()
        {
            this.aimType = ClockHandAimType.NULL;
        }
        public enum ClockHandAimType
        {
            MINUTE_HAND,
            HOUR_HAND,
            SECOND_HAND,
            NULL
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();

            DateTime time = DateTime.Now;
            //ETGModConsole.Log(time.Minute.ToString());
            int minute = time.Minute;
            int hour = -1;
            int second = time.Second;
            if (time.Hour > 12) { hour = time.Hour - 12; }
            else { hour = time.Hour; }
            //ETGModConsole.Log("Hour: " + hour + "\nHour Angle: " + hour * 30);
            //ETGModConsole.Log("Minute: " + minute + "\nMinute Angle: " + minute * 6);

            float minuteAngle = ((minute * 6f) - 90f) * -1f;
            float hourAngle = ((hour * 30f) - 90f) * -1f;
            float secondAngle = ((second * 6f) - 90f) * -1f;

            if (this.aimType == ClockHandAimType.MINUTE_HAND)
            {
                this.m_projectile.SendInDirection(minuteAngle.DegreeToVector2(), false);
            }
            else if (this.aimType == ClockHandAimType.HOUR_HAND)
            {
                this.m_projectile.SendInDirection(hourAngle.DegreeToVector2(), false);
            }
            else if (this.aimType == ClockHandAimType.SECOND_HAND)
            {
                this.m_projectile.SendInDirection(secondAngle.DegreeToVector2(), false);
            }
        }
        private Projectile m_projectile;
        public ClockHandAimType aimType;
    }
}