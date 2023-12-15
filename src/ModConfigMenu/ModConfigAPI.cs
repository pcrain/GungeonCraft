namespace CwaffingTheGungy;

/* Major API stuff to be done, from highest to lowest priority
    - create actual API surface
    - clean up code

   Nitpicks I really don't care to fix at all, but should be aware of:
    - can't colorize anything except labels
    - can't back out of one level of menus at a time (vanilla behavior; maybe hook CloseAndMaybeApplyChangesWithPrompt)
    - occasional double select sound when entering a mod menu
    - can't dynamically enable / disable options
    - haven't implemented progress / fill bars
    - can't have first item of submenu be a label or it doesn't get focused correctly
    - using magic numbers in a few places to fix panel offsets
*/

/// <summary>
/// ModConfigUpdate determines when the following events should happen:
///   1) when calling <c>Get()</c> or <c>GetBool()</c> should return the new option value
///   2) when the callback (if any) associated with the option should be triggered
///   3) when the new value for the option should be written back to the configuration file
/// For all update types except <c>Immediate</c>, if the player backs out of the menu without confirming changes, none of the above events will occur.
/// </summary>
public enum ModConfigUpdate {
  /// <summary>
  /// Updates immediately when changed, without confirmation.
  /// </summary>
  Immediate,
  /// <summary>
  /// (Default) Updates when the options menu is closed with changes confirmed, or discards the change if the menu is closed without confirming changes
  /// </summary>
  OnConfirm,
  // OnNextRun, // updates when a new run is started with changes confirmed (NOT IMPLEMENTED YET)
  /// <summary>
  /// Saves the new option to the configuration file when the options menu is closed with changes confirmed, or discards the change if the menu is closed without confirming changes
  /// </summary>
  OnRestart,
}

// Public portion of ModConfig API
public partial class ModConfig
{
  /// <summary>
  /// Retrieves the unique ModConfig associated with the given <paramref name="modName"/>, creating it if it doesn't yet exist.
  /// </summary>
  /// <param name="modName">A name to uniquely identify your mod's configuration. The subpage in the MOD CONFIG menu will be set to this name.</param>
  /// <returns>A unique <paramref name="ModConfig"/> associated with the given <paramref name="modName"/>. This can be safely stored in a variable and retrieved for later use.</returns>
  public static ModConfig GetConfigForMod(string modName)
  {
    if (!_ActiveConfigs.ContainsKey(modName))
    {
      Lazy.DebugLog($"Creating new ModConfig instance for {modName}");
      ModConfig modConfig     = new ModConfig();
      modConfig._modName      = modName;
      modConfig._configFile   = Path.Combine(SaveManager.SavePath, $"{modName}.{ModConfigMenu._GUNFIG_EXTENSION}");
      modConfig.LoadFromDisk();
      _ActiveConfigs[modName] = modConfig;
    }
    return _ActiveConfigs[modName];
  }

  /// <summary>
  /// Appends a new togglable option to the current <paramref name="ModConfig"/>'s config page.
  /// </summary>
  /// <param name="key">The key for accessing the toggle's value through <c>GetBool()</c> and passed as the first parameter to the toggle's <paramref name="callback"/>.</param>
  /// <param name="label">The label displayed for the toggle on the config page.</param>
  /// <param name="callback">An Action to call when changes to the toggle are applied.
  /// The callback's first argument will be the toggle's <paramref name="key"/>.
  /// The callback's second argument will be the toggle's value ("1" if enabled, "0" if disabled).</param>
  /// <param name="updateType">Determines when changes to the option are applied. See <see cref="ModConfigUpdate"/> documentation for descriptions of each option.</param>
  /// <param name="enabled">Determines whether the toggle should be enabled by default if no prior configuration has been set.</param>
  public void AddToggle(string key, string label, Action<string, string> callback, ModConfigUpdate updateType = ModConfigUpdate.OnConfirm, bool enabled = false)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.CheckBox,
      _updateType = updateType,
      _key        = key,
      _label      = label,
      _callback   = callback,
      _values     = enabled ? _CheckedBoxValues : _UncheckedBoxValues,
    });
  }

  /// <summary>
  /// Appends a new scrollbox option to the current <paramref name="ModConfig"/>'s config page.
  /// </summary>
  /// <param name="key">The key for accessing the scrollbox's value through <c>Get()</c> and passed as the first parameter to the scrollbox's <paramref name="callback"/>.</param>
  /// <param name="label">The label displayed for the scrollbox on the config page.</param>
  /// <param name="options">A list of strings determining the valid values for the scrollbox, displayed verbatim on the config page.</param>
  /// <param name="callback">An Action to call when changes to the scrollbox are applied.
  /// The callback's first argument will be the scrollbox's <paramref name="key"/>.
  /// The callback's second argument will be the scrollbox's displayed value.</param>
  /// <param name="info">A list of strings determining informational text to be displayed alongside each value of the scrollbox. Must match the length of <paramref name="options"/> exactly.</param>
  /// <param name="updateType">Determines when changes to the option are applied. See <see cref="ModConfigUpdate"/> documentation for descriptions of each option.</param>
  public void AddScrollBox(string key, string label, List<string> options, Action<string, string> callback, List<string> info = null, ModConfigUpdate updateType = ModConfigUpdate.OnConfirm)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.ArrowBox,
      _updateType = updateType,
      _key        = key,
      _label      = label,
      _callback   = callback,
      _values     = options,
      _info       = info,
    });
  }

  /// <summary>
  /// Appends a new button to the current <paramref name="ModConfig"/>'s config page.
  /// </summary>
  /// <param name="key">A unique key associated with the button, passed as the first parameter to the scrollbox's <paramref name="callback"/>.</param>
  /// <param name="label">The label displayed for the button on the config page.</param>
  /// <param name="callback">An Action to call when the button is pressed.
  /// The callback's first argument will be the button's <paramref name="key"/>.
  /// The callback's second argument will always be "1", and is only set for compatibility with other option callbacks.</param>
  public void AddButton(string key, string label, Action<string, string> callback)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.Button,
      _updateType = ModConfigUpdate.Immediate,
      _key        = key,
      _label      = label,
      _callback   = callback,
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
      _updateType = ModConfigUpdate.Immediate,
      _key        = $"{label} label",
      _label      = label,
      _callback   = null,
    });
  }

  /// <summary>
  /// Retrieves the effective current value (i.e., not including changes awaiting menu confirmation) of the option with key <paramref name="key"/> for the current <paramref name="ModConfig"/>.
  /// </summary>
  /// <param name="string">The key for the option we're interested in.</param>
  /// <returns>The value of the option with key <paramref name="key"/>, or <c>null</c> if no such option exists.</returns>
  public string Get(string key)
  {
    return this._options.ContainsKey(key) ? this._options[key] : null;
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
    if (!this._options.TryGetValue(key, out val))
      return false;
    return (val == "1");
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
    if (!this._options.TryGetValue(key, out val))
      return false;
    return (val == "0");
  }
}
