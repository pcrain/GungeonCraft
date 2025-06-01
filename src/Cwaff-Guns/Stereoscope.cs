

namespace CwaffingTheGungy;

public class Stereoscope : CwaffGun
{
    public static string ItemName         = "Stereoscope";
    public static string ShortDescription = "Surround Sound";
    public static string LongDescription  = "Continuously emits sound at a pitch corresponding to aim direction, and fires sonic waves which only damage enemies that resonate with the current pitch. Each enemy type resonates with a specific pitch that remains the same until leaving the Gungeon. Enemies are stunned while they are resonating, and sonic waves deal extra damage when fired in time with Stereoscope's beat.";
    public static string Lore             = "A sonic weapon that was once very popular among 90's kids and Kins, doomed to fall into obscurity after the advent of the iBomb and later the iPwn. While the Stereoscope's ability to incapacitate the Gundead is indisputable, it's anyone's guess as to whether the prevailing theory regarding the Gundead's sensitivity to certain frequencies holds any weight, or if they just really despise the average Gungeoneer's taste in music.";

    private const int _IDLE_FPS           = 19;
    private const int _SOUND_MS           = 469;
    private const float _VFX_RATE         = 0.1f;
    private const float _STUN_LINGER_TIME = 0.25f;
    private const int _MAX_SOUNDS         = 16;

    internal static GameObject _ResonancePrefab               = null;
    internal static GameObject _StereoPrefab                  = null;
    private static ResonantProjectile _StereoscopeProjectile  = null;
    private static Dictionary<string, int> _FrequencyMap      = new();
    private static uint[] _PlayingIds                         = new uint[_MAX_SOUNDS];

    private int _frequency                  = 0;
    private int _lastSoundPos               = 0;
    private uint _soundId                   = 0;
    private float _lastVfx                  = 0.0f;
    private StereoscopeStereo _extantStereo = null;

    public static void Init()
    {
        Lazy.SetupGun<Stereoscope>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.RIFLE, reloadTime: 0.0f, ammo: 261, idleFps: _IDLE_FPS, shootFps: _IDLE_FPS,
            fireAudio: "stereoscope_fire_sound", attacksThroughWalls: true, suppressReloadAnim: true, autoPlay: false)
          .InitSpecialProjectile<ResonantProjectile>(GunData.New(sprite: null, clipSize: -1, cooldown: 0.3f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 17.0f, speed: 25f, range: 18f, force: 12f, invisibleProjectile: true, customClip: true))
          .Assign(out _StereoscopeProjectile);

        _ResonancePrefab = VFX.Create("resonance_vfx");
        _StereoPrefab = VFX.Create("stereoscope_stereo", fps: 19).Attach<StereoscopeStereo>();
    }

    private void Resonate(Vector2 pos, int freq)
    {
        CwaffVFX.SpawnBurst(prefab: _ResonancePrefab, numToSpawn: 6, basePosition: pos, positionVariance: 0.4f, velocityVariance: 10f,
            velType: CwaffVFX.Vel.AwayRadial, rotType: CwaffVFX.Rot.Position, lifetime: 0.25f, fadeOutTime: 0.25f, uniform: true,
            startScale: 1.0f, endScale: 0.1f, specificFrame: freq + 6, height: 8f);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        gun.SetAnimationFPS(gun.idleAnimation, _IDLE_FPS); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.Play();
        CwaffEvents.OnChangedRooms -= OnChangedRooms;
        CwaffEvents.OnChangedRooms += OnChangedRooms;
    }

    private void OnChangedRooms(PlayerController player, RoomHandler oldRoom, RoomHandler newRoom)
    {
        if (player == this.PlayerOwner)
            CleanUpStereos();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        CleanUpStereos();
        gun.SetAnimationFPS(gun.idleAnimation, 0); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.StopAndResetFrameToDefault();
        CwaffEvents.OnChangedRooms -= OnChangedRooms;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (player.IsDodgeRolling || !player.AcceptingNonMotionInput || !this.Mastered)
            return;

        CleanUpStereos();
        this._extantStereo = _StereoPrefab.Instantiate(position: player.CenterPosition).AddComponent<StereoscopeStereo>();
        this._extantStereo.Setup(this);
        base.gameObject.Play("place_stereo_sound");
        Lazy.DoSmokeAt(player.CenterPosition);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this._extantStereo)
            this._extantStereo.linked = false;
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        if (this._extantStereo)
            this._extantStereo.linked = true;
    }

    public override void OnDestroy()
    {
        CleanUpStereos();
        CwaffEvents.OnChangedRooms -= OnChangedRooms;
        base.OnDestroy();
    }

    private void CleanUpStereos()
    {
        if (!this._extantStereo)
            return;
        Lazy.DoPickupAt(this._extantStereo.pos);
        UnityEngine.Object.Destroy(this._extantStereo.gameObject);
        this._extantStereo = null;
    }

    private bool IsGunResonating()
    {
        uint maxSounds = _MAX_SOUNDS;
        AKRESULT result = AkSoundEngine.GetPlayingIDsFromGameObject(this.PlayerOwner.gameObject, ref maxSounds, _PlayingIds);
        for (int i = 0; i < maxSounds; i++)
            if (_PlayingIds[i] == this._soundId)
            {
                AKRESULT status = AkSoundEngine.GetSourcePlayPosition(this._soundId, out int pos);
                this._lastSoundPos = (status == AKRESULT.AK_Success) ? pos : 0;
                return true;
            }
        return false;
    }

    private static string GetSoundForFrequency(int freq)
    {
        if (freq > 0)
            return $"stereoscope_charge_sound_{freq}_up";
        if (freq < 0)
            return $"stereoscope_charge_sound_{-freq}_down";
        return "stereoscope_charge_sound_0";
    }

    internal static int FrequencyFromGunAngle(PlayerController player)
    {
        return Mathf.FloorToInt(player.m_currentGunAngle.Clamp360() / 30f) - 6;
    }

    public void HandleAudioChecks()
    {
        if (!this.PlayerOwner)
            return;

        bool isCurrentGun = this.gun == this.PlayerOwner.CurrentGun;
        if (isCurrentGun)
            this._frequency = FrequencyFromGunAngle(this.PlayerOwner);
        else if (this._extantStereo)
            this._frequency = this._extantStereo.freq;
        float now = BraveTime.ScaledTimeSinceStartup;
        bool playedSoundThisFrame = (now > this._lastVfx + _VFX_RATE) && !IsGunResonating();
        if (playedSoundThisFrame)
        {
            playedSoundThisFrame = true;
            this._lastSoundPos = 0;
            this._lastVfx = now;
            this._soundId = AkSoundEngine.PostEvent(GetSoundForFrequency(this._frequency),
                in_gameObjectID: this.PlayerOwner.gameObject,
                in_uFlags: (uint)AkCallbackType.AK_EnableGetSourcePlayPosition);
            if (isCurrentGun)
                Resonate(this.gun.sprite.WorldCenter, this._frequency);
            if (this._extantStereo)
            {
                Resonate(this._extantStereo.pos, this._extantStereo.freq);
                if (isCurrentGun && (this._frequency != this._extantStereo.freq))
                    this._extantStereo.gameObject.Play(GetSoundForFrequency(this._extantStereo.freq));
            }
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
            if (resonantFrequency != this._frequency && (!this._extantStereo || resonantFrequency != this._extantStereo.freq))
                continue;
            if (!bs.ImmuneToStun)
                bs.Stun(_STUN_LINGER_TIME, createVFX: true);
            if (playedSoundThisFrame)
                Resonate(enemy.CenterPosition, resonantFrequency);
        }
    }

    public override void Update()
    {
        base.Update();
        if (GameManager.Instance.IsLoadingLevel || GameManager.Instance.IsPaused || BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        HandleAudioChecks();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile is not ResonantProjectile res)
            return;
        // get distance from sound loop point as timing accuracy
        float accuracy = 1f - (2f * (Mathf.Min(this._lastSoundPos, _SOUND_MS - this._lastSoundPos) / (float)_SOUND_MS));
        int damage = Mathf.RoundToInt(8f * accuracy * accuracy); // square power falloff

        res.frequency = this._frequency;
        res.power     = damage;
        res.gun       = this.gun;

        if (!this._extantStereo)
            return;

        Projectile p = SpawnManager.SpawnProjectile(
            prefab   : _StereoscopeProjectile.gameObject,
            position : this._extantStereo.pos,
            rotation : Quaternion.identity).GetComponent<Projectile>();
        p.SetOwnerAndStats(this.PlayerOwner);
        ResonantProjectile res2 = p.gameObject.GetComponent<ResonantProjectile>();
        res2.frequency = this._extantStereo.freq;
        res2.power     = damage;
        res2.gun       = this.gun;
    }

    private class ResonantProjectile : Projectile
    {
        private const int NO_FREQ = 99; // guaranteed unused initial value

        public int frequency = NO_FREQ;
        public int power = 8;
        public Gun gun = null;

        public override void Start()
        {
            base.Start();
            this.m_usesNormalMoveRegardless = true;
        }

        public override void Move()
        {
            if (this.frequency == NO_FREQ)
            {
                if (this.m_owner is PlayerController player)
                    this.frequency = Stereoscope.FrequencyFromGunAngle(player);
                else
                    this.frequency = UnityEngine.Random.Range(-6, 6);
            }
            Vector3 pos = base.transform.position;
            Reverberate(pos);
            RoomHandler absoluteRoom = pos.GetAbsoluteRoom();
            absoluteRoom.ApplyActionToNearbyEnemies(pos.XY(), 100f, ProcessEnemy);
            DieInAir(true, false, false, false);
        }

        private void Reverberate(Vector2 pos)
        {
            CwaffVFX.SpawnBurst(prefab: _ResonancePrefab, numToSpawn: power, basePosition: pos, positionVariance: 1.75f, velocityVariance: 1f,
              velType: CwaffVFX.Vel.AwayRadial, rotType: CwaffVFX.Rot.Position, lifetime: 0.25f, fadeOutTime: 0.25f, uniform: true,
              startScale: 1.0f, endScale: 0.1f, specificFrame: this.frequency + 6, height: 8f);
            Exploder.DoDistortionWave(center: pos, distortionIntensity: 1.5f, distortionRadius: 0.05f, maxRadius: 1.75f, duration: 0.25f);
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

    public class StereoscopeStereo : MonoBehaviour
    {
        public Vector2 pos;
        public int freq;
        public bool linked;

        private Stereoscope _gun;

        public void Setup(Stereoscope gun)
        {
            this.pos    = base.transform.position;
            this._gun   = gun;
            this.freq   = gun._frequency;
            this.linked = true;
        }

        private void Update()
        {
            if (!this.linked && this._gun)
                this._gun.HandleAudioChecks();
        }
    }
}
