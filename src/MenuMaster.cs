namespace CwaffingTheGungy;

/* Major API stuff to be done, from highest to lowest priority
    - fix placement of mod options menu item on pre-options page
    - store status of checkboxes and arrowboxes to persistent storage
    - load status of checkboxes and arrowboxes from persistent storage
    - create actual API surface
    - clean up code

   Minor issues I'm not worrying about now, from highest to lowest priority
    - can't back out of one level of menus at a time (look into CloseAndMaybeApplyChangesWithPrompt)
    - allow adding descriptions to arrowBoxes
    - changing padding on standalone labels
    - dynamically enabling / disabling options

   Nitpicks I really don't care to fix at all, but should be aware of:
    - can't have first item of submenu be a label or it doesn't get focused correctly
    - using magic numbers in a few places to fix panel offsets
*/

internal class CustomCheckboxHandler : MonoBehaviour
  { public PropertyChangedEventHandler<bool> onChanged; }

internal class CustomLeftRightArrowHandler : MonoBehaviour
  { public PropertyChangedEventHandler<string> onChanged; }

internal class CustomButtonHandler : MonoBehaviour
  { public Action<dfControl> onClicked; }

public static class MenuMaster
{
    private const string _MOD_MENU_LABEL = "Yo New Button Dropped O:";

    private static bool _DidInitHooks = false;
    private static List<dfControl> _RegisteredTabs = new();

    public static void InitHooksIfNecessary()
    {
      if (_DidInitHooks)
        return;

      // for backing out of one menu at a time -> calls CloseAndMaybeApplyChangesWithPrompt() when escape is pressed
      // new Hook(
      //     typeof(PreOptionsMenuController).GetMethod("ReturnToPreOptionsMenu", BindingFlags.Instance | BindingFlags.Public),
      //     typeof(MenuMaster).GetMethod("ReturnToPreOptionsMenu", BindingFlags.Static | BindingFlags.NonPublic)
      //     );

      // Make sure our menus are loaded in the main menu
      new Hook(
          typeof(MainMenuFoyerController).GetMethod("InitializeMainMenu", BindingFlags.Instance | BindingFlags.Public),
          typeof(MenuMaster).GetMethod("InitializeMainMenu", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Make sure our menus are loaded in game
      new Hook(
          typeof(GameManager).GetMethod("Pause", BindingFlags.Instance | BindingFlags.Public),
          typeof(MenuMaster).GetMethod("Pause", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Make sure our menus appear and disappear properly
      new Hook(
          typeof(FullOptionsMenuController).GetMethod("ToggleToPanel", BindingFlags.Instance | BindingFlags.Public),
          typeof(MenuMaster).GetMethod("ToggleToPanel", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Custom checkbox events
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("HandleCheckboxValueChanged", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(MenuMaster).GetMethod("HandleCheckboxValueChanged", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Custom arrowbox events
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("HandleLeftRightArrowValueChanged", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(MenuMaster).GetMethod("HandleLeftRightArrowValueChanged", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Custom button events
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("DoSelectedAction", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(MenuMaster).GetMethod("DoSelectedAction", BindingFlags.Static | BindingFlags.NonPublic)
          );

      _DidInitHooks = true;
    }

    private static void ReturnToPreOptionsMenu(Action<PreOptionsMenuController> orig, PreOptionsMenuController pm)
    {
      Debug.Log($"PREPAUSE");
      orig(pm);
    }

    private static void InitializeMainMenu(Action<MainMenuFoyerController> orig, MainMenuFoyerController mm)
    {
      if (GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu is PreOptionsMenuController preOptions)
        if (!preOptions.m_panel.Find<dfButton>(_MOD_MENU_LABEL))
          RebuildOptionsPanels();
      orig(mm);
    }

    private static void Pause(Action<GameManager> orig, GameManager gm)
    {
      if (GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu is PreOptionsMenuController preOptions)
        if (!preOptions.m_panel.Find<dfButton>(_MOD_MENU_LABEL))
          RebuildOptionsPanels();
      orig(gm);
    }

    private static void ToggleToPanel(Action<FullOptionsMenuController, dfScrollPanel, bool> orig, FullOptionsMenuController controller, dfScrollPanel targetPanel, bool doFocus)
    {
      ETGModConsole.Log($"toggling to panel {targetPanel.name}");
      bool isOurPanel = false;
      foreach (dfControl tab in _RegisteredTabs)
      {
        bool match = (tab == targetPanel);  // need to cache this because tab.IsVisible property doesn't return as expected
        ETGModConsole.Log($"  matches {tab.name}? {match}");
        tab.IsVisible = match;
        isOurPanel |= match;
      }
      orig(controller, targetPanel, doFocus);
      if (isOurPanel)
        targetPanel.controls.First().HighlightChildrenAndFocus();  // fix bug where first item isn't highlighted
    }

    private static void HandleCheckboxValueChanged(Action<BraveOptionsMenuItem> orig, BraveOptionsMenuItem item)
    {
      orig(item);
      if (item.GetComponent<CustomCheckboxHandler>() is CustomCheckboxHandler handler)
        handler.onChanged(item.m_self, item.m_selectedIndex == 1);
    }

    private static void HandleLeftRightArrowValueChanged(Action<BraveOptionsMenuItem> orig, BraveOptionsMenuItem item)
    {
      orig(item);
      if (item.GetComponent<CustomLeftRightArrowHandler>() is CustomLeftRightArrowHandler handler)
        handler.onChanged(item.m_self, item.labelOptions[item.m_selectedIndex]);
    }

    private static void DoSelectedAction(Action<BraveOptionsMenuItem> orig, BraveOptionsMenuItem item)
    {
      orig(item);
      if (item.GetComponent<CustomButtonHandler>() is CustomButtonHandler handler)
        handler.onClicked(item.m_self);
    }

    private static void FocusControl(dfControl control, dfMouseEventArgs args)
    {
      control.Focus();
    }

    // TODO: make this more selective about when it plays
    private static void PlayMenuCursorSound(dfControl control)
    {
      AkSoundEngine.PostEvent("Play_UI_menu_select_01", control.gameObject);
    }

    private static void PlayMenuCursorSound(dfControl control, dfMouseEventArgs args)
    {
      PlayMenuCursorSound(control);
    }

    private static void PlayMenuCursorSound(dfControl control, dfFocusEventArgs args)
    {
      PlayMenuCursorSound(control);
    }

    // TODO: issues caching this, so build it fresh each time
    internal static dfButton GetPrototypeRawButton()
    {
      return GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu.m_panel.Find<dfButton>("AudioTab (1)");
    }

    internal static dfPanel GetPrototypeCheckboxWrapperPanel()
    {
      return GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo.Find<dfPanel>("V-SyncCheckBoxPanel");
    }

    internal static dfPanel GetPrototypeCheckboxInnerPanel()
    {
      return GetPrototypeCheckboxWrapperPanel().Find<dfPanel>("Panel");
    }

    internal static dfCheckbox GetPrototypeCheckbox()
    {
      return GetPrototypeCheckboxInnerPanel().Find<dfCheckbox>("Checkbox");
    }

    internal static dfSprite GetPrototypeEmptyCheckboxSprite()
    {
      return GetPrototypeCheckbox().Find<dfSprite>("EmptyCheckbox");
    }

    internal static dfSprite GetPrototypeCheckedCheckboxSprite()
    {
      return GetPrototypeCheckbox().Find<dfSprite>("CheckedCheckbox");
    }

    internal static dfLabel GetPrototypeCheckboxLabel()
    {
      return GetPrototypeCheckboxInnerPanel().Find<dfLabel>("CheckboxLabel");
    }

    internal static dfPanel GetPrototypeLeftRightWrapperPanel()
    {
      return GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo.Find<dfPanel>("VisualPresetArrowSelectorPanel");
    }

    internal static dfPanel GetPrototypeLeftRightInnerPanel()
    {
      return GetPrototypeLeftRightWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    }

    internal static dfLabel GetPrototypeLeftRightPanelLabel()
    {
      return GetPrototypeLeftRightInnerPanel().Find<dfLabel>("OptionsArrowSelectorLabel");
    }

    internal static dfSprite GetPrototypeLeftRightPanelLeftSprite()
    {
      return GetPrototypeLeftRightInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowLeft");
    }

    internal static dfSprite GetPrototypeLeftRightPanelRightSprite()
    {
      return GetPrototypeLeftRightInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowRight");
    }

    internal static dfLabel GetPrototypeLeftRightPanelSelection()
    {
      return GetPrototypeLeftRightInnerPanel().Find<dfLabel>("OptionsArrowSelectorSelection");
    }

    internal static dfPanel GetPrototypeButtonWrapperPanel()
    {
      return GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabControls.Find<dfPanel>("EditKeyboardBindingsButtonPanel");
    }

    internal static dfPanel GetPrototypeButtonInnerPanel()
    {
      return GetPrototypeButtonWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    }

    internal static dfButton GetPrototypeButton()
    {
      return GetPrototypeButtonInnerPanel().Find<dfButton>("EditKeyboardBindingsButton");
    }

    internal static dfPanel GetPrototypeLabelWrapperPanel()
    {
      return GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabControls.Find<dfPanel>("PlayerOneLabelPanel");
    }

    internal static dfPanel GetPrototypeLabelInnerPanel()
    {
      return GetPrototypeLabelWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    }

    internal static dfLabel GetPrototypeLabel()
    {
      return GetPrototypeLabelInnerPanel().Find<dfLabel>("Label");
    }

    public static void PrintControlRecursive(dfControl control, string indent = "->", bool dissect = false)
    {
        System.Console.WriteLine($"  {indent} control with name={control.name}, type={control.GetType()}, position={control.Position}, relposition={control.RelativePosition}, size={control.Size}, anchor={control.Anchor}, pivot={control.Pivot}");
        if (dissect)
          Dissect.DumpFieldsAndProperties(control);
        foreach (dfControl child in control.controls)
            PrintControlRecursive(child, "--"+indent);
    }

    public static void CopyAttributes<T>(this T self, T other) where T : dfControl
    {
      if (self is dfButton button && other is dfButton otherButton)
      {
        button.Atlas                  = otherButton.Atlas;
        button.ClickWhenSpacePressed  = otherButton.ClickWhenSpacePressed;
        button.State                  = otherButton.State;
        button.PressedSprite          = otherButton.PressedSprite;
        button.ButtonGroup            = otherButton.ButtonGroup;
        button.AutoSize               = otherButton.AutoSize;
        button.TextAlignment          = otherButton.TextAlignment;
        button.VerticalAlignment      = otherButton.VerticalAlignment;
        button.Padding                = otherButton.Padding;
        button.Font                   = otherButton.Font;
        button.Text                   = otherButton.Text;
        button.TextColor              = otherButton.TextColor;
        button.HoverTextColor         = otherButton.HoverTextColor;
        button.NormalBackgroundColor  = otherButton.NormalBackgroundColor;
        button.HoverBackgroundColor   = otherButton.HoverBackgroundColor;
        button.PressedTextColor       = otherButton.PressedTextColor;
        button.PressedBackgroundColor = otherButton.PressedBackgroundColor;
        button.FocusTextColor         = otherButton.FocusTextColor;
        button.FocusBackgroundColor   = otherButton.FocusBackgroundColor;
        button.DisabledTextColor      = otherButton.DisabledTextColor;
        button.TextScale              = otherButton.TextScale;
        button.TextScaleMode          = otherButton.TextScaleMode;
        button.WordWrap               = otherButton.WordWrap;
        button.Shadow                 = otherButton.Shadow;
        button.ShadowColor            = otherButton.ShadowColor;
        button.ShadowOffset           = otherButton.ShadowOffset;
      }
      if (self is dfPanel panel && other is dfPanel otherPanel)
      {
        panel.Atlas            = otherPanel.Atlas;
        panel.BackgroundSprite = otherPanel.BackgroundSprite;
        panel.BackgroundColor  = otherPanel.BackgroundColor;
        panel.Padding          = otherPanel.Padding;
      }
      // TODO: this probably needs to be set up manually
      if (self is dfCheckbox checkbox && other is dfCheckbox otherCheckbox)
      {
        // IsChecked
        // CheckIcon
        // Label
        // GroupContainer
      }
      if (self is dfSprite sprite && other is dfSprite otherSprite)
      {
        sprite.Atlas         = otherSprite.Atlas;
        sprite.SpriteName    = otherSprite.SpriteName;
        sprite.Flip          = otherSprite.Flip;
        sprite.FillDirection = otherSprite.FillDirection;
        sprite.FillAmount    = otherSprite.FillAmount;
        sprite.InvertFill    = otherSprite.InvertFill;
      }
      if (self is dfLabel label && other is dfLabel otherLabel)
      {
        label.Atlas             = otherLabel.Atlas;
        label.Font              = otherLabel.Font;
        label.BackgroundSprite  = otherLabel.BackgroundSprite;
        label.BackgroundColor   = otherLabel.BackgroundColor;
        label.TextScale         = otherLabel.TextScale;
        label.TextScaleMode     = otherLabel.TextScaleMode;
        label.CharacterSpacing  = otherLabel.CharacterSpacing;
        label.ColorizeSymbols   = otherLabel.ColorizeSymbols;
        label.ProcessMarkup     = true; // always want this to be true
        label.ShowGradient      = otherLabel.ShowGradient;
        label.BottomColor       = otherLabel.BottomColor;
        label.Text              = otherLabel.Text;
        label.AutoSize          = otherLabel.AutoSize;
        label.AutoHeight        = otherLabel.AutoHeight;
        label.WordWrap          = otherLabel.WordWrap;
        label.TextAlignment     = otherLabel.TextAlignment;
        label.VerticalAlignment = otherLabel.VerticalAlignment;
        label.Outline           = otherLabel.Outline;
        label.OutlineSize       = otherLabel.OutlineSize;
        label.OutlineColor      = otherLabel.OutlineColor;
        label.Shadow            = otherLabel.Shadow;
        label.ShadowColor       = otherLabel.ShadowColor;
        label.ShadowOffset      = otherLabel.ShadowOffset;
        label.Padding           = otherLabel.Padding;
        label.TabSize           = otherLabel.TabSize;
      }
      self.AllowSignalEvents = other.AllowSignalEvents;
      self.MinimumSize       = other.MinimumSize;
      self.MaximumSize       = other.MaximumSize;
      // self.ZOrder            = other.ZOrder;  // don't set this or children won't be processed in the order we add them
      self.TabIndex          = other.TabIndex;
      self.IsInteractive     = other.IsInteractive;
      self.Pivot             = other.Pivot;
      // self.Position          = other.Position;  // not sure this actually matters, as long as we set RelativePosition
      self.RelativePosition  = other.RelativePosition;
      self.HotZoneScale      = other.HotZoneScale;
      self.useGUILayout      = other.useGUILayout;
      self.Color             = other.Color;
      self.DisabledColor     = other.DisabledColor;
      self.Anchor            = other.Anchor;
      self.CanFocus          = other.CanFocus;
      self.AutoFocus         = other.AutoFocus;
      self.Size              = other.Size;
      self.Opacity           = other.Opacity;
      self.enabled           = other.enabled;

      self.renderOrder       = other.renderOrder;
      self.isControlClipped  = other.isControlClipped;

      // self.Anchor            = dfAnchorStyle.CenterHorizontal | dfAnchorStyle.CenterVertical;

      // self.Invalidate();
      // self.ResetLayout();
      // self.PerformLayout();
      // self.Hide();
      // self.Disable();
    }

    public static dfButton InsertRawButton(this dfPanel panel, string previousButtonName, string label, MouseEventHandler onclick)
    {
        dfButton prevButton = panel.Find<dfButton>(previousButtonName);
        if (prevButton == null)
        {
          ETGModConsole.Log($"  no button {previousButtonName} to clone");
          return null; //
        }
        dfControl nextButton = prevButton.GetComponent<UIKeyControls>().down;

        dfButton newButton = panel.AddControl<dfButton>();
        newButton.CopyAttributes(GetPrototypeRawButton());
        newButton.Text = label;
        newButton.name = label;
        newButton.RelativePosition = newButton.RelativePosition + new Vector3(300.0f, 10.0f, 0.0f);
        newButton.Click += onclick;
        newButton.MouseEnter += FocusControl;
        newButton.GotFocus += PlayMenuCursorSound;

        UIKeyControls uikeys = newButton.gameObject.AddComponent<UIKeyControls>();
        uikeys.button                                 = newButton;
        uikeys.selectOnAction                         = true;
        uikeys.clearRepeatingOnSelect                 = true;
        uikeys.up                                     = prevButton;
        uikeys.down                                   = nextButton;
        prevButton.GetComponent<UIKeyControls>().down = newButton;
        nextButton.GetComponent<UIKeyControls>().up   = newButton;
        // uikeys.OnNewControlSelected                  += PlayMenuCursorSound;

        newButton.Invalidate();
        newButton.BringToFront();
        newButton.ForceUpdateCachedParentTransform();
        newButton.Show();
        newButton.Enable();
        newButton.ForceUpdateCachedParentTransform();
        newButton.Invalidate();
        panel.ResetLayout(true, true);
        panel.PerformLayout();
        panel.Disable();
        panel.Enable();

        return newButton;
    }

    public static dfScrollPanel NewOptionsPanel(string name)
    {
      // Get a reference options panel
      dfScrollPanel refPanel = GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo;

      // Add our options panel to the PauseMenuController and copy some basic attributes from our reference
      dfScrollPanel newPanel = GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.m_panel.AddControl<dfScrollPanel>();
        newPanel.UseScrollMomentum    = refPanel.UseScrollMomentum;
        newPanel.ScrollWithArrowKeys  = refPanel.ScrollWithArrowKeys;
        newPanel.Atlas                = refPanel.Atlas;
        newPanel.BackgroundSprite     = refPanel.BackgroundSprite;
        newPanel.BackgroundColor      = refPanel.BackgroundColor;
        newPanel.ScrollPadding        = refPanel.ScrollPadding;
        newPanel.WrapLayout           = refPanel.WrapLayout;
        newPanel.FlowDirection        = refPanel.FlowDirection;
        newPanel.FlowPadding          = refPanel.FlowPadding;
        newPanel.ScrollPosition       = refPanel.ScrollPosition;
        newPanel.ScrollWheelAmount    = refPanel.ScrollWheelAmount;
        newPanel.HorzScrollbar        = refPanel.HorzScrollbar;
        newPanel.VertScrollbar        = refPanel.VertScrollbar;
        newPanel.WheelScrollDirection = refPanel.WheelScrollDirection;
        newPanel.UseVirtualScrolling  = refPanel.UseVirtualScrolling;
        newPanel.VirtualScrollingTile = refPanel.VirtualScrollingTile;
        newPanel.CanFocus             = refPanel.CanFocus;
        newPanel.AllowSignalEvents    = refPanel.AllowSignalEvents;
        newPanel.IsEnabled            = refPanel.IsEnabled;
        newPanel.IsVisible            = refPanel.IsVisible;
        newPanel.IsInteractive        = refPanel.IsInteractive;
        newPanel.Tooltip              = refPanel.Tooltip;
        newPanel.Anchor               = refPanel.Anchor;
        newPanel.Opacity              = refPanel.Opacity;
        newPanel.Color                = refPanel.Color;
        newPanel.DisabledColor        = refPanel.DisabledColor;
        newPanel.Pivot                = refPanel.Pivot;
        // newPanel.Size                 = refPanel.Size;  // don't set size manually since we want to clip
        newPanel.Width                = refPanel.Width;
        newPanel.Height               = refPanel.Height;
        newPanel.MinimumSize          = refPanel.MinimumSize;
        newPanel.MaximumSize          = refPanel.MaximumSize;
        // newPanel.ZOrder               = refPanel.ZOrder;
        newPanel.TabIndex             = refPanel.TabIndex;
        newPanel.ClipChildren         = refPanel.ClipChildren;
        newPanel.InverseClipChildren  = refPanel.InverseClipChildren;
        // newPanel.Tag               = refPanel.Tag;
        newPanel.IsLocalized          = refPanel.IsLocalized;
        newPanel.HotZoneScale         = refPanel.HotZoneScale;
        newPanel.useGUILayout         = refPanel.useGUILayout;
        newPanel.AutoFocus            = refPanel.AutoFocus;
        newPanel.AutoLayout           = refPanel.AutoLayout;
        newPanel.AutoReset            = refPanel.AutoReset;
        newPanel.AutoScrollPadding    = refPanel.AutoScrollPadding;
        newPanel.AutoFitVirtualTiles  = refPanel.AutoFitVirtualTiles;
        newPanel.AutoFitVirtualTiles  = refPanel.AutoFitVirtualTiles;
        // newPanel.enabled = refPanel.enabled;

      // Set up a few additional variables to suit our needs
      newPanel.ClipChildren         = true;
      newPanel.InverseClipChildren  = true;
      newPanel.ScrollPadding        = new RectOffset(0,0,0,0);
      newPanel.AutoScrollPadding    = new RectOffset(0,0,0,0);
      newPanel.Size                -= new Vector2(0, 50f);  //TODO: figure out why this offset is wrong in the first place
      newPanel.Position            -= new Vector3(0, 50f, 0f);  //TODO: figure out why this offset is wrong in the first place
      // newOptionsPanel.Anchor               = dfAnchorStyle.CenterVertical | dfAnchorStyle.Proportional;
      // newPanel.RelativePosition = new Vector3(100f, 100f, 0f);
      // newPanel.ZOrder               = 4;
      newPanel.name = name;
      newPanel.Enable();

      // Add it to our known panels so we can make visible / invisible as necessary
      _RegisteredTabs.Add(newPanel);

      return newPanel;
    }

    // based on V-SyncCheckBoxPanel
    public static void AddCheckBox(this dfScrollPanel panel, string label, PropertyChangedEventHandler<bool> onchange = null)
    {
      dfPanel newCheckboxWrapperPanel = panel.AddControl<dfPanel>();
      newCheckboxWrapperPanel.CopyAttributes(GetPrototypeCheckboxWrapperPanel());

      dfPanel newCheckboxInnerPanel = newCheckboxWrapperPanel.AddControl<dfPanel>();
      newCheckboxInnerPanel.CopyAttributes(GetPrototypeCheckboxInnerPanel());

      dfCheckbox newCheckbox = newCheckboxInnerPanel.AddControl<dfCheckbox>();
      newCheckbox.CopyAttributes(GetPrototypeCheckbox());

      dfSprite newEmptyCheckboxSprite = newCheckbox.AddControl<dfSprite>();
      newEmptyCheckboxSprite.CopyAttributes(GetPrototypeEmptyCheckboxSprite());

      dfSprite newCheckedCheckboxSprite = newCheckbox.AddControl<dfSprite>();
      newCheckedCheckboxSprite.CopyAttributes(GetPrototypeCheckedCheckboxSprite());

      dfLabel newCheckboxLabel = newCheckboxInnerPanel.AddControl<dfLabel>();
      newCheckboxLabel.CopyAttributes(GetPrototypeCheckboxLabel());

      newCheckboxLabel.Text = label;

      BraveOptionsMenuItem menuItem = newCheckboxWrapperPanel.gameObject.AddComponent<BraveOptionsMenuItem>();
        menuItem.optionType           = BraveOptionsMenuItem.BraveOptionsOptionType.NONE;
        menuItem.itemType             = BraveOptionsMenuItem.BraveOptionsMenuItemType.Checkbox;
        menuItem.labelControl         = newCheckboxLabel;
        menuItem.selectedLabelControl = null;
        menuItem.infoControl          = null;
        menuItem.fillbarControl       = null;
        menuItem.buttonControl        = null;
        menuItem.checkboxChecked      = newCheckedCheckboxSprite;
        menuItem.checkboxUnchecked    = newEmptyCheckboxSprite;
        menuItem.labelOptions         = null;
        menuItem.infoOptions          = null;
        menuItem.up                   = null;
        menuItem.down                 = null;
        menuItem.left                 = null;
        menuItem.right                = null;
        menuItem.selectOnAction       = true;
        menuItem.OnNewControlSelected = null;

      menuItem.checkboxChecked.IsVisible = menuItem.m_selectedIndex == 1;

      newCheckboxWrapperPanel.MouseEnter += FocusControl;
      newCheckboxWrapperPanel.GotFocus += PlayMenuCursorSound;
      newCheckboxWrapperPanel.name = $"{label} panel";
      panel.RegisterBraveMenuItem(newCheckboxWrapperPanel);
      menuItem.gameObject.AddComponent<CustomCheckboxHandler>().onChanged += onchange;
    }

    // based on VisualPresetArrowSelectorPanel
    public static void AddArrowBox(this dfScrollPanel panel, string label, List<string> options, PropertyChangedEventHandler<string> onchange = null)
    {
      dfPanel newArrowboxWrapperPanel = panel.AddControl<dfPanel>();
      newArrowboxWrapperPanel.CopyAttributes(GetPrototypeLeftRightWrapperPanel());
      newArrowboxWrapperPanel.Anchor = dfAnchorStyle.CenterVertical | dfAnchorStyle.CenterHorizontal;
      // newArrowboxWrapperPanel.Size += new Vector2(0, 4);  // TODO: fixes clipping near the bottom of the scroll box, but really shouldn't be necessary

      dfPanel newArrowboxInnerPanel = newArrowboxWrapperPanel.AddControl<dfPanel>();
      newArrowboxInnerPanel.CopyAttributes(GetPrototypeLeftRightInnerPanel());

      dfLabel newArrowSelectorLabel = newArrowboxInnerPanel.AddControl<dfLabel>();
      newArrowSelectorLabel.CopyAttributes(GetPrototypeLeftRightPanelLabel());

      dfLabel newArrowSelectorSelection = newArrowboxInnerPanel.AddControl<dfLabel>();
      newArrowSelectorSelection.CopyAttributes(GetPrototypeLeftRightPanelSelection());
      // newArrowSelectorSelection.RelativePosition += new Vector3(0, -10f, 0); // TODO: figure out why this offset is wrong in the first place

      dfSprite newArrowLeftSprite = newArrowboxInnerPanel.AddControl<dfSprite>();
      newArrowLeftSprite.CopyAttributes(GetPrototypeLeftRightPanelLeftSprite());

      dfSprite newArrowRightSprite = newArrowboxInnerPanel.AddControl<dfSprite>();
      newArrowRightSprite.CopyAttributes(GetPrototypeLeftRightPanelRightSprite());

      newArrowSelectorLabel.Text = label;
      newArrowSelectorSelection.Text = options[0];

      BraveOptionsMenuItem menuItem = newArrowboxWrapperPanel.gameObject.AddComponent<BraveOptionsMenuItem>();
        menuItem.optionType           = BraveOptionsMenuItem.BraveOptionsOptionType.NONE;
        menuItem.itemType             = BraveOptionsMenuItem.BraveOptionsMenuItemType.LeftRightArrow;
        menuItem.labelControl         = newArrowSelectorLabel;
        menuItem.selectedLabelControl = newArrowSelectorSelection/*null*/;
        menuItem.infoControl          = null;  // TODO: allow info boxes
        menuItem.fillbarControl       = null;
        menuItem.buttonControl        = null;
        menuItem.checkboxChecked      = null;
        menuItem.checkboxUnchecked    = null;
        menuItem.labelOptions         = options.ToArray();
        menuItem.infoOptions          = null;  // TODO: allow info boxes
        menuItem.up                   = null;
        menuItem.down                 = null;
        menuItem.left                 = newArrowLeftSprite;
        menuItem.right                = newArrowRightSprite;
        menuItem.selectOnAction       = true;
        menuItem.OnNewControlSelected = null;

      newArrowboxWrapperPanel.MouseEnter += FocusControl;
      newArrowboxWrapperPanel.GotFocus += PlayMenuCursorSound;
      newArrowboxWrapperPanel.name = $"{label} panel";
      panel.RegisterBraveMenuItem(newArrowboxWrapperPanel);
      menuItem.gameObject.AddComponent<CustomLeftRightArrowHandler>().onChanged += onchange;
    }

    // based on EditKeyboardBindingsButtonPanel
    public static void AddButton(this dfScrollPanel panel, string label, Action<dfControl> onclick = null)
    {
      dfPanel newButtonWrapperPanel = panel.AddControl<dfPanel>();
      newButtonWrapperPanel.CopyAttributes(GetPrototypeButtonWrapperPanel());

      dfPanel newButtonInnerPanel = newButtonWrapperPanel.AddControl<dfPanel>();
      newButtonInnerPanel.CopyAttributes(GetPrototypeButtonInnerPanel());

      dfButton newButton = newButtonInnerPanel.AddControl<dfButton>();
      newButton.CopyAttributes(GetPrototypeButton());

      newButton.Text = label;

      BraveOptionsMenuItem menuItem = newButtonWrapperPanel.gameObject.AddComponent<BraveOptionsMenuItem>();
        menuItem.optionType           = BraveOptionsMenuItem.BraveOptionsOptionType.NONE;
        menuItem.itemType             = BraveOptionsMenuItem.BraveOptionsMenuItemType.Button;
        menuItem.labelControl         = null;
        menuItem.selectedLabelControl = null;
        menuItem.infoControl          = null;
        menuItem.fillbarControl       = null;
        menuItem.buttonControl        = newButton;
        menuItem.checkboxChecked      = null;
        menuItem.checkboxUnchecked    = null;
        menuItem.labelOptions         = null;
        menuItem.infoOptions          = null;
        menuItem.up                   = null;
        menuItem.down                 = null;
        menuItem.left                 = null;
        menuItem.right                = null;
        menuItem.selectOnAction       = true;
        menuItem.OnNewControlSelected = null;

      newButtonWrapperPanel.MouseEnter += FocusControl;
      newButtonWrapperPanel.GotFocus += PlayMenuCursorSound;
      newButtonWrapperPanel.name = $"{label} panel";
      panel.RegisterBraveMenuItem(newButtonWrapperPanel);
      menuItem.gameObject.AddComponent<CustomButtonHandler>().onClicked += onclick;
    }

    // based on PlayerOneLabelPanel
    public static void AddLabel(this dfScrollPanel panel, string label, Color? color = null)
    {
      dfPanel newLabelWrapperPanel = panel.AddControl<dfPanel>();
      newLabelWrapperPanel.CopyAttributes(GetPrototypeLabelWrapperPanel());

      dfPanel newLabelInnerPanel = newLabelWrapperPanel.AddControl<dfPanel>();
      newLabelInnerPanel.CopyAttributes(GetPrototypeLabelInnerPanel());

      dfLabel newLabel = newLabelInnerPanel.AddControl<dfLabel>();
      newLabel.CopyAttributes(GetPrototypeLabel());

      newLabel.Text = label;
      newLabel.Color = color ?? Color.white;

      newLabelWrapperPanel.name = $"{label} panel";
    }

    public static void RegisterBraveMenuItem(this dfScrollPanel panel, dfControl item)
    {
      if (panel.controls == null || panel.controls.Count < 2) // includes this object
        return;
      BraveOptionsMenuItem menuItem = item.GetComponent<BraveOptionsMenuItem>();
      for (int prevItemIndex = panel.Controls.Count - 2; prevItemIndex >= 0; prevItemIndex--)
      {
        dfControl prevItem = panel.controls[prevItemIndex];
        if (prevItem.GetComponent<BraveOptionsMenuItem>() is not BraveOptionsMenuItem prevMenuItem)
          continue;
        menuItem.up = prevItem;
        prevMenuItem.down = item;
        break;
      }
    }

    private static void HighlightChildrenAndFocus(this dfControl root, bool canFocus = true)
    {
      root.Color = Color.white;
      if (root is dfButton button)
        button.TextColor = Color.white;

      root.canFocus  = canFocus;
      root.AutoFocus = true;
      foreach (dfControl child in root.controls)
        child.HighlightChildrenAndFocus(canFocus: false);
    }

    public static void Finalize(this dfScrollPanel panel)
    {
      panel.controls.Last().Height += 16f; // fix a weird clipping issue for arrowboxes at the bottom
      panel.PerformLayout();  // register all changes
    }

    public static void RebuildOptionsPanels()
    {
        if (GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu is not PreOptionsMenuController preOptions)
          return;

        System.Diagnostics.Stopwatch tempWatchWatch = System.Diagnostics.Stopwatch.StartNew();

        // Clear out all registered UI tabs, since we need to build everything fresh
        _RegisteredTabs.Clear();

        // Create the new modded options panel and add a few test items
        dfScrollPanel newOptionsPanel = NewOptionsPanel("modded options");

          // newOptionsPanel.AddLabel(label: $"First Label", color: new Color(1.0f, 0.75f, 0.75f));  // TODO: we can't have a label as the first item since it can't be focused -> vanilla oversight
          // Add a subpanel
          dfScrollPanel subOptionsPanel = NewOptionsPanel("secret modded options");
            for (int i = 1; i <= 2; ++i)
            {
              subOptionsPanel.AddButton(label: $"Secret Align {i}", onclick: (control) => {
                ETGModConsole.Log($"secret clikin on {control.name}");
              });
              subOptionsPanel.AddCheckBox(label: $"Secret Align {i}", onchange:  (control, boolValue) => {
                ETGModConsole.Log($"secret checkeroo {boolValue} on {control.name}");
              });
              subOptionsPanel.AddArrowBox(label: $"Secret Align {i}", options: new(){$"Align {i}", "^O^", ">>>o<<<", "LOOOOOOOOOOOOOOOOOOOOOOONG"}, onchange:  (control, stringValue) => {
                ETGModConsole.Log($"secret arrowboi {stringValue} on {control.name}");
              });
            }
          subOptionsPanel.Finalize();
          // Add our subpanel to our main panel
          newOptionsPanel.AddButton(label: $"Secret Menu O:", onclick: (control) => {
            ETGModConsole.Log($"entered secret options menu");
            preOptions.ToggleToPanel(subOptionsPanel, true, force: true); // force true so it works even if it's invisible
          });

          // Add some normal options
          for (int i = 1; i <= 5; ++i)
          {
            newOptionsPanel.AddLabel(label: $"Secret Label {i}", color: new Color(0.75f, 1.0f, 0.75f));
            newOptionsPanel.AddButton(label: $"Align {i}", onclick: (control) => {
              ETGModConsole.Log($"clikin on {control.name}");
            });
            newOptionsPanel.AddCheckBox(label: $"Align {i}", onchange:  (control, boolValue) => {
              ETGModConsole.Log($"checkeroo {boolValue} on {control.name}");
            });
            newOptionsPanel.AddArrowBox(label: $"Align {i}", options: new(){$"Align {i}", "^O^", ">>>o<<<"}, onchange:  (control, stringValue) => {
              ETGModConsole.Log($"arrowboi {stringValue} on {control.name}");
            });
          }
        newOptionsPanel.Finalize();

        // Register the new button on the PreOptions menu
        dfButton newButton = preOptions.m_panel.InsertRawButton(previousButtonName: "AudioTab (1)", label: _MOD_MENU_LABEL, onclick: (control, args) => {
          ETGModConsole.Log($"entered modded options menu");
          preOptions.ToggleToPanel(newOptionsPanel, true, force: true); // force true so it works even if it's invisible
        });

        // Dissect.DumpFieldsAndProperties<dfScrollPanel>(newOptionsPanel);
        // PrintControlRecursive(newOptionsPanel);
        tempWatchWatch.Stop(); System.Console.WriteLine($"    panel built in {tempWatchWatch.ElapsedMilliseconds} milliseconds");
    }
}