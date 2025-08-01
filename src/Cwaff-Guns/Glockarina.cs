﻿namespace CwaffingTheGungy;

public class Glockarina : CwaffGun
{
    public static string ItemName         = "Glockarina";
    public static string ShortDescription = "ShOOT 'em Up";
    public static string LongDescription  = "Fires musical notes as projectiles. Reloading with a full clip while aiming in a cardinal direction will play a note corresponding to that direction. Playing certain songs can change the properties of the projectiles or have other side effects.";
    public static string Lore             = "An unorthodox toy gun brought into the Gungeon by a teary-eyed child, who received it as a seasonal gift from 'one of Santa's elves'. Legend holds that the spirits of various phantoms have been masked inside this gun since ages long past, and that breathing wind through the gun while raising it skyward at twilight can awaken their diminished powers.";

    internal const string _StormSpriteUI = $"{C.MOD_PREFIX}:_GlockStormSpriteUI";
    internal const string _TimeSpriteUI  = $"{C.MOD_PREFIX}:_GlockTimeSpriteUI";
    internal const string _SariaSpriteUI = $"{C.MOD_PREFIX}:_GlockSariaSpriteUI";
    internal const string _EmptySpriteUI = $"{C.MOD_PREFIX}:_GlockEmptySpriteUI";

    private const float _MOUSE_DEAD_ZONE_SQR      = 4f;
    private const float _CONTROLLER_DEAD_ZONE_SQR = 0.16f;
    private const float _DECOY_LIFE               = 2f;

    internal enum Mode {
        DEFAULT,  // no special effects
        STORM,    // spreads electrified water goop in transit
        TIME,     // slows down enemy bullets close to notes
        SARIA,    // homes in on nearby enemies with slightly increased damage
        EMPTY,    // killed enemies become decoys
        BOLERO,   // spreads fire goop in transit
        REQUIEM,  // pierces enemies

        DOUBLE,   // not a real mode, but song should clear room for 1/3 of max ammo
        SUN,      // not a real mode, but song should clear darkness effects
        PRELUDE,  // not a real mode, but song should warp player to shop
        HEALING,  // not a real mode, but restores half a heart once per floor
        WHAT,     // heh
    }

    private enum Note {
        UP,
        LEFT,
        RIGHT,
        DOWN,
        A,
    }

    internal static GameObject _DecoyPrefab   = null;
    internal static GameObject _NoteVFXPrefab = null;
    private static Projectile _Projectile = null;
    private static int _GlockarinaPickupID    = -1;
    private static List<List<Note>> _Songs = new(){
        /* DEFAULT */ null,
        /* STORM   */ new(){Note.A, Note.DOWN, Note.UP, Note.A, Note.DOWN, Note.UP},
        /* TIME    */ new(){Note.RIGHT, Note.A, Note.DOWN, Note.RIGHT, Note.A, Note.DOWN},
        /* SARIA   */ new(){Note.DOWN, Note.RIGHT, Note.LEFT, Note.DOWN, Note.RIGHT, Note.LEFT},
        /* EMPTY   */ new(){Note.RIGHT, Note.LEFT, Note.RIGHT, Note.DOWN, Note.RIGHT, Note.UP, Note.LEFT},
        /* BOLERO  */ new(){Note.DOWN, Note.A, Note.DOWN, Note.A, Note.RIGHT, Note.DOWN, Note.RIGHT, Note.DOWN},
        /* REQUIEM */ new(){Note.A, Note.DOWN, Note.A, Note.RIGHT, Note.DOWN, Note.A},
        /* DOUBLE  */ new(){Note.RIGHT, Note.RIGHT, Note.A, Note.A, Note.DOWN, Note.DOWN},
        /* SUN     */ new(){Note.RIGHT, Note.DOWN, Note.UP, Note.RIGHT, Note.DOWN, Note.UP},
        /* PRELUDE */ new(){Note.UP, Note.RIGHT, Note.UP, Note.RIGHT, Note.LEFT, Note.UP},
        /* HEALING */ new(){Note.LEFT, Note.RIGHT, Note.DOWN, Note.LEFT, Note.RIGHT, Note.DOWN},
        /* WHAT    */ new(){Note.A, Note.A, Note.UP, Note.RIGHT},
    };

    internal Mode _mode = Mode.DEFAULT;
    private List<Note> _lastNotes = new();
    private DamageTypeModifier _electricImmunity = null;

    [SerializeField]
    private bool _didHealThisFloor = false;

    public static void Init()
    {
        Lazy.SetupGun<Glockarina>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 400, canReloadNoMatterAmmo: true,
            shootFps: 24, reloadFps: 20, muzzleFrom: Items.Mailbox, fireAudio: "glockarina_shoot_sound", reloadAudio: "glockarina_reload_sound")
          .Attach<GlockarinaAmmoDisplay>()
          .InitProjectile(GunData.New(clipSize: 12, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic, speed: 35f, damage: 7.5f, customClip: true,
            sprite: "glockarina_projectile", fps: 12, anchor: Anchor.MiddleLeft, shouldRotate: false))
          .Assign(out _Projectile);

        _DecoyPrefab = ItemHelper.Get(Items.Decoy).GetComponent<SpawnObjectPlayerItem>().objectToSpawn.ClonePrefab();
        _DecoyPrefab.GetComponent<Decoy>().DeathExplosionTimer = _DECOY_LIFE;

        _NoteVFXPrefab = VFX.Create("note_vfx");
    }

    [HarmonyPatch(typeof(Chest), nameof(Chest.PresentItem), MethodType.Enumerator)]
    private class ChestOpenPatch // REFACTOR: this patch is setup twice due to BadItemOffsetsFromChestHotfix
    {
        [HarmonyILManipulator]
        private static void OnSpewContentsOntoGroundIL(ILContext il, MethodBase original)
        {
            ILCursor cursor = new ILCursor(il);
            Type ot = original.DeclaringType;
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld(ot, ot.GetEnumeratorFieldName("displayTime"))))
                return; // play our sound right before we begin the item display countdown
            cursor.CallPrivate(typeof(Glockarina), nameof(OnChestOpen));
        }
    }

    private static void OnChestOpen()
    {
        if (_GlockarinaPickupID < 0)
            _GlockarinaPickupID = Lazy.PickupId<Glockarina>();
        if (Lazy.AnyoneHasGun(_GlockarinaPickupID))
            GameManager.Instance.gameObject.Play("zelda_chest_sound");
    }

    private void UpdateMode()
    {
        if (this.PlayerOwner is not PlayerController pc)
            return;
        if (this._mode == Mode.STORM)
            pc.healthHaver.damageTypeModifiers.AddUnique(this._electricImmunity);
        else
            pc.healthHaver.damageTypeModifiers.TryRemove(this._electricImmunity);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        this._electricImmunity ??= new DamageTypeModifier {
            damageType = CoreDamageTypes.Electric,
            damageMultiplier = 0f,
        };
        base.OnPlayerPickup(player);
        UpdateMode();
        UpdateAmmo();
        GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewLevelFullyLoaded;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewLevelFullyLoaded;
        base.OnDroppedByPlayer(player);
        player.healthHaver.damageTypeModifiers.TryRemove(this._electricImmunity);
    }

    public override void OnDestroy()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewLevelFullyLoaded;
        if (this.PlayerOwner)
            this.PlayerOwner.healthHaver.damageTypeModifiers.TryRemove(this._electricImmunity);
        base.OnDestroy();
    }

    private void OnNewLevelFullyLoaded()
    {
        this._didHealThisFloor = false;
    }

    private void UpdateAmmo()
    {
        this.gun.LocalInfiniteAmmo = this.Mastered;
        this.gun.DefaultModule.ammoCost = this.Mastered ? 0 : 1;
    }

    public override void OnMasteryStatusChanged()
    {
        UpdateAmmo();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        UpdateAmmo();
    }

    // Returns true if we handled a special song, false if we pass it along
    private bool HandleSpecialSong(Mode song)
    {
        if (this.PlayerOwner is not PlayerController player)
            return false;

        switch (song)
        {
            case Mode.DOUBLE:
                if (this.gun.CurrentAmmo < 0.35f * this.gun.AdjustedMaxAmmo)
                    return false; // can't nuke enemies under 1/3 ammo
                if (player.CurrentRoom == null || player.CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                    return false; // can't insta-clear boss rooms
                List<AIActor> activeEnemies = player.CurrentRoom.SafeGetEnemiesInRoom();
                player.CurrentRoom.ClearReinforcementLayers();
                for (int i = activeEnemies.Count - 1; i >= 0; --i)
                {
                    AIActor otherEnemy = activeEnemies[i];
                    if (otherEnemy.IsHostileAndNotABoss(canBeNeutral: true))
                        otherEnemy.healthHaver.ApplyDamage(10000000f, Vector2.zero, "Double Time",
                            CoreDamageTypes.Void, DamageCategory.Unstoppable, true);
                }
                this.gun.LoseAmmo(Mathf.CeilToInt(0.35f * this.gun.AdjustedMaxAmmo));
                return true;

            case Mode.SUN:
                if (player.CurrentRoom != null && player.CurrentRoom.IsDarkAndTerrifying)
                {
                    player.CurrentRoom.EndTerrifyingDarkRoom();
                    return true;
                }
                return false;

            case Mode.PRELUDE:
                if (player.CurrentRoom == null || !player.CurrentRoom.CanTeleportFromRoom())
                    return false;
                foreach (RoomHandler room in GameManager.Instance.Dungeon.data.rooms)
                {
                    if (room.area.PrototypeRoomCategory != PrototypeDungeonRoom.RoomCategory.SPECIAL)
                        continue;
                    if (room.area.PrototypeRoomSpecialSubcategory != PrototypeDungeonRoom.RoomSpecialSubCategory.STANDARD_SHOP)
                        continue;
                    player.AttemptTeleportToRoom(room, force: true, noFX: false);
                    return true;
                }
                return false;

            case Mode.HEALING:
                if (this._didHealThisFloor)
                    return false;
                LootEngine.SpawnHealth(player.CenterPosition, 1, null);
                this._didHealThisFloor = true;
                return true;

            case Mode.WHAT:
                base.gameObject.Play("sans_laugh");
                return false;
        }
        return false;
    }

    public override void OnActualReload(PlayerController player, Gun gun, bool manual)
    {
        base.OnActualReload(player, gun, manual);
        if (gun.ClipShotsRemaining > 0 || !this.Mastered)
            return;
        this.gun.StartCoroutine(DoRadialMusic());
    }

    private IEnumerator DoRadialMusic()
    {
        const int _NUM_RINGS             = 3;
        const int _RING_SIZE             = 8;
        const float _GAP                 = 360f / _RING_SIZE;
        const float _DELAY_BETWEEN_RINGS = 0.3f;

        for (int i = 0; i < _NUM_RINGS; ++i)
        {
            if (!this || !this.gun || !this.PlayerOwner)
                yield break;
            float offset = UnityEngine.Random.value * _GAP;
            for (int j = 0; j < _RING_SIZE; ++j)
            {
                Projectile proj = SpawnManager.SpawnProjectile(
                    prefab   : _Projectile.gameObject,
                    position : this.gun.barrelOffset.position,
                    rotation : (j * _GAP + offset).EulerZ()).GetComponent<Projectile>();
                proj.SetOwnerAndStats(this.PlayerOwner);
                this.PostProcessProjectile(proj);
                proj.SetSpeed(proj.baseData.speed / 2f);
            }
            yield return new WaitForSeconds(_DELAY_BETWEEN_RINGS);
        }
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        // Get a note based on the direction the player is aiming
        bool onKeyboard = player.IsKeyboardAndMouse();
        Vector2 aimVec = onKeyboard ? (player.unadjustedAimPoint.XY() - player.CenterPosition) : player.m_activeActions.Aim.Vector;
        Note note = Note.A;
        if (aimVec.sqrMagnitude > (onKeyboard ? _MOUSE_DEAD_ZONE_SQR : _CONTROLLER_DEAD_ZONE_SQR))
        {
            float aimAngle = aimVec.ToAngle().Clamp180();
            if (Mathf.Abs(aimAngle) < 45)
                note = Note.RIGHT;
            else if (Mathf.Abs(aimAngle) > 135)
                note = Note.LEFT;
            else
                note = (aimAngle > 0) ? Note.UP : Note.DOWN;
        }

        // Stop all note sounds and play the correct sound pertaining to the note
        base.gameObject.Play("ocarina_note_up_stop_all");
        base.gameObject.Play("ocarina_note_left_stop_all");
        base.gameObject.Play("ocarina_note_right_stop_all");
        base.gameObject.Play("ocarina_note_down_stop_all");
        base.gameObject.Play("ocarina_note_neutral_stop_all");
        switch(note)
        {
            case Note.UP:    base.gameObject.Play("ocarina_note_up");      break;
            case Note.LEFT:  base.gameObject.Play("ocarina_note_left");    break;
            case Note.RIGHT: base.gameObject.Play("ocarina_note_right");   break;
            case Note.DOWN:  base.gameObject.Play("ocarina_note_down");    break;
            case Note.A:     base.gameObject.Play("ocarina_note_neutral"); break;
        }

        CwaffVFX.Spawn(_NoteVFXPrefab, position: player.sprite.WorldTopCenter, velocity: UnityEngine.Random.Range(40f,50f).ToVector(4f),
            lifetime: 0.65f, fadeOutTime: 0.4f, specificFrame: (int)note);

        // Trim our list of notes played if necessary
        if (this._lastNotes.Count >= 8)
            this._lastNotes.RemoveAt(0);

        // Figure out if we've played a valid song
        this._lastNotes.Add(note);
        Mode? finishedSong = null;
        for (int s = 1; s < _Songs.Count; ++s)
        {
            List<Note> song = _Songs[s];

            int songPos = song.Count - 1;
            for (int i = this._lastNotes.Count - 1; i >= 0; --i)
            {
                if (this._lastNotes[i] != song[songPos])
                    break;
                if ((--songPos) < 0)
                {
                    finishedSong = (Mode)s;
                    break;
                }
            }
        }

        if (!finishedSong.HasValue)
            return;
        if (finishedSong.Value == this._mode)
            return;

        if (finishedSong.Value >= Mode.DOUBLE)
        {
            if (HandleSpecialSong(finishedSong.Value))
            {
                base.gameObject.Play("ocarina_song_success");
                this._lastNotes.Clear();
            }
            return;
        }

        this._mode = finishedSong.Value;
        UpdateMode();

        for (int i = 0; i < 6; ++i) //TODO: use SpawnBurst
            CwaffVFX.Spawn(prefab: _NoteVFXPrefab, position: player.sprite.WorldTopCenter, randomFrame: true,
                velocity: UnityEngine.Random.Range(45f + 15f * i, 45f + 15f * (i + 1)).ToVector(4f), lifetime: 0.65f, fadeOutTime: 0.4f);
        base.gameObject.Play("ocarina_song_success");
        this._lastNotes.Clear();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        switch(this._mode)
        {
            case Mode.STORM:
                projectile.damageTypes        |= CoreDamageTypes.Electric;
                GoopModifier goopmod           = projectile.gameObject.AddComponent<GoopModifier>();
                goopmod.SpawnGoopOnCollision   = true;
                goopmod.CollisionSpawnRadius   = 1f;
                goopmod.SpawnGoopInFlight      = true;
                goopmod.InFlightSpawnRadius    = 0.4f;
                goopmod.InFlightSpawnFrequency = C.FRAME;
                goopmod.goopDefinition         = EasyGoopDefinitions.WaterGoop;
                break;
            case Mode.TIME:
                projectile.gameObject.AddComponent<SlowNearbyBullets>();
                break;
            case Mode.SARIA:
                HomingModifier home         = projectile.gameObject.AddComponent<HomingModifier>();
                home.HomingRadius           = 10f;
                home.AngularVelocity        = 720f;
                projectile.baseData.damage *= 1.25f;
                break;
            case Mode.EMPTY:
                projectile.gameObject.AddComponent<CreateDecoyOnKill>();
                break;
            case Mode.BOLERO:
                projectile.damageTypes        |= CoreDamageTypes.Fire;
                GoopModifier firemod           = projectile.gameObject.AddComponent<GoopModifier>();
                firemod.SpawnGoopOnCollision   = true;
                firemod.CollisionSpawnRadius   = 1f;
                firemod.SpawnGoopInFlight      = true;
                firemod.InFlightSpawnRadius    = 0.4f;
                firemod.InFlightSpawnFrequency = C.FRAME;
                firemod.goopDefinition         = EasyGoopDefinitions.FireDef;
                break;
            case Mode.REQUIEM:
                PierceProjModifier pierce = projectile.gameObject.AddComponent<PierceProjModifier>();
                pierce.penetration = 999;
                pierce.penetratesBreakables = true;
                break;
        }
    }

    private static float _LastReloadNoteSpriteTime = 0.0f;
    public override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsReloading)
            return;
        if ((BraveTime.ScaledTimeSinceStartup - _LastReloadNoteSpriteTime) < 0.1f)
            return;
        if (!this.PlayerOwner)
            return;
        int frame = this.gun.spriteAnimator.CurrentFrame;
        if (frame < 4 || frame > 12)
            return; // don't play notes from the ocarina unless it's near our character's face
        _LastReloadNoteSpriteTime = BraveTime.ScaledTimeSinceStartup;
        CwaffVFX.Spawn(_NoteVFXPrefab, position: this.PlayerOwner.sprite.WorldTopCenter, randomFrame: true,
            velocity: UnityEngine.Random.Range(45f,135f).ToVector(4f), lifetime: 0.65f, fadeOutTime: 0.4f);
    }

    private void LateUpdate()
    {
        if (this.gun.spriteAnimator.currentClip.name == this.gun.reloadAnimation)
            this.gun.RenderInFrontOfPlayer();
    }
}

public class GlockarinaAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private Glockarina _glock;
    private PlayerController _owner;
    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._glock = this._gun.GetComponent<Glockarina>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        string uiString = null;
        switch(this._glock._mode)
        {
            case Glockarina.Mode.STORM:   uiString = "glockarina_storm_ui_icon";  break;
            case Glockarina.Mode.TIME:    uiString = "glockarina_time_ui_icon";   break;
            case Glockarina.Mode.SARIA:   uiString = "glockarina_saria_ui_icon";  break;
            case Glockarina.Mode.EMPTY:   uiString = "glockarina_empty_ui_icon";  break;
            case Glockarina.Mode.BOLERO:  uiString = "glockarina_fire_ui_icon";   break;
            case Glockarina.Mode.REQUIEM: uiString = "glockarina_spirit_ui_icon"; break;
            default:
                uic.GunAmmoCountLabel.Text = this._owner.VanillaAmmoDisplay();
                return true;
        }

        uic.GunAmmoCountLabel.Text = $"[sprite \"{uiString}\"]\n{this._owner.VanillaAmmoDisplay()}";
        return true;
    }
}

public class SlowNearbyBullets : MonoBehaviour
{
    private class SlowedByGlockarina : MonoBehaviour {}

    private const float _REACH_SQR         = 4f;
    private const float _BULLET_TIME_SCALE = 0.5f;

    private Projectile _projectile;
    private PlayerController _owner;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
    }

    private void Update()
    {
        foreach (Projectile p in StaticReferenceManager.AllProjectiles)
        {
            if (!p.isActiveAndEnabled)
                continue;
            if (p.Owner is PlayerController player)
                continue;
            if (p.GetComponent<SlowedByGlockarina>())
                continue;
            float sqrDistance = (p.transform.position - this._projectile.transform.position).sqrMagnitude;
            if (sqrDistance > _REACH_SQR)
                continue;

            if (p.GetComponent<BulletScriptBehavior>() is BulletScriptBehavior bsb && bsb.bullet is Bullet bullet)
                bullet.Speed *= _BULLET_TIME_SCALE;
            else
                p.MultiplySpeed(_BULLET_TIME_SCALE);
            p.AddComponent<SlowedByGlockarina>();
            p.transform.DoMovingDistortionWave(distortionIntensity: 1.5f, distortionRadius: 0.25f, maxRadius: 0.25f, duration: 0.15f);
        }
    }
}

public class CreateDecoyOnKill : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.OnWillKillEnemy += OnWillKillEnemy;
    }

    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
    {
        Vector3 pos = enemy.UnitCenter.ToVector3ZUp();
        GameObject decoy = Glockarina._DecoyPrefab.Instantiate(position: pos, anchor: Anchor.LowerCenter);
        if (decoy.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
            body.RegisterGhostCollisionException(this._owner.specRigidbody);
        Lazy.DoSmokeAt(pos);
    }
}
