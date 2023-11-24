namespace CwaffingTheGungy;

public class CampingSupplies : PassiveItem
{
    public static string ItemName         = "Camping Supplies";
    public static string SpritePath       = "cozy_camper_icon";
    public static string ShortDescription = "In for the Long Gun";
    public static string LongDescription  = "Increases damage over time while standing still. Damage boost is reset after moving.";
    public static string Lore             = "Camping has proven time and again to be an effective strategy to any Gungeoneer that has the mental fortitude to endure insults to their skill, their mother, their face, and their mother's face.";

    private static float[] _CampTimes = { 0.00f, 2.00f, 5.00f, 10.0f }; // how long we stand still before each bonus kicks in
    private static float[] _CampMults = { 1.00f, 1.30f, 1.60f, 1.90f }; // bonus at each camp level
    private static int _MaxCampLevel  = _CampMults.Count() - 1;
    private static GameObject _BonfirePrefab;
    private static GameObject _SmokePrefab;
    private static GameObject[] _CampfirePrefabs;
    private static GameObject[] _SodaCanPrefabs;

    private const float _MOVEMENT_THRESHOLD   = 0.01f;
    private const float _SODA_CAN_TOSS_CHANCE = 0.003f;
    private const float _MAX_CAN_THROW_RATE   = 1f;

    private int _campLevel             = 0;    // our current camp level w.r.t. _CampTimes
    private float _timeSinceMoving     = 0.0f; // time since we've last moved
    private GameObject _activeCampfire = null; // active campfire vfx
    private Vector3 _campfirePos       = Vector3.zero; // active campfire vfx position
    private float _lastCanToss         = 0.0f;

    private StatModifier _campMod = new StatModifier
    {
        amount      = 1.0f,
        statToBoost = PlayerStats.StatType.Damage,
        modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
    };

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<CampingSupplies>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;

        _SmokePrefab     = ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof") as GameObject;
        _BonfirePrefab   = (ItemHelper.Get(Items.GunSoul) as ExtraLifeItem).BonfireSynergyBonfire;
        _CampfirePrefabs = new GameObject[]{
            VFX.RegisterVFXObject("CampfireA", ResMap.Get("campfire_a"),
                fps: 6, loops: true, anchor: Anchor.LowerCenter, scale: 0.5f, emissivePower: 1f), // level 0 (unused)
            VFX.RegisterVFXObject("CampfireB", ResMap.Get("campfire_b"),
                fps: 6, loops: true, anchor: Anchor.LowerCenter, scale: 0.5f, emissivePower: 2f), // level 1
            VFX.RegisterVFXObject("CampfireC", ResMap.Get("campfire_c"),
                fps: 6, loops: true, anchor: Anchor.LowerCenter, scale: 0.5f, emissivePower: 3f), // level 2
            VFX.RegisterVFXObject("CampfireD", ResMap.Get("campfire_d"),
                fps: 6, loops: true, anchor: Anchor.LowerCenter, scale: 0.5f, emissivePower: 4f), // level 3
        };

        _SodaCanPrefabs  = new GameObject[] {
            VFX.RegisterVFXObject("CanOfCoke", ResMap.Get("can_of_coke"),
                fps: 2, loops: true, anchor: Anchor.MiddleCenter, scale: 0.5f),
            VFX.RegisterVFXObject("CanOfPepsi", ResMap.Get("can_of_pepsi"),
                fps: 2, loops: true, anchor: Anchor.MiddleCenter, scale: 0.5f),
            VFX.RegisterVFXObject("CanOfSprite", ResMap.Get("can_of_sprite"),
                fps: 2, loops: true, anchor: Anchor.MiddleCenter, scale: 0.5f),
        };

    }

    public override void Pickup(PlayerController player)
    {
        if (!m_pickedUpThisRun)
            this.passiveStatModifiers = new StatModifier[] { this._campMod };
        base.Pickup(player);
        ResetStats(player);
    }

    public override DebrisObject Drop(PlayerController player)
    {
        ResetStats(player);
        return base.Drop(player);
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner)
            return;
        if (this.Owner.Velocity.magnitude <= _MOVEMENT_THRESHOLD)
        {
            if (this._campLevel > 0 && UnityEngine.Random.Range(0f, 1f) < _SODA_CAN_TOSS_CHANCE)
                TossASodaCan();
            RecalculateStats(this.Owner);
            return;
        }

        ResetStats(this.Owner);
    }

    private void ResetStats(PlayerController player)
    {
        if (this._activeCampfire)
        {
            TossASodaCan();
            DoSmokeAt(this._campfirePos);
            UnityEngine.Object.Destroy(this._activeCampfire);
        }
        this._activeCampfire         = null;
        this._campfirePos            = Vector3.zero;
        this._timeSinceMoving        = 0.0f;
        this._campLevel              = 0;
        this._campMod.amount         = 1.0f;
        this.passiveStatModifiers[0] = this._campMod;
        player.stats.RecalculateStats(player, false, false);
    }

    private void RecalculateStats(PlayerController player)
    {
        if (this._campLevel == _MaxCampLevel)
            return; // nothing else to do

        this._timeSinceMoving += BraveTime.DeltaTime;
        if (this._timeSinceMoving < _CampTimes[this._campLevel + 1])
            return; // haven't reached the next level of camping yet

        this._campLevel += 1;
        this._campMod.amount = _CampMults[this._campLevel];

        player.stats.RecalculateStats(player, false, false);

        if (!this._activeCampfire)
        {
            Vector3 pos = (player.sprite.WorldCenter - player.m_currentGunAngle.ToVector(1.0f)).ToVector3ZisY(1f);
            this._campfirePos = pos;
            DoSmokeAt(this._campfirePos);
        }
        else
            UnityEngine.Object.Destroy(this._activeCampfire);
        this._activeCampfire = SpawnManager.SpawnVFX(_CampfirePrefabs[this._campLevel], this._campfirePos, Quaternion.identity);
    }

    private void DoSmokeAt(Vector3 pos)
    {
        UnityEngine.Object.Instantiate(_SmokePrefab)
            .GetComponent<tk2dBaseSprite>()
            .PlaceAtPositionByAnchor(pos, Anchor.MiddleCenter);
    }

    private void TossASodaCan()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return; // don't toss cans while game is paused
        if (BraveTime.ScaledTimeSinceStartup - this._lastCanToss < _MAX_CAN_THROW_RATE)
            return; // don't toss cans too frequently
        this._lastCanToss = BraveTime.ScaledTimeSinceStartup;

        AkSoundEngine.PostEvent("pop_soda_can_sound", this.Owner.gameObject);
        GameObject can        = SpawnManager.SpawnVFX(_SodaCanPrefabs.ChooseRandom(), this.Owner.sprite.WorldCenter, Quaternion.identity);
        Vector3 startingForce = Lazy.RandomVector(5f).ToVector3ZUp(UnityEngine.Random.Range(1f,3f));
        float startingHeight  = 1f;
        DebrisObject debris = can.GetOrAddComponent<DebrisObject>();
            debris.angularVelocity         = 180f;  // toss in any direction
            debris.angularVelocityVariance = 180f;
            debris.decayOnBounce           = 0.5f;
            debris.bounceCount             = 2;
            debris.canRotate               = true;
            debris.Trigger(startingForce, startingHeight);

        Invoke("BeginBeamFire", 0.75f); // make sure this is at least as long as the rail's lifetime
        Invoke("Expire", 2f); // make sure this is at least as long as the rail's lifetime
    }
}
