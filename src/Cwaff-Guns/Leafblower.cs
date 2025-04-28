
namespace CwaffingTheGungy;

public class Leafblower : CwaffGun
{
    public static string ItemName         = "Leafblower";
    public static string ShortDescription = "Winds of Change";
    public static string LongDescription  = "Pushes around debris on the floor. Deals no direct damage, but can push enemies into pits.";
    public static string Lore             = "Any sensible project manager in charge of designing an industrial strength leafblower would ensure the final product was as effective as possible. Instead, the entire design budget of this leafblower was allocated towards making it look intimidating to the Gundead, with the hopes of scaring them away as the Gungeon's janitors performed their duties. Not only was this approach ineffective -- as the Gundead couldn't care less about the aesthetics of invaders' equipment -- but the horsepower of this leafblower is mediocre at best.";

    private const float _MAX_REACH        = 10.00f; // how far (in tiles) the leafblower reaches
    private const float _MIN_REACH        =  3.00f; // how far (in tiles) the leafblower blows at max power
    private const float _SPREAD           =    30f; // radius (in degrees) of gust cone at the end of our reach
    private const float _DEBRIS_FORCE     =   2.0f; // force with which debris is blown around
    private const float _ACTOR_FORCE      =  20.0f; // force with which enemies are blown around
    private const float _PROJ_FORCE       = 120.0f; // force with which projectiles are blown around

    private const float _SQR_MAX_REACH = _MAX_REACH * _MAX_REACH;
    private const float _SQR_MIN_REACH = _MIN_REACH * _MIN_REACH;

    private readonly Dictionary<AIActor, ActiveKnockbackData> _Knockbacks = new();
    private readonly List<AIActor> _RemovableKeys = new();

    public static void Init()
    {
        Lazy.SetupGun<Leafblower>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: CwaffGunClass.UTILITY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true,
            chargeFps: 60, banFromBlessedRuns: true)
          .AddDualWieldSynergy(Synergy.FULL_CIRCULATION)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .AddToShop(ModdedShopType.Rusty)
          .InitProjectile(GunData.New(clipSize: -1, shootStyle: ShootStyle.Charged, hideAmmo: true, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        float dtime = BraveTime.DeltaTime;
        if (dtime == 0.0f)
            return;
        if (!this.gun.IsCharging)
            return;
        Vector2 gunpos = this.gun.barrelOffset.position;

        // do sfx and vfx
        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "leafblower_loop", source: this.gun.gameObject, timer: 0.05f);
        if (UnityEngine.Random.value < 0.66f * (dtime * C.FPS))
        {
            float angleFromGun = this.gun.CurrentAngle + UnityEngine.Random.Range(-_SPREAD, _SPREAD);
            GameObject o = SpawnManager.SpawnVFX(VacuumCleaner._VacuumVFX, gunpos, Lazy.RandomEulerZ(), ignoresPools: true);
            o.AddComponent<LeafblowerParticle>().Setup(angleFromGun);
        }

        // blow around debris
        float debrisForce = _DEBRIS_FORCE * (this.Mastered ? 2f : 1f);
        foreach (DebrisObject debris in gunpos.DebrisWithinCone(_SQR_MAX_REACH, this.gun.CurrentAngle, _SPREAD, limit: 100))
        {
            Vector2 debrisCenter = debris.sprite ? debris.sprite.WorldCenter : debris.gameObject.transform.position.XY();
            Vector2 angleFromPlayer = debrisCenter - gunpos;
            float sqrDist = angleFromPlayer.sqrMagnitude;
            Vector2 applyVelocity = (debrisForce * (1f - Mathf.Clamp01((sqrDist - _SQR_MIN_REACH) / (_SQR_MAX_REACH - _SQR_MIN_REACH)))) * angleFromPlayer.normalized;
            if (debris.HasBeenTriggered)
                debris.ApplyVelocity(applyVelocity);
            else
                debris.Trigger(applyVelocity, 0.5f);
        }

        // blow around enemies
        float actorForce = _ACTOR_FORCE * (this.Mastered ? 2f : 1f);
        foreach (AIActor enemy in Lazy.AllEnemiesWithinConeOfVision(gunpos, this.gun.CurrentAngle, _SPREAD, _MAX_REACH))
        {
            if (enemy.knockbackDoer is not KnockbackDoer kb || kb.m_isImmobile.Value)
                continue;
            Vector2 knockbackAngle = enemy.CenterPosition - gunpos;
            float sqrDist = knockbackAngle.sqrMagnitude;
            Vector2 applyVelocity = (actorForce * (1f - Mathf.Clamp01((sqrDist - _SQR_MIN_REACH) / (_SQR_MAX_REACH - _SQR_MIN_REACH)))) * knockbackAngle.normalized;
            float force = applyVelocity.magnitude;
            if (kb.ApplySourcedKnockback(applyVelocity, force, base.gameObject) is ActiveKnockbackData data)
                _Knockbacks[enemy] = data;
            else if (_Knockbacks.TryGetValue(enemy, out ActiveKnockbackData previousData))
            {
                // fancy logic for replacing the old knockback
                previousData.knockback = Lazy.MaxMagnitude(previousData.knockback, applyVelocity.normalized * (force / (kb.weight / 10f)));
                previousData.initialKnockback = previousData.knockback;
                previousData.elapsedTime = 0.0f;
            }
        }

        // blow around projectiles if mastered
        if (this.Mastered)
        {
            ReadOnlyCollection<Projectile> allProj = StaticReferenceManager.AllProjectiles;
            for (int j = allProj.Count - 1; j >= 0; j--)
            {
                Projectile p = allProj[j];
                if (!p || p.Owner is PlayerController)
                    continue;

                Vector2 delta = p.SafeCenter - gunpos;
                float sqrMag = delta.sqrMagnitude;
                if (sqrMag > _SQR_MAX_REACH)
                    continue;

                float angle = delta.ToAngle().Clamp360();
                float angleDeviation = Mathf.Abs((this.gun.CurrentAngle - angle).Clamp180());
                if (angleDeviation > _SPREAD)
                    continue;

                p.RemoveBulletScriptControl();
                Vector2 applyVelocity = (_PROJ_FORCE * (1f - Mathf.Clamp01((sqrMag - _SQR_MIN_REACH) / (_SQR_MAX_REACH - _SQR_MIN_REACH)))) * delta.normalized;
                p.gameObject.GetOrAddComponent<BlownByLeafblowerBehavior>().Blow(applyVelocity);
            }
        }

        // remove stale knockback data
        foreach (AIActor key in _Knockbacks.Keys)
        {
            ActiveKnockbackData tempData = _Knockbacks[key];
            if (tempData.elapsedTime >= tempData.curveTime)
                _RemovableKeys.Add(key);
        }
        foreach (AIActor key in _RemovableKeys)
            _Knockbacks.Remove(key);
        _RemovableKeys.Clear();
    }

    public class BlownByLeafblowerBehavior : MonoBehaviour
    {
        private Projectile _projectile;
        private PlayerController _owner;
        private bool _setup = false;
        private bool _active = false;
        private Vector2 _windVector;

        public void Blow(Vector2 windVector)
        {
            if (!this._setup)
                Setup();
            if (!this._projectile)
            {
                UnityEngine.Object.Destroy(this);
                return;
            }
            this._windVector = windVector;
            this._active = true;
        }

        private void Setup()
        {
            this._projectile = base.GetComponent<Projectile>();
            this._owner = this._projectile.Owner as PlayerController;
            this._projectile.ModifyVelocity += this.ModifyVelocity;
            this._setup = true;
        }

        private Vector2 ModifyVelocity(Vector2 inVector)
        {
            if (!this._active)
                return inVector;
            this._active = false;
            return inVector + BraveTime.DeltaTime * this._windVector;
        }
    }

    private class LeafblowerParticle : MonoBehaviour
    {
        private const float _MAX_LIFE    = 1.0f;
        private const float _DRAG        = 0.25f;
        private const float _MIN_SPEED   = 15f;
        private const float _MAX_SPEED   = 25f;
        private const float _MAX_ANG_VEL = 100f;

        private float _lifetime        = 0.0f;
        private float _angle           = 0.0f;
        private float _mag             = 0.0f;
        private float _angularVel      = 0.0f;
        private tk2dBaseSprite _sprite = null;
        private Vector2 _spriteCenter  = Vector2.zero;

        public void Setup(float startAngle)
        {
            this._sprite       = base.gameObject.GetComponent<tk2dSprite>();
            this._spriteCenter = this._sprite.WorldCenter;
            this._mag          = UnityEngine.Random.Range(_MIN_SPEED, _MAX_SPEED);
            this._angle        = startAngle;
            this._angularVel   = UnityEngine.Random.Range(-_MAX_ANG_VEL, _MAX_ANG_VEL);
        }

        // Using LateUpdate() here so alpha is updated correctly
        private void LateUpdate()
        {
            float dtime = BraveTime.DeltaTime;
            if (dtime == 0.0f)
                return; // nothing to do if time isn't passing

            if ((this._lifetime += dtime) > _MAX_LIFE)
            {
                UnityEngine.GameObject.Destroy(base.gameObject);
                return;
            }

            float percentLeft = 1f - (this._lifetime / _MAX_LIFE);
            this._mag *= Mathf.Pow(_DRAG, dtime);
            this._angle += this._angularVel * dtime;
            this._sprite.scale = new Vector3(percentLeft, percentLeft, 1f);
            this._spriteCenter += this._angle.ToVector(this._mag) * BraveTime.DeltaTime;
            this._sprite.PlaceAtRotatedPositionByAnchor(this._spriteCenter, Anchor.MiddleCenter);
            this._sprite.renderer.SetAlpha(percentLeft);
        }
    }
}
