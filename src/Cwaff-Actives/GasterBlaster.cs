using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel; // ReadOnlyCollection
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
    class GasterBlaster : PlayerItem
    {
        public static string ItemName         = "Gaster Blaster";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/gaster_blaster_icon";
        public static string ShortDescription = "Use Your Best Attack First";
        public static string LongDescription  = "(sans intensifies)";

        internal static Projectile _GasterBlast;
        internal static GameObject _GasterBlaster;

        private bool _anyGunFiredInRoom = false;
        private PlayerController _owner = null;

        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<GasterBlaster>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.S;
            item.consumable   = false;
            item.CanBeDropped = true;
            // item.SetCooldownType(ItemBuilder.CooldownType.Damage, 100f);
            // item.SetCooldownType(ItemBuilder.CooldownType.PerRoom, 1f);
            item.SetCooldownType(ItemBuilder.CooldownType.Timed, 1f);

            _GasterBlast = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items.MarineSidearm) as Gun, false);
            _GasterBlast.baseData.damage         = 700f;
            _GasterBlast.baseData.force          = 70f;
            _GasterBlast.baseData.range          = 200f;
            _GasterBlast.baseData.speed          = 150f;
            _GasterBlast.ignoreDamageCaps        = false;
            _GasterBlast.PenetratesInternalWalls = true;
            _GasterBlast.pierceMinorBreakables   = true;

            BasicBeamController beamComp = _GasterBlast.SetupBeamSprites(
              spriteName: "gaster_beam", fps: 60, dims: new Vector2(35, 39), impactDims: new Vector2(36, 36), impactFps: 16);
                beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
                beamComp.ContinueBeamArtToWall = false;
                beamComp.PenetratesCover       = true;
                beamComp.penetration           = 1000;
            PierceProjModifier pierce = _GasterBlast.gameObject.GetOrAddComponent<PierceProjModifier>();
                pierce.penetration          = 100;
                pierce.penetratesBreakables = true;

            _GasterBlaster = VFX.animations["GasterBlaster"];
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            this._owner = player;
            player.OnEnteredCombat += this.OnEnteredCombat;
            player.PostProcessProjectile += this.OnFired;
        }

        public override void OnPreDrop(PlayerController player)
        {
            player.PostProcessProjectile -= this.OnFired;
            player.OnEnteredCombat -= this.OnEnteredCombat;
            this._owner = null;
            base.OnPreDrop(player);
        }

        private void OnFired(Projectile p, float f)
        {
            this._anyGunFiredInRoom = true;
        }

        private void OnEnteredCombat()
        {
            this._anyGunFiredInRoom = false;
        }

        // public override bool CanBeUsed(PlayerController user)
        // {
        //     return user.IsInCombat && base.CanBeUsed(user);
        // }

        public override void DoEffect(PlayerController user)
        {
            if (user != this._owner)
                return;
            user.StartCoroutine(BlasterWithGaster());
        }

        private IEnumerator BlasterWithGaster()
        {
            const float SWING_RADIUS = 16f;

            float angle = this._owner.m_currentGunAngle;
            // Vector2 targetPos = this._owner.sprite.WorldCenter - BraveMathCollege.DegreesToVector(angle, 4f);
            Vector2 targetPos = this._owner.sprite.WorldCenter.ToNearestWallOrObject((180f + angle).Clamp180(), 0f);
            Vector2 delta = targetPos - this._owner.sprite.WorldCenter;
            targetPos = this._owner.sprite.WorldCenter + (delta.magnitude - 1f).Clamp(0f, 2f) * delta.normalized;

            GameObject blaster = SpawnManager.SpawnVFX(_GasterBlaster, this._owner.sprite.WorldCenter.ToVector3ZUp(10f), Quaternion.identity);
            RotateIntoPositionBehavior rotcomp = blaster.AddComponent<RotateIntoPositionBehavior>();
                rotcomp.m_radius       = SWING_RADIUS;
                rotcomp.m_fulcrum      = targetPos + BraveMathCollege.DegreesToVector(angle, SWING_RADIUS);
                rotcomp.m_start_angle  = angle;
                rotcomp.m_end_angle    = (180f + angle).Clamp180();
                rotcomp.m_rotate_time  = 0.5f;
                rotcomp.Setup();
            AkSoundEngine.PostEvent("gaster_blaster_sound_effect_stop_all", blaster);
            AkSoundEngine.PostEvent("gaster_blaster_sound_effect", blaster);
            yield return new WaitForSeconds(0.75f);

            BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(
                _GasterBlast, this._owner, /*blaster*/ null, // TODO: not sure why blaster doesn't work here
                blaster.GetComponent<tk2dSprite>().WorldCenter + rotcomp.m_start_angle.ToVector(0.33f), rotcomp.m_start_angle, 0.75f, true, true);
            beam.sprite.gameObject.SetGlowiness(300f); // TODO: not sure why this doesn't actually seem to have any effect
            blaster.ExpireIn(1.25f, fadeFor: 0.25f);
        }
    }

    public class RotateIntoPositionBehavior : MonoBehaviour
    {
        public Vector2 m_fulcrum;
        public float m_radius;
        public float m_start_angle;
        public float m_end_angle;
        public float m_rotate_time;

        private float timer;
        private float angle_delta;
        private bool has_been_init = false;

        public void Setup()
        {
            this.timer         = 0;
            this.angle_delta   = this.m_end_angle - this.m_start_angle;
            this.has_been_init = true;
            this.Relocate();
        }

        private void Update()
        {
            if ((!this.has_been_init) || (this.timer > m_rotate_time))
                return;
            this.timer += BraveTime.DeltaTime;
            if (this.timer > this.m_rotate_time)
                this.timer = this.m_rotate_time;
            this.Relocate();
        }

        private void Relocate()
        {
            float percentDone  = this.timer / this.m_rotate_time;
            float curAngle     = this.m_start_angle + (float)Math.Tanh(percentDone*Mathf.PI) * this.angle_delta;
            Vector2 curPos     = this.m_fulcrum + BraveMathCollege.DegreesToVector(curAngle, this.m_radius);
            base.gameObject.transform.position = curPos.ToVector3ZUp(base.gameObject.transform.position.z);
            base.gameObject.transform.rotation =
                Quaternion.Euler(0f, 0f, curAngle + (curAngle > 180 ? 180 : (-180)));
        }
    }
}
