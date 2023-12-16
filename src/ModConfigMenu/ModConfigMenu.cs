namespace CwaffingTheGungy;

internal static class ModConfigMenu
{
    internal const string _GUNFIG_EXTENSION        = "gunfig";

    private const string _MOD_MENU_LABEL           = "MOD CONFIG";
    private const string _MOD_MENU_TITLE           = "Modded Options";
    private static bool _DidInitHooks              = false;
    private static List<dfControl> _RegisteredTabs = new();

    internal class CustomCheckboxHandler : MonoBehaviour
      { public PropertyChangedEventHandler<bool> onChanged; }

    internal class CustomLeftRightArrowHandler : MonoBehaviour
      { public PropertyChangedEventHandler<string> onChanged; }

    internal class CustomButtonHandler : MonoBehaviour
      { public Action<dfControl> onClicked; }

    internal static void InitHooksIfNecessary()
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
          typeof(ModConfigMenu).GetMethod("InitializeMainMenu", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Make sure our menus are loaded in game
      new Hook(
          typeof(GameManager).GetMethod("Pause", BindingFlags.Instance | BindingFlags.Public),
          typeof(ModConfigMenu).GetMethod("Pause", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Make sure our menus appear and disappear properly
      new Hook(
          typeof(FullOptionsMenuController).GetMethod("ToggleToPanel", BindingFlags.Instance | BindingFlags.Public),
          typeof(ModConfigMenu).GetMethod("ToggleToPanel", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Make sure we respect discarded changes
      new Hook(
          typeof(GameOptions).GetMethod("CompareSettings", BindingFlags.Static | BindingFlags.Public),
          typeof(ModConfigMenu).GetMethod("CompareSettings", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Custom checkbox events
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("HandleCheckboxValueChanged", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(ModConfigMenu).GetMethod("HandleCheckboxValueChanged", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Custom arrowbox events
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("HandleLeftRightArrowValueChanged", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(ModConfigMenu).GetMethod("HandleLeftRightArrowValueChanged", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Custom button events
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("DoSelectedAction", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(ModConfigMenu).GetMethod("DoSelectedAction", BindingFlags.Static | BindingFlags.NonPublic)
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

      // Update custom colors when loaded in
      // new Hook(
      //     typeof(BraveOptionsMenuItem).GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public),
      //     typeof(ModConfigOption).GetMethod("OnAwake", BindingFlags.Static | BindingFlags.NonPublic)
      //     );

      // Update custom colors on focus gained
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("DoFocus", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(ModConfigOption).GetMethod("OnGotFocus", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Update custom colors on focus lost
      new Hook(
          typeof(BraveOptionsMenuItem).GetMethod("SetUnselectedColors", BindingFlags.Instance | BindingFlags.NonPublic),
          typeof(ModConfigOption).GetMethod("OnSetUnselectedColors", BindingFlags.Static | BindingFlags.NonPublic)
          );

      // Update config options when a new run is started TODO: not implemented
      // new Hook(
      //     typeof(___).GetMethod("___", BindingFlags.Instance | BindingFlags.NonPublic),
      //     typeof(ModConfigOption).GetMethod("OnNextRun", BindingFlags.Static | BindingFlags.NonPublic)
      //     );

      _DidInitHooks = true;
    }

    // private static void ReturnToPreOptionsMenu(Action<PreOptionsMenuController> orig, PreOptionsMenuController pm)
    // {
    //   orig(pm);
    // }

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
      {
        targetPanel.controls.First().RecursiveFocus();  // fix bug where first item isn't highlighted
        SetOptionsPageTitle(targetPanel.name);
      }
      else
        SetOptionsPageTitle("Options");
      // if (isOurPanel)
      // {
      //   foreach (dfControl control in targetPanel.controls) // find first focusable element and select it
      //   {
      //     if (!control.GetComponent<BraveOptionsMenuItem>())
      //       continue;
      //     control.RecursiveFocus();  // fix bug where first item isn't highlighted
      //     break;
      //   }
      // }
    }

    private static bool CompareSettings(Func<GameOptions, GameOptions, bool> orig, GameOptions clone, GameOptions source)
    {
      if (ModConfigOption.HasPendingChanges())
        return false; // we have pending updates, so prompt to discard
      return orig(clone, source);
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

    private static dfPanel GetPrototypeCheckboxWrapperPanel() =>
      GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo.Find<dfPanel>("V-SyncCheckBoxPanel");
    private static dfPanel GetPrototypeCheckboxInnerPanel() =>
      GetPrototypeCheckboxWrapperPanel().Find<dfPanel>("Panel");
    private static dfCheckbox GetPrototypeCheckbox() =>
      GetPrototypeCheckboxInnerPanel().Find<dfCheckbox>("Checkbox");
    private static dfSprite GetPrototypeEmptyCheckboxSprite() =>
      GetPrototypeCheckbox().Find<dfSprite>("EmptyCheckbox");
    private static dfSprite GetPrototypeCheckedCheckboxSprite() =>
      GetPrototypeCheckbox().Find<dfSprite>("CheckedCheckbox");
    private static dfLabel GetPrototypeCheckboxLabel() =>
      GetPrototypeCheckboxInnerPanel().Find<dfLabel>("CheckboxLabel");
    private static dfPanel GetPrototypeLeftRightWrapperPanel() =>
      GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo.Find<dfPanel>("VisualPresetArrowSelectorPanel");
    private static dfPanel GetPrototypeLeftRightInnerPanel() =>
      GetPrototypeLeftRightWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    private static dfLabel GetPrototypeLeftRightPanelLabel() =>
      GetPrototypeLeftRightInnerPanel().Find<dfLabel>("OptionsArrowSelectorLabel");
    private static dfSprite GetPrototypeLeftRightPanelLeftSprite() =>
      GetPrototypeLeftRightInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowLeft");
    private static dfSprite GetPrototypeLeftRightPanelRightSprite() =>
      GetPrototypeLeftRightInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowRight");
    private static dfLabel GetPrototypeLeftRightPanelSelection() =>
      GetPrototypeLeftRightInnerPanel().Find<dfLabel>("OptionsArrowSelectorSelection");
    private static dfPanel GetPrototypeInfoWrapperPanel() =>
      GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo.Find<dfPanel>("ResolutionArrowSelectorPanelWithInfoBox");
    private static dfPanel GetPrototypeInfoInnerPanel() =>
      GetPrototypeInfoWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    private static dfLabel GetPrototypeInfoPanelLabel() =>
      GetPrototypeInfoInnerPanel().Find<dfLabel>("OptionsArrowSelectorLabel");
    private static dfSprite GetPrototypeInfoPanelLeftSprite() =>
      GetPrototypeInfoInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowLeft");
    private static dfSprite GetPrototypeInfoPanelRightSprite() =>
      GetPrototypeInfoInnerPanel().Find<dfSprite>("OptionsArrowSelectorArrowRight");
    private static dfLabel GetPrototypeInfoPanelSelection() =>
      GetPrototypeInfoInnerPanel().Find<dfLabel>("OptionsArrowSelectorSelection");
    private static dfLabel GetPrototypeInfoInfoPanel() =>
      GetPrototypeInfoWrapperPanel().Find<dfLabel>("OptionsArrowSelectorInfoLabel");
    private static dfPanel GetPrototypeButtonWrapperPanel() =>
      GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabControls.Find<dfPanel>("EditKeyboardBindingsButtonPanel");
    private static dfPanel GetPrototypeButtonInnerPanel() =>
      GetPrototypeButtonWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    private static dfButton GetPrototypeButton() =>
      GetPrototypeButtonInnerPanel().Find<dfButton>("EditKeyboardBindingsButton");
    private static dfPanel GetPrototypeLabelWrapperPanel() =>
      GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabControls.Find<dfPanel>("PlayerOneLabelPanel");
    private static dfPanel GetPrototypeLabelInnerPanel() =>
      GetPrototypeLabelWrapperPanel().Find<dfPanel>("PanelEnsmallenerThatmakesDavesLifeHardandBrentsLifeEasy");
    private static dfLabel GetPrototypeLabel() =>
      GetPrototypeLabelInnerPanel().Find<dfLabel>("Label");

    private static void PrintControlRecursive(dfControl control, string indent = "->", bool dissect = false)
    {
        System.Console.WriteLine($"  {indent} control with name={control.name}, type={control.GetType()}, position={control.Position}, relposition={control.RelativePosition}, size={control.Size}, anchor={control.Anchor}, pivot={control.Pivot}");
        if (dissect)
          Dissect.DumpFieldsAndProperties(control);
        foreach (dfControl child in control.controls)
            PrintControlRecursive(child, "--"+indent);
    }

    private static void CopyAttributes<T>(this T self, T other) where T : dfControl
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

    private static void CreateModConfigButton(this PreOptionsMenuController preOptions, dfScrollPanel newOptionsPanel)
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

    internal static dfScrollPanel NewOptionsPanel(string name)
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
    internal static dfPanel AddCheckBox(this dfScrollPanel panel, string label, PropertyChangedEventHandler<bool> onchange = null)
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
    internal static dfPanel AddArrowBox(this dfScrollPanel panel, string label, List<string> options, List<string> info = null, PropertyChangedEventHandler<string> onchange = null, bool compact = true)
    {
      bool hasInfo = (info != null && info.Count > 0 && info.Count == options.Count);

      dfPanel newArrowboxWrapperPanel = panel.AddControl<dfPanel>();
      newArrowboxWrapperPanel.CopyAttributes(hasInfo ? GetPrototypeInfoWrapperPanel() : GetPrototypeLeftRightWrapperPanel());
      // newArrowboxWrapperPanel.Anchor = dfAnchorStyle.CenterVertical | dfAnchorStyle.CenterHorizontal;

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

      if (compact)
      {
        if (hasInfo)
        {
          int maxLines = 1;
          foreach (string line in info)
            maxLines = Mathf.Max(maxLines, line.Split('\n').Length);
          newArrowboxWrapperPanel.Size -= new Vector2(0, 66f - 22f * maxLines);  // NOTE: don't shrink it too small or scrolling gets very messed up
        }
        else
          newArrowboxWrapperPanel.Size -= new Vector2(0, 8f);  // NOTE: don't shrink it too small or scrolling gets very messed up
      }

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
    internal static dfPanel AddButton(this dfScrollPanel panel, string label, Action<dfControl> onclick = null)
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
    internal static dfPanel AddLabel(this dfScrollPanel panel, string label, bool compact = true)
    {
      dfPanel newLabelWrapperPanel = panel.AddControl<dfPanel>();
      newLabelWrapperPanel.CopyAttributes(GetPrototypeLabelWrapperPanel());

      dfPanel newLabelInnerPanel = newLabelWrapperPanel.AddControl<dfPanel>();
      newLabelInnerPanel.CopyAttributes(GetPrototypeLabelInnerPanel());

      dfLabel newLabel = newLabelInnerPanel.AddControl<dfLabel>();
      newLabel.CopyAttributes(GetPrototypeLabel());

      Color color;
      newLabel.Text = label.ProcessColors(out color);
      newLabel.Color = color;

      if (compact)
      {
        newLabelWrapperPanel.Size -= new Vector2(0, 56f);
        newLabelInnerPanel.Position += new Vector3(0, 56f, 0);
      }

      newLabelWrapperPanel.name = $"{label} panel";

      return newLabelWrapperPanel;
    }

    private static void RegisterBraveMenuItem(this dfScrollPanel panel, dfControl item)
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

    private static void RecursiveFocus(this dfControl control, bool isRoot = true)
    {
      control.canFocus  = isRoot;
      control.AutoFocus = true;
      foreach (dfControl child in control.controls)
        child.RecursiveFocus(isRoot: false);
      // control.focusOnNextFrame();
    }

    internal static void Finalize(this dfScrollPanel panel)
    {
      panel.controls.Last().Height += 16f; // fix a weird clipping issue for arrowboxes at the bottom
      foreach (dfControl child in panel.controls)
      {
        if (child.GetComponent<BraveOptionsMenuItem>() is not BraveOptionsMenuItem menuItem)
          continue;
        if (child.GetComponent<ModConfigOption>() is not ModConfigOption option)
          continue;
        // ETGModConsole.Log($"updating colors for {child.name}");
        option.UpdateColors(menuItem, true); // make sure our colors our properly set on first load
        child.PerformLayout();
      }
      panel.PerformLayout();  // register all changes
    }

    private static void OpenSubMenu(dfScrollPanel panel)
    {
      GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>(
        ).OptionsMenu.PreOptionsMenu.ToggleToPanel(panel, true, force: true); // force true so it works even if it's invisible
    }

    private static void SetOptionsPageTitle(string title)
    {
      if (GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu is not FullOptionsMenuController optionsMenu)
        return;
      Color color;
      dfLabel titleControl = optionsMenu.m_panel.Find<dfLabel>("Title");
      titleControl.Text = title.ProcessColors(out color);
      titleControl.Color = color;
    }

    private static void RebuildOptionsPanels()
    {
        if (GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu is not PreOptionsMenuController preOptions)
          return;

        System.Diagnostics.Stopwatch panelBuildWatch = System.Diagnostics.Stopwatch.StartNew();

        // Clear out all registered UI tabs, since we need to build everything fresh
        _RegisteredTabs.Clear();

        // Create the new modded options panel
        dfScrollPanel newOptionsPanel = NewOptionsPanel(_MOD_MENU_TITLE);

        // Add submenus for each active mod
        foreach (string modName in ModConfig._ActiveConfigs.Keys)
        {
          ModConfig modConfig = ModConfig._ActiveConfigs[modName];
          dfScrollPanel modConfigPage = modConfig.RegenConfigPage();
          newOptionsPanel.AddButton(label: modName).gameObject.AddComponent<ModConfigOption>().Setup(
            parentConfig: modConfig, key: $"{modName} subpage button", values: new(){"1"},
            updateType: ModConfigUpdate.Immediate, update: (_, _) => OpenSubMenu(modConfigPage));
        }

        // Finalize the options panel
        newOptionsPanel.Finalize();

        // Register the new button on the PreOptions menu
        preOptions.CreateModConfigButton(newOptionsPanel);

        // Dissect.DumpFieldsAndProperties<dfScrollPanel>(newOptionsPanel);
        // PrintControlRecursive(GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.m_panel);
        panelBuildWatch.Stop(); System.Console.WriteLine($"    panel built in {panelBuildWatch.ElapsedMilliseconds} milliseconds");
    }
}
