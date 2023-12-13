namespace CwaffingTheGungy;

public static class MenuMaster
{

    public static void PrintControlRecursive(dfControl control, string indent = "  ")
    {
        System.Console.WriteLine($"  control with name={control.name}, position={control.Position}, type={control.GetType()}");

        // PrintEventInfo(control, "KeyPress");
        // PrintEventInfo(control, "KeyDown");
        // PrintEventInfo(control, "KeyUp");
        // Debug.Log($"{indent}  KeyPress has {control.KeyPress?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  KeyDown has {control.KeyDown?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  KeyUp has {control.KeyUp?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  MouseEnter has {control.MouseEnter?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  MouseMove has {control.MouseMove?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  MouseHover has {control.MouseHover?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  MouseLeave has {control.MouseLeave?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  MouseDown has {control.MouseDown?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  MouseUp has {control.MouseUp?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  MouseWheel has {control.MouseWheel?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  Click has {control.Click?.GetInvocationList().Length} listeners");
        // Debug.Log($"{indent}  DoubleClick has {control.DoubleClick?.GetInvocationList().Length} listeners");

        // Dissect.DumpFieldsAndProperties(control);
        foreach (dfControl child in control.controls)
            PrintControlRecursive(child, indent+"  ");
    }

    public static void CopyAttributes(this dfButton self, dfButton other)
    {
        self.Text = other.Text;
        self.ClickWhenSpacePressed = other.ClickWhenSpacePressed;
        self.Font = other.Font;
        self.State = other.State;
        self.Padding = other.Padding;
        self.TextColor = other.TextColor;
        self.HoverTextColor = other.HoverTextColor;
        self.NormalBackgroundColor = other.NormalBackgroundColor;
        self.HoverBackgroundColor = other.HoverBackgroundColor;
        self.PressedTextColor = other.PressedTextColor;
        self.PressedBackgroundColor = other.PressedBackgroundColor;
        self.FocusTextColor = other.FocusTextColor;
        self.FocusBackgroundColor = other.FocusBackgroundColor;
        self.DisabledTextColor = other.DisabledTextColor;
        self.Color = other.Color;
        self.DisabledColor = other.DisabledColor;
        self.Anchor = other.Anchor;
        self.CanFocus = other.CanFocus;
        self.AutoFocus = other.AutoFocus;
        self.Size = other.Size;
        self.AutoSize = other.AutoSize;
        self.Opacity = other.Opacity;
        self.AllowSignalEvents = other.AllowSignalEvents;
        self.TextScale = other.TextScale;
        self.TextScaleMode = other.TextScaleMode;
        self.Atlas = other.Atlas;
        self.MinimumSize = other.MinimumSize;
        self.MaximumSize = other.MaximumSize;
        self.ZOrder = other.ZOrder;
        self.TabIndex = other.TabIndex;
        self.IsInteractive = other.IsInteractive;
        self.Pivot = other.Pivot;
        self.TextAlignment = other.TextAlignment;
        self.VerticalAlignment = other.VerticalAlignment;
        self.Position = other.Position;
        self.RelativePosition = other.RelativePosition;
        self.HotZoneScale = other.HotZoneScale;
        self.useGUILayout = other.useGUILayout;
        self.enabled = other.enabled;
    }

    public static dfButton AddNewButton(this dfPanel panel, string oldButtonName, string newbuttonName, MouseEventHandler onclick)
    {
        dfButton prevButton = panel.Find<dfButton>(oldButtonName);
        if (prevButton == null)
        {
          ETGModConsole.Log($"  no button {oldButtonName} to clone");
          return null; //
        }
        dfControl nextButton = prevButton.GetComponent<UIKeyControls>().down;

        dfButton newButton = panel.AddControl<dfButton>();
        newButton.CopyAttributes(prevButton);
        newButton.Text = newbuttonName;
        newButton.name = newbuttonName;
        newButton.RelativePosition = newButton.RelativePosition + new Vector3(300.0f, 10.0f, 0.0f);
        newButton.Click += onclick;

        UIKeyControls uikeys = newButton.gameObject.AddComponent<UIKeyControls>();
            uikeys.up = prevButton;
            uikeys.down = nextButton;
            prevButton.GetComponent<UIKeyControls>().down = newButton;
            nextButton.GetComponent<UIKeyControls>().up = newButton;

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

    public static dfScrollPanel NewOptionsPanel(dfControl parent, dfButton firstButtonToCopy)
    {
      dfScrollPanel refPanel = GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo;
      // Dissect.DumpFieldsAndProperties<dfScrollPanel>(refPanel);
      // dfScrollPanel newPanel = refPanel.transform.parent.gameObject.AddComponent<dfScrollPanel>(); // new dfScrollPanel(){
      dfScrollPanel newPanel = parent.AddControl<dfScrollPanel>();
        newPanel.UseScrollMomentum = refPanel.UseScrollMomentum;
        newPanel.ScrollWithArrowKeys = refPanel.ScrollWithArrowKeys;
        newPanel.Atlas = refPanel.Atlas;
        newPanel.BackgroundSprite = refPanel.BackgroundSprite;
        newPanel.BackgroundColor = refPanel.BackgroundColor;
        newPanel.AutoReset = refPanel.AutoReset;
        newPanel.ScrollPadding = refPanel.ScrollPadding;
        newPanel.AutoScrollPadding = refPanel.AutoScrollPadding;
        newPanel.AutoLayout = refPanel.AutoLayout;
        newPanel.WrapLayout = refPanel.WrapLayout;
        newPanel.FlowDirection = refPanel.FlowDirection;
        newPanel.FlowPadding = refPanel.FlowPadding;
        newPanel.ScrollPosition = refPanel.ScrollPosition;
        newPanel.ScrollWheelAmount = refPanel.ScrollWheelAmount;
        newPanel.HorzScrollbar = refPanel.HorzScrollbar;
        newPanel.VertScrollbar = refPanel.VertScrollbar;
        newPanel.WheelScrollDirection = refPanel.WheelScrollDirection;
        newPanel.UseVirtualScrolling = refPanel.UseVirtualScrolling;
        newPanel.AutoFitVirtualTiles = refPanel.AutoFitVirtualTiles;
        newPanel.VirtualScrollingTile = refPanel.VirtualScrollingTile;
        newPanel.CanFocus = refPanel.CanFocus;
        newPanel.AllowSignalEvents = refPanel.AllowSignalEvents;
        newPanel.IsEnabled = refPanel.IsEnabled;
        newPanel.IsVisible = refPanel.IsVisible;
        newPanel.IsInteractive = refPanel.IsInteractive;
        newPanel.Tooltip = refPanel.Tooltip;
        newPanel.Anchor = refPanel.Anchor;
        newPanel.Opacity = refPanel.Opacity;
        newPanel.Color = refPanel.Color;
        newPanel.DisabledColor = refPanel.DisabledColor;
        newPanel.Pivot = refPanel.Pivot;
        newPanel.RelativePosition = refPanel.RelativePosition;
        newPanel.Position = refPanel.Position;
        newPanel.Size = refPanel.Size;
        newPanel.Width = refPanel.Width;
        newPanel.Height = refPanel.Height;
        newPanel.MinimumSize = refPanel.MinimumSize;
        newPanel.MaximumSize = refPanel.MaximumSize;
        newPanel.ZOrder = refPanel.ZOrder;
        newPanel.TabIndex = refPanel.TabIndex;
        newPanel.ClipChildren = refPanel.ClipChildren;
        newPanel.InverseClipChildren = refPanel.InverseClipChildren;
        // newPanel.Tag = refPanel.Tag;
        newPanel.IsLocalized = refPanel.IsLocalized;
        newPanel.HotZoneScale = refPanel.HotZoneScale;
        newPanel.AutoFocus = refPanel.AutoFocus;
        newPanel.useGUILayout = refPanel.useGUILayout;
        newPanel.autoReset = true;
        newPanel.autoLayout = true;
        newPanel.enabled = refPanel.enabled;
        newPanel.Enable();

      dfButton testButton = newPanel.AddControl<dfButton>();
      testButton.CopyAttributes(firstButtonToCopy);
      testButton.Text = "Dummy";
      testButton.name = "Dummy";
      testButton.AutoFocus = true;
      testButton.AutoSize = true;
      // testButton.Position = new Vector3(0.0f, 0.0f, 0.0f);
      // testButton.RelativePosition = new Vector3(0.0f, 0.0f, 0.0f);
      // testButton.CanFocus = true;
      // testButton.AutoFocus = true;
      // testButton.IsVisible = true;

      newPanel.Reset();
      newPanel.FitToContents();
      newPanel.CenterChildControls();
      newPanel.ScrollToTop();

      newPanel.ResetLayout(true, true);
      newPanel.PerformLayout();
      newPanel.Disable();
      newPanel.Enable();
      return newPanel;
    }

    public static void NewOptions(this dfScrollPanel panel, dfButton baseButton, string newbuttonName, MouseEventHandler onclick)
    {
      dfButton newButton = panel.AddControl<dfButton>();
      newButton.CopyAttributes(baseButton);

      // newButton.Position = new Vector3(-400f, 0f, 0f);
      newButton.Text = newbuttonName;
      newButton.name = newbuttonName;
      newButton.Position = new Vector3(0.0f, 0.0f, 0.0f);
      newButton.RelativePosition = new Vector3(0.0f, 0.0f, 0.0f);
      newButton.CanFocus = true;
      newButton.AutoFocus = true;
      newButton.Click += onclick;
      // newButton.IsVisible = true;
      // Dissect.DumpFieldsAndProperties<dfButton>(newButton);

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
    }

    /* TODO:
      - apparently needs to be initialized each run before the pause menu is opened for the first time
      - menu items are creating one screen too far back
    */
    public static void SetupUITest()
    {
        if (GameUIRoot.Instance.PauseMenuPanel is not dfPanel pausePanel)
            return;
        if (pausePanel.GetComponent<PauseMenuController>().OptionsMenu.PreOptionsMenu is not PreOptionsMenuController preOptions)
            return;

        dfButton basicButton = preOptions.m_panel.Find<dfButton>("AudioTab (1)");

        ETGModConsole.Log($"got a pause panel");
        // PrintControlRecursive(preOptionsPanel);
        dfScrollPanel newOptionsPanel = NewOptionsPanel(preOptions.m_panel, basicButton);
        newOptionsPanel.NewOptions(basicButton, "Test", (control, args) => {
          ETGModConsole.Log($"did a new clickyboi");
        });
        newOptionsPanel.NewOptions(basicButton, "Test 2", (control, args) => {
          ETGModConsole.Log($"did another new clickyboi");
        });

        dfButton newButton = preOptions.m_panel.AddNewButton("AudioTab (1)", "Yo New Button Dropped O:", (control, args) => {
          ETGModConsole.Log($"did a clickyboi");
          preOptions.ToggleToPanel(newOptionsPanel, true);
          newOptionsPanel.IsVisible = true;
          // PrintControlRecursive(newOptionsPanel);
          // Dissect.DumpFieldsAndProperties<dfScrollPanel>(newOptionsPanel);
        });

        // PrintControlRecursive(pausePanel.GetComponent<PauseMenuController>().OptionsMenu.TabVideo);
    }

}
