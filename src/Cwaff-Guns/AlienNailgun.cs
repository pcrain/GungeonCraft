namespace CwaffingTheGungy;

/* TODO:
    - nothing for now :D
*/

public class AlienNailgun : CwaffGun
{
    public static string ItemName         = "Alien Nailgun";
    public static string ShortDescription = "Attack, Adapt, Assimilate";
    public static string LongDescription  = "Fires nails that extract DNA when dealing the fatal blow to enemies. Charging the gun while in combat consumes a full clip of ammo to assemble a replicant enemy from DNA. Replicants are invulnerable, have no collision, cannot harm the player with projectiles or contact damage, and dissipate when combat ends. Reloading with a full clip cycles through extracted DNA sequences.";
    public static string Lore             = "Having arrived on Earth en masse in a strange meteorite, this gadget launches fingers that quickly retract on impact to physically scrape data off of recently-deceased life forms. Unbeknownst to Gungeoneers, the meteorite was actually an intergalactic standard wastebin, and this eons-old prototype was discarded after its inventors discovered light-based replicators were cheaper, faster, less convoluted, and ultimately less weird.";

    private const float _RECONSTRUCT_DELAY   = 0.2f;
    private const float _RECONSTRUCT_TIME    = 1.3f;
    private const int   _RECONSTRUCT_COST    = 16;
    private const float _FRAGMENT_SPAWN_TIME = 0.3f;
    private const float _PREVIEW_TIME        = 0.8f;
    private const int _FRAGMENT_EDGE         = 4;
    private const int _FRAGMENTS             = _FRAGMENT_EDGE * _FRAGMENT_EDGE;
    private const float _FRAGMENT_GAP        = _RECONSTRUCT_TIME / (float)_FRAGMENTS;

    private static HashSet<AIActor> _Replicants   = new();

    private Coroutine _dnaReconstruct       = null;
    private int _spawnIndex                 = -1;
    private string _targetGuid              = null;
    private float _curChargeTime            = 0.0f;
    private bool _constructionComplete      = false;
    private List<GameObject> _fragments     = new();
    private GameObject _preview             = null;

    [SerializeField]
    private List<string> _registeredEnemies = new();

    public static void Init()
    {
        Lazy.SetupGun<AlienNailgun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.PISTOL, reloadTime: 0.7f, ammo: 480, idleFps: 24, shootFps: 24, reloadFps: 60,
            loopReloadAt: 0, muzzleFrom: Items.Mailbox, fireAudio: "alien_nailgun_shoot_sound", reloadAudio: "gorgun_eye_activate")
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .InitProjectile(GunData.New(clipSize: _RECONSTRUCT_COST, cooldown: 0.14f, shootStyle: ShootStyle.Charged, damage: 2.0f,
            sprite: "alien_nailgun_projectile", customClip: true, chargeTime: 0.0f, useDummyChargeModule: true))
          .Attach<ExtractDNAOnKill>();
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        if (player.IsDodgeRolling)
            return;
        if (this._registeredEnemies.Count == 0)
            return;

        if (this._preview) // only cycle if the preview is already visible, otherwise just show the current selection
            this._spawnIndex = (this._spawnIndex + 1) % this._registeredEnemies.Count;
        if (this._spawnIndex < 0)
            this._spawnIndex = 0;
        SwitchEnemyToSpawn(this._registeredEnemies[this._spawnIndex]);
    }

    private void SwitchEnemyToSpawn(string guid, bool isNew = false)
    {
        if (this.PlayerOwner is not PlayerController pc)
            return;
        this._targetGuid = guid;

        if (this._preview)
            this._preview.SafeDestroy();

        AIActor actor = EnemyDatabase.GetOrLoadByGuid(guid);
        tk2dSprite sprite = Lazy.SpriteObject(actor.sprite.collection, Lazy.GetIdForBestIdleAnimation(actor));
            sprite.PlaceAtPositionByAnchor(pc.sprite.WorldTopCenter + new Vector2(0f, 0.5f), Anchor.LowerCenter);
            sprite.MakeHolographic(green: true);

        this._preview = sprite.gameObject;
        this._preview.transform.parent = pc.transform;

        this._preview.ExpireIn(_PREVIEW_TIME);
        if (isNew)
            this._preview.Play("replicant_select_new_sound");
        else
            this._preview.Play("replicant_select_sound");
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging || !this.PlayerOwner.IsInCombat)
        {
            StopReconstruction();
            this.gun.SynchronizeReloadAcrossAllModules();
            return;
        }
        if (this._constructionComplete)
            return; // idle until we release the charge button
        if (string.IsNullOrEmpty(this._targetGuid))
            return; // nothing to construct
        if (this.gun.CurrentAmmo < _RECONSTRUCT_COST)
            return; // can't afford to construct
        this._dnaReconstruct ??= StartCoroutine(ReconstructFromDNA(this._targetGuid));
    }

    private static void ApplyReplicantShaders(tk2dBaseSprite sprite)
    {
        sprite.MakeHolographic(green: true);
    }

    private IEnumerator ReconstructFromDNA(string guid)
    {
        // delay before reconstruction begins
        Vector2 position = this.PlayerOwner.CenterPosition;
        for (float elapsed = 0f; elapsed < _RECONSTRUCT_DELAY; elapsed += BraveTime.DeltaTime)
            yield return null;

        // begin reconstruction process
        float timer = 0.0f;
        for (int i = 0; i < _FRAGMENTS; timer += BraveTime.DeltaTime)
        {
            while (timer > _FRAGMENT_GAP)
            {
                Vector2 startPos = (this.PlayerOwner && this.PlayerOwner.CurrentGun)
                    ? this.PlayerOwner.CurrentGun.barrelOffset.position
                    : position + Lazy.RandomVector(4f);
                GameObject fragment = CreateEnemyFragment(guid, i, position, startPos, _FRAGMENT_SPAWN_TIME);
                this._fragments.Add(fragment);
                fragment.Play("replicant_assemble_sound");
                timer -= _FRAGMENT_GAP;
                if ((++i) >= _FRAGMENTS)
                    break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(_FRAGMENT_SPAWN_TIME);
        if (this.PlayerOwner)
            this.PlayerOwner.gameObject.Play("replicant_created_sound");

        // finish reconstruction process
        AIActor replicant = Replicant.Create(guid, position, ApplyReplicantShaders, hasCollision: false);
        if (replicant)
            _Replicants.Add(replicant);
        foreach (GameObject g in this._fragments)
            g.SafeDestroy();
        this._fragments.Clear();
        this.gun.LoseAmmo(_RECONSTRUCT_COST);
        this._constructionComplete = true;
        yield break;
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        StaticReferenceManager.ProjectileAdded -= CheckFromReplicantOwner;
        StaticReferenceManager.ProjectileAdded += CheckFromReplicantOwner;
        CwaffEvents.OnBankBulletOwnerAssigned -= CheckFromReplicantOwner;
        CwaffEvents.OnBankBulletOwnerAssigned += CheckFromReplicantOwner;
        player.OnAnyEnemyReceivedDamage -= this.CheckIfEnemyKilled;
        player.OnAnyEnemyReceivedDamage += this.CheckIfEnemyKilled;
        base.OnPlayerPickup(player);
        player.OnRoomClearEvent += DestroyReplicants;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnRoomClearEvent -= DestroyReplicants;
        player.OnAnyEnemyReceivedDamage -= this.CheckIfEnemyKilled;
        StopReconstruction();
        DestroyReplicants(player);
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnAnyEnemyReceivedDamage -= this.CheckIfEnemyKilled;
        base.OnDestroy();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        StopReconstruction();
    }

    private void CheckIfEnemyKilled(float damage, bool fatal, HealthHaver enemy)
    {
        if (!this)
        {
            Lazy.RuntimeWarn("Calling an event from a nonexistent Alien Nailgun, tell pretzel");
            return;
        }
        if (!fatal)
            return;
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (!player.IsInCombat || !player.HasSynergy(Synergy.MASTERY_ALIEN_NAILGUN))
            return;
        if (enemy.aiActor is not AIActor actor)
            return;
        if (string.IsNullOrEmpty(actor.EnemyGuid))
            return;
        if (!this._registeredEnemies.Contains(actor.EnemyGuid))
            return;

        AIActor replicant = Replicant.Create(actor.EnemyGuid, actor.CenterPosition, ApplyReplicantShaders, hasCollision: false);
        if (!replicant)
            return;

        _Replicants.Add(replicant);
        player.gameObject.Play("replicant_created_sound");
    }

    private static void CheckFromReplicantOwner(Projectile p)
    {
        if (!p || p.Owner is not AIActor enemy)
            return;
        if (!_Replicants.Contains(enemy))
            return;
        p.StopCollidingWithPlayers();
        p.collidesWithEnemies = true;
        p.AddComponent<ReplicantProjectile>();
    }

    private void StopReconstruction()
    {
        if (this._dnaReconstruct != null)
        {
            StopCoroutine(this._dnaReconstruct);
            this._dnaReconstruct = null;
            foreach (GameObject g in this._fragments)
                g.SafeDestroy();
            this._fragments.Clear();
        }
        this._curChargeTime = 0.0f;
        this._constructionComplete = false;
    }

    private static GameObject CreateEnemyFragment(string guid, int index, Vector2 targetPosition, Vector2 startPosition, float travelTime, float delay = 0.0f, bool autoDestroy = false)
    {
        AIActor enemy                 = EnemyDatabase.GetOrLoadByGuid(guid);
        int bestSpriteId              = Lazy.GetIdForBestIdleAnimation(enemy);
        tk2dSpriteCollectionData coll = enemy.sprite.collection;
        tk2dSpriteDefinition baseDef  = coll.spriteDefinitions[bestSpriteId];
        tk2dSpriteDefinition fragDef  = Lazy.GetSpriteFragment(
            orig     : baseDef,
            x        : index % _FRAGMENT_EDGE,
            y        : index / _FRAGMENT_EDGE,
            edgeSize : _FRAGMENT_EDGE
            );

        string fragName = fragDef.name;
        int newSpriteId;
        if (coll.spriteNameLookupDict == null)
            coll.InitDictionary();
        if (!coll.spriteNameLookupDict.TryGetValue(fragName, out newSpriteId))
        {
            newSpriteId = coll.spriteDefinitions.Length;
            Array.Resize(ref coll.spriteDefinitions, coll.spriteDefinitions.Length + 1);
            coll.spriteDefinitions[newSpriteId] = fragDef;
            coll.spriteNameLookupDict[fragName] = newSpriteId;
        }

        tk2dSprite sprite = Lazy.SpriteObject(spriteColl: coll, spriteId: newSpriteId);
        sprite.PlaceAtPositionByAnchor(startPosition, Anchor.MiddleCenter);
        sprite.AddComponent<DissipatingSpriteFragment>().Setup(startPosition, targetPosition, travelTime, delay, autoDestroy);
        sprite.MakeHolographic(green: true);
        return sprite.gameObject;
    }

    private static void DestroyReplicants(PlayerController player)
    {
        foreach (AIActor replicant in _Replicants)
        {
            if (!replicant)
                continue;
            Vector2 pos = replicant.CenterPosition;
            for (int i = 0; i < _FRAGMENTS; ++i)
                CreateEnemyFragment(replicant.EnemyGuid, i, pos + Lazy.RandomVector(16f), pos, 0.2f, 0.05f * i, true);
            replicant.EraseFromExistence();
        }
        _Replicants.Clear();
    }

    public void RegisterEnemyDNA(string guid)
    {
        if (this._registeredEnemies.Contains(guid))
            return;
        this._registeredEnemies.Add(guid);
        this._spawnIndex = this._registeredEnemies.Count - 1;
        SwitchEnemyToSpawn(this._registeredEnemies[this._spawnIndex], isNew: true);
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        data.Add(this._registeredEnemies.Count);
        foreach (string enemy in this._registeredEnemies)
            data.Add(enemy);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        int count = (int)data[i++];
        for (int n = 0; n < count; ++n)
            this._registeredEnemies.Add((string)data[i++]);
        if (count > 0)
            this._spawnIndex = 0;
    }
}

/// <summary>Class to temporarily make projectiles replicants without the shader effects bleeding back into the projectile pool</summary>
public class ReplicantProjectile : MonoBehaviour
{
    private Shader _oldShader = null;

    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        if (!p || !p.sprite)
            return;
        this._oldShader = p.sprite.renderer.material.shader;
        p.sprite.MakeHolographic(green: true);
        p.OnDestruction += DestroyReplicantProjectile;
    }

    private void DestroyReplicantProjectile(Projectile p)
    {
        p.sprite.usesOverrideMaterial = false;
        p.sprite.renderer.material.shader = this._oldShader;
        this.SafeDestroy();
    }
}

public class ExtractDNAOnKill : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private AlienNailgun _gun;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner || !this._owner.CurrentGun)
            return;
        if (this._gun = this._owner.CurrentGun.gameObject.GetComponent<AlienNailgun>())
            this._projectile.OnWillKillEnemy += OnWillKillEnemy;
    }

    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemyBody)
    {
        if (enemyBody.GetComponent<AIActor>() is not AIActor enemy)
            return;
        if (this._gun && enemy.IsHostileAndNotABoss(canBeDead: true, canBeNeutral: false))
            this._gun.RegisterEnemyDNA(enemy.EnemyGuid);
    }
}
