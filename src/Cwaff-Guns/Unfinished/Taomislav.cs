namespace CwaffingTheGungy
{
    public class Taomislav : AdvancedGunBehavior
    {
        public static string ItemName         = "Taomislav";
        public static string SpriteName       = "taomislav";
        public static string ProjectileName   = "38_special";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        internal static tk2dSpriteAnimationClip _BulletSprite;
        internal static float                   _BaseCooldownTime = 0.4f;
        internal static int                     _FireAnimationFrames = 8;

        private const float _NATASHA_PROJECTILE_SCALE = 0.5f;
        private float _speedMult                      = 1.0f;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun<Taomislav>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
                gun.DefaultModule.ammoCost            = 1;
                gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
                gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Ordered;
                gun.reloadTime                        = 1.1f;
                gun.DefaultModule.angleVariance       = 15.0f;
                gun.DefaultModule.cooldownTime        = _BaseCooldownTime;
                gun.DefaultModule.numberOfShotsInClip = -1;
                gun.quality                           = PickupObject.ItemQuality.D;
                gun.SetBaseMaxAmmo(2500);
                gun.CurrentAmmo = 2500;
                gun.SetAnimationFPS(gun.shootAnimation, (int)((float)_FireAnimationFrames / _BaseCooldownTime) + 1);

            _BulletSprite = AnimateBullet.CreateProjectileAnimation(
                ResMap.Get("natascha_bullet").Base(),
                12, true, new IntVector2((int)(_NATASHA_PROJECTILE_SCALE * 15), (int)(_NATASHA_PROJECTILE_SCALE * 7)),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, true);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.AddAnimation(_BulletSprite);
                projectile.SetAnimation(_BulletSprite);
                projectile.baseData.damage  = 3f;
                projectile.baseData.speed   = 20.0f;
                projectile.transform.parent = gun.barrelOffset;
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            AkSoundEngine.PostEvent("tomislav_shoot", gun.gameObject);
        }

        private void RecalculateGunStats()
        {
            if (!this.Player)
                return;

            this.gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
            this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, (float)Math.Sqrt(this._speedMult), StatModifier.ModifyMethod.MULTIPLICATIVE);
            this.gun.RemoveStatFromGun(PlayerStats.StatType.RateOfFire);
            this.gun.AddStatToGun(PlayerStats.StatType.RateOfFire, 1.0f / this._speedMult, StatModifier.ModifyMethod.MULTIPLICATIVE);
            this.Player.stats.RecalculateStats(this.Player);
        }
    }
}
