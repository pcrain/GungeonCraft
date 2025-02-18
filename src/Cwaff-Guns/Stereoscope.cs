namespace CwaffingTheGungy;

public class Stereoscope : CwaffGun
{
    public static string ItemName         = "Stereoscope";
    public static string ShortDescription = "Surround Sound";
    public static string LongDescription  = "Continuously emits sound at a pitch corresponding to aim direction, and fires sound waves which only damage enemies that resonate with the current pitch. Each enemy type resonates with a specific pitch that remains the same until leaving the Gungeon. Enemies are stunned while they are resonating, and sound waves deal extra damage when fired in time with Stereoscope's beat.";
    public static string Lore             = "TBD";

    private const int _IDLE_FPS           = 19;
    private const int _SOUND_MS           = 469;
    private const float _VFX_RATE         = 0.1f;
    private const float _STUN_LINGER_TIME = 0.25f;

    internal static GameObject _ResonancePrefab          = null;
    private static Dictionary<string, int> _FrequencyMap = new();

    private int _frequency    = 0;
    private int _lastSoundPos = 0;
    private uint _soundId     = 0;
    private float _lastVfx    = 0.0f;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Stereoscope>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.RIFLE, reloadTime: 0.0f, ammo: 261, idleFps: _IDLE_FPS, shootFps: _IDLE_FPS,
                fireAudio: "stereoscope_fire_sound", attacksThroughWalls: true, suppressReloadAnim: true, autoPlay: false);

        gun.InitSpecialProjectile<ResonantProjectile>(GunData.New(sprite: null, clipSize: -1, cooldown: 0.3f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 17.0f, speed: 25f, range: 18f, force: 12f, invisibleProjectile: true));

        gun.reloadAnimation = gun.idleAnimation; // animation shouldn't automatically change when reloading

        _ResonancePrefab = VFX.Create("resonance_vfx");
    }

    private void Resonate(Vector2 pos)
    {
        CwaffVFX.SpawnBurst(
            prefab           : _ResonancePrefab,
            numToSpawn       : 6,
            basePosition     : pos,
            positionVariance : 0.4f,
            velocityVariance : 10f,
            velType          : CwaffVFX.Vel.AwayRadial,
            rotType          : CwaffVFX.Rot.Position,
            lifetime         : 0.25f,
            fadeOutTime      : 0.25f,
            uniform          : true,
            startScale       : 1.0f,
            endScale         : 0.1f,
            specificFrame    : this._frequency + 6,
            height           : 3f
          );
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        gun.SetAnimationFPS(gun.idleAnimation, _IDLE_FPS); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.Play();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        gun.SetAnimationFPS(gun.idleAnimation, 0); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.StopAndResetFrameToDefault();
    }

    public override void Update()
    {
        base.Update();
        if (GameManager.Instance.IsLoadingLevel || GameManager.Instance.IsPaused || BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        this._frequency = Mathf.FloorToInt(this.PlayerOwner.m_currentGunAngle.Clamp360() / 30f) - 6;
        bool soundIsPlaying = AkSoundEngine.GetSourcePlayPosition(this._soundId, out int pos) == AKRESULT.AK_Success;
        bool playedSoundThisFrame = false;
        if (!soundIsPlaying || pos < this._lastSoundPos)
        {
            string sound_name = "stereoscope_charge_sound_0";
            if (this._frequency > 0)
                sound_name = $"stereoscope_charge_sound_{this._frequency}_up";
            else if (this._frequency < 0)
                sound_name = $"stereoscope_charge_sound_{-this._frequency}_down";
            this._soundId = AkSoundEngine.PostEvent(sound_name, this.gun.gameObject, in_uFlags: (uint)AkCallbackType.AK_EnableGetSourcePlayPosition);
            playedSoundThisFrame = true;
            this._lastSoundPos = 0;
        }
        if (soundIsPlaying)
            this._lastSoundPos = pos;

        float now = BraveTime.ScaledTimeSinceStartup;
        bool doVfx = false;
        if (playedSoundThisFrame && now > this._lastVfx + _VFX_RATE)
        {
            this._lastVfx = now;
            doVfx = true;
            Resonate(this.gun.sprite.WorldCenter);
        }

        RoomHandler room = this.PlayerOwner.CurrentRoom;
        foreach(AIActor enemy in room.SafeGetEnemiesInRoom())
        {
            if (!enemy || enemy.IsGone)
                continue;
            if (enemy.behaviorSpeculator is not BehaviorSpeculator bs)
                continue;
            string guid = enemy.EnemyGuid;
            if (guid.IsNullOrWhiteSpace())
                continue;
            if (!_FrequencyMap.TryGetValue(guid, out int resonantFrequency))
                _FrequencyMap[guid] = resonantFrequency = UnityEngine.Random.Range(-6, 6);
            if (resonantFrequency != this._frequency)
                continue;
            if (!bs.ImmuneToStun)
                bs.Stun(_STUN_LINGER_TIME, createVFX: true);
            if (doVfx)
                Resonate(enemy.CenterPosition);
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile is not ResonantProjectile res)
            return;
        res.frequency = this._frequency;
        // get distance from sound loop point as timing accuracy
        float accuracy = 1f - (2f * (Mathf.Min(this._lastSoundPos, _SOUND_MS - this._lastSoundPos) / (float)_SOUND_MS));
        res.power = Mathf.RoundToInt(8f * accuracy * accuracy); // square power falloff
        res.gun = this.gun;
    }

    private class ResonantProjectile : Projectile
    {
        public int frequency = 99; // guaranteed unused
        public int power = 4;
        public Gun gun = null;

        private void Reverberate(Vector2 pos)
        {
            CwaffVFX.SpawnBurst(
                prefab           : _ResonancePrefab,
                numToSpawn       : power,
                basePosition     : pos,
                positionVariance : 1.75f,
                velocityVariance : 1f,
                velType          : CwaffVFX.Vel.AwayRadial,
                rotType          : CwaffVFX.Rot.Position,
                lifetime         : 0.25f,
                fadeOutTime      : 0.25f,
                uniform          : true,
                startScale       : 1.0f,
                endScale         : 0.1f,
                specificFrame    : this.frequency + 6,
                height           : 3f
              );
            Exploder.DoDistortionWave(center: pos, distortionIntensity: 1.5f, distortionRadius: 0.05f, maxRadius: 1.75f, duration: 0.25f);
        }

        public override void Move()
        {
            if (gun)
                Reverberate(gun.sprite.WorldCenter);
            Vector3 pos = base.transform.position;
            RoomHandler absoluteRoom = pos.GetAbsoluteRoom();
            absoluteRoom.ApplyActionToNearbyEnemies(pos.XY(), 100f, ProcessEnemy);
            DieInAir(true, false, false, false);
        }

        private void ProcessEnemy(AIActor enemy, float b)
        {
            if (!enemy || !enemy.IsNormalEnemy || !enemy.healthHaver || enemy.IsGone)
                return;

            string guid = enemy.EnemyGuid;
            if (guid.IsNullOrWhiteSpace())
                return;
            if (!_FrequencyMap.TryGetValue(guid, out int resonantFrequency))
                _FrequencyMap[guid] = resonantFrequency = UnityEngine.Random.Range(-6, 6);
            if (resonantFrequency != this.frequency)
                return;
            Reverberate(enemy.CenterPosition);
            enemy.healthHaver.ApplyDamage((0.125f * this.power) * base.ModifiedDamage, Vector2.zero, base.Owner ? base.OwnerName : "projectile", damageTypes);
            if (enemy.healthHaver.IsDead && !enemy.healthHaver.IsBoss && !enemy.healthHaver.IsSubboss)
            {
                SunderbussProjectile.ShatterViolentlyIntoAMillionPieces(enemy);
                enemy.EraseFromExistenceWithRewards(suppressDeathSounds: true);
            }
        }
    }
}
