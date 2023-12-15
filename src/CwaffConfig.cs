namespace CwaffingTheGungy;

public static class CwaffConfig
{
  public static ModConfig Gunfig = null;

  public static void Init()
  {
    Gunfig = ModConfig.GetConfigForMod("GungeonCraft");

    for (int i = 0; i < 3; ++i)
    {
      Gunfig.AddToggle(key: "testCheck", label: "Hello there! :D", enabled: false, callback: (_, newVal) => ETGModConsole.Log($"it worked O: {(newVal == "1" ? "on" : "off")}") );
      Gunfig.AddLabel("A Label *O*");
      // config.AddScrollBox("testScroll", "Look at it Go!", options: new(){"this", "that", "the other"}, info: new(){"good", "bad\nbad\nbad", "ugly"},
      //   callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      // config.AddScrollBox("testScroll", "Line Test!", options: new(){"one", "two"}, info: new(){"one line", "two\nlines"},
      //   callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      Gunfig.AddScrollBox(key: "testScroll", label: "Another Line Test!", options: new(){"one", "two"}, info: new(){"one line", "still one line"},
        callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      Gunfig.AddScrollBox(key: "testScroll2", label: "Last Line Test!", options: new(){"one", "two"},
        callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      Gunfig.AddButton(key: "testButton", label: "Click me!", callback: (key, _) => ETGModConsole.Log($"{key} button clicked!"));
    }
  }

}
