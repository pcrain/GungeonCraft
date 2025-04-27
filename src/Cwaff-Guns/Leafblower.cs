namespace CwaffingTheGungy;

public class Leafblower : CwaffGun
{
    public static string ItemName         = "Leafblower";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _MAX_REACH        = 10.00f; // how far (in tiles) the leafblower reaches
    private const float _MIN_REACH        =  3.00f; // how far (in tiles) the leafblower blows at max power
    private const float _SPREAD           =    30f; // radius (in degrees) of gust cone at the end of our reach
    private const float _DEBRIS_FORCE     =   2.0f; // force with which debris is blown around
    private const float _ACTOR_FORCE      =  20.0f; // force with which enemies are blown around

    private const float _SQR_MAX_REACH = _MAX_REACH * _MAX_REACH;
    private const float _SQR_MIN_REACH = _MIN_REACH * _MIN_REACH;

    private readonly Dictionary<AIActor, ActiveKnockbackData> _Knockbacks = new();
    private readonly List<AIActor> _RemovableKeys = new();

    public static void Init()
    {
        Lazy.SetupGun<Leafblower>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: CwaffGunClass.UTILITY, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true,
            chargeFps: 60, banFromBlessedRuns: true)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .AddToShop(ModdedShopType.Rusty)
          .InitProjectile(GunData.New(clipSize: -1, shootStyle: ShootStyle.Charged, hideAmmo: true, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging)
            return;
        Vector2 gunpos = this.gun.barrelOffset.position;

        // do sfx and vfx
        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "leafblower_loop", source: this.gun.gameObject, timer: 0.05f);
        if (UnityEngine.Random.value < 0.66f * (BraveTime.DeltaTime * C.FPS))
        {
            float angleFromGun = this.gun.CurrentAngle + UnityEngine.Random.Range(-_SPREAD, _SPREAD);
            GameObject o = SpawnManager.SpawnVFX(VacuumCleaner._VacuumVFX, gunpos, Lazy.RandomEulerZ(), ignoresPools: true);
            o.AddComponent<LeafblowerParticle>().Setup(angleFromGun);
        }

        // blow around debris
        foreach (DebrisObject debris in gunpos.DebrisWithinCone(_SQR_MAX_REACH, this.gun.CurrentAngle, _SPREAD, limit: 100))
        {
            Vector2 debrisCenter = debris.sprite ? debris.sprite.WorldCenter : debris.gameObject.transform.position.XY();
            Vector2 angleFromPlayer = debrisCenter - gunpos;
            float sqrDist = angleFromPlayer.sqrMagnitude;
            Vector2 applyVelocity = (_DEBRIS_FORCE * (1f - Mathf.Clamp01((sqrDist - _SQR_MIN_REACH) / (_SQR_MAX_REACH - _SQR_MIN_REACH)))) * angleFromPlayer.normalized;
            if (debris.HasBeenTriggered)
                debris.ApplyVelocity(applyVelocity);
            else
                debris.Trigger(applyVelocity, 0.5f);
        }

        // blow around enemies
        foreach (AIActor enemy in Lazy.AllEnemiesWithinConeOfVision(gunpos, this.gun.CurrentAngle, _SPREAD, _MAX_REACH))
        {
            if (enemy.knockbackDoer is not KnockbackDoer kb || kb.m_isImmobile.Value)
                continue;
            Vector2 knockbackAngle = enemy.CenterPosition - gunpos;
            float sqrDist = knockbackAngle.sqrMagnitude;
            Vector2 applyVelocity = (_ACTOR_FORCE * (1f - Mathf.Clamp01((sqrDist - _SQR_MIN_REACH) / (_SQR_MAX_REACH - _SQR_MIN_REACH)))) * knockbackAngle.normalized;
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
