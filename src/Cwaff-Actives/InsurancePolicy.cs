namespace CwaffingTheGungy;

public class InsurancePolicy : PlayerItem
{
    public static string ItemName         = "Insurance Policy";
    public static string SpritePath       = "insurance_policy_robot_icon";
    public static string ShortDescription = "Kill Your Past, not Your Future";
    public static string LongDescription  = "Can be used when near a dropped item to insure it. Insured items will spawn in a special chest at the start of the next new run. Pseudo-restarts (such as those triggered by Clone) do not count as a new run for insurance purposes.";
    public static string Lore             = "You can never quite know what the Gungeon has in store for you. One moment you might be minding your own business fighting off a wave of Blobulons, the next moment you might find a bottomless pit has formed underneath your feet to consume you and all of your hard-earned loot. Regardless of the outcome of your journey into the Gungeon, a personalized insurance policy offers the peace of mind that you'll get a second chance to put your favorite item to poor use.";

    private const float _MAX_DIST = 5f;

    internal static Chest _InsuranceChestPrefab = null;
    internal static GameObject _InsuranceSparklePrefab = null;

    internal static int _InsuranceSpriteRobot;
    internal static int _InsuranceSpriteConvict;
    internal static int _InsuranceSpritePilot;
    internal static int _InsuranceSpriteParadox;
    internal static int _InsuranceSpriteGunslinger;
    internal static int _InsuranceSpriteHunter;
    internal static int _InsuranceSpriteMarine;
    internal static int _InsuranceSpriteBullet;

    internal static GameObject _InsuranceVFXRobot;
    internal static GameObject _InsuranceVFXConvict;
    internal static GameObject _InsuranceVFXPilot;
    internal static GameObject _InsuranceVFXParadox;
    internal static GameObject _InsuranceVFXGunslinger;
    internal static GameObject _InsuranceVFXHunter;
    internal static GameObject _InsuranceVFXMarine;
    internal static GameObject _InsuranceVFXBullet;

    internal static GameObject _InsuranceParticleVFX;
    internal static int _InsurancePickupId;

    private static List<int> _InsuredItems = new();
    private static string _InsuranceFile;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<InsurancePolicy>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        item.consumable   = true;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 0.5f);

        _InsuranceSpriteRobot      = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_robot_icon")[0],      item.sprite.Collection);
        _InsuranceSpriteConvict    = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_convict_icon")[0],    item.sprite.Collection);
        _InsuranceSpritePilot      = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_pilot_icon")[0],      item.sprite.Collection);
        _InsuranceSpriteParadox    = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_paradox_icon")[0],    item.sprite.Collection);
        _InsuranceSpriteGunslinger = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_gunslinger_icon")[0], item.sprite.Collection);
        _InsuranceSpriteHunter     = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_hunter_icon")[0],     item.sprite.Collection);
        _InsuranceSpriteMarine     = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_marine_icon")[0],     item.sprite.Collection);
        _InsuranceSpriteBullet     = SpriteBuilder.AddSpriteToCollection(ResMap.Get("insurance_policy_bullet_icon")[0],     item.sprite.Collection);

        _InsuranceVFXRobot      = VFX.Create("insurance_policy_robot_icon",      fps: 1, loops: true, anchor: Anchor.MiddleCenter);
        _InsuranceVFXConvict    = VFX.Create("insurance_policy_convict_icon",    fps: 1, loops: true, anchor: Anchor.MiddleCenter);
        _InsuranceVFXPilot      = VFX.Create("insurance_policy_pilot_icon",      fps: 1, loops: true, anchor: Anchor.MiddleCenter);
        _InsuranceVFXParadox    = VFX.Create("insurance_policy_paradox_icon",    fps: 1, loops: true, anchor: Anchor.MiddleCenter);
        _InsuranceVFXGunslinger = VFX.Create("insurance_policy_gunslinger_icon", fps: 1, loops: true, anchor: Anchor.MiddleCenter);
        _InsuranceVFXHunter     = VFX.Create("insurance_policy_hunter_icon",     fps: 1, loops: true, anchor: Anchor.MiddleCenter);
        _InsuranceVFXMarine     = VFX.Create("insurance_policy_marine_icon",     fps: 1, loops: true, anchor: Anchor.MiddleCenter);
        _InsuranceVFXBullet     = VFX.Create("insurance_policy_bullet_icon",     fps: 1, loops: true, anchor: Anchor.MiddleCenter);

        _InsuranceChestPrefab = GameManager.Instance.RewardManager.GetTargetChestPrefab(ItemQuality.B).gameObject.ClonePrefab().GetComponent<Chest>();
            _InsuranceChestPrefab.groundHitDelay = 0.10f;
            _InsuranceChestPrefab.groundHitDelay = 0.40f;
            _InsuranceChestPrefab.spawnAnimName = _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_appear", 11);
                _InsuranceChestPrefab.spriteAnimator.GetClipByName("insurance_chest_appear").frames[0].triggerEvent = true;
                _InsuranceChestPrefab.spriteAnimator.GetClipByName("insurance_chest_appear").frames[0].eventAudio = "Play_OBJ_smallchest_spawn_01";
            _InsuranceChestPrefab.openAnimName  = _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_open", 12);
            _InsuranceChestPrefab.breakAnimName = _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_break", 11);
            _InsuranceChestPrefab.sprite.SetUpAnimation("insurance_chest_idle", 11);
            _InsuranceChestPrefab.IsLocked = false; // can't get lock renderer to attach properly after adjusting appearance animation
            _InsuranceChestPrefab.GetComponent<MajorBreakable>().HitPoints = float.MaxValue; // insurance chest should be unbreakable

        _InsuranceFile     = Path.Combine(SaveManager.SavePath,"insurance.csv");
        _InsurancePickupId = item.PickupObjectId;

        _InsuranceParticleVFX = VFX.Create("midas_sparkle",
            fps: 8, loops: false, anchor: Anchor.MiddleCenter, emissivePower: 5);

        CwaffEvents.OnFirstFloorFullyLoaded += InsuranceCheck;
    }

    internal static int GetSpriteIdForCharacter()
    {
        switch(GameManager.Instance.PrimaryPlayer.characterIdentity)
        {
            case PlayableCharacters.Robot:      return _InsuranceSpriteRobot;
            case PlayableCharacters.Convict:    return _InsuranceSpriteConvict;
            case PlayableCharacters.Pilot:      return _InsuranceSpritePilot;
            case PlayableCharacters.Eevee:      return _InsuranceSpriteParadox;
            case PlayableCharacters.Gunslinger: return _InsuranceSpriteGunslinger;
            case PlayableCharacters.Guide:      return _InsuranceSpriteHunter;
            case PlayableCharacters.Soldier:    return _InsuranceSpriteMarine;
            case PlayableCharacters.Bullet:     return _InsuranceSpriteBullet;
        }
        return _InsuranceSpritePilot;
    }

    internal static GameObject GetVFXForCharacter()
    {
        switch(GameManager.Instance.PrimaryPlayer.characterIdentity)
        {
            case PlayableCharacters.Robot:      return _InsuranceVFXRobot;
            case PlayableCharacters.Convict:    return _InsuranceVFXConvict;
            case PlayableCharacters.Pilot:      return _InsuranceVFXPilot;
            case PlayableCharacters.Eevee:      return _InsuranceVFXParadox;
            case PlayableCharacters.Gunslinger: return _InsuranceVFXGunslinger;
            case PlayableCharacters.Guide:      return _InsuranceVFXHunter;
            case PlayableCharacters.Soldier:    return _InsuranceVFXMarine;
            case PlayableCharacters.Bullet:     return _InsuranceVFXBullet;
        }
        return _InsuranceVFXPilot;
    }

    public override void Start()
    {
        base.Start();
        base.sprite.SetSprite(GetSpriteIdForCharacter());
    }

    public static void InsuranceCheck()
    {
        GameManager.Instance.StartCoroutine(InsuranceCheck_CR());
    }

    public static IEnumerator InsuranceCheck_CR()
    {
        PlayerController p1 = GameManager.Instance.PrimaryPlayer;
        while (!p1.AcceptingAnyInput)
            yield return null; // wait for player to finish falling to the ground

        LoadInsuredItems();
        ClearInsuredItemsFile();
        if (_InsuredItems.Count() == 0)
            yield break;

        bool success;
        Chest chest = Chest.Spawn(_InsuranceChestPrefab, GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out success));
        chest.m_isMimic = false;
        chest.forceContentIds = new(_InsuredItems);
        _InsuredItems.Clear();
    }

    public override void DoEffect(PlayerController user)
    {
        this.numberOfUses += 1; // adjust in case we fail our usage
        PickupObject nearestPickup = null;
        float nearestDist = _MAX_DIST;
        foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
        {
            if (!debris.IsPickupObject)
                continue;
            if (debris.GetComponentInChildren<PickupObject>() is not PickupObject pickup)
                continue;
            if (pickup.IsBeingSold)
                continue;  // no stealing ):<
            if (pickup.GetComponent<Insured>() || _InsuredItems.Contains(pickup.PickupObjectId))
                continue;  // 2nd check needed to prevent duplicate insured items
            if (pickup.PickupObjectId == _InsurancePickupId)
                continue;  // can't insure insurance!!! (leads to weird graphical glitches)

            float pickupDist = (debris.sprite.WorldCenter - user.sprite.WorldCenter).magnitude;
            if (pickupDist >= nearestDist)
                continue;

            nearestPickup = pickup;
            nearestDist   = pickupDist;
        }
        if (!nearestPickup)
            return;
        this.numberOfUses -= 1; // we didn't fail, so actually use it now

        nearestPickup.gameObject.AddComponent<Insured>().DoSparkles();
        _InsuredItems.Add(nearestPickup.PickupObjectId);
        SaveInsuredItems();

        // Lazy.CustomNotification(nearestPickup.DisplayName,"Item Insured", nearestPickup.sprite,
        //     color: UINotificationController.NotificationColor.PURPLE);
        AkSoundEngine.PostEvent("the_sound_of_buying_insurance", base.gameObject);
    }

    internal static void SaveInsuredItems()
    {
        using (StreamWriter file = File.CreateText(_InsuranceFile))
        {
            bool first = true;
            foreach(int itemId in _InsuredItems)
            {
                file.Write($"{(first ? "" : ",")}{itemId}");
                first = false;
            }
        }
    }

    internal static void LoadInsuredItems()
    {
        _InsuredItems.Clear();
        if (!File.Exists(_InsuranceFile))
            return;
        try
        {
            string[] itemIds = File.ReadAllLines(_InsuranceFile)[0].Split(',');
            foreach(string itemId in itemIds)
                _InsuredItems.Add(Int32.Parse(itemId));
        }
        catch (Exception)
        {
            _InsuredItems.Clear(); // if there's any sort of parse error, give up immediately
        }
    }

    internal static void ClearInsuredItemsFile()
    {
        if (File.Exists(_InsuranceFile))
            File.Delete(_InsuranceFile);
    }
}

public class Insured : MonoBehaviour
{
    private PickupObject _pickup;
    private int _pickupId;
    private bool _doSparkles = false;

    // these need to be public because Guns create copies of themselves when picked up
    // since private fields don't serialize, they get reset, and the Start() method gets repeatedly called
    public bool dropped = false;
    public GameObject vfx = null;

    private void Start()
    {
        this._pickup = base.GetComponent<PickupObject>();
        this._pickupId = this._pickup.PickupObjectId;
        this.dropped = true;
        OnDrop();
        if (this._doSparkles)
            this._pickup.StartCoroutine(DoSparkles_CR());
    }

    private void LateUpdate()
    {
        if (this.dropped)
            UpdateVFX();

        bool dropped = !GameManager.Instance.AnyPlayerHasPickupID(this._pickupId);
        if (this.dropped == dropped)
            return; // cached state is the same

        if (dropped)
            OnDrop();
        else
            OnPickup();
        this.dropped = dropped;
    }

    private void UpdateVFX()
    {
        if (this.vfx == null)
            return;

        this.vfx.transform.position = this._pickup.sprite.WorldTopCenter + new Vector2(0f, 0.75f + 0.25f * Mathf.Sin(4f * BraveTime.ScaledTimeSinceStartup));
    }

    private void OnDrop()
    {
        if (this.vfx != null)
            UnityEngine.Object.Destroy(this.vfx);
        this.vfx = SpawnManager.SpawnVFX(InsurancePolicy.GetVFXForCharacter(), this._pickup.sprite.WorldTopCenter + new Vector2(0f, 0.5f), Quaternion.identity);
        this.vfx.transform.parent = this._pickup.gameObject.transform;
        this._pickup.StartCoroutine(SpinIntoExistence(this.vfx));
    }

    const float SPIN_TIME    = 3f;
    const float SPIN_AMOUNT  = 4.25f; // since we're using Sin, we need the extra 1/4 rotation to have a scale of 1.0f
    const float SPIN_RADIANS = 2f * Mathf.PI * SPIN_AMOUNT;
    public static IEnumerator SpinIntoExistence(GameObject vfx)
    {
        vfx.SetAlphaImmediate(0.5f);
        vfx.transform.localScale = vfx.transform.localScale.WithX(0f);
        for (float elapsed = 0f; elapsed < SPIN_TIME; elapsed += BraveTime.DeltaTime)
        {
            if (!vfx)
                yield break;
            float percentLeft = (1.0f - elapsed / SPIN_TIME);
            float easeAmount = (1.0f - percentLeft * percentLeft * percentLeft);
            vfx.transform.localScale = vfx.transform.localScale.WithX(Mathf.Sin(easeAmount * SPIN_RADIANS));
            yield return null;
        }
        yield break;
    }

    private void OnPickup()
    {
        if (this.vfx == null)
            return;

        UnityEngine.Object.Destroy(this.vfx);
        this.vfx = null;
    }

    public void DoSparkles()
    {
        this._doSparkles = true;
    }

    public IEnumerator DoSparkles_CR()
    {
        const int NUM_SPARKLES = 20;
        Vector3 basePos = this._pickup.sprite.WorldCenter.ToVector3ZisY(-10f);
        for (int i = 0; i < NUM_SPARKLES; ++i)
        {
            SpawnManager.SpawnVFX(
                InsurancePolicy._InsuranceParticleVFX, basePos + Lazy.RandomVector(0.5f).ToVector3ZUp(0), Quaternion.identity);
            yield return null;
        }
        yield break;
    }
}
