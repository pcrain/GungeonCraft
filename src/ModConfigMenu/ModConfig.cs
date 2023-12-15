namespace CwaffingTheGungy;

public class ModConfig
{

  private enum ItemType
  {
    Label,
    Button,
    CheckBox,
    ArrowBox,
  }

  private class Item
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

  private Dictionary<string, string> _options                  = new(); // dictionary of mod options as key value pairs
  private List<Item> _registeredOptions                        = new(); // list of options from which we can dynamically regenerate the options panel

  private static readonly List<string> _CheckBoxValues         = new(){"0", "1"};
  private static readonly List<string> _ButtonValues           = new(){"1"};

  private bool _dirty = false; // whether we've been changed since last saving to disk
  private string _configFile = null; // the file on disk to which we're writing
  private string _modName = null;

  public void AddToggle(string key, string label, Action<string, string> callback, ModConfigOption.Update updateType = ModConfigOption.Update.OnConfirm)
  {
    this._registeredOptions.Add(new Item(){
      _itemType   = ItemType.CheckBox,
      _updateType = updateType,
      _key        = key,
      _label      = label,
      _callback   = callback,
    });
  }

  public void AddScrollBox(string key, string label, List<string> options, Action<string, string> callback, List<string> info = null, ModConfigOption.Update updateType = ModConfigOption.Update.OnConfirm)
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
      _updateType = ModConfigOption.Update.Immediate,
      _key        = key,
      _label      = label,
      _callback   = callback,
    });
  }

  public void AddLabel(string label)
  {
    this._registeredOptions.Add(new Item(){
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
      modConfig._configFile   = Path.Combine(SaveManager.SavePath, $"{modName}.{ModConfigMenu._GUNFIG_EXTENSION}");
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
    dfScrollPanel subOptionsPanel = ModConfigMenu.NewOptionsPanel($"{this._modName}");
    foreach (Item item in this._registeredOptions)
    {
      switch (item._itemType)
      {
        case ItemType.Label:
          subOptionsPanel.AddLabel(label: item._label, color: Color.green);
          break;
        case ItemType.Button:
          subOptionsPanel.AddButton(label: item._label).gameObject.AddComponent<ModConfigOption>()
            .Setup(parentConfig: this, key: item._key, values: _ButtonValues, update: item._callback, updateType: item._updateType);
          break;
        case ItemType.CheckBox:
          subOptionsPanel.AddCheckBox(label: item._label).gameObject.AddComponent<ModConfigOption>()
            .Setup(parentConfig: this, key: item._key, values: _CheckBoxValues, update: item._callback, updateType: item._updateType);
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
