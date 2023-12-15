namespace CwaffingTheGungy;

public static class CwaffConfig
{

  public static void Init()
  {
    ModConfig config = ModConfig.GetConfigForMod("GungeonCraft");

    for (int i = 0; i < 3; ++i)
    {
      config.AddToggle("testCheck", "Hello there! :D", (_, newVal) => ETGModConsole.Log($"it worked O: {(newVal == "1" ? "on" : "off")}") );
      config.AddLabel("A Label *O*");
      // config.AddScrollBox("testScroll", "Look at it Go!", options: new(){"this", "that", "the other"}, info: new(){"good", "bad\nbad\nbad", "ugly"},
      //   callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      // config.AddScrollBox("testScroll", "Line Test!", options: new(){"one", "two"}, info: new(){"one line", "two\nlines"},
      //   callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      config.AddScrollBox("testScroll", "Another Line Test!", options: new(){"one", "two"}, info: new(){"one line", "still one line"},
        callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      config.AddScrollBox("testScroll", "Last Line Test!", options: new(){"one", "two"},
        callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      config.AddButton("testButton", "Click me!", callback: (key, _) => ETGModConsole.Log($"{key} button clicked!"));
    }
  }

}
