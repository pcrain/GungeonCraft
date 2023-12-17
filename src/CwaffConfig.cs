namespace CwaffingTheGungy;

using Gunfiguration;

public static class CwaffConfig
{
  public static Gunfig _Gunfig = null;

  public static void Init()
  {
    _Gunfig = Gunfig.Get("GungeonCraft".WithColor(C.MOD_COLOR));

    for (int i = 0; i < 3; ++i)
    {
      _Gunfig.AddToggle(key: "testCheck", label: "Hello there! :D".Cyan(), enabled: false, callback: (_, newVal) => ETGModConsole.Log($"it worked O: {(newVal == "1" ? "on" : "off")}") );
      _Gunfig.AddLabel("A Label *O*".Magenta());
      // config.AddScrollBox("testScroll", "Look at it Go!", options: new(){"this", "that", "the other"}, info: new(){"good", "bad\nbad\nbad", "ugly"},
      //   callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      // config.AddScrollBox("testScroll", "Line Test!", options: new(){"one", "two"}, info: new(){"one line", "two\nlines"},
      //   callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      _Gunfig.AddScrollBox(key: "testScroll", label: "Another Line Test!".Red(), options: new(){"one".Yellow(), "two"}, info: new(){"one line".Green(), "still one line"},
        callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      _Gunfig.AddScrollBox(key: "testScroll2", label: "Last Line Test!".Blue(), options: new(){"one", "two"},
        callback: (_, newVal) => ETGModConsole.Log($"toggled to: {newVal}"));
      _Gunfig.AddButton(key: "testButton", label: "Click me!", callback: (key, _) => ETGModConsole.Log($"{key} button clicked!"));
    }
  }
}
