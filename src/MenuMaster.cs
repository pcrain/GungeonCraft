namespace CwaffingTheGungy;

/* Major API stuff to be done, from highest to lowest priority
    - store status of checkboxes and arrowboxes to persistent storage
    - load status of checkboxes and arrowboxes from persistent storage
    - create actual API surface
    - clean up code

   Minor issues I'm not worrying about now, from highest to lowest priority
    - changing padding on standalone labels / arrow boxes / info boxes

   Nitpicks I really don't care to fix at all, but should be aware of:
    - can't colorize info boxes
    - can't back out of one level of menus at a time (vanilla behavior; maybe hook CloseAndMaybeApplyChangesWithPrompt)
    - haven't implemented progress / fill bars
    - can't dynamically enable / disable options
    - can't have first item of submenu be a label or it doesn't get focused correctly
    - using magic numbers in a few places to fix panel offsets
*/

public static class MenuMaster
{
    internal const string _GUNFIG_EXTENSION        = "gunfig";

    private const string _MOD_MENU_LABEL           = "MOD CONFIG";
    private static bool _DidInitHooks              = false;
    private static List<dfControl> _RegisteredTabs = new();

    internal class CustomCheckboxHandler : MonoBehaviour
      { public PropertyChangedEventHandler<bool> onChanged; }

    internal class CustomLeftRightArrowHandler : MonoBehaviour
      { public PropertyChangedEventHandler<string> onChanged; }

    internal class CustomButtonHandler : MonoBehaviour
      { public Action<dfControl> onClicked; }

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

      // Custom infobox updates
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("UpdateInfoControl", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(MenuMaster).GetMethod("UpdateInfoControl", BindingFlags.Static | BindingFlags.NonPublic)
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

      // Update config options when menu choices are cancelled
      new Hook(
          typeof(FullOptionsMenuController).GetMethod("CloseAndRevertChanges", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(ModConfigOption).GetMethod("OnMenuCancel", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Update config options when menu choices are confirmed
      new Hook(
          typeof(FullOptionsMenuController).GetMethod("CloseAndApplyChanges", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(ModConfigOption).GetMethod("OnMenuConfirm", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Update config options when a new run is started TODO: not implemented
      // new Hook(
      //     typeof(___).GetMethod("___", BindingFlags.Instance | BindingFlags.NonPublic),
      //     typeof(ModConfigOption).GetMethod("OnNextRun", BindingFlags.Static | BindingFlags.NonPublic)
      //     );

      _DidInitHooks = true;
    }

    private static void ReturnToPreOptionsMenu(Action<PreOptionsMenuController> orig, PreOptionsMenuController pm)
    {
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
      bool isOurPanel = false;
      foreach (dfControl tab in _RegisteredTabs)
      {
        bool match = (tab == targetPanel);  // need to cache this because tab.IsVisible property doesn't return as expected
        tab.IsVisible = match;
        isOurPanel |= match;
      }
      orig(controller, targetPanel, doFocus);
      if (isOurPanel)
        targetPanel.controls.First().HighlightChildrenAndFocus();  // fix bug where first item isn't highlighted
    }

    private static void UpdateInfoControl(Action<BraveOptionsMenuItem> orig, BraveOptionsMenuItem item)
    {
      orig(item);
      if (item.itemType != BraveOptionsMenuItem.BraveOptionsMenuItemType.LeftRightArrowInfo)
        return;
      if (item.optionType != BraveOptionsMenuItem.BraveOptionsOptionType.NONE)
        return;

      item.infoControl.Color = Color.cyan; // TODO: dynamic color
      item.infoControl.Text = item.infoOptions[item.m_selectedIndex];
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

    internal static dfPanel GetPrototypeInfoWrapperPanel()
    {
      return GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo.Find<dfPanel>("ResolutionArrowSelectorPanelWithInfoBox");
    }

    internal static dfPanel GetPrototypeInfoInnerPanel()
    {
      return GetPrototypeInfoWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    }

    internal static dfLabel GetPrototypeInfoPanelLabel()
    {
      return GetPrototypeInfoInnerPanel().Find<dfLabel>("OptionsArrowSelectorLabel");
    }

    internal static dfSprite GetPrototypeInfoPanelLeftSprite()
    {
      return GetPrototypeInfoInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowLeft");
    }

    internal static dfSprite GetPrototypeInfoPanelRightSprite()
    {
      return GetPrototypeInfoInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowRight");
    }

    internal static dfLabel GetPrototypeInfoPanelSelection()
    {
      return GetPrototypeInfoInnerPanel().Find<dfLabel>("OptionsArrowSelectorSelection");
    }

    internal static dfLabel GetPrototypeInfoInfoPanel()
    {
      return GetPrototypeInfoWrapperPanel().Find<dfLabel>("OptionsArrowSelectorInfoLabel");
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

    internal static void PrintControlRecursive(dfControl control, string indent = "->", bool dissect = false)
    {
        System.Console.WriteLine($"  {indent} control with name={control.name}, type={control.GetType()}, position={control.Position}, relposition={control.RelativePosition}, size={control.Size}, anchor={control.Anchor}, pivot={control.Pivot}");
        if (dissect)
          Dissect.DumpFieldsAndProperties(control);
        foreach (dfControl child in control.controls)
            PrintControlRecursive(child, "--"+indent);
    }

    internal static void CopyAttributes<T>(this T self, T other) where T : dfControl
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
    }

    internal static void CreateModConfigButton(this PreOptionsMenuController preOptions, dfScrollPanel newOptionsPanel)
    {
        dfPanel panel        = preOptions.m_panel;
        dfButton prevButton  = panel.Find<dfButton>("AudioTab (1)");
        dfControl nextButton = prevButton.GetComponent<UIKeyControls>().down;

        // Get a list of all buttons in the menu
        List<dfButton> buttonsInMenu  = new();
        foreach (dfControl control in panel.Controls)
          if (control is dfButton button)
            buttonsInMenu.Add(button);

        // Sort them from top to bottom and compute the new gap needed after adding a new button
        buttonsInMenu.Sort((a,b) => (a.Position.y < b.Position.y) ? 1 : -1);
        float minY = buttonsInMenu.First().Position.y;
        float maxY = buttonsInMenu.Last().Position.y;
        float gap  = (maxY - minY) / buttonsInMenu.Count;

        // Shift the buttons up to make room for the new button
        for (int i = 0; i < buttonsInMenu.Count; ++i)
          buttonsInMenu[i].Position = buttonsInMenu[i].Position.WithY(minY + gap * i);

        // Add the new button to the list
        dfButton newButton = panel.AddControl<dfButton>();
        newButton.CopyAttributes(prevButton);
        newButton.Text = _MOD_MENU_LABEL;
        newButton.name = _MOD_MENU_LABEL;
        newButton.Position = newButton.Position.WithY(maxY);  // Add it to the original position of the final button
        newButton.Click += (control, args) => {
          // if (C.DEBUG_BUILD)
          //   ETGModConsole.Log($"entered modded options menu");
          preOptions.ToggleToPanel(newOptionsPanel, true, force: true); // force true so it works even if the pre-options menu is invisible
        };
        newButton.MouseEnter += FocusControl;
        newButton.GotFocus += PlayMenuCursorSound;

        // Add it to the UI
        UIKeyControls uikeys = newButton.gameObject.AddComponent<UIKeyControls>();
        uikeys.button                                 = newButton;
        uikeys.selectOnAction                         = true;
        uikeys.clearRepeatingOnSelect                 = true;
        uikeys.up                                     = prevButton;
        uikeys.down                                   = nextButton;
        prevButton.GetComponent<UIKeyControls>().down = newButton;
        nextButton.GetComponent<UIKeyControls>().up   = newButton;

        // Adjust the layout
        newButton.PerformLayout();
        panel.PerformLayout();
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
        // newPanel.ZOrder               = refPanel.ZOrder; // don't set this or children won't be processed in the order we add them
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

      // Set up a few additional variables to suit our needs
      newPanel.ClipChildren         = true;
      newPanel.InverseClipChildren  = true;
      newPanel.ScrollPadding        = new RectOffset(0,0,0,0);
      newPanel.AutoScrollPadding    = new RectOffset(0,0,0,0);
      newPanel.Size                -= new Vector2(0, 50f);  //TODO: figure out why this offset is wrong in the first place
      newPanel.Position            -= new Vector3(0, 50f, 0f);  //TODO: figure out why this offset is wrong in the first place
      newPanel.name = name;
      newPanel.Enable();

      // Add it to our known panels so we can make visible / invisible as necessary
      _RegisteredTabs.Add(newPanel);

      return newPanel;
    }

    // based on V-SyncCheckBoxPanel
    public static dfPanel AddCheckBox(this dfScrollPanel panel, string label, PropertyChangedEventHandler<bool> onchange = null)
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
        menuItem.checkboxChecked      = newCheckedCheckboxSprite;
        menuItem.checkboxUnchecked    = newEmptyCheckboxSprite;
        menuItem.selectOnAction       = true;

      menuItem.checkboxChecked.IsVisible = menuItem.m_selectedIndex == 1;

      newCheckboxWrapperPanel.MouseEnter += FocusControl;
      newCheckboxWrapperPanel.GotFocus += PlayMenuCursorSound;
      newCheckboxWrapperPanel.name = $"{label} panel";
      panel.RegisterBraveMenuItem(newCheckboxWrapperPanel);
      if (onchange != null)
        menuItem.gameObject.AddComponent<CustomCheckboxHandler>().onChanged += onchange;
      return newCheckboxWrapperPanel;
    }

    // based on VisualPresetArrowSelectorPanel (without info) and ResolutionArrowSelectorPanelWithInfoBox (with info)
    public static dfPanel AddArrowBox(this dfScrollPanel panel, string label, List<string> options, List<string> info = null, PropertyChangedEventHandler<string> onchange = null)
    {
      bool hasInfo = (info != null && info.Count > 0 && info.Count == options.Count);

      dfPanel newArrowboxWrapperPanel = panel.AddControl<dfPanel>();
      newArrowboxWrapperPanel.CopyAttributes(hasInfo ? GetPrototypeInfoWrapperPanel() : GetPrototypeLeftRightWrapperPanel());
      newArrowboxWrapperPanel.Anchor = dfAnchorStyle.CenterVertical | dfAnchorStyle.CenterHorizontal;

      dfPanel newArrowboxInnerPanel = newArrowboxWrapperPanel.AddControl<dfPanel>();
      newArrowboxInnerPanel.CopyAttributes(hasInfo ? GetPrototypeInfoInnerPanel() : GetPrototypeLeftRightInnerPanel());

      dfLabel newArrowSelectorLabel = newArrowboxInnerPanel.AddControl<dfLabel>();
      newArrowSelectorLabel.CopyAttributes(hasInfo ? GetPrototypeInfoPanelLabel() : GetPrototypeLeftRightPanelLabel());

      dfLabel newArrowSelectorSelection = newArrowboxInnerPanel.AddControl<dfLabel>();
      newArrowSelectorSelection.CopyAttributes(hasInfo ? GetPrototypeInfoPanelSelection() : GetPrototypeLeftRightPanelSelection());

      dfSprite newArrowLeftSprite = newArrowboxInnerPanel.AddControl<dfSprite>();
      newArrowLeftSprite.CopyAttributes(hasInfo ? GetPrototypeInfoPanelLeftSprite() : GetPrototypeLeftRightPanelLeftSprite());

      dfSprite newArrowRightSprite = newArrowboxInnerPanel.AddControl<dfSprite>();
      newArrowRightSprite.CopyAttributes(hasInfo ? GetPrototypeInfoPanelRightSprite() : GetPrototypeLeftRightPanelRightSprite());

      dfLabel newArrowInfoLabel = hasInfo ? newArrowboxWrapperPanel.AddControl<dfLabel>() : null;
      newArrowInfoLabel?.CopyAttributes(GetPrototypeInfoInfoPanel());

      newArrowSelectorLabel.Text = label;
      newArrowSelectorSelection.Text = options[0];
      if (newArrowInfoLabel != null)
        newArrowInfoLabel.Text = info[0];

      BraveOptionsMenuItem menuItem = newArrowboxWrapperPanel.gameObject.AddComponent<BraveOptionsMenuItem>();
        menuItem.optionType           = BraveOptionsMenuItem.BraveOptionsOptionType.NONE;
        menuItem.itemType             = hasInfo ? BraveOptionsMenuItem.BraveOptionsMenuItemType.LeftRightArrowInfo : BraveOptionsMenuItem.BraveOptionsMenuItemType.LeftRightArrow;
        menuItem.labelControl         = newArrowSelectorLabel;
        menuItem.selectedLabelControl = newArrowSelectorSelection;
        menuItem.infoControl          = newArrowInfoLabel;
        menuItem.labelOptions         = options.ToArray();
        menuItem.infoOptions          = hasInfo ? info.ToArray() : null;
        menuItem.left                 = newArrowLeftSprite;
        menuItem.right                = newArrowRightSprite;
        menuItem.selectOnAction       = true;

      newArrowboxWrapperPanel.MouseEnter += FocusControl;
      newArrowboxWrapperPanel.GotFocus += PlayMenuCursorSound;
      newArrowboxWrapperPanel.name = $"{label} panel";
      panel.RegisterBraveMenuItem(newArrowboxWrapperPanel);
      if (onchange != null)
        menuItem.gameObject.AddComponent<CustomLeftRightArrowHandler>().onChanged += onchange;
      return newArrowboxWrapperPanel;
    }

    // based on EditKeyboardBindingsButtonPanel
    public static dfPanel AddButton(this dfScrollPanel panel, string label, Action<dfControl> onclick = null)
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
        menuItem.buttonControl        = newButton;
        menuItem.selectOnAction       = true;

      newButtonWrapperPanel.MouseEnter += FocusControl;
      newButtonWrapperPanel.GotFocus += PlayMenuCursorSound;
      newButtonWrapperPanel.name = $"{label} panel";
      panel.RegisterBraveMenuItem(newButtonWrapperPanel);
      if (onclick != null)
        menuItem.gameObject.AddComponent<CustomButtonHandler>().onClicked += onclick;
      return newButtonWrapperPanel;
    }

    // based on PlayerOneLabelPanel
    public static dfPanel AddLabel(this dfScrollPanel panel, string label, Color? color = null)
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

      return newLabelWrapperPanel;
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

    public static void OpenSubMenu(dfScrollPanel panel)
    {
      GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>(
        ).OptionsMenu.PreOptionsMenu.ToggleToPanel(panel, true, force: true); // force true so it works even if it's invisible
    }

    // public static void RebuildOptionsPanelsOld()
    // {
    //     if (GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu is not PreOptionsMenuController preOptions)
    //       return;

    //     System.Diagnostics.Stopwatch panelBuildWatch = System.Diagnostics.Stopwatch.StartNew();

    //     // Clear out all registered UI tabs, since we need to build everything fresh
    //     _RegisteredTabs.Clear();

    //     // Create the new modded options panel and add a few test items
    //     dfScrollPanel newOptionsPanel = NewOptionsPanel("modded options");

    //       // newOptionsPanel.AddLabel(label: $"First Label", color: new Color(1.0f, 0.75f, 0.75f));  // TODO: we can't have a label as the first item since it can't be focused -> vanilla oversight
    //       // Add a subpanel
    //       dfScrollPanel subOptionsPanel = NewOptionsPanel("secret modded options");
    //         for (int i = 1; i <= 2; ++i)
    //         {
    //           subOptionsPanel.AddButton(label: $"Secret Align {i}", onclick: (control) => {
    //             ETGModConsole.Log($"secret clikin on {control.name}");
    //           });
    //           subOptionsPanel.AddCheckBox(label: $"Secret Align {i}", onchange:  (control, boolValue) => {
    //             ETGModConsole.Log($"secret checkeroo {boolValue} on {control.name}");
    //           });
    //           subOptionsPanel.AddArrowBox(label: $"Secret Align {i}", options: new(){$"Align {i}", "^O^", ">>>o<<<", "LOOOOOOOOOOOOOOOOOOOOOOONG"}, onchange:  (control, stringValue) => {
    //             ETGModConsole.Log($"secret arrowboi {stringValue} on {control.name}");
    //           });
    //         }
    //       subOptionsPanel.Finalize();
    //       // Add our subpanel to our main panel
    //       newOptionsPanel.AddButton(label: $"Secret Menu O:", onclick: (control) => {
    //         ETGModConsole.Log($"entered secret options menu");
    //         OpenSubMenu(subOptionsPanel);
    //       });

    //       // Add some normal options
    //       for (int i = 1; i <= 5; ++i)
    //       {
    //         newOptionsPanel.AddLabel(label: $"Secret Label {i}", color: new Color(0.75f, 1.0f, 0.75f));
    //         newOptionsPanel.AddButton(label: $"Button {i}", onclick: (control) => {
    //           ETGModConsole.Log($"clikin on {control.name}");
    //         });
    //         newOptionsPanel.AddCheckBox(label: $"CheckBox {i}", onchange:  (control, boolValue) => {
    //           ETGModConsole.Log($"checkeroo {boolValue} on {control.name}");
    //         });
    //         newOptionsPanel.AddArrowBox(label: $"ArrowBox {i}", options: new(){$"Align {i}", "^O^", ">>>o<<<"}, onchange:  (control, stringValue) => {
    //           ETGModConsole.Log($"arrowboi {stringValue} on {control.name}");
    //         });
    //         newOptionsPanel.AddArrowBox(label: $"InfoBox {i}", options: new(){$"Align {i}", "^O^", ">>>o<<<"}, info: new(){$"hi there C:\nmultiline test\none more for good measure", "how's it going? O:", "what are you up to?"}, onchange:  (control, stringValue) => {
    //           ETGModConsole.Log($"da infobox {stringValue} on {control.name}");
    //         });
    //       }
    //     newOptionsPanel.Finalize();

    //     // Register the new button on the PreOptions menu
    //     preOptions.CreateModConfigButton(newOptionsPanel);

    //     // Dissect.DumpFieldsAndProperties<dfScrollPanel>(newOptionsPanel);
    //     // PrintControlRecursive(newOptionsPanel);
    //     panelBuildWatch.Stop(); System.Console.WriteLine($"    panel built in {panelBuildWatch.ElapsedMilliseconds} milliseconds");
    // }

    private static bool _configBuilt = false;
    private static void OneTimeBuildConfig(this ModConfig config)
    {
      if (_configBuilt)
        return;

      config.AddToggle("testCheck", "Hello there! :D", (_, newVal) => ETGModConsole.Log($"it worked O: {(newVal == "1" ? "on" : "off")}") );
      config.AddLabel("A Label *O*");
      config.AddScrollBox("testScroll", "Look at it Go!", options: new(){"this", "that", "the other"}, info: new(){"good", "bad", "ugly"},
        callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      config.AddButton("testButton", "Click me!", callback: (key, _) => ETGModConsole.Log($"{key} button clicked!"));
      _configBuilt = true;
    }

    public static void RebuildOptionsPanels()
    {
        if (GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu is not PreOptionsMenuController preOptions)
          return;

        System.Diagnostics.Stopwatch panelBuildWatch = System.Diagnostics.Stopwatch.StartNew();

        // Clear out all registered UI tabs, since we need to build everything fresh
        _RegisteredTabs.Clear();

        // Get and build the GungeonCraft Mod Config
        ModConfig.GetConfigForMod("GungeonCraft").OneTimeBuildConfig();

        // Create the new modded options panel
        dfScrollPanel newOptionsPanel = NewOptionsPanel("modded options");

        // Add submenus for each active mod
        foreach (string modName in ModConfig._ActiveConfigs.Keys)
        {
          ModConfig modConfig = ModConfig._ActiveConfigs[modName];
          dfScrollPanel modConfigPage = modConfig.RegenConfigPage();
          newOptionsPanel.AddButton(label: modName, onclick: (control) => {
            // Lazy.DebugLog($"entered {modName} options menu");
            OpenSubMenu(modConfigPage);
          });
        }

        // Finalize the options panel
        newOptionsPanel.Finalize();

        // Register the new button on the PreOptions menu
        preOptions.CreateModConfigButton(newOptionsPanel);

        // Dissect.DumpFieldsAndProperties<dfScrollPanel>(newOptionsPanel);
        // PrintControlRecursive(newOptionsPanel);
        panelBuildWatch.Stop(); System.Console.WriteLine($"    panel built in {panelBuildWatch.ElapsedMilliseconds} milliseconds");
    }
}

public class ModConfig
{
  /* TODO:
      - on startup, need to register a mod to use the config menu
      - on startup, for all registered mods, need to load current configuration from a file if it exists, or create defaults is not
      - need to save configuration to file on menu exit
      - need to revert changes to non-immediate options on menu cancel
  */

  private enum ItemType
  {
    Label,
    Button,
    CheckBox,
    ArrowBox,
  }

  private class ModConfigMenuItem
  {
    internal ItemType                _itemType   = ItemType.Label;
    internal ModConfigOption.Update  _updateType = ModConfigOption.Update.OnConfirm;
    internal string                  _key        = null;
    internal string                  _label      = null;
    internal List<string>            _values     = null;
    internal List<string>            _info       = null;
    internal Action<string, string>  _callback   = null;
  }

  internal static Dictionary<string, ModConfig> _ActiveConfigs = new(); // dictionary of all mods using ModConfig to their respective configurations
  private Dictionary<string, string> _options = new(); // dictionary of mod options as key value pairs
  private List<ModConfigMenuItem> _registeredOptions = new(); // list of options from which we can dynamically regenerate the options panel

  private bool _dirty = false; // whether we've been changed since last saving to disk
  private string _configFile = null; // the file on disk to which we're writing
  private string _modName = null;

  public void AddToggle(string key, string label, Action<string, string> callback, ModConfigOption.Update updateType = ModConfigOption.Update.OnConfirm)
  {
    this._registeredOptions.Add(new ModConfigMenuItem(){
      _itemType   = ItemType.CheckBox,
      _updateType = updateType,
      _key        = key,
      _label      = label,
      _callback   = callback,
    });
  }

  public void AddScrollBox(string key, string label, List<string> options, Action<string, string> callback, List<string> info = null, ModConfigOption.Update updateType = ModConfigOption.Update.OnConfirm)
  {
    this._registeredOptions.Add(new ModConfigMenuItem(){
      _itemType   = ItemType.ArrowBox,
      _updateType = updateType,
      _key        = key,
      _label      = label,
      _callback   = callback,
      _values     = options,
      _info       = info,
    });
  }

  public void AddButton(string key, string label, Action<string, string> callback)
  {
    this._registeredOptions.Add(new ModConfigMenuItem(){
      _itemType   = ItemType.Button,
      _updateType = ModConfigOption.Update.Immediate,
      _key        = key,
      _label      = label,
      _callback   = callback,
    });
  }

  public void AddLabel(string label)
  {
    this._registeredOptions.Add(new ModConfigMenuItem(){
      _itemType   = ItemType.Label,
      _updateType = ModConfigOption.Update.Immediate,
      _key        = $"{label} label",
      _label      = label,
      _callback   = null,
    });
  }

  public static ModConfig GetConfigForMod(string modName)
  {
    if (!_ActiveConfigs.ContainsKey(modName))
    {
      Lazy.DebugLog($"Creating new ModConfig instance for {modName}");
      ModConfig modConfig     = new ModConfig();
      modConfig._modName      = modName;
      modConfig._configFile   = Path.Combine(SaveManager.SavePath, $"{modName}.{MenuMaster._GUNFIG_EXTENSION}");
      modConfig.LoadFromDisk();
      _ActiveConfigs[modName] = modConfig;
    }
    return _ActiveConfigs[modName];
  }

  internal static void SaveActiveConfigsToDisk()
  {
    foreach (ModConfig config in _ActiveConfigs.Values)
    {
      if (!config._dirty)
        continue;
      config.SaveToDisk();
      config._dirty = false;
    }
  }

  private void LoadFromDisk()
  {
    if (!File.Exists(this._configFile))
        return;
    try
    {
        string[] lines = File.ReadAllLines(this._configFile);
        foreach (string line in lines)
        {
          string[] tokens = line.Split('=');
          if (tokens.Length != 2 || tokens[0] == null || tokens[1] == null)
            continue;
          string key = tokens[0].Trim();
          string val = tokens[1].Trim();
          if (key.Length == 0 || val.Length == 0)
            continue;
          // Lazy.DebugLog($"    loading config option {key} = {val}");
          this._options[key] = val;
        }
    }
    catch (Exception e)
    {
      ETGModConsole.Log($"    error loading mod config file {this._configFile}: {e}");
    }
  }

  private void SaveToDisk()
  {
    try
    {
      using (StreamWriter file = File.CreateText(this._configFile))
      {
          foreach(string key in this._options.Keys)
          {
            // Lazy.DebugLog($"    saving config option {key} = {this._options[key]}");
            file.WriteLine($"{key} = {this._options[key]}");
          }
      }
    }
    catch (Exception e)
    {
      ETGModConsole.Log($"    error saving mod config file {this._configFile}: {e}");
    }
  }

  internal dfScrollPanel RegenConfigPage()
  {
    dfScrollPanel subOptionsPanel = MenuMaster.NewOptionsPanel($"{this._modName}");
    foreach (ModConfigMenuItem item in this._registeredOptions)
    {
      switch (item._itemType)
      {
        case ItemType.Label:
          subOptionsPanel.AddLabel(label: item._label, color: Color.green);
          break;
        case ItemType.Button:
          subOptionsPanel.AddButton(label: item._label).gameObject.AddComponent<ModConfigOption>()
            .Setup(parentConfig: this, key: item._key, values: new(){"1"}, update: item._callback, updateType: item._updateType);
          break;
        case ItemType.CheckBox:
          subOptionsPanel.AddCheckBox(label: item._label).gameObject.AddComponent<ModConfigOption>()
            .Setup(parentConfig: this, key: item._key, values: new(){"0", "1"}, update: item._callback, updateType: item._updateType);
          break;
        case ItemType.ArrowBox:
          subOptionsPanel.AddArrowBox(label: item._label, options: item._values, info: item._info).gameObject.AddComponent<ModConfigOption>()
            .Setup(parentConfig: this, key: item._key, values: item._values, update: item._callback, updateType: item._updateType);
          break;
      }
    }
    subOptionsPanel.Finalize();
    return subOptionsPanel;
  }

  // Set a config key to a value and return the value
  internal string Set(string key, string value)
  {
    this._options[key] = value;
    this._dirty = true;
    return value;
  }

  public string Get(string key)
  {
    return this._options.ContainsKey(key) ? this._options[key] : null;
  }

  public bool? GetBool(string key)
  {
    return this._options.ContainsKey(key) ? (this._options[key] == "1") : null;
  }
}

public class ModConfigOption : MonoBehaviour
{
  public enum Update {
    Immediate, // updates immediately when changed (without confirmation)
    OnConfirm, // updates when menu is closed with changes confirmed
    // OnNextRun, // updates when a new run is started with changes confirmed
    OnRestart, // updates when game is closed with changes confirmed (just saved to the configuration file, but not updates in game)
  }

  private static List<ModConfigOption> pendingUpdatesOnConfirm = new();
  // private static List<ModConfigOption> pendingUpdatesOnNextRun = new();

  private string _lookupKey                      = "";               // key for looking up in our configuration file
  private string _defaultValue                   = "";               // our default value if the key is not found in the configuration file
  private string _currentValue                   = "";               // our current effective value, barring any pending changes
  private string _pendingValue                   = "";               // our pending value after changes are applies
  private Update _updateType                     = Update.OnConfirm; // when to apply any pending changes
  private List<string> _validValues              = new();            // valid values for the option (auto populated with "true" and "false" for toggles)
  private Action<string, string> _onApplyChanges = null;             // event handler for execution
  private dfControl _control                     = null;             // the dfControl to which we're attached
  private ModConfig _parent                      = null;             // the ModConfig instance that's handling us

  private static void OnMenuCancel(Action<FullOptionsMenuController> orig, FullOptionsMenuController menu) // hooked to call when menu choices are cancelled
  {
    // ETGModConsole.Log($"menu cancelled, discarding {pendingUpdatesOnConfirm.Count} changes");
    foreach (ModConfigOption option in pendingUpdatesOnConfirm)
      option.ResetMenuItemState();
    pendingUpdatesOnConfirm.Clear();
    orig(menu);
  }

  private static void OnMenuConfirm(Action<FullOptionsMenuController> orig, FullOptionsMenuController menu) // hooked to call when menu choices are confirmed
  {
    // ETGModConsole.Log($"menu confirmed, applying {pendingUpdatesOnConfirm.Count} changes");
    foreach (ModConfigOption option in pendingUpdatesOnConfirm)
    {
      if (option._updateType == Update.OnConfirm)
        option.CommitPendingChanges();
      // else if (option._updateType == Update.OnNextRun)
      // {
      //   if (!pendingUpdatesOnNextRun.Contains(option))
      //     pendingUpdatesOnNextRun.Add(option);
      // }
      option._parent.Set(option._lookupKey, option._pendingValue);  // register change in the config handler even if the option's pending changes are deferred
    }
    ModConfig.SaveActiveConfigsToDisk();  // save all committed changes
    pendingUpdatesOnConfirm.Clear();
    orig(menu);
  }

  // private static void OnNextRun()  // hooked to call when a new run is started UNIMPLEMENTED
  // {
  //   ETGModConsole.Log($"new run started");
  //   foreach (ModConfigOption option in pendingUpdatesOnNextRun)
  //     option.CommitPendingChanges();
  //   pendingUpdatesOnNextRun.Clear();
  // }

  private void CommitPendingChanges()
  {
    if (this._pendingValue == this._currentValue)
      return;  // we didn't change, so we shouldn't do anything

    // ETGModConsole.Log($"  applying changes for {this._lookupKey} -> {this._pendingValue}");
    if (this._onApplyChanges != null)
      this._onApplyChanges(this._lookupKey, this._pendingValue);

    this._currentValue = this._pendingValue;  // set our current value to our pending value after applying changes in case we throw an exception and break the config

    if (this._updateType == Update.Immediate) // register and save immediate changes to disk TODO: maybe be more conservative with this?
    {
      this._parent.Set(this._lookupKey, this._pendingValue);
      ModConfig.SaveActiveConfigsToDisk();
    }
  }

  private void OnControlChanged(dfControl control, string stringValue)
  {
    this._pendingValue = stringValue;
    OnControlChanged();
  }

  private void OnControlChanged(dfControl control, bool toggleValue)
  {
    this._pendingValue = toggleValue ? "1" : "0";
    OnControlChanged();
  }

  private void OnButtonClicked(dfControl control)
  {
    if (this._onApplyChanges != null)
      this._onApplyChanges(this._lookupKey, this._pendingValue);
  }

  private void OnControlChanged()
  {
    if (this._updateType == Update.Immediate)
      CommitPendingChanges();
    else if (!pendingUpdatesOnConfirm.Contains(this))
      pendingUpdatesOnConfirm.Add(this);
  }

  private void ResetMenuItemState()
  {
    // Make sure we have a menu item
    BraveOptionsMenuItem menuItem = base.GetComponent<BraveOptionsMenuItem>();
    if (!menuItem)
    {
      ETGModConsole.Log($"  NULL BRAVE MENU ITEM");
      return;
    }

    // Set up the state of our menu item from our config
    if (menuItem.buttonControl is dfButton button)
    {
      this._control.gameObject.AddComponent<MenuMaster.CustomButtonHandler>().onClicked += OnButtonClicked;
    }
    if (menuItem.checkboxChecked is dfControl checkBox)
    {
      bool isChecked = (this._currentValue.Trim() == "1");
      // ETGModConsole.Log($"  creating checkbox for {this._lookupKey} with state {isChecked}");
      checkBox.IsVisible = isChecked;
      if (menuItem.checkboxUnchecked is dfControl checkBoxUnched)
        checkBoxUnched.IsVisible = true;
      menuItem.m_selectedIndex = isChecked ? 1 : 0;
      this._control.gameObject.AddComponent<MenuMaster.CustomCheckboxHandler>().onChanged += OnControlChanged;
    }
    if ((menuItem.selectedLabelControl is dfLabel settingLabel) && menuItem.labelOptions != null)
    {
      for (int i = 0; i < menuItem.labelOptions.Length; ++i)
      {
        if (menuItem.labelOptions[i] != this._currentValue)
          continue;
        settingLabel.Text = menuItem.labelOptions[i];
        if ((menuItem.infoControl is dfLabel infoLabel) && menuItem.infoOptions != null && menuItem.infoOptions.Length < i)
          infoLabel.Text = menuItem.infoOptions[i];
        menuItem.m_selectedIndex = i;
        break;
      }
      this._control.gameObject.AddComponent<MenuMaster.CustomLeftRightArrowHandler>().onChanged += OnControlChanged;
    }
  }

  public void Setup(ModConfig parentConfig, string key, List<string> values, Action<string, string> update, Update updateType = Update.OnConfirm)
  {
    this._control        = base.GetComponent<dfControl>();
    this._parent         = parentConfig;
    this._lookupKey      = key;
    this._validValues    = values;
    this._onApplyChanges = update;
    this._updateType     = updateType;

    // Load our default and current values from our config, or from the options passed to us
    this._defaultValue = values[0];
    this._currentValue = this._parent.Get(this._lookupKey) ?? this._parent.Set(this._lookupKey, this._defaultValue);
    this._pendingValue = this._currentValue;

    // ETGModConsole.Log($">>> current value of {this._lookupKey} is {this._currentValue}");

    ResetMenuItemState();
  }
}
