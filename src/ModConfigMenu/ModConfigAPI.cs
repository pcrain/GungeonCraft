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

// ModConfigUpdate determines 1) when changes to options are committed internally, and 2) when callbacks for those options being changed are triggered
public enum ModConfigUpdate {
  Immediate, // updates immediately when changed (without confirmation)
  OnConfirm, // updates when menu is closed with changes confirmed
  // OnNextRun, // updates when a new run is started with changes confirmed (NOT IMPLEMENTED YET)
  OnRestart, // updates when game is closed with changes confirmed (just saved to the configuration file, but not updated in game)
}

// Public portion of ModConfig API
public partial class ModConfig
{
  public void AddToggle(string key, string label, Action<string, string> callback, ModConfigUpdate updateType = ModConfigUpdate.OnConfirm)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.CheckBox,
      _updateType = updateType,
      _key        = key,
      _label      = label,
      _callback   = callback,
    });
  }

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

  public string Get(string key)
  {
    return this._options.ContainsKey(key) ? this._options[key] : null;
  }

  public bool? GetBool(string key)
  {
    return this._options.ContainsKey(key) ? (this._options[key] == "1") : null;
  }
}
