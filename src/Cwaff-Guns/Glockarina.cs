namespace CwaffingTheGungy;

public class Glockarina : AdvancedGunBehavior
{
    public static string ItemName         = "Glockarina";
    public static string SpriteName       = "glockarina";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "ShOOT 'em Up";
    public static string LongDescription  = " Fires musical notes as projectiles. Reloading with a full clip while aiming in a cardinal direction will play a note corresponding to that direction. Playing certain songs can change the properties of the projectiles or have other side effects.";
    public static string Lore             = "An unorthodox toy gun brought into the Gungeon by a teary-eyed child, who received it as a seasonal gift from 'one of Santa's elves'. Legend holds that the spirits of various phantoms have been masked inside this gun since ages long past, and that breathing wind through the gun while raising it skyward at twilight can awaken their diminished powers.";

    internal const string _StormSpriteUI = $"{C.MOD_PREFIX}:_GlockStormSpriteUI";
    internal const string _TimeSpriteUI  = $"{C.MOD_PREFIX}:_GlockTimeSpriteUI";
    internal const string _SariaSpriteUI = $"{C.MOD_PREFIX}:_GlockSariaSpriteUI";
    internal const string _EmptySpriteUI = $"{C.MOD_PREFIX}:_GlockEmptySpriteUI";

    private const float _DEAD_ZONE_SQR = 4f;
    private const float _DECOY_LIFE    = 2f;

    internal enum Mode {
        DEFAULT,  // no special effects
        STORM,    // lightning shoots from notes when close to enemies
        TIME,     // slows down enemy bullets close to notes
        SARIA,    // homes in on nearby enemies with slightly increased damage
        EMPTY,    // killed enemies become decoys

        DOUBLE,   // not a real mode, but song should clear room for 1/3 of max ammo
        SUN,      // not a real mode, but song should clear darkness effects
        PRELUDE,  // not a real mode, but song should warp player to shop
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
    private static ILHook _ChestOpenHookIL    = null;
    private static int _GlockarinaPickupID    = -1;
    private static List<List<Note>> _Songs = new(){
        /* DEFAULT */ null,
        /* STORM   */ new(){Note.A, Note.DOWN, Note.UP, Note.A, Note.DOWN, Note.UP},
        /* TIME    */ new(){Note.RIGHT, Note.A, Note.DOWN, Note.RIGHT, Note.A, Note.DOWN},
        /* SARIA   */ new(){Note.DOWN, Note.RIGHT, Note.LEFT, Note.DOWN, Note.RIGHT, Note.LEFT},
        /* EMPTY   */ new(){Note.RIGHT, Note.LEFT, Note.RIGHT, Note.DOWN, Note.RIGHT, Note.UP, Note.LEFT},
        /* DOUBLE  */ new(){Note.RIGHT, Note.RIGHT, Note.A, Note.A, Note.DOWN, Note.DOWN},
        /* SUN     */ new(){Note.RIGHT, Note.DOWN, Note.UP, Note.RIGHT, Note.DOWN, Note.UP},
        /* PRELUDE */ new(){Note.UP, Note.RIGHT, Note.UP, Note.RIGHT, Note.LEFT, Note.UP},
    };

    internal Mode _mode = Mode.DEFAULT;
    private List<Note> _lastNotes = new();
    private DamageTypeModifier _electricImmunity = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Glockarina>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 400, canReloadNoMatterAmmo: true);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetAnimationFPS(gun.reloadAnimation, 20);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("glockarina_shoot_sound");
            gun.SetReloadAudio("glockarina_reload_sound");

        gun.gameObject.AddComponent<GlockarinaAmmoDisplay>();

        gun.InitProjectile(new(clipSize: 12, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic, speed: 35f, damage: 4f, customClip: SpriteName,
          sprite: "glockarina_projectile", fps: 12, anchor: Anchor.MiddleLeft, shouldRotate: false));

        _DecoyPrefab = ItemHelper.Get(Items.Decoy).GetComponent<SpawnObjectPlayerItem>().objectToSpawn.ClonePrefab();
        Decoy decoy = _DecoyPrefab.GetComponent<Decoy>();
            decoy.DeathExplosionTimer = _DECOY_LIFE;

        _NoteVFXPrefab = VFX.Create("note_vfx", 0.01f, loops: false, anchor: Anchor.MiddleCenter); // FPS must be nonzero or sprites don't update properly

        _ChestOpenHookIL = new ILHook(  // dark magic to hook into ienumerator
            typeof(Chest).GetNestedType("<PresentItem>c__Iterator6", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext"),
            OnSpewContentsOntoGroundIL
            );

        _GlockarinaPickupID = gun.PickupObjectId;
    }

    private static void OnSpewContentsOntoGroundIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        Type iterType = typeof(Chest).GetNestedType("<PresentItem>c__Iterator6", BindingFlags.NonPublic | BindingFlags.Instance);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld(iterType.FullName, "<displayTime>__1")))
            return; // play our sound right before we begin the item display countdown
        cursor.Emit(OpCodes.Call, typeof(Glockarina).GetMethod("OnChestOpen", BindingFlags.Static | BindingFlags.NonPublic));
    }

    private static void OnChestOpen()
    {
        if (GameManager.Instance.AnyPlayerHasPickupID(_GlockarinaPickupID))
            AkSoundEngine.PostEvent("zelda_chest_sound", GameManager.Instance.gameObject);
    }

    private void UpdateMode()
    {
        if (this.Owner is not PlayerController pc)
            return;
        if (this._mode == Mode.STORM && !pc.healthHaver.damageTypeModifiers.Contains(this._electricImmunity))
            pc.healthHaver.damageTypeModifiers.Add(this._electricImmunity);
        if (this._mode != Mode.STORM && pc.healthHaver.damageTypeModifiers.Contains(this._electricImmunity))
            pc.healthHaver.damageTypeModifiers.Remove(this._electricImmunity);
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        if (!everPickedUpByPlayer)
            this._electricImmunity = new DamageTypeModifier {
                damageType = CoreDamageTypes.Electric,
                damageMultiplier = 0f,
            };
        base.OnPickedUpByPlayer(player);
        UpdateMode();
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);

        if (player.healthHaver.damageTypeModifiers.Contains(this._electricImmunity))
            player.healthHaver.damageTypeModifiers.Remove(this._electricImmunity);
    }

    // Returns true if we handled a special song, false if we pass it along
    private bool HandleSpecialSong(Mode song)
    {
        if (this.Owner is not PlayerController player)
            return false;

        switch (song)
        {
            case Mode.DOUBLE:
                if (this.gun.CurrentAmmo < 0.35f * this.gun.AdjustedMaxAmmo)
                    return false; // can't nuke enemies under 1/3 ammo
                if (player.CurrentRoom == null || player.CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                    return false; // can't insta-clear boss rooms
                List<AIActor> activeEnemies = player.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if ((activeEnemies?.Count ?? 0) == 0)
                    return false; // can't insta-clear rooms that are already clear
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
                if (!(player.CurrentRoom?.CanTeleportFromRoom() ?? false))
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

        }
        return false;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (gun.IsReloading || !manualReload || (gun.ClipShotsRemaining < gun.ClipCapacity))
            return;

        // Get a note based on the direction the player is aiming
        Vector2 aimVec = player.IsKeyboardAndMouse() ? (player.unadjustedAimPoint.XY() - player.CenterPosition) : player.m_activeActions.Aim.Vector;
        Note note = Note.A;
        if (aimVec.sqrMagnitude > _DEAD_ZONE_SQR)
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
        AkSoundEngine.PostEvent("ocarina_note_up_stop_all",      base.gameObject);
        AkSoundEngine.PostEvent("ocarina_note_left_stop_all",    base.gameObject);
        AkSoundEngine.PostEvent("ocarina_note_right_stop_all",   base.gameObject);
        AkSoundEngine.PostEvent("ocarina_note_down_stop_all",    base.gameObject);
        AkSoundEngine.PostEvent("ocarina_note_neutral_stop_all", base.gameObject);
        switch(note)
        {
            case Note.UP:    AkSoundEngine.PostEvent("ocarina_note_up",      base.gameObject); break;
            case Note.LEFT:  AkSoundEngine.PostEvent("ocarina_note_left",    base.gameObject); break;
            case Note.RIGHT: AkSoundEngine.PostEvent("ocarina_note_right",   base.gameObject); break;
            case Note.DOWN:  AkSoundEngine.PostEvent("ocarina_note_down",    base.gameObject); break;
            case Note.A:     AkSoundEngine.PostEvent("ocarina_note_neutral", base.gameObject); break;
        }

        FancyVFX fv = FancyVFX.Spawn(_NoteVFXPrefab, position: player.sprite.WorldTopCenter,
            velocity: UnityEngine.Random.Range(40f,50f).ToVector(4f), lifetime: 0.65f, fadeOutTime: 0.4f);
        fv.sprite.SetSprite(fv.GetComponent<tk2dSpriteAnimator>().currentClip.frames[(int)note].spriteId);

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
                AkSoundEngine.PostEvent("ocarina_song_success", base.gameObject);
                this._lastNotes.Clear();
            }
            return;
        }

        this._mode = finishedSong.Value;
        UpdateMode();

        for (int i = 0; i < 6; ++i)
        {
            FancyVFX fv2 = FancyVFX.Spawn(_NoteVFXPrefab, position: this.Owner.sprite.WorldTopCenter,
                velocity: UnityEngine.Random.Range(45f + 15f * i, 45f + 15f * (i + 1)).ToVector(4f), lifetime: 0.65f, fadeOutTime: 0.4f);
            fv2.sprite.SetSprite(fv2.GetComponent<tk2dSpriteAnimator>().currentClip.frames[UnityEngine.Random.Range(0,5)].spriteId);
        }
        AkSoundEngine.PostEvent("ocarina_song_success", base.gameObject);
        this._lastNotes.Clear();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
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
        }
    }

    private static float _LastReloadNoteSpriteTime = 0.0f;
    protected override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsReloading)
            return;
        if ((BraveTime.ScaledTimeSinceStartup - _LastReloadNoteSpriteTime) < 0.1f)
            return;
        int frame = this.gun.spriteAnimator.CurrentFrame;
        if (frame < 4 || frame > 12)
            return; // don't play notes from the ocarina unless it's near our character's face
        _LastReloadNoteSpriteTime = BraveTime.ScaledTimeSinceStartup;
        FancyVFX fv = FancyVFX.Spawn(_NoteVFXPrefab, position: this.Owner.sprite.WorldTopCenter,
            velocity: UnityEngine.Random.Range(45f,135f).ToVector(4f), lifetime: 0.65f, fadeOutTime: 0.4f);
        fv.sprite.SetSprite(fv.GetComponent<tk2dSpriteAnimator>().currentClip.frames[UnityEngine.Random.Range(0,5)].spriteId);
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

        uic.SetAmmoCountLabelColor(Color.white);
        Vector3 relVec = Vector3.zero;
        uic.GunAmmoCountLabel.AutoHeight = true; // enable multiline text
        uic.GunAmmoCountLabel.ProcessMarkup = true; // enable multicolor text

        string uiString = null;
        switch(this._glock._mode)
        {
            case Glockarina.Mode.STORM: uiString = Glockarina._StormSpriteUI; break;
            case Glockarina.Mode.TIME:  uiString = Glockarina._TimeSpriteUI;  break;
            case Glockarina.Mode.SARIA: uiString = Glockarina._SariaSpriteUI; break;
            case Glockarina.Mode.EMPTY: uiString = Glockarina._EmptySpriteUI; break;
            default:
                return false;
        }

        uic.GunAmmoCountLabel.Text = $"[sprite \"{uiString}\"]\n{this._gun.CurrentAmmo}/{this._gun.AdjustedMaxAmmo}";
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

            if (p.GetComponent<BulletScriptBehavior>()?.bullet is Bullet bullet)
                bullet.Speed *= _BULLET_TIME_SCALE;
            else
                p.baseData.speed *= _BULLET_TIME_SCALE;
            p.UpdateSpeed();
            p.AddComponent<SlowedByGlockarina>();
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
        GameObject gameObject = UnityEngine.Object.Instantiate(Glockarina._DecoyPrefab, enemy.UnitCenter, Quaternion.identity);
        tk2dBaseSprite sprite = gameObject.GetComponent<tk2dBaseSprite>();
        sprite.PlaceAtPositionByAnchor(enemy.UnitCenter.ToVector3ZUp(sprite.transform.position.z), tk2dBaseSprite.Anchor.MiddleCenter);
        sprite.specRigidbody?.RegisterGhostCollisionException(this._owner.specRigidbody);
        gameObject.transform.position = gameObject.transform.position.Quantize(0.0625f);
    }
}
