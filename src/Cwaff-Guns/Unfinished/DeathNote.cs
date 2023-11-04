namespace CwaffingTheGungy;

public class DeathNote : AdvancedGunBehavior
{
    public static string ItemName         = "Death Note";
    public static string SpriteName       = "death_note";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Notably Dangerous";
    public static string LongDescription  = "(TBD)";

    internal static Dictionary<int, Nametag> _Nametags = new();
    internal static string _Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<DeathNote>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.InfiniteAmmo                      = true;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.quality                           = PickupObject.ItemQuality.D;

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        WhoAreTheyAgain();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        WhoAreTheyAgain();
        base.OnSwitchedAwayFromThisGun();
    }

    protected override void Update()
    {
        base.Update();
        YouShallKnowTheirNames();
    }

    private void WhoAreTheyAgain()
    {
        UpdateNametags(false);
    }

    private void UpdateNametags(bool enable)
    {
        List<int> deadEnemies = new();
        foreach(KeyValuePair<int, Nametag> entry in _Nametags)
        {
            if (!entry.Value.UpdateWhileParentAlive())
                deadEnemies.Add(entry.Key);
            else
                entry.Value.SetEnabled(enable);
        }
        foreach (int key in deadEnemies)
            _Nametags.Remove(key);
    }

    private void YouShallKnowTheirNames()
    {
        UpdateNametags(true);

        List<AIActor> activeEnemies = this.Owner.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        if (activeEnemies == null)
            return;

        foreach (AIActor enemy in activeEnemies)
        {
            if (!enemy || !enemy.specRigidbody || enemy.IsGone || !enemy.healthHaver || enemy.healthHaver.IsDead)
                continue;
            if (!enemy.gameObject.GetComponent<Nametag>())
                _Nametags[enemy.GetHashCode()] = enemy.gameObject.AddComponent<Nametag>();
        }
    }
}

public class Nametag : MonoBehaviour
{
    private string _name = "";
    private Text _nametag; // Reference to the Text component.
    private AIActor _actor;
    private GameObject _canvasGo;
    private GameObject _textGo;

    private static int _NumNames = 0;
    private static Font _Font;

    private void Start()
    {
        _Font ??= Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        this._actor = base.GetComponent<AIActor>();
        this._name = (++_NumNames).ToString();

        // Create Canvas GameObject.
        this._canvasGo = new GameObject();
        this._canvasGo.name = "Canvas";
        Canvas canvas = this._canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        this._canvasGo.AddComponent<CanvasScaler>();

        // Create the Text GameObject.
        this._textGo = new GameObject();
            this._textGo.transform.parent = _canvasGo.transform;

        // Set Text component properties.
        this._nametag           = this._textGo.AddComponent<Text>();
        this._nametag.font      = _Font;
        this._nametag.text      = $"I'm {this._name}";
        this._nametag.fontSize  = 32;
        this._nametag.alignment = TextAnchor.UpperCenter;
        this._nametag.color     = Color.green;

        // Provide Text position and size using RectTransform.
        RectTransform rectTransform;
        rectTransform = this._nametag.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(0, 0, 0);
        rectTransform.sizeDelta = new Vector2(500, 100); // make this big enough to fit a pretty big name

        this._actor.healthHaver.OnPreDeath += (_) => HandleEnemyDied();
    }

    internal bool UpdateWhileParentAlive()
    {
        if (this._actor?.healthHaver?.IsDead ?? true)
        {
            HandleEnemyDied();
            return false;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(this._actor.sprite.WorldTopCenter);
        this._nametag.transform.position = screenPos;
        return true;
    }

    private void HandleEnemyDied()
    {
        UnityEngine.Object.Destroy(this._canvasGo);
        UnityEngine.Object.Destroy(this._textGo);
        UnityEngine.Object.Destroy(this);
    }

    public void SetEnabled(bool v)
    {
        this._textGo.SetActive(v);
    }
}
