namespace CwaffingTheGungy;

public class AdrenalineShot : PassiveItem
{
    public static string ItemName         = "Adrenaline Shot";
    public static string SpritePath       = "adrenaline_shot_icon";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";

    internal const float _MAX_ADRENALINE_TIME = 60f;
    internal const float _MAX_ADRENALINE_LOSS = 4f; // loss from taking damage while under effects of adrenaline
    internal static int _AdrenalineShotId;
    internal static dfSprite _AdrenalineHeart;
    internal static float _LastHeartbeatTime = 0f;

    private bool  _adrenalineActive = false;
    private float _adrenalineTimer  = _MAX_ADRENALINE_TIME;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<AdrenalineShot>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality      = PickupObject.ItemQuality.B;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        _AdrenalineHeart = Lazy.SetupUISprite(ResMap.Get("adrenaline_heart"));

        _AdrenalineShotId = item.PickupObjectId;

        new Hook(
            typeof(GameUIHeartController).GetMethod("UpdateHealth", BindingFlags.Public | BindingFlags.Instance),
            typeof(AdrenalineShot).GetMethod("UpdateHealth", BindingFlags.NonPublic | BindingFlags.Static));
    }

    internal static bool didEffect = false;
    private static void UpdateHealth(Action<GameUIHeartController, HealthHaver> orig, GameUIHeartController guihc, HealthHaver hh)
    {
        orig(guihc, hh);
        if (hh?.m_player is not PlayerController player)
            return;

        int nHearts = guihc.extantHearts.Count();
        if (nHearts == 0)
            return; // we have nothing to do if there are no hearts

        if (!player.gameObject.GetComponent<UnderAdrenalineEffects>())
            nHearts = 0; // if we don't have this item, we have 0 adrenaline hearts

        AdrenalineHeartOverlay overlay = guihc.gameObject.GetOrAddComponent<AdrenalineHeartOverlay>();
        int aHearts = overlay.adrenalineHearts.Count();
        if (aHearts == nHearts)
            return; // if our current and cached hearts are the same, we have nothing to do

        float scale = Pixelator.Instance.CurrentTileScale;

        // Remove old hearts as necessary (all hearts except the first have a grandparent that manages them)
        dfControl heartManager = guihc.extantHearts[0].Parent?.Parent ?? guihc.extantHearts[0].Parent;
        if (!heartManager) // should never happen
        {
            if (C.DEBUG_BUILD)
                ETGModConsole.Log($"NO HEART MANAGER");
            return;
        }

        for (int i = aHearts - 1; i >= nHearts; --i)
        {
            overlay.adrenalineHearts[i].transform.parent = null;
            heartManager.RemoveControl(overlay.adrenalineHearts[i].GetComponent<dfControl>());
            UnityEngine.Object.Destroy(overlay.adrenalineHearts[i]);
            overlay.adrenalineHearts.RemoveAt(i);
        }

        // Add new hearts as necessary
        for (int i = aHearts; i < nHearts; ++i)
        {
            dfSprite heart = guihc.extantHearts[i];
            if (!heart)
                continue;
            GameObject gameObject = UnityEngine.Object.Instantiate(_AdrenalineHeart.gameObject);
            gameObject.transform.parent = guihc.transform.parent;
            gameObject.layer = guihc.gameObject.layer;
            dfSprite component = gameObject.GetComponent<dfSprite>();
            component.BringToFront();
            heartManager.AddControl(component);
            heartManager.BringToFront();
            component.ZOrder = heart.ZOrder - 1;
            component.RelativePosition = heart.RelativePosition + new Vector3(scale, 2 * scale, 0f);
            component.Size = component.SpriteInfo.sizeInPixels * scale;
            overlay.adrenalineHearts.Add(gameObject);

            gameObject.transform.parent = heart.transform;  // make sure it disappears when minimap or pause is toggled
        }
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.healthHaver.ModifyDamage += this.OnTakeDamage;

    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.healthHaver.OnHealthChanged -= this.OnHealthChanged;
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this.Owner)
            this.Owner.healthHaver.ModifyDamage -= this.OnTakeDamage;
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner || !this._adrenalineActive)
            return; // nothing to do if we don't have active adrenaline
        if (GameManager.Instance.IsPaused || GameManager.Instance.IsLoadingLevel || !this.Owner.AcceptingNonMotionInput)
            return; // nothing to do if we're not in control of our character

        this._adrenalineTimer -= BraveTime.DeltaTime;
        float heartRate =
            (this._adrenalineTimer > 30) ? 2f   :
            (this._adrenalineTimer > 10) ? 1f   :
            (this._adrenalineTimer > 3)  ? 0.5f : 0.25f;
        if (BraveTime.ScaledTimeSinceStartup - _LastHeartbeatTime > heartRate)
        {
            _LastHeartbeatTime = BraveTime.ScaledTimeSinceStartup;
            AkSoundEngine.PostEvent("heartbeat", this.Owner.gameObject);
        }

        if (this._adrenalineTimer > 0f)
            return; // clock's still ticking

        // ded D:
        this.Owner.healthHaver.ModifyDamage -= this.OnTakeDamage;
        this.Owner.healthHaver.OnHealthChanged -= this.OnHealthChanged;
        this.Owner.healthHaver.ApplyDamage(999f, Vector2.zero, ItemName, CoreDamageTypes.None, DamageCategory.Unstoppable, ignoreInvulnerabilityFrames: true);
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!hh.PlayerWillDieFromHit(data))
            return; // if we're not going to die, we don't need to activate

        if (this._adrenalineActive)
        {
            this._adrenalineTimer -= _MAX_ADRENALINE_LOSS;
            if (this._adrenalineTimer <= 0)
                return;
            AkSoundEngine.PostEvent("adrenaline_tank_damage_sound", hh.gameObject);
        }

        data.ModifiedDamage = 0f;
        hh.TriggerInvulnerabilityPeriod();
        DoFlash(hh);
        if (!this._adrenalineActive)
            ActivateAdrenaline();
    }

    // yoinked from HealthHaver.ApplyDamageDirectional()
    private void DoFlash(HealthHaver hh)
    {
        if (!(hh.flashesOnDamage && hh.spriteAnimator != null && !hh.m_isFlashing))
            return;

        if (hh.m_flashOnHitCoroutine != null)
            hh.StopCoroutine(hh.m_flashOnHitCoroutine);
        hh.m_flashOnHitCoroutine = null;
        if (hh.materialsToFlash == null)
        {
            hh.materialsToFlash = new List<Material>();
            hh.outlineMaterialsToFlash = new List<Material>();
            hh.sourceColors = new List<Color>();
        }
        if ((bool)hh.gameActor)
            for (int k = 0; k < hh.materialsToFlash.Count; k++)
                hh.materialsToFlash[k].SetColor("_OverrideColor", hh.gameActor.CurrentOverrideColor);
        if (hh.outlineMaterialsToFlash != null)
            for (int l = 0; l < hh.outlineMaterialsToFlash.Count; l++)
            {
                if (l >= hh.sourceColors.Count)
                    break;
                hh.outlineMaterialsToFlash[l].SetColor("_OverrideColor", hh.sourceColors[l]);
            }
        hh.m_flashOnHitCoroutine = hh.StartCoroutine(hh.FlashOnHit(DamageCategory.Normal, null));
    }

    private void ActivateAdrenaline()
    {
        this._adrenalineActive = true;
        this.CanBeDropped      = false;
        this.CanBeSold         = false;
        this.Owner.gameObject.AddComponent<UnderAdrenalineEffects>();
        this.Owner.healthHaver.ForceSetCurrentHealth(0.5f);
        this.Owner.healthHaver.OnHealthChanged += this.OnHealthChanged;
        Color faded = Color.Lerp(Color.gray, Color.clear, 0.25f);
        this.Owner.FlatColorOverridden = true;
        this.Owner.baseFlatColorOverride = faded;
        this.Owner.ChangeFlatColorOverride(faded);
        this.Owner.DoGenericItemActivation(this);

        this._adrenalineTimer  = _MAX_ADRENALINE_TIME;
    }

    private void OnHealthChanged(float resultValue, float maxValue)
    {
        if (resultValue > 0.5f)
            DeactivateAdrenaline();
    }

    private void DeactivateAdrenaline()
    {
        this._adrenalineActive = false;
        this.CanBeDropped      = true;
        this.CanBeSold         = true;
        this.Owner.healthHaver.OnHealthChanged -= this.OnHealthChanged;
        this.Owner.healthHaver.ForceSetCurrentHealth(Mathf.Max(this.Owner.healthHaver.currentHealth - 0.5f, 0.5f));
        this.Owner.baseFlatColorOverride = Color.clear;
        this.Owner.ChangeFlatColorOverride(Color.clear);
        AkSoundEngine.PostEvent("adrenaline_deactivate_sound", this.Owner.gameObject);

        UnityEngine.Object.Destroy(this.Owner.gameObject.GetComponent<UnderAdrenalineEffects>());
    }
}

public class AdrenalineHeartOverlay : MonoBehaviour
{
    public List<GameObject> adrenalineHearts = new();
}

public class UnderAdrenalineEffects : MonoBehaviour {}