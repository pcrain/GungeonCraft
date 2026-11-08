namespace CwaffingTheGungy;

public class SteelDrum : CwaffGun
{
    public static string ItemName         = "Steel Drum";
    public static string ShortDescription = "The Beatdown Goes On";
    public static string LongDescription  = "Fires melodic notes forward and percussive notes in random directions. More notes are fired the longer the drum plays uninterrupted.";
    public static string Lore             = "When morale is low on the battlefield, good music is a surefire way to lift everyone's spirits. Unfortunately, instruments are scarce in combat, and the average solider doesn't exactly have the voice of an angel. Gungeoneers thus typically resort to drum magazines for their musical needs, which works out pretty well for everyone...except the shmuck who ends up getting stranded without a drum clip in the heat of battle.";

    private static readonly List<int> _ActiveParts = [
      1         + 8,
          2     + 8 + 16,
      1 + 2     + 8 + 16,
              4 + 8 + 16 + 32,
      1 + 2 + 4 + 8 + 16 + 32,
      ];

    private float _songTime          = 0.0f;
    private List<int> _songPos       = [0, 0, 0, 0, 0, 0];
    private int _loop                = 0;
    private bool _left               = false;
    private bool _cachedInfiniteAmmo = false;

    public static void Init()
    {
        Lazy.SetupGun<SteelDrum>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 2018, shootFps: 30, reloadFps: 12,/* preventRotation: true,*/
            dynamicBarrelOffsets: true, suppressFireAnim: true, suppressReloadAnim: true, doesScreenShake: false, lowersAudioWhileFiring: true,
            carryOffset: new IntVector2(4, -2), attacksThroughWalls: true, manuallyHandleCooldown: true)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(baseProjectile: Items.FaceMelter.Projectile(), clipSize: -1, cooldown: 0.0f, shootStyle: ShootStyle.Automatic,
            damage: 6.0f, speed: 30f, range: 9999f, force: 12f, angleVariance: 16.0f, barrageSize: _TheSong.Count))
          .Assign(out Projectile proj);
        for (int i = 1; i <= 5; ++i)
        {
          gun.QuickUpdateGunAnimation($"{i}_left", fps: 5, returnToIdle: true);
          gun.QuickUpdateGunAnimation($"{i}_right", fps: 5, returnToIdle: true);
        }
        for (int i = 0; i < _TheSong.Count; ++i)
        {
          ProjectileModule mod = gun.Volley.projectiles[i];
          if (i <= 3)
          {
            mod.projectiles[0] = proj.Clone(GunData.New(
              sprite: $"steel_drum_{(i+1)}_proj", glowSensitivity: 2.0f, glowColor: Color.white, glowAmount: 10.0f, glowColorPower: 5.0f, useBetterEmissiveShader: true,
              speed: (i < 3) ? 30.0f : 16.0f));
            if (i == 3)
            {
              mod.projectiles[0] = mod.projectiles[0].ConvertToSpecialtyType<DrumProjectile>();
              mod.projectiles[0].collidesWithProjectiles = true;
            }
          }
          else // everything after 3 uses drum projectiles
            mod.projectiles[0] = gun.Volley.projectiles[3].projectiles[0];
          if (i >= 3)
          {
            mod.angleVariance = 360.0f; // drum projectiles can go in any direction
            mod.ammoCost = 0; // drum projectiles don't cost ammo
          }
        }
    }

    public override void OnMasteryStatusChanged()
    {
      base.OnMasteryStatusChanged();
      this.canAttackWhileRolling = this.Mastered;
    }

    public override void OnSwitchedToThisGun()
    {
      base.OnSwitchedToThisGun();
      this.canAttackWhileRolling = this.Mastered;
    }

    public class DrumProjectile : Projectile
    {
      private bool _mastered = false;

      public void SetMastered()
      {
        this._mastered = true;
      }

      public override void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
      {
        base.OnPreCollision(myRigidbody, myCollider, otherRigidbody, otherCollider);
        if (otherRigidbody.projectile is Projectile p && p.isActiveAndEnabled && (!this._mastered || p.m_owner is PlayerController))
          PhysicsEngine.SkipCollision = true;
      }
    }

    private bool CanPlayNextNote(int part)
    {
      if ((_ActiveParts[(int)Mathf.Min(this._loop / 2, _ActiveParts.Count - 1)] & (1 << part)) == 0)
        return false;
      return this._songTime >= _TheSong[part][this._songPos[part]].time;
    }

    private void PlayNextNote()
    {
      bool nonDrumNotePlayed = false;
      for (int i = _TheSong.Count - 1; i >= 0; --i)
      {
        if (!CanPlayNextNote(i))
          continue;
        if (_TheSong[i][this._songPos[i]].note < 0)
        {
          ResetSong(incrementLoop: true);
          if (!CanPlayNextNote(i))
            continue;
        }
        if (i >= 3)
          base.gameObject.Play(_DrumNotes[_TheSong[i][this._songPos[i]].note]);
        else
        {
          base.gameObject.Play(_TheNotes[_TheSong[i][this._songPos[i]].note]);
          nonDrumNotePlayed = true;
        }
        ++(this._songPos[i]);
      }
      if (nonDrumNotePlayed)
      {
        gun.spriteAnimator.StopAndResetFrameToDefault();
        int frame = UnityEngine.Random.Range(1, 6);
        if (this._left)
          gun.spriteAnimator.Play($"steel_drum_{frame}_left");
        else
          gun.spriteAnimator.Play($"steel_drum_{frame}_right");
        this._left = !this._left;
      }
    }

    private void ResetSong(bool resetCooldowns = false, bool incrementLoop = false)
    {
      this._songTime = 0.0f;
      for (int i = 0; i < _TheSong.Count; ++i)
        this._songPos[i] = 0;
      if (resetCooldowns)
      {
        foreach (ProjectileModule mod in this.gun.Volley.projectiles)
        {
          int index = mod.TrueSourceIndex(this.gun);
          this.gun.m_moduleData[mod].onCooldown = (index > 0); // TODO: figure out how to disable infinite spam clicking
        }
      }
      if (incrementLoop)
        ++this._loop;
      else
        this._loop = 0;
    }

    private void UpdateSong()
    {
      this._songTime += Time.unscaledDeltaTime * this.PlayerOwner.FireRateMult();
      foreach (ProjectileModule mod in this.gun.Volley.projectiles)
      {
        int index = mod.TrueSourceIndex(this.gun);
        if (index < 0 || index >= _TheSong.Count)
          continue;
        this.gun.m_moduleData[mod].onCooldown = !CanPlayNextNote(part: index);
      }
    }

    private void UpdateAmmo(PlayerController pc)
    {
      bool infiniteAmmo = pc && pc.HasSynergy(Synergy.BEAT_OF_YOUR_OWN_DRUMS);
      if (infiniteAmmo == this._cachedInfiniteAmmo)
        return;
      this._cachedInfiniteAmmo = infiniteAmmo;
      this.gun.LocalInfiniteAmmo = infiniteAmmo;
      foreach (ProjectileModule mod in this.gun.Volley.projectiles)
        if (mod.TrueSourceIndex(this.gun) < 3)
          mod.ammoCost = infiniteAmmo ? 0 : 1;
    }

    public override void Update()
    {
      base.Update();
      if (this.gun.m_moduleData == null)
        return;
      if (this.PlayerOwner is not PlayerController pc)
        return;
      UpdateAmmo(pc);
      if (this.gun.IsFiring && pc.m_CanAttack)
        UpdateSong();
      else
        ResetSong(resetCooldowns: true);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
      base.OnPostFired(player, gun);
      PlayNextNote();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
      base.PostProcessProjectile(projectile);
      if (this.Mastered && projectile.gameObject.GetComponent<DrumProjectile>() is DrumProjectile dp)
        dp.SetMastered();
    }

    private struct DrumNote
    {
      public float time;
      public int note;

      public DrumNote(float time, int note)
      {
        this.time = time;
        this.note = note;
      }
    }

    private static readonly List<string> _TheNotes = [
      "steel_drum_note_0_c4",
      "steel_drum_note_1_c4s",
      "steel_drum_note_2_d4",
      "steel_drum_note_3_d4s",
      "steel_drum_note_4_e4",
      "steel_drum_note_5_f4",
      "steel_drum_note_6_f4s",
      "steel_drum_note_7_g4",
      "steel_drum_note_8_g4s",
      "steel_drum_note_9_a4",
      "steel_drum_note_10_a4s",
      "steel_drum_note_11_b4",
      "steel_drum_note_12_c5",
      "steel_drum_note_13_c5s",
      "steel_drum_note_14_d5",
      "steel_drum_note_15_d5s",
      "steel_drum_note_16_e5",
      "steel_drum_note_17_f5",
      "steel_drum_note_18_f5s",
      "steel_drum_note_19_g5",
      "steel_drum_note_20_g5s",
      "steel_drum_note_21_a5",
      "steel_drum_note_22_a5s",
      "steel_drum_note_23_b5",
      "steel_drum_note_24_c6",
      "steel_drum_note_25_c6s",
      "steel_drum_note_26_d6",
    ];

    private static readonly List<string> _DrumNotes = [
      "sd_maracas",
      "sd_castanets",
      "sd_claves",
      "sd_bongo_hi",
      "sd_conga_hi",
    ];

    private static readonly List<DrumNote> _TheSongPart1 = [
      new(0.0f, 22 - 12),
      new(0.4651159999999999f, 22 - 12),
      new(0.8134685041666665f, 21 - 12),
      new(1.1623055041666666f, 21 - 12),
      new(1.3953479999999998f, 21 - 12),
      new(1.8599795041666665f, 21 - 12),
      new(2.3250955041666663f, 21 - 12),
      new(2.6739325041666664f, 22 - 12),
      new(3.022769504166666f, 22 - 12),
      new(3.255327504166666f, 22 - 12),
      new(3.4883699999999993f, 21 - 12),
      new(3.7209279999999989f, -1),
    ];

    private static readonly List<DrumNote> _TheSongPart2 = [
      new(0.0f, 14),
      new(0.23304249583333325f, 22),
      new(0.4651160000000001f, 19),
      new(0.6981584958333329f, 19),
      new(0.8139529999999997f, 14),
      new(1.0465109999999997f, 14),
      new(1.163274495833333f, 21),
      new(1.3953479999999994f, 17),
      new(1.6279059999999994f, 17),
      new(1.7446694958333326f, 12),
      new(1.976743f, 12),
      new(2.0930219999999995f, 21),
      new(2.3255799999999995f, 17),
      new(2.5581379999999996f, 17),
      new(2.674901495833333f, 12),
      new(2.906974999999999f, 12),
      new(3.1395329999999992f, 16),
      new(3.3720909999999993f, 16),
      new(3.4883699999999997f, 17),
      new(3.6046489999999993f, 16),
      new(3.7209279999999989f, -1),
    ];

    private static readonly List<DrumNote> _TheSongPart3 = [
      new(0.0f, 14 - 12),
      new(0.11579450416666681f, 19 - 12),
      new(0.23207350416666728f, 22 - 12),
      new(0.34835250416666685f, 14 - 12),
      new(0.4651160000000001f, 19 - 12),
      new(0.580910504166666f, 22 - 12),
      new(0.6971895041666665f, 14 - 12),
      new(0.8134685041666669f, 22 - 12),
      new(0.9297475041666674f, 12 - 12),
      new(1.0465109999999997f, 17 - 12),
      new(1.1623055041666666f, 21 - 12),
      new(1.278584504166667f, 24 - 12),
      new(1.3948635041666675f, 22 - 12),
      new(1.5116269999999998f, 21 - 12),
      new(1.6274215041666666f, 19 - 12),
      new(1.743700504166667f, 14 - 12),
      new(1.8599795041666658f, 17 - 12),
      new(1.976743f, 21 - 12),
      new(2.0925375041666667f, 24 - 12),
      new(2.208816504166667f, 17 - 12),
      new(2.325095504166666f, 24 - 12),
      new(2.441859f, 26 - 12),
      new(2.5581380000000005f, 22 - 12),
      new(2.6739325041666673f, 21 - 12),
      new(2.790211504166666f, 22 - 12),
      new(2.9064905041666664f, 21 - 12),
      new(3.0232540000000006f, 22 - 12),
      new(3.1390485041666656f, 24 - 12),
      new(3.255327504166666f, 22 - 12),
      new(3.3716065041666665f, 21 - 12),
      new(3.488369999999999f, 19 - 12),
      new(3.6041645041666657f, 17 - 12),
      new(3.7209279999999989f, -1),
    ];

    private static readonly List<DrumNote> _Drumline = [
      new(0.0f                 , 1),
      new(0.11579450416666681f , 0),
      new(0.23207350416666728f , 0),
      new(0.34835250416666685f , 1),
      new(0.4651160000000001f  , 0),
      new(0.580910504166666f   , 0),
      new(0.6971895041666665f  , 1),
      new(0.8134685041666669f  , 0),
      new(0.9297475041666674f  , 1),
      new(1.0465109999999997f  , 0),
      new(1.1623055041666666f  , 0),
      new(1.278584504166667f   , 1),
      new(1.3948635041666675f  , 0),
      new(1.5116269999999998f  , 0),
      new(1.6274215041666666f  , 1),
      new(1.743700504166667f   , 0),
      new(1.8599795041666658f  , 1),
      new(1.976743f            , 0),
      new(2.0925375041666667f  , 0),
      new(2.208816504166667f   , 1),
      new(2.325095504166666f   , 0),
      new(2.441859f            , 0),
      new(2.5581380000000005f  , 1),
      new(2.6739325041666673f  , 0),
      new(2.790211504166666f   , 1),
      new(2.9064905041666664f  , 0),
      new(3.0232540000000006f  , 0),
      new(3.1390485041666656f  , 1),
      new(3.255327504166666f   , 0),
      new(3.3716065041666665f  , 0),
      new(3.488369999999999f   , 1),
      new(3.6041645041666657f  , 0),
      new(3.7209279999999989f  , -1),
    ];

    private static readonly List<DrumNote> _Drumline2 = [
      // new(0.0f                 , 1),
      // new(0.11579450416666681f , 0),
      new(0.23207350416666728f , 2),
      // new(0.34835250416666685f , 1),
      // new(0.4651160000000001f  , 0),
      // new(0.580910504166666f   , 0),
      new(0.6971895041666665f  , 2),
      // new(0.8134685041666669f  , 0),
      // new(0.9297475041666674f  , 1),
      // new(1.0465109999999997f  , 0),
      new(1.1623055041666666f  , 2),
      // new(1.278584504166667f   , 1),
      // new(1.3948635041666675f  , 0),
      // new(1.5116269999999998f  , 0),
      new(1.6274215041666666f  , 2),
      // new(1.743700504166667f   , 0),
      // new(1.8599795041666658f  , 1),
      // new(1.976743f            , 0),
      new(2.0925375041666667f  , 2),
      // new(2.208816504166667f   , 1),
      // new(2.325095504166666f   , 0),
      // new(2.441859f            , 0),
      new(2.5581380000000005f  , 2),
      // new(2.6739325041666673f  , 0),
      // new(2.790211504166666f   , 1),
      // new(2.9064905041666664f  , 0),
      new(3.0232540000000006f  , 2),
      // new(3.1390485041666656f  , 1),
      // new(3.255327504166666f   , 0),
      // new(3.3716065041666665f  , 0),
      new(3.488369999999999f   , 2),
      // new(3.6041645041666657f  , 0),
      new(3.7209279999999989f  , -1),
    ];

    private static readonly List<DrumNote> _Drumline3 = [
      new(0.0f                 , 3),
      // new(0.11579450416666681f , 0),
      // new(0.23207350416666728f , 0),
      // new(0.34835250416666685f , 1),
      new(0.4651160000000001f  , 4),
      // new(0.580910504166666f   , 0),
      // new(0.6971895041666665f  , 1),
      new(0.8134685041666669f  , 3),
      // new(0.9297475041666674f  , 1),
      // new(1.0465109999999997f  , 0),
      new(1.1623055041666666f  , 3),
      // new(1.278584504166667f   , 1),
      new(1.3948635041666675f  , 4),
      // new(1.5116269999999998f  , 0),
      // new(1.6274215041666666f  , 1),
      // new(1.743700504166667f   , 0),
      new(1.8599795041666658f  , 3),
      // new(1.976743f            , 0),
      // new(2.0925375041666667f  , 0),
      // new(2.208816504166667f   , 1),
      new(2.325095504166666f   , 4),
      // new(2.441859f            , 0),
      // new(2.5581380000000005f  , 1),
      new(2.6739325041666673f  , 3),
      // new(2.790211504166666f   , 1),
      // new(2.9064905041666664f  , 0),
      new(3.0232540000000006f  , 3),
      // new(3.1390485041666656f  , 1),
      new(3.255327504166666f   , 4),
      // new(3.3716065041666665f  , 0),
      // new(3.488369999999999f   , 1),
      // new(3.6041645041666657f  , 0),
      new(3.7209279999999989f  , -1),
    ];

    private static readonly List<List<DrumNote>> _TheSong = [
      _TheSongPart1,
      _TheSongPart2,
      _TheSongPart3,
      _Drumline,
      _Drumline2,
      _Drumline3,
    ];
}
