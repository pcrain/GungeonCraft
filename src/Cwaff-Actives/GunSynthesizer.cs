namespace CwaffingTheGungy;

public class GunSynthesizer : CwaffActive
{
    public static string ItemName         = "Gun Synthesizer";
    public static string ShortDescription = "Transient Guns, Lasting Damage";
    public static string LongDescription  = "Synthesizes a random gun for 20 seconds. Synthesized guns have 25% of their max ammo and cannot be switched, thrown, or dropped. Recharges every 3 rooms.";
    public static string Lore             = "A strange device made entirely of unrecognizable alien parts, save for a one quettabyte USB thumb drive sticking out of the side. Previous examinations have revealed the USB drive to contain detailed blueprints and specifications for almost every gun ever encountered in the Gungeon. The remaining 99.99999999995% of space on the drive is filled with assorted memes, cat pictures, and Touhou remixes.";

    private const float _SYNTH_LIFETIME       = 20f;
    private const float _AMMO_PERCENT         = 0.25f;
    private const int _COOLDOWN               = 3; // in rooms
    private const string _OVERRIDE            = "Synthetic Gun";

    private static GameObject _SPAWN_VFX      = null;

    private PlayerController _owner           = null;
    private SyntheticGun _currentSyntheticGun = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<GunSynthesizer>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.PerRoom, _COOLDOWN);
        item.consumable                 = false;
        item.CanBeDropped               = true;

        _SPAWN_VFX = VFX.Create("basic_green_square", fps: 2, anchor: Anchor.MiddleCenter);
        _SPAWN_VFX.GetComponent<tk2dSprite>().MakeHolographic(green: true);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return !this._currentSyntheticGun;
    }

    public override void DoEffect(PlayerController user)
    {
        Gun gun = PickupObjectDatabase.GetRandomGun();
        if (!gun)
            gun = ItemHelper.Get(Items.Ak47) as Gun; // fallback in case we can't actually get a proper gun
        MakeSyntheticGun(gun, user);

        // MakeSyntheticGun(ItemHelper.Get(Items.PrototypeRailgun) as Gun, user);
        // MakeSyntheticGun(PickupObjectDatabase.GetById(IDs.Pickups["deadline"]).GetComponent<Gun>(), user);
        this.m_activeDuration  = _SYNTH_LIFETIME;
        this.m_activeElapsed   = 0f;
        this.IsCurrentlyActive = true;
        user.gameObject.Play("gun_synthesizer_activate_sound");
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this._owner = player;
    }

    public override void OnPreDrop(PlayerController player)
    {
        DestroySyntheticGun(player);
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override void OnDestroy()
    {
        if (this._owner)
            DestroySyntheticGun(this._owner);
        base.OnDestroy();
    }

    private const int _SPAWN_PARTICLES = 64;
    private void MakeSyntheticGun(Gun gun, PlayerController pc)
    {
        pc.inventory.GunChangeForgiveness = true;  // NOTE: only used by Metronome
        Gun synthGun = pc.inventory.AddGunToInventory(gun, true);
        this._currentSyntheticGun = synthGun.gameObject.AddComponent<SyntheticGun>();
        FancyVFX.SpawnBurst(
            prefab           : _SPAWN_VFX,
            numToSpawn       : _SPAWN_PARTICLES,
            basePosition     : synthGun.barrelOffset.position,
            positionVariance : 0.25f,
            baseVelocity     : null,
            minVelocity      : 15f,
            velocityVariance : 0f,
            velType          : FancyVFX.Vel.Random,
            rotType          : FancyVFX.Rot.None,
            lifetime         : 0.85f,
            fadeOutTime      : null, // NOTE: ruins the shader
            parent           : null,
            emissivePower    : 0f,
            emissiveColor    : null,
            fadeIn           : false,
            uniform          : false,
            startScale       : 1.0f,
            endScale         : 0.1f,
            height           : null
          );
    }

    public override void Update()
    {
        base.Update();
        if (!this._currentSyntheticGun)
            return;
        if (this.CurrentRoomCooldown != _COOLDOWN)
            this.CurrentRoomCooldown = _COOLDOWN; // prevent charging while synthetic gun is active
        if ((this.m_activeElapsed >= this.m_activeDuration) || this._currentSyntheticGun.GetComponent<Gun>().CurrentAmmo == 0)
        {
            DestroySyntheticGun(this.LastOwner);
            return;
        }
        // if ((this._timeLeft -= BraveTime.DeltaTime) <= 0.0f)
        // {
        //     DestroySyntheticGun(this.LastOwner);
        //     return;
        // }
    }

    private const int _FRAGMENT_EDGE = 4;
    private const int _FRAGMENTS     = _FRAGMENT_EDGE * _FRAGMENT_EDGE;
    private const float _DECONSTRUCT_DELAY = 0.0625f;
    private void DestroySyntheticGun(PlayerController pc)
    {
        this.IsCurrentlyActive = false;
        if (!this._currentSyntheticGun)
            return;

        // get the current gun's sprite and assemble a list of fragments
        Gun gun = this._currentSyntheticGun.GetComponent<Gun>();
        tk2dSpriteCollectionData coll = gun.sprite.collection;
        tk2dSpriteDefinition gunSprite = gun.sprite.GetCurrentSpriteDef();
        List<tk2dSpriteDefinition> fragments = new(_FRAGMENTS);
        for (int i = 0; i < _FRAGMENT_EDGE; ++i)
            for (int j = 0; j < _FRAGMENT_EDGE; ++j)
                fragments.Add(Lazy.GetSpriteFragment(gunSprite, i, j, _FRAGMENT_EDGE));

        // if we haven't registered the first fragment, we haven't registered any of them
        if (coll.spriteNameLookupDict == null)
            coll.InitDictionary();
        if (!coll.spriteNameLookupDict.TryGetValue(fragments[0].name, out int newSpriteId))
        {
            int nextSpriteId = newSpriteId = coll.spriteDefinitions.Length;
            Array.Resize(ref coll.spriteDefinitions, coll.spriteDefinitions.Length + _FRAGMENTS);
            foreach(tk2dSpriteDefinition fragDef in fragments)
            {
                coll.spriteDefinitions[nextSpriteId]    = fragDef;
                coll.spriteNameLookupDict[fragDef.name] = nextSpriteId;
                ++nextSpriteId;
            }
        }

        // create a dissipating fragment for each segment of the gun
        Vector3 gunPos    = gun.sprite.WorldCenter;
        Vector3 targetPos = gunPos.WithY(gunPos.y + 16f);
        List<int> ids     = new IntVector2(newSpriteId, _FRAGMENTS).AsRange();
        float delay       = 0.0f;
        foreach (int id in ids)
        {
            tk2dSprite sprite = Lazy.SpriteObject(coll, id);
            sprite.FlipX = gun.sprite.FlipX;
            sprite.FlipY = gun.sprite.FlipY;
            sprite.transform.rotation = gun.sprite.transform.rotation;
            sprite.PlaceAtRotatedPositionByAnchor(gunPos, Anchor.MiddleCenter);
            sprite.AddComponent<DissipatingSpriteFragment>().Setup(gunPos, targetPos, 0.5f, delay, true);
            sprite.MakeHolographic(green: true);
            delay += _DECONSTRUCT_DELAY;
        }

        // clean up the player's inventory
        pc.gameObject.Play("gun_synthesizer_activate_sound");
        pc.inventory.GunLocked.RemoveOverride(GunSynthesizer._OVERRIDE);
        pc.inventory.DestroyGun(gun);
        this._currentSyntheticGun = null;
        pc.inventory.GunChangeForgiveness = false;
    }

    private class SyntheticGun : MonoBehaviour
    {
        private Gun _gun;
        private PlayerController _owner;
        private void Start()
        {
            Gun gun = this._gun = base.GetComponent<Gun>();
            this._owner = this._gun.GunPlayerOwner();

            gun.CanBeDropped = false;
            gun.CanBeSold = false;
            this._owner.inventory.GunLocked.SetOverride(_OVERRIDE, true);

            if (!gun.InfiniteAmmo)
                gun.CurrentAmmo = Math.Max(1, Mathf.CeilToInt(_AMMO_PERCENT * (float)gun.GetBaseMaxAmmo()));

            gun.sprite.MakeHolographic(green: true);
        }

        private void Update()
        {
            if (!this._gun)
                return;
            this._gun.m_prepThrowTime = -999f; //HACK: prevent the gun from being thrown
            this._gun.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/HologramShader");  // force holographics
        }
    }
}

