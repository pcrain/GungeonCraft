namespace CwaffingTheGungy;

/*
   QoL improvements to make, from most to least important:
    - can't dynamically enable / disable options at runtime (must restart the game)
    - can't back out of one level of menus at a time (vanilla behavior; maybe hook CloseAndMaybeApplyChangesWithPrompt)

   Unimportant stuff I probably won't do:
    - haven't implemented progress / fill bars (not particularly useful outside vanilla volume controls, so not in a hurry to implement this)
    - haven't implemented sprites for options (e.g. like vanilla crosshair selection) (very hard, requires modifying sprite atlas, and it is minimally useful)
    - can't have first item of submenu be a label or it breaks focusing (vanilla ToggleToPanel() function assumes first control is selectable)
*/

/// <summary>
/// Public portion of ModConfig API
/// </summary>
public partial class ModConfig
{
  /// <summary>
  /// ModConfig.Update determines
  ///   1) when the new value for an option is actully set (i.e., when <c>Get()</c>, <c>Enabled()</c>, or <c>Disabled()</c> will return the new value),
  ///   2) when the callback (if any) associated with the option should be triggered, and
  ///   3) when the new value for the option should be written back to the configuration file.
  /// For all update types except <c>Immediate</c>, if the player backs out of the menu without confirming changes, none of the above events will occur.
  /// </summary>
  public enum Update {
    /// <summary>
    /// Immediately sets the option's new value, writes it to the gunfig file, and triggers any callbacks when the menu item is changed, without confirmation.
    /// </summary>
    Immediate,

    /// <summary>
    /// (Default) Sets the option's new value, writes it to the gunfig file, and triggers any callbacks when the options menu is closed with changes confirmed.
    /// Discards the change if the menu is closed without confirming changes.
    /// </summary>
    OnConfirm,

    /// <summary>
    /// Writes the new option's value to the gunfig file when the options menu is closed with changes confirmed.
    /// Does not set the option's value in memory or trigger any callbacks.
    /// Discards the change if the menu is closed without confirming changes.
    /// </summary>
    OnRestart,
  }

  /// <summary>
  /// Retrieves the unique ModConfig associated with the given <paramref name="modName"/>, creating it if it doesn't yet exist.
  /// </summary>
  /// <param name="modName">A name to uniquely identify your mod's configuration. The subpage in the MOD CONFIG menu will be set to this name.</param>
  /// <returns>A unique <paramref name="ModConfig"/> associated with the given <paramref name="modName"/>. This can be safely stored in a variable and retrieved for later use.</returns>
  public static ModConfig GetConfigForMod(string modName)
  {
    string cleanModName = modName.ProcessColors(out Color _);
    if (!_ActiveConfigs.ContainsKey(cleanModName))
    {
      Lazy.DebugLog($"Creating new ModConfig instance for {cleanModName}");
      ModConfig modConfig     = new ModConfig();
      modConfig._modName      = modName;  // need to keep colors intact here
      modConfig._configFile   = Path.Combine(SaveManager.SavePath, $"{cleanModName}.{ModConfigMenu._GUNFIG_EXTENSION}");
      modConfig.LoadFromDisk();
      _ActiveConfigs[cleanModName] = modConfig;
    }
    return _ActiveConfigs[cleanModName];
  }

  /// <summary>
  /// Appends a new togglable option to the current <paramref name="ModConfig"/>'s config page.
  /// </summary>
  /// <param name="key">The key for accessing the toggle's value through <c>GetBool()</c> and passed as the first parameter to the toggle's <paramref name="callback"/>.</param>
  /// <param name="enabled">Whether the toggle should be enabled by default if no prior configuration has been set.</param>
  /// <param name="label">The label displayed for the toggle on the config page. The toggle's <paramref name="key"/> will be displayed if no label is specified.</param>
  /// <param name="callback">An optional Action to call when changes to the toggle are applied.
  /// The callback's first argument will be the toggle's <paramref name="key"/>.
  /// The callback's second argument will be the toggle's value ("1" if enabled, "0" if disabled).</param>
  /// <param name="updateType">Determines when changes to the option are applied. See <see cref="ModConfig.Update"/> documentation for descriptions of each option.</param>
  public void AddToggle(string key, bool enabled = false, string label = null, Action<string, string> callback = null, ModConfig.Update updateType = ModConfig.Update.OnConfirm)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.CheckBox,
      _updateType = updateType,
      _key        = key,
      _label      = label ?? key,
      _callback   = callback,
      _values     = enabled ? _CheckedBoxValues : _UncheckedBoxValues,
    });
  }

  /// <summary>
  /// Appends a new scrollbox option to the current <paramref name="ModConfig"/>'s config page.
  /// </summary>
  /// <param name="key">The key for accessing the scrollbox's value through <c>Get()</c> and passed as the first parameter to the scrollbox's <paramref name="callback"/>.</param>
  /// <param name="options">A list of strings determining the valid values for the scrollbox, displayed verbatim on the config page.</param>
  /// <param name="label">The label displayed for the scrollbox on the config page. The scrollbox's <paramref name="key"/> will be displayed if no label is specified.</param>
  /// <param name="callback">An optional Action to call when changes to the scrollbox are applied.
  /// The callback's first argument will be the scrollbox's <paramref name="key"/>.
  /// The callback's second argument will be the scrollbox's displayed value.</param>
  /// <param name="info">A list of strings determining informational text to be displayed alongside each value of the scrollbox. Must match the length of <paramref name="options"/> exactly.</param>
  /// <param name="updateType">Determines when changes to the option are applied. See <see cref="ModConfig.Update"/> documentation for descriptions of each option.</param>
  public void AddScrollBox(string key, List<string> options, string label = null, Action<string, string> callback = null, List<string> info = null, ModConfig.Update updateType = ModConfig.Update.OnConfirm)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.ArrowBox,
      _updateType = updateType,
      _key        = key,
      _label      = label ?? key,
      _callback   = callback,
      _values     = options,
      _info       = info,
    });
  }

  /// <summary>
  /// Appends a new button to the current <paramref name="ModConfig"/>'s config page.
  /// </summary>
  /// <param name="key">A unique key associated with the button, passed as the first parameter to the scrollbox's <paramref name="callback"/>.</param>
  /// <param name="label">The label displayed for the button on the config page. The button's <paramref name="key"/> will be displayed if no label is specified.</param>
  /// <param name="callback">An optional Action to call when the button is pressed.
  /// The callback's first argument will be the button's <paramref name="key"/>.
  /// The callback's second argument will always be "1", and is only set for compatibility with other option callbacks.</param>
  public void AddButton(string key, string label = null, Action<string, string> callback = null)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.Button,
      _updateType = ModConfig.Update.Immediate,
      _key        = key,
      _label      = label ?? key,
      _callback   = callback,
      _values     = _DefaultValues,
    });
  }

  /// <summary>
  /// Appends a new label to the current <paramref name="ModConfig"/>'s config page.
  /// </summary>
  /// <param name="label">The text displayed for the label on the config page.</param>
  public void AddLabel(string label)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.Label,
      _updateType = ModConfig.Update.Immediate,
      _key        = $"{label} label",
      _label      = label,
      _values     = _DefaultValues,
    });
  }

  /// <summary>
  /// Retrieves the effective current value (i.e., not including changes awaiting menu confirmation) of the option with key <paramref name="key"/> for the current <paramref name="ModConfig"/>.
  /// </summary>
  /// <param name="string">The key for the option we're interested in.</param>
  /// <returns>The value of the option with key <paramref name="key"/>, or <c>null</c> if no such option exists.</returns>
  public string Get(string key)
  {
    string val;
    return this._options.TryGetValue(key, out val) ? val : null;
  }

  /// <summary>
  /// Convenience function to retrieve the effective current enabled-ness (i.e., not including changes awaiting menu confirmation) of the toggle option with key <paramref name="key"/> for the current <paramref name="ModConfig"/>.
  /// </summary>
  /// <param name="string">The key for the option we're interested in.</param>
  /// <returns><c>true</c> if the boolean option with key <paramref name="key"/> is enabled, <c>false</c> if the boolean option is disabled or if no such boolean option exists.</returns>
  /// <remarks>Will always return false for options that aren't toggles.</remarks>
  public bool Enabled(string key)
  {
    string val;
    return this._options.TryGetValue(key, out val) && (val == "1");
  }

  /// <summary>
  /// Convenience function to retrieve the effective current disabled-ness (i.e., not including changes awaiting menu confirmation) of the toggle option with key <paramref name="key"/> for the current <paramref name="ModConfig"/>.
  /// </summary>
  /// <param name="string">The key for the option we're interested in.</param>
  /// <returns><c>true</c> if the boolean option with key <paramref name="key"/> is disabled, <c>false</c> if the boolean option is enabled or if no such boolean option exists.</returns>
  /// <remarks>Will always return false for options that aren't toggles.</remarks>
  public bool Disabled(string key)
  {
    string val;
    return this._options.TryGetValue(key, out val) && (val == "0");
  }
}
