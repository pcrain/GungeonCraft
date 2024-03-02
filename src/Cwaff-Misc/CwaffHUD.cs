namespace CwaffingTheGungy;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance = null;  //singleton class

    public SGroup hudContainer;
    public Dictionary<string, HUDElement> elements = new Dictionary<string, HUDElement>();

    public bool showable = false;

    public static void Init()
    {
        AddHook(typeof(GameManager), "Pause");
        AddHook(typeof(GameManager), "Unpause");
        AddHook(typeof(PauseMenuController), "ToggleVisibility", "TogglePauseMenuVisibility");
        AddHook(typeof(GameUIRoot), "HideCoreUI");
        AddHook(typeof(GameUIRoot), "ShowCoreUI");
        AddHook(typeof(PlayerController), "Die");
        AddHook(typeof(ETGModConsole), "OnOpen", "OnConsoleOpen");
        AddHook(typeof(ETGModConsole), "OnClose", "OnConsoleClose");

        ETGModMainBehaviour.Instance.gameObject.AddComponent<HUDController>();
    }

    public static Hook AddHook(Type type, string sourceMethodName, string hookMethodName = null)
    {
        if (hookMethodName == null) hookMethodName = sourceMethodName;
        return new Hook(
            type.GetMethod(sourceMethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
            typeof(HUDController).GetMethod(hookMethodName, BindingFlags.NonPublic | BindingFlags.Static)
            // MethodBase.GetCurrentMethod().DeclaringType.GetMethod(hookMethodName, BindingFlags.NonPublic | BindingFlags.Static)
        );
    }

    static void Die(Action<PlayerController, Vector2> orig, PlayerController self, Vector2 finalDamageDirection)
    {
        HUDController.Instance.showable = false;
        orig(self, finalDamageDirection);
    }

    static void Unpause(Action<GameManager> orig, GameManager self)
    {
        orig(self);
        HUDController.Instance.showable = true;
    }

    static void Pause(Action<GameManager> orig, GameManager self)
    {
        HUDController.Instance.showable = false;
        orig(self);
    }

    static void TogglePauseMenuVisibility(Action<PauseMenuController, bool> orig, PauseMenuController self, bool visible)
    {
        HUDController.Instance.showable = visible;
        orig(self, visible);
    }

    static void HideCoreUI(Action<GameUIRoot, string> orig, GameUIRoot self, string reason)
    {
        HUDController.Instance.showable = false;
        orig(self, reason);
    }

    static void ShowCoreUI(Action<GameUIRoot, string> orig, GameUIRoot self, string reason)
    {
        HUDController.Instance.showable = true;
        orig(self, reason);
    }

    static void OnConsoleOpen(Action<ETGModMenu> orig, ETGModMenu self)
    {
        HUDController.Instance.showable = false;
        orig(self);
    }

    static void OnConsoleClose(Action<ETGModMenu> orig, ETGModMenu self)
    {
        HUDController.Instance.showable = true;
        orig(self);
    }

    public void SpawnLabels()
    {
        Instance.hudContainer = new SGroup() { Background = Color.clear, Size = new Vector2(400, 2), AutoGrowDirection = SGroup.EDirection.Vertical };

        Instance.hudContainer.Children.Add(new SRect(Color.clear) { Size = Vector2.zero }); // add empty element to the beginning due to a bug in SGUI's code TT

        Instance.hudContainer.AutoLayout = (SGroup g) => new Action<int, SElement>(g.AutoLayoutVertical);
        SGUIRoot.Main.Children.Add(Instance.hudContainer);
    }

    public void AddHUDElement(string name, HUDElement h)
    {
        Instance.elements[name] = h;
        Instance.hudContainer.Children.Add(Instance.elements[name].container);
    }

    void Awake()
    {
        Instance = this;    // singleton
        Instance.SpawnLabels();
    }

    void LateUpdate()
    {
        hudContainer.Visible = showable;
        if(!hudContainer.Visible)
            return;

        // update stats
        foreach (HUDElement el in elements.Values)
        {
            if (el.active)
            {
                el.container.Visible = true;
                el.Update();
            }
            else
                el.container.Visible = false;
        }

        // update hud
        hudContainer.ContentSize = hudContainer.Size.WithY(0);
        hudContainer.UpdateStyle();
        hudContainer.Position.y = hudContainer.Root.Size.y / 2 - hudContainer.Size.y / 2 + 50;
    }
}

public class HUDElement
{
    public SGroup container;
    public SGroup layout;
    public SImage icon = null;
    public SLabel text = null;
    public bool active = false;
    public Func<HUDElement,bool> updater = null;
    public float updateFreq = 1.0f/60.0f;

    private float timeSinceLastUpdate = 0f;
    private static Texture2D defaultIcon =
        ResourceExtractor.GetTextureFromResource($"{C.MOD_INT_NAME}/Resources/HUD/Coolness.png");

    public HUDElement(string name, string initText = null, string initIconPath = null, bool addImmediately = true)
    {
        if (initText != null)
            text = new SLabel(initText);
        else
        {
            text = new SLabel("");
            text.Visible = false;
        }
        if (initIconPath != null)
        {
            Texture2D itex = ResourceExtractor.GetTextureFromResource(initIconPath);
            icon = new SImage(itex);
            icon.UpdateStyle();
        }
        else
        {
            icon = new SImage(defaultIcon);
            icon.Foreground = icon.Foreground.WithAlpha(0);
            // icon.Visible = false;
        }

        container = new SGroup() { Background = Color.clear, Size = new Vector2(300, 50) };

        layout = new SGroup() { Background = Color.clear, Size = container.Size, AutoLayoutVerticalStretch = false };
        layout.Children.Add(new SRect(Color.clear) { Size = Vector2.zero.WithX(8) });
        layout.Children.Add(icon);
        layout.Children.Add(text);
        layout.AutoLayout = (SGroup g) => new Action<int, SElement>(g.AutoLayoutHorizontal);
        container.Children.Add(layout);

        if (addImmediately)
            HUDController.Instance.AddHUDElement(name,this);
    }

    public void Activate()
    {
        this.active = true;
    }


    public void Deactivate()
    {
        this.active = false;
    }

    public bool Update()
    {
        this.timeSinceLastUpdate += BraveTime.DeltaTime;
        if (this.timeSinceLastUpdate >= this.updateFreq && this.updater != null)
        {
            this.timeSinceLastUpdate = 0.0f;
            this.updater(this);
        }
        text.Update();
        return true;
    }
}
