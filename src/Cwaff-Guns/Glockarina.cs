namespace CwaffingTheGungy;


/* TODO:
    - gun animations
    - mode sprites in item box
    - better shooting sound
    - fading note sprites when playing the ocarina
    - electric immunity in storm mode
    - secret song implementations
*/

public class Glockarina : AdvancedGunBehavior
{
    public static string ItemName         = "Glockarina";
    public static string SpriteName       = "glockarina";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "ShOOT 'em Up";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _DEAD_ZONE_SQR = 4f;
    private const float _DECOY_LIFE    = 2f;

    private enum Mode {
        DEFAULT,  // no special effects
        STORM,    // lightning shoots from notes when close to enemies
        TIME,     // slows down enemy bullets close to notes
        SARIA,    // homes in on nearby enemies
        EMPTY,    // killed enemies become decoys
        // DOUBLE,   // not a real mode, but song should clear room for 1/3 of max ammo
        // SUN,      // not a real mode, but song should clear darkness effects
        // PRELUDE,  // not a real mode, but song should warp player to shop
    }

    private enum Note {
        UP,
        LEFT,
        RIGHT,
        DOWN,
        A,
    }

    internal static GameObject _DecoyPrefab = null;
    private static List<List<Note>> _Songs = new(){
        /* DEFAULT */ null,
        /* STORM   */ new(){Note.A, Note.DOWN, Note.UP, Note.A, Note.DOWN, Note.UP},
        /* TIME    */ new(){Note.RIGHT, Note.A, Note.DOWN, Note.RIGHT, Note.A, Note.DOWN},
        /* SARIA   */ new(){Note.DOWN, Note.RIGHT, Note.LEFT, Note.DOWN, Note.RIGHT, Note.LEFT},
        /* EMPTY   */ new(){Note.RIGHT, Note.LEFT, Note.RIGHT, Note.DOWN, Note.RIGHT, Note.UP, Note.LEFT},
        // /* DOUBLE  */ new(){Note.RIGHT, Note.RIGHT, Note.A, Note.A, Note.DOWN, Note.DOWN},
        // /* SUN     */ new(){Note.RIGHT, Note.DOWN, Note.UP, Note.RIGHT, Note.DOWN, Note.UP},
        // /* PRELUDE */ new(){Note.UP, Note.RIGHT, Note.UP, Note.RIGHT, Note.LEFT, Note.UP},
    };

    private Mode _mode = Mode.DEFAULT;
    private List<Note> _lastNotes = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Glockarina>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.2f, ammo: 80, canReloadNoMatterAmmo: true);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("blowgun_fire_sound");
            gun.SetReloadAudio("blowgun_reload_sound");

        gun.InitProjectile(new(clipSize: 12, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, speed: 35f,
          sprite: "glockarina_projectile", fps: 12, anchor: Anchor.MiddleLeft, shouldRotate: false));

        _DecoyPrefab = ItemHelper.Get(Items.Decoy).GetComponent<SpawnObjectPlayerItem>().objectToSpawn.ClonePrefab();
        Decoy decoy = _DecoyPrefab.GetComponent<Decoy>();
            decoy.DeathExplosionTimer = _DECOY_LIFE;
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

        // Trim our list of notes played
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

        this._mode = finishedSong.Value;
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
                goopmod.InFlightSpawnFrequency = 0.01f;
                goopmod.goopDefinition         = EasyGoopDefinitions.WaterGoop;
                break;
            case Mode.TIME:
                projectile.gameObject.AddComponent<SlowNearbyBullets>();
                break;
            case Mode.SARIA:
                HomingModifier home         = projectile.gameObject.AddComponent<HomingModifier>();
                home.HomingRadius           = 10f;
                home.AngularVelocity        = 720f;
                projectile.baseData.damage *= 2f;
                break;
            case Mode.EMPTY:
                projectile.gameObject.AddComponent<CreateDecoyOnKill>();
                break;
        }
    }
}

public class SlowNearbyBullets : MonoBehaviour
{
    private class SlowedByGlockarina : MonoBehaviour {}

    private const float REACH_SQR = 16f;

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
            if (sqrDistance > REACH_SQR)
                continue;

            p.AddComponent<SlowedByGlockarina>();
            ETGModConsole.Log($"slowing projectile {p.GetHashCode()}!");
            p.baseData.speed *= 0.33f;
            p.UpdateSpeed();
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
