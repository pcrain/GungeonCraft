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
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class Clicker : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Clicker", "clicker");
            Game.Items.Rename("outdated_gun_mods:clicker", "nn:clicker");
            var behav = gun.gameObject.AddComponent<Clicker>();
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_MouseClickNoise";
            gun.SetShortDescription("Clacker");
            gun.SetLongDescription("A remarkably strange invention, this arrow requires no bow to fire." + "\n\nCan shred enemies apart as fast as you can click on them!");
            gun.SetupSprite(null, "clicker_idle_001", 13);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0f;
            gun.barrelOffset.transform.localPosition = new Vector3(0.5f, 0.62f, 0f);
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.SetBaseMaxAmmo(1000);
            gun.gunClass = GunClass.SILLY;

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration = 100;
            pierce.penetratesBreakables = true;
            projectile.pierceMinorBreakables = true;
            projectile.specRigidbody.CollideWithTileMap = false;
            projectile.baseData.damage = 4f;
            projectile.baseData.speed = 0.1f;
            projectile.baseData.force = 0f;
            projectile.baseData.range *= 1f;
            projectile.SetProjectileSpriteRight("16x16_white_circle", 16, 16, false, tk2dBaseSprite.Anchor.MiddleCenter, 16, 16);

            //projectile.hitEffects.suppressMidairDeathVfx = true;
            // projectile.hitEffectHandler.SuppressAllHitEffects = true;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Clicker Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/clicker_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/clicker_clipempty");

            projectile.gameObject.AddComponent<ClickProjMod>();
            projectile.sprite.renderer.enabled = false;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            ID = gun.PickupObjectId;
            gun.SetTag("arrow_bolt_weapon");

            clickerCollection = SpriteBuilder.ConstructCollection(gun.gameObject, "Clicker_Collection");
            crosshairSpriteID = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/MiscVFX/clicker_crosshair", clickerCollection);

        }
        public static tk2dSpriteCollectionData clickerCollection;
        public static int crosshairSpriteID;
        public static int ID;
        public RoomHandler lastRoom;
        public void regenerateExtantCrosshair(PlayerController user)
        {
            if (this.m_extantReticleQuad)
            {
                removeManualCrosshair();
                createManualCrosshairForController(user);
            }
            else
            {
                Debug.LogError("Clicker: Attempted to regenerate a crosshair that didn't exist?");
            }
        }
        public void removeManualCrosshair()
        {
            if (this.m_extantReticleQuad)
            {
                UnityEngine.Object.Destroy(m_extantReticleQuad);
            }
            else
            {
                Debug.LogError("Clicker: Attempted to remove a crosshair that didn't exist?");
            }
        }
        public void createManualCrosshairForController(PlayerController playerOwner)
        {
            if (BraveInput.GetInstanceForPlayer(playerOwner.PlayerIDX).IsKeyboardAndMouse(false))
            {
                Debug.LogError("Clicker: Attempted to create a crosshair for a user playing on keyboard and mouse.");
                return;
            }
            GameObject newSprite = new GameObject("Controller_Crosshair", new Type[] { typeof(tk2dSprite) }) { layer = 0 };
            newSprite.transform.position = (playerOwner.transform.position + new Vector3(0.5f, 2));
            tk2dSprite m_ItemSprite = newSprite.AddComponent<tk2dSprite>();

            m_ItemSprite.SetSprite(clickerCollection, crosshairSpriteID);
            m_ItemSprite.PlaceAtPositionByAnchor(newSprite.transform.position, tk2dBaseSprite.Anchor.MiddleCenter);
            m_ItemSprite.transform.localPosition = m_ItemSprite.transform.localPosition.Quantize(0.0625f);

            if (m_ItemSprite)
            {
                playerOwner.sprite.AttachRenderer(m_ItemSprite);
                m_ItemSprite.depthUsesTrimmedBounds = true;
                m_ItemSprite.UpdateZDepth();
            }
            m_extantReticleQuad = m_ItemSprite;
        }

        protected override void Update()
        {
            if (gun && gun.GunPlayerOwner())
            {
                if (!this.m_extantReticleQuad)
                {
                    if (BraveInput.GetInstanceForPlayer(gun.GunPlayerOwner().PlayerIDX).IsKeyboardAndMouse(false)) { return; }
                    else
                    {
                        if (gun.GunPlayerOwner().CurrentGun != null)
                        {
                            if ((gun.GunPlayerOwner().CurrentGun.PickupObjectId == gun.PickupObjectId) || (gun.GunPlayerOwner().CurrentSecondaryGun.PickupObjectId == gun.PickupObjectId))
                            {
                                createManualCrosshairForController(gun.GunPlayerOwner());
                            }

                        }
                    }
                }
                if (this.m_extantReticleQuad)
                {
                    if (BraveInput.GetInstanceForPlayer(gun.GunPlayerOwner().PlayerIDX).IsKeyboardAndMouse(false))
                    {
                        removeManualCrosshair();
                    }
                    else
                    {
                        if (gun.GunPlayerOwner().CurrentGun != null)
                        {
                            if ((gun.GunPlayerOwner().CurrentGun.PickupObjectId == gun.PickupObjectId) || (gun.GunPlayerOwner().CurrentSecondaryGun.PickupObjectId == gun.PickupObjectId))
                            {

                                //Room change positional update
                                if (gun.GunPlayerOwner().CurrentRoom != lastRoom)
                                {
                                    regenerateExtantCrosshair(gun.GunPlayerOwner());
                                    lastRoom = gun.GunPlayerOwner().CurrentRoom;
                                }
                                else
                                {
                                    this.UpdateReticlePosition();
                                }
                            }
                            else { removeManualCrosshair(); }
                        }
                        else { removeManualCrosshair(); }
                    }
                }

            }
            else if (this.m_extantReticleQuad)
            {
                removeManualCrosshair();
            }
            base.Update();
        }
        private void UpdateReticlePosition()
        {
            PlayerController user = this.gun.GunPlayerOwner();
            if (user)
            {
                if (BraveInput.GetInstanceForPlayer(user.PlayerIDX).IsKeyboardAndMouse(false))
                {
                    Debug.LogError("Clicker: Attempted to update a crosshair for a user playing on keyboard and mouse???");
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
        public tk2dBaseSprite m_extantReticleQuad;
        private float m_currentAngle;
        private float m_currentDistance = 5f;
    }
    public class ClickProjMod : MonoBehaviour
    {
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile.Owner is PlayerController) this.owner = m_projectile.Owner as PlayerController;
            if (owner)
            {
                if (!BraveInput.GetInstanceForPlayer(owner.PlayerIDX).IsKeyboardAndMouse(false))
                {
                    m_projectile.baseData.damage *= 2;
                    m_projectile.RuntimeUpdateScale(1.5f);
                }
                if (owner.PlayerHasActiveSynergy("One Click Away!"))
                {
                    m_projectile.RuntimeUpdateScale(1.5f);
                }
            }
            HandleForceToPosition();

            StartCoroutine(HandleClickDeath());

        }
        private void HandleForceToPosition()
        {
            if (owner)
            {
                if (BraveInput.GetInstanceForPlayer(owner.PlayerIDX).IsKeyboardAndMouse(false))
                {
                    Vector2 vec = owner.GetCursorPosition(1);
                    vec += new Vector2(0, 0.56f);
                    m_projectile.specRigidbody.Position = new Position(vec);
                    m_projectile.specRigidbody.UpdateColliderPositions();
                }
                else
                {

                    if (m_projectile.PossibleSourceGun != null && m_projectile.PossibleSourceGun.GetComponent<Clicker>() != null)
                    {
                        Clicker sourceclicker = m_projectile.PossibleSourceGun.GetComponent<Clicker>();
                        if (sourceclicker.m_extantReticleQuad != null)
                        {
                            Vector2 pos = sourceclicker.m_extantReticleQuad.WorldCenter;
                            m_projectile.specRigidbody.Position = new Position(pos);
                            m_projectile.specRigidbody.UpdateColliderPositions();
                        }
                    }
                    else
                    {
                        m_projectile.specRigidbody.Position = new Position(owner.GetCursorPosition(UnityEngine.Random.Range(5f, 10f)));
                        m_projectile.specRigidbody.UpdateColliderPositions();
                    }
                }
            }
        }
        private IEnumerator HandleClickDeath()
        {
            UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.WhiteCircleVFX, m_projectile.specRigidbody.UnitCenter, Quaternion.identity);
            for (int i = 0; i < 10; i++)
            {
                HandleForceToPosition();
                yield return null;
            }
            m_projectile.DieInAir();
            yield break;
        }
        private Projectile m_projectile;
        private PlayerController owner;
    }
}
