namespace CwaffingTheGungy;

/* What needs to be done:
    - allow for mod options subpages
    - load status of checkboxes and arrowboxes from persistent storage
    - store status of checkboxes and arrowboxes to persistent storage

   Minor issues I'm not worrying about now
    - using magic numbers in a few places to fix panel offsets
    - in arrowboxes, letters with underhangs are cut off at the bottom of the scroll panel
*/

internal class CustomCheckboxHandler : MonoBehaviour
  { public PropertyChangedEventHandler<bool> onChanged; }

internal class CustomLeftRightArrowHandler : MonoBehaviour
  { public PropertyChangedEventHandler<string> onChanged; }

internal class CustomButtonHandler : MonoBehaviour
  { public Action<dfControl> onClicked; }

public static class MenuMaster
{
    private static bool _DidInitHooks = false;
    private static List<dfControl> _RegisteredTabs = new();

    public static void InitHooksIfNecessary()
    {
      if (_DidInitHooks)
        return;

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

    private static void ToggleToPanel(Action<FullOptionsMenuController, dfScrollPanel, bool> orig, FullOptionsMenuController controller, dfScrollPanel targetPanel, bool doFocus = false)
    {
      foreach (dfControl tab in _RegisteredTabs)
        tab.IsVisible = (tab == targetPanel);
      orig(controller, targetPanel, doFocus);
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
        label.ProcessMarkup     = otherLabel.ProcessMarkup;
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
      // self.Position          = other.Position;
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

    public static dfScrollPanel NewOptionsPanel(dfPanel parent)
    {
      dfScrollPanel refPanel = GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo;
      // Dissect.DumpFieldsAndProperties<dfScrollPanel>(refPanel);
      // dfScrollPanel newPanel = refPanel.transform.parent.gameObject.AddComponent<dfScrollPanel>(); // new dfScrollPanel(){
      dfScrollPanel newPanel = parent.AddControl<dfScrollPanel>();
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

        // newPanel.RelativePosition = new Vector3(100f, 100f, 0f);
        // newPanel.ZOrder               = 4;
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
        menuItem.infoControl          = null;
        menuItem.fillbarControl       = null;
        menuItem.buttonControl        = null;
        menuItem.checkboxChecked      = null;
        menuItem.checkboxUnchecked    = null;
        menuItem.labelOptions         = options.ToArray();
        menuItem.infoOptions          = null;
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

    public static void RegisterBraveMenuItem(this dfScrollPanel panel, dfControl item)
    {
      if (panel.controls == null || panel.controls.Count < 2) // includes this object
        return;
      BraveOptionsMenuItem menuItem = item.GetComponent<BraveOptionsMenuItem>();
      dfControl nextItem = panel.controls[0];
      dfControl prevItem  = panel.controls[panel.controls.Count - 2];
      menuItem.up = prevItem;
      menuItem.down = nextItem;
      if (nextItem.GetComponent<BraveOptionsMenuItem>() is BraveOptionsMenuItem nextMenuItem)
        nextMenuItem.up = item;
      else if (nextItem.GetComponent<UIKeyControls>() is UIKeyControls nextMenuItemUI)
        nextMenuItemUI.up = item;
      if (prevItem.GetComponent<BraveOptionsMenuItem>() is BraveOptionsMenuItem prevMenuItem)
        prevMenuItem.down = item;
      else if (prevItem.GetComponent<UIKeyControls>() is UIKeyControls prevMenuItemUI)
        prevMenuItemUI.down = item;
    }

    public static void ResetAllLayouts(this dfControl control)
    {
      control.ResetLayout();
      foreach (dfControl child in control.controls)
        child.ResetAllLayouts();
    }

    /* TODO:
      - apparently needs to be initialized each run before the pause menu is opened for the first time
    */
    public static void RebuildOptionsPanels()
    {
        InitHooksIfNecessary();
        if (GameUIRoot.Instance.PauseMenuPanel is not dfPanel pausePanel)
            return;
        if (pausePanel.GetComponent<PauseMenuController>().OptionsMenu is not FullOptionsMenuController fullOptions)
          return;
        if (fullOptions.PreOptionsMenu is not PreOptionsMenuController preOptions)
            return;
        ETGModConsole.Log($"got a pause panel");

        // Clear out all registered UI tabs, since we need to build everything fresh
        _RegisteredTabs.Clear();

        // Create the new modded options panel
        dfScrollPanel newOptionsPanel = NewOptionsPanel(fullOptions.m_panel);
        // newOptionsPanel.MaximumSize = new Vector2(110f, 110f);
        newOptionsPanel.ClipChildren = true;
        newOptionsPanel.InverseClipChildren = true;
        ETGModConsole.Log($"scroll padding was {newOptionsPanel.ScrollPadding}");
        newOptionsPanel.ScrollPadding = new RectOffset(0,0,0,0);
        ETGModConsole.Log($"AutoScrollPadding was {newOptionsPanel.AutoScrollPadding}");
        newOptionsPanel.AutoScrollPadding = new RectOffset(0,0,0,0);
        newOptionsPanel.Size -= new Vector2(0, 50f);  //TODO: figure out why this offset is wrong in the first place
        newOptionsPanel.Position -= new Vector3(0, 50f, 0f);  //TODO: figure out why this offset is wrong in the first place
        // newOptionsPanel.Anchor               = dfAnchorStyle.CenterVertical | dfAnchorStyle.Proportional;
        // newOptionsPanel.ResetLayout(true, false);

        // newOptionsPanel.ResetVirtualScrollingData();
        // newOptionsPanel.ResetAllLayouts();
        // newOptionsPanel.Reset();
        // newOptionsPanel.PerformLayout();

        // Add a few test items
        for (int i = 1; i <= 5; ++i)
        {
          newOptionsPanel.AddButton(label: $"Align {i}", onclick: (control) => {
            ETGModConsole.Log($"clikin on {control.name}");
          });
          newOptionsPanel.AddCheckBox(label: $"Align {i}", onchange:  (control, boolValue) => {
            ETGModConsole.Log($"checkeroo {boolValue} on {control.name}");
          });
          newOptionsPanel.AddArrowBox(label: $"Align {i}", options: new(){$"Align {i}", ">>>>>>>>>>>O<<<<<<<<<<<", ">>>o<<<"}, onchange:  (control, stringValue) => {
            ETGModConsole.Log($"arrowboi {stringValue} on {control.name}");
          });
        }

        // // newOptionsPanel.MaximumSize = new Vector2(110f, 110f);
        // newOptionsPanel.ClipChildren = true;
        // newOptionsPanel.InverseClipChildren = true;
        // // newOptionsPanel.ScrollPadding = new RectOffset(0,0,0,0);
        // // newOptionsPanel.Size -= new Vector2(0, 150f);
        // newOptionsPanel.Anchor               = dfAnchorStyle.CenterVertical | dfAnchorStyle.Proportional;
        // // newOptionsPanel.ResetVirtualScrollingData();
        // newOptionsPanel.ResetLayout(true, false);
        // newOptionsPanel.ResetAllLayouts();
        // newOptionsPanel.Reset();
        newOptionsPanel.controls.Last().Height += 16f; // fix a weird clipping issue for arrowboxes at the bottom
        newOptionsPanel.PerformLayout();

        // Register the new button on the PreOptions menu
        dfButton newButton = preOptions.m_panel.InsertRawButton(previousButtonName: "AudioTab (1)", label: "Yo New Button Dropped O:", onclick: (control, args) => {
          ETGModConsole.Log($"did a clickyboi");
          newOptionsPanel.IsVisible = true;
          preOptions.ToggleToPanel(newOptionsPanel, true);
        });

        // Dissect.DumpFieldsAndProperties<dfScrollPanel>(newOptionsPanel);
        // PrintControlRecursive(newOptionsPanel);
    }
}
