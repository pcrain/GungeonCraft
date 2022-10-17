using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using CwaffingTheGungy;
using System.Collections;

namespace ItemAPI
{
    /// <summary>
    /// Advanced version of a GunBehaviour. Still has all methods of a GunBehaviour, but also has some new ones.
    /// </summary>
    public class AdvancedGunBehavior : BraveBehaviour, IGunInheritable, ILevelLoadedListener
    {
        /// <summary>
        /// Update() is called every tick when the gun is the player's current gun or is dropped.
        /// </summary>
        protected virtual void Update()
        {
            if (this.Owner != null && !this.pickedUpLast)
            {
                this.OnPickup(this.Owner);
                this.pickedUpLast = true;
            }
            if (this.Owner == null && this.pickedUpLast)
            {
                if (this.lastOwner != null)
                {
                    this.OnPostDrop(this.lastOwner);
                    this.lastOwner = null;
                }
                this.pickedUpLast = false;
            }
            if (this.lastOwner != this.Owner)
            {
                this.lastOwner = this.Owner;
            }
            if (this.Player != null)
            {
                if (!this.everPickedUpByPlayer)
                {
                    this.everPickedUpByPlayer = true;
                }
            }
            if (this.Owner != null)
            {
                if (!this.everPickedUp)
                {
                    this.everPickedUp = true;
                }
            }
            if (this.gun != null && !this.gun.IsReloading && !this.hasReloaded)
            {
                this.hasReloaded = true;
            }
            this.gun.PreventNormalFireAudio = this.preventNormalFireAudio;
            this.gun.OverrideNormalFireAudioEvent = this.overrideNormalFireAudio;
        }

        /// <summary>
        /// Inherits data from another gun. Inherit the variables you want to be saved here!
        /// </summary>
        /// <param name="source">The source gun.</param>
        public virtual void InheritData(Gun source)
        {
            AdvancedGunBehavior component = source.GetComponent<AdvancedGunBehavior>();
            if (component != null)
            {
                this.preventNormalFireAudio = component.preventNormalFireAudio;
                this.preventNormalReloadAudio = component.preventNormalReloadAudio;
                this.overrideNormalReloadAudio = component.overrideNormalReloadAudio;
                this.overrideNormalFireAudio = component.overrideNormalFireAudio;
                this.everPickedUpByPlayer = component.everPickedUpByPlayer;
                this.everPickedUp = component.everPickedUp;
                this.usesOverrideHeroSwordCooldown = component.usesOverrideHeroSwordCooldown;
                this.overrideHeroSwordCooldown = component.overrideHeroSwordCooldown;
            }
        }

        /// <summary>
        /// Saves the data of the gun to a list. Save the variables you want to be saved here!
        /// </summary>
        /// <param name="data">The list.</param>
        /// <param name="dataIndex">DataIndex. You don't need to use this argument.</param>
        public virtual void MidGameSerialize(List<object> data, int dataIndex)
        {
            data.Add(this.preventNormalFireAudio);
            data.Add(this.preventNormalReloadAudio);
            data.Add(this.overrideNormalReloadAudio);
            data.Add(this.overrideNormalFireAudio);
            data.Add(this.everPickedUpByPlayer);
            data.Add(this.everPickedUp);
            data.Add(this.usesOverrideHeroSwordCooldown);
            data.Add(this.overrideHeroSwordCooldown);
        }

        /// <summary>
        /// Sets the data of the gun to the contents of a list. Set the variables you want to be saved here!
        /// </summary>
        /// <param name="data">The list.</param>
        /// <param name="dataIndex">DataIndex. Add a number equal to the amount of your data to it.</param>
        public virtual void MidGameDeserialize(List<object> data, ref int dataIndex)
        {
            this.preventNormalFireAudio = (bool)data[dataIndex];
            this.preventNormalReloadAudio = (bool)data[dataIndex + 1];
            this.overrideNormalReloadAudio = (string)data[dataIndex + 2];
            this.overrideNormalFireAudio = (string)data[dataIndex + 3];
            this.everPickedUpByPlayer = (bool)data[dataIndex + 4];
            this.everPickedUp = (bool)data[dataIndex + 5];
            this.usesOverrideHeroSwordCooldown = (bool)data[dataIndex + 6];
            this.overrideHeroSwordCooldown = (float)data[dataIndex + 7];
            dataIndex += 8;
        }

        /// <summary>
        /// Start() is called when the gun is created. It's also called when the player picks up or drops the gun.
        /// </summary>
        public virtual void Start()
        {
            this.gun = base.GetComponent<Gun>();
            this.gun.OnInitializedWithOwner += this.OnInitializedWithOwner;
            if (this.gun.CurrentOwner != null)
            {
                this.OnInitializedWithOwner(this.gun.CurrentOwner);
            }
            this.gun.PostProcessProjectile += this.PostProcessProjectile;
            this.gun.PostProcessVolley += this.PostProcessVolley;
            this.gun.OnDropped += this.OnDropped;
            this.gun.OnAutoReload += this.OnAutoReload;
            this.gun.OnReloadPressed += this.OnReloadPressed;
            this.gun.OnFinishAttack += this.OnFinishAttack;
            this.gun.OnPostFired += this.OnPostFired;
            this.gun.OnAmmoChanged += this.OnAmmoChanged;
            this.gun.OnBurstContinued += this.OnBurstContinued;
            this.gun.OnPreFireProjectileModifier += this.OnPreFireProjectileModifier;
            base.StartCoroutine(this.UpdateCR());
        }

        public virtual void BraveOnLevelWasLoaded()
        {
        }

        private IEnumerator UpdateCR()
        {
            while (true)
            {
                this.NonCurrentGunUpdate();
                yield return null;
            }
        }

        /// <summary>
        /// NonCurrentGunUpdate() is called every tick EVEN IF THE GUN ISN'T ENABLED. That means it's able to run even if the player's current gun isn't this beh
        /// </summary>
        protected virtual void NonCurrentGunUpdate()
        {
        }

        /// <summary>
        /// OnInitializedWithOwner() is called when a GunInventory creates a gun to add (for example when the player picks the gun up.) 
        /// </summary>
        /// <param name="actor">The gun's owner.</param>
        public virtual void OnInitializedWithOwner(GameActor actor)
        {
        }

        /// <summary>
        /// PostProcessProjectile() is called right after the gun shoots a projectile. If you want to change properties of a projectile in runtime, this is the place to do it.
        /// </summary>
        /// <param name="projectile">The target projectile.</param>
        public virtual void PostProcessProjectile(Projectile projectile)
        {
        }

        /// <summary>
        /// PostProcessVolley() is called when PlayerStats rebuilds a gun's volley. It's used by things like VolleyModificationSynergyProcessor to change the gun's volley if the player has a synergy.
        /// </summary>
        /// <param name="volley">The target volley.</param>
        public virtual void PostProcessVolley(ProjectileVolleyData volley)
        {
        }

        /// <summary>
        /// OnDropped() is called when an a player drops the gun. gun.CurrentOwner is set to null before this method is even called, so I wouldn't reccomend using it.
        /// </summary>
        public virtual void OnDropped()
        {
        }

        /// <summary>
        /// OnAutoReload() is called when a player reloads the gun with an empty clip.
        /// </summary>
        /// <param name="player">The player that reloaded the gun. Will be null if the gun's owner isn't a player.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnAutoReload(PlayerController player, Gun gun)
        {
            if (player != null)
            {
                this.OnAutoReloadSafe(player, gun);
            }
        }

        /// <summary>
        /// OnAutoReloadSafe() is called when a player reloads the gun with an empty clip and the gun's owner is a player.
        /// </summary>
        /// <param name="player">The player that reloaded the gun. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnAutoReloadSafe(PlayerController player, Gun gun)
        {
        }

        /// <summary>
        /// OnReloadPressed() is called when the owner reloads the gun or the player presses the reload key.
        /// </summary>
        /// <param name="player">The player that reloaded the gun/pressed the reload key. Will be null if the gun's owner isn't a player.</param>
        /// <param name="gun">The gun.</param>
        /// <param name="manualReload">True if the owner reloaded the gun by pressing the reload key. False if the owner reloaded the gun by firing with an empty clip.</param>
        public virtual void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (this.hasReloaded && gun.IsReloading)
            {
                this.OnReload(player, gun);
                this.hasReloaded = false;
            }
            if (player != null)
            {
                this.OnReloadPressedSafe(player, gun, manualReload);
            }
        }

        /// <summary>
        /// OnGunsChanged() is called when the player changes the current gun.
        /// </summary>
        /// <param name="previous">The previous current gun.</param>
        /// <param name="current">The new current gun.</param>
        /// <param name="newGun">True if the gun was changed because player picked up a new gun.</param>
        public virtual void OnGunsChanged(Gun previous, Gun current, bool newGun)
        {
            if (previous != this.gun && current == this.gun)
            {
                this.OnSwitchedToThisGun();
            }
            if (previous == this.gun && current != this.gun)
            {
                this.OnSwitchedAwayFromThisGun();
            }
        }
        
        /// <summary>
        /// OnSwitchedToThisGun() when the player switches to this behaviour's affected gun.
        /// </summary>
        public virtual void OnSwitchedToThisGun()
        {
        }

        /// <summary>
        /// OnSwitchedToThisGun() when the player switches away from this behaviour's affected gun.
        /// </summary>
        public virtual void OnSwitchedAwayFromThisGun()
        {
        }

        /// <summary>
        /// OnReloadPressedSafe() is called when the owner reloads the gun or the player presses the reload key and the gun's owner is a player.
        /// </summary>
        /// <param name="player">The player that reloaded the gun/pressed the reload key. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        /// <param name="manualReload">True if the owner reloaded the gun by pressing the reload key. False if the owner reloaded the gun by firing with an empty clip.</param>
        public virtual void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        {
            if (this.hasReloaded && gun.IsReloading)
            {
                this.OnReloadSafe(player, gun);
                this.hasReloaded = false;
            }
        }

        /// <summary>
        /// OnReload() is called when the gun is reloaded.
        /// </summary>
        /// <param name="player">The player that reloaded the gun. Will be null if the gun's owner isn't a player.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnReload(PlayerController player, Gun gun)
        {
            if (this.preventNormalReloadAudio)
            {
                AkSoundEngine.PostEvent("Stop_WPN_All", base.gameObject);
                if (!string.IsNullOrEmpty(this.overrideNormalReloadAudio))
                {
                    AkSoundEngine.PostEvent(this.overrideNormalReloadAudio, base.gameObject);
                }
            }
        }

        /// <summary>
        /// OnReloadEnded() is called at the end of reload.
        /// </summary>
        /// <param name="player">The player that reloaded the gun. Will be null if the gun's owner isn't a player.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnReloadEnded(PlayerController player, Gun gun)
        {
            if (player != null)
            {
                this.OnReloadEndedSafe(player, gun);
            }
        }

        /// <summary>
        /// OnReloadEndedSafe() is called at the end of reload and if the gun's owner is a player.
        /// </summary>
        /// <param name="player">The player that reloaded the gun. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnReloadEndedSafe(PlayerController player, Gun gun)
        {

        }

        /// <summary>
        /// OnReloadSafe() is called when the gun is reloaded and the gun's owner is a player.
        /// </summary>
        /// <param name="player">The player that reloaded the gun. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnReloadSafe(PlayerController player, Gun gun)
        {
        }

        /// <summary>
        /// OnFinishAttack() is called when the gun finishes firing, for example when the player releases the Shoot key or the gun's clip empties and if the owner is a player.
        /// </summary>
        /// <param name="player">The player. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnFinishAttack(PlayerController player, Gun gun)
        {
        }

        /// <summary>
        /// OnPostFired() is called after the gun fired and if the owner is a player.
        /// </summary>
        /// <param name="player">The player. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnPostFired(PlayerController player, Gun gun)
        {
            if (gun.IsHeroSword)
            {
                if (this.HeroSwordCooldown == 0.5f)
                {
                    this.OnHeroSwordCooldownStarted(player, gun);
                }
            }
        }

        /// <summary>
        /// OnHeroSwordCooldownStarted() when the gun's Sword Slash started and if the gun is a HeroSword (if gun.IsHeroSword = true).
        /// </summary>
        /// <param name="player"></param>
        /// <param name="gun"></param>
        public virtual void OnHeroSwordCooldownStarted(PlayerController player, Gun gun)
        {
            if (this.usesOverrideHeroSwordCooldown)
            {
                this.HeroSwordCooldown = this.overrideHeroSwordCooldown;
            }
        }

        /// <summary>
        /// OnAmmoChanged() is called when the gun's ammo amount increases/decreases.
        /// </summary>
        /// <param name="player">The player. Will be null if the gun's owner isn't a player.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnAmmoChanged(PlayerController player, Gun gun)
        {
            if (player != null)
            {
                this.OnAmmoChangedSafe(player, gun);
            }
        }

        /// <summary>
        /// OnAmmoChangedSafe() is called when the gun's ammo amount increases/decreases and if the gun's owner is a player.
        /// </summary>
        /// <param name="player">The player. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnAmmoChangedSafe(PlayerController player, Gun gun)
        {
        }

        /// <summary>
        /// OnBurstContinued() is called when the gun continues a burst (attacks while bursting).
        /// </summary>
        /// <param name="player">The player. Will be null if the gun's owner isn't a player.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnBurstContinued(PlayerController player, Gun gun)
        {
            if (player != null)
            {
                this.OnBurstContinuedSafe(player, gun);
            }
        }

        /// <summary>
        /// OnBurstContinuedSafe() is called when the gun continues a burst (attacks while bursting) and if the gun's owner is a player.
        /// </summary>
        /// <param name="player">The player. Can't be null.</param>
        /// <param name="gun">The gun.</param>
        public virtual void OnBurstContinuedSafe(PlayerController player, Gun gun)
        {
        }

        /// <summary>
        /// OnPreFireProjectileModifier() is called before the gun shoots a projectile. If the method returns something that's not the projectile argument, the projectile the gun will shoot will be replaced with the returned projectile.
        /// </summary>
        /// <param name="gun">The gun.</param>
        /// <param name="projectile">Original projectile.</param>
        /// <param name="mod">Target ProjectileModule.</param>
        /// <returns>The replacement projectile.</returns>
        public virtual Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            return projectile;
        }

        public AdvancedGunBehavior()
        {
        }

        /// <summary>
        /// OnPickup() is called when an actor picks the gun up.
        /// </summary>
        /// <param name="owner">The actor that picked up the gun.</param>
        protected virtual void OnPickup(GameActor owner)
        {
            
            if (owner is PlayerController)
            {
                this.OnPickedUpByPlayer(owner as PlayerController);
                (owner as PlayerController).PostProcessBeam += this.CheckForPostProcessBeam;
            }
            if (owner is AIActor)
            {
                this.OnPickedUpByEnemy(owner as AIActor);
            }
        }       

        /// <summary>
        /// OnPostDrop() is called AFTER the owner drops the gun.
        /// </summary>
        /// <param name="owner">The actor that dropped the gun.</param>
        protected virtual void OnPostDrop(GameActor owner)
        {
            if (owner is PlayerController)
            {
                this.OnPostDroppedByPlayer(owner as PlayerController);
                (owner as PlayerController).PostProcessBeam -= this.CheckForPostProcessBeam;
            }
            if (owner is AIActor)
            {
                this.OnPostDroppedByEnemy(owner as AIActor);
            }
        }

        private void CheckForPostProcessBeam(BeamController beam)
        {
            if (beam && beam.projectile && beam.Owner is PlayerController)
            {
                if (beam.projectile.PossibleSourceGun && beam.projectile.PossibleSourceGun == this.gun)
                {
                    PostProcessBeam(beam);
                }
            }
        }
        protected virtual void PostProcessBeam(BeamController beam)
        {

        }

        /// <summary>
        /// OnPickup() is called when a player picks the gun up.
        /// </summary>
        /// <param name="player">The player that picked up the gun.</param>
        protected virtual void OnPickedUpByPlayer(PlayerController player)
        {
            player.GunChanged += this.OnGunsChanged;
        }

        /// <summary>
        /// OnPostDrop() is called AFTER the player drops the gun. If you modify player's stats here, don't forget to call player.stats.RecalculateStats()!
        /// </summary>
        /// <param name="player">The player that dropped the gun.</param>
        protected virtual void OnPostDroppedByPlayer(PlayerController player)
        {
        }

        /// <summary>
        /// OnPickup() is called when an enemy picks the gun up.
        /// </summary>
        /// <param name="enemy">The enemy that picked up the gun.</param>
        protected virtual void OnPickedUpByEnemy(AIActor enemy)
        {
        }

        /// <summary>
        /// OnPostDrop() is called AFTER the enemy drops the gun.
        /// </summary>
        /// <param name="enemy">The enemy that dropped the gun.</param>
        protected virtual void OnPostDroppedByEnemy(AIActor enemy)
        {
        }

        /// <summary>
        /// Returns true if the gun's current owner isn't null.
        /// </summary>
        public bool PickedUp
        {
            get
            {
                return this.gun.CurrentOwner != null;
            }
        }

        /// <summary>
        /// If the gun's owner is a player, returns the gun's current owner as a player.
        /// </summary>
        public PlayerController Player
        {
            get
            {
                if (this.gun.CurrentOwner is PlayerController)
                {
                    return this.gun.CurrentOwner as PlayerController;
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the HeroSwordCooldown of the gun if it isn't null. If it's null, returns -1.
        /// </summary>
        public float HeroSwordCooldown
        {
            get
            {
                if (this.gun != null)
                {
                    return (float)heroSwordCooldown.GetValue(this.gun);
                }
                return -1f;
            }
            set
            {
                if (this.gun != null)
                {
                    heroSwordCooldown.SetValue(this.gun, value);
                }
            }
        }

        /// <summary>
        /// Returns the gun's current owner.
        /// </summary>
        public GameActor Owner
        {
            get
            {
                return this.gun.CurrentOwner;
            }
        }

        /// <summary>
        /// Returns true if the gun's owner isn't null and is a player.
        /// </summary>
        public bool PickedUpByPlayer
        {
            get
            {
                return this.Player != null;
            }
        }

        private bool pickedUpLast = false;
        private GameActor lastOwner = null;
        /// <summary>
        /// Returns true if the gun was ever picked up by a player.
        /// </summary>
        public bool everPickedUpByPlayer = false;
        /// <summary>
        /// Returns true if the gun was ever picked up.
        /// </summary>
        public bool everPickedUp = false;
        /// <summary>
        /// Returns the gun this behaviour is applied to.
        /// </summary>
        private bool hasReloaded = true;
        protected Gun gun;
        /// <summary>
        /// If true, prevents the gun's normal fire audio.
        /// </summary>
        public bool preventNormalFireAudio;
        /// <summary>
        /// If true, prevents the gun's normal reload audio.
        /// </summary>
        public bool preventNormalReloadAudio;
        /// <summary>
        /// The gun's override fire audio. Only works if preventNormalFireAudio is true.
        /// </summary>
        public string overrideNormalFireAudio;
        /// <summary>
        /// The gun's override reload audio. Only works if preventNormalReloadAudio is true.
        /// </summary>
        public string overrideNormalReloadAudio;
        public bool usesOverrideHeroSwordCooldown;
        public float overrideHeroSwordCooldown;
        private static FieldInfo heroSwordCooldown = typeof(Gun).GetField("HeroSwordCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
