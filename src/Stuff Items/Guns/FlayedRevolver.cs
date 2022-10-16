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

    public class FlayedRevolver : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Flayed Revolver", "flayedrevolver");
            Game.Items.Rename("outdated_gun_mods:flayed_revolver", "nn:flayed_revolver");
            gun.gameObject.AddComponent<FlayedRevolver>();
            gun.SetShortDescription("Sinister Bells");
            gun.SetLongDescription("The favoured weapon of the cruel Mine Flayer, Planar lord of rings.\n\n" + "Reloading a full clip allows the bearer to slip beyond the curtain, if only briefly.");
            gun.SetupSprite(null, "flayedrevolver_idle_001", 13);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(35) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 24);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(35) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.81f, 0f);
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.SetBaseMaxAmmo(250);
            gun.gunClass = GunClass.PISTOL;
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.10f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            FlayedRevolverID = gun.PickupObjectId;

            gun.SetupUnlockOnCustomStat(CustomTrackedStats.MINEFLAYER_KILLS, 9, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);

        }
        public static int FlayedRevolverID;
        public override void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        {
            if ((gun.ClipCapacity == gun.ClipShotsRemaining) || (gun.CurrentAmmo == gun.ClipShotsRemaining))
            {
                if (gun.CurrentAmmo >= 5)
                {
                    if (this.m_extantReticleQuad) //If the cursor is there, then do the teleport
                    {
                        gun.CurrentAmmo -= 5;
                        Vector2 worldCenter = this.m_extantReticleQuad.WorldCenter;
                        UnityEngine.Object.Destroy(this.m_extantReticleQuad.gameObject);
                        worldCenter += new Vector2(1.5f, 1.5f);
                        //worldCenter = new Vector2(Mathf.FloorToInt(worldCenter.x), Mathf.FloorToInt(worldCenter.y));
                       // UnityEngine.Object.Instantiate<GameObject>((PickupObjectDatabase.GetById(Pencil.pencilID) as Gun).DefaultModule.projectiles[0].gameObject, new Vector3(worldCenter.x, worldCenter.y), Quaternion.identity);
                        TeleportPlayerToCursorPosition.StartTeleport(player, worldCenter);
                    }
                    else //If the cursor is not there, make it
                    {
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.reticleQuad);
                        this.m_extantReticleQuad = gameObject.GetComponent<tk2dBaseSprite>();
                        this.m_currentAngle = BraveMathCollege.Atan2Degrees(player.unadjustedAimPoint.XY() - player.CenterPosition);
                        this.m_currentDistance = 5f;
                        this.UpdateReticlePosition();
                    }
                }
            }
        }
        protected override void Update()
        {
            if (this.m_extantReticleQuad)
            {
                this.UpdateReticlePosition();
            }
            base.Update();
        }
        public override void OnSwitchedAwayFromThisGun()
        {
            if (this.m_extantReticleQuad)
            {
                UnityEngine.Object.Destroy(this.m_extantReticleQuad.gameObject);
            }
            base.OnSwitchedAwayFromThisGun();
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            if (this.m_extantReticleQuad)
            {
                UnityEngine.Object.Destroy(this.m_extantReticleQuad.gameObject);
            }
            base.OnPostFired(player, gun);
        }
        private void UpdateReticlePosition()
        {
            PlayerController user = this.gun.GunPlayerOwner();
            if (user)
            {
                if (BraveInput.GetInstanceForPlayer(user.PlayerIDX).IsKeyboardAndMouse(false))
                {
                    Vector2 vector = user.unadjustedAimPoint.XY();
                    Vector2 vector2 = vector - this.m_extantReticleQuad.GetBounds().extents.XY();
                    this.m_extantReticleQuad.transform.position = vector2;
                }
                else
                {
                    BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(user.PlayerIDX);
                    Vector2 vector3 = user.CenterPosition + (Quaternion.Euler(0f, 0f, this.m_currentAngle) * Vector2.right).XY() * this.m_currentDistance;
                    vector3 += instanceForPlayer.ActiveActions.Aim.Vector * 12f * BraveTime.DeltaTime;
                    this.m_currentAngle = BraveMathCollege.Atan2Degrees(vector3 - user.CenterPosition);
                    this.m_currentDistance = Vector2.Distance(vector3, user.CenterPosition);
                    this.m_currentDistance = Mathf.Min(this.m_currentDistance, 100);
                    vector3 = user.CenterPosition + (Quaternion.Euler(0f, 0f, this.m_currentAngle) * Vector2.right).XY() * this.m_currentDistance;
                    Vector2 vector4 = vector3 - this.m_extantReticleQuad.GetBounds().extents.XY();
                    this.m_extantReticleQuad.transform.position = vector4;
                }
            }
        }
        public override void OnDestroy()
        {
            if (this.m_extantReticleQuad)
            {
                UnityEngine.Object.Destroy(this.m_extantReticleQuad.gameObject);
            }
            base.OnDestroy();
        }
        public override void OnDropped()
        {
            if (this.m_extantReticleQuad)
            {
                UnityEngine.Object.Destroy(this.m_extantReticleQuad.gameObject);
            }
            base.OnDropped();
        }
        public FlayedRevolver()
        {
            reticleQuad = (PickupObjectDatabase.GetById(443) as TargetedAttackPlayerItem).reticleQuad;
        }
        public GameObject reticleQuad;
        private tk2dBaseSprite m_extantReticleQuad;
        private float m_currentAngle;
        private float m_currentDistance = 5f;
    }
}
