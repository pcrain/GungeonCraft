namespace CwaffingTheGungy;

internal class ModConfigOption : MonoBehaviour
{
  private static List<ModConfigOption> pendingUpdatesOnConfirm = new();
  // private static List<ModConfigOption> pendingUpdatesOnNextRun = new();

  private string _lookupKey                      = "";                        // key for looking up in our configuration file
  private string _defaultValue                   = "";                        // our default value if the key is not found in the configuration file
  private string _currentValue                   = "";                        // our current effective value, barring any pending changes
  private string _pendingValue                   = "";                        // our pending value after changes are applies
  private ModConfigUpdate _updateType            = ModConfigUpdate.OnConfirm; // when to apply any pending changes
  private List<string> _validValues              = new();                     // valid values for the option (auto populated with "true" and "false" for toggles)
  private Action<string, string> _onApplyChanges = null;                      // event handler for execution
  private dfControl _control                     = null;                      // the dfControl to which we're attached
  private ModConfig _parent                      = null;                      // the ModConfig instance that's handling us

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
      if (option._updateType == ModConfigUpdate.OnConfirm)
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

    if (this._updateType == ModConfigUpdate.Immediate) // register and save immediate changes to disk TODO: maybe be more conservative with this?
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
    if (this._updateType == ModConfigUpdate.Immediate)
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
      this._control.gameObject.AddComponent<ModConfigMenu.CustomButtonHandler>().onClicked += OnButtonClicked;
    }
    if (menuItem.checkboxChecked is dfControl checkBox)
    {
      bool isChecked = (this._currentValue.Trim() == "1");
      // ETGModConsole.Log($"  creating checkbox for {this._lookupKey} with state {isChecked}");
      checkBox.IsVisible = isChecked;
      if (menuItem.checkboxUnchecked is dfControl checkBoxUnchecked)
        checkBoxUnchecked.IsVisible = true;
      menuItem.m_selectedIndex = isChecked ? 1 : 0;
      this._control.gameObject.AddComponent<ModConfigMenu.CustomCheckboxHandler>().onChanged += OnControlChanged;
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
      this._control.gameObject.AddComponent<ModConfigMenu.CustomLeftRightArrowHandler>().onChanged += OnControlChanged;
    }
  }

  internal void Setup(ModConfig parentConfig, string key, List<string> values, Action<string, string> update, ModConfigUpdate updateType = ModConfigUpdate.OnConfirm)
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
