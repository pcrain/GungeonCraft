namespace CwaffingTheGungy;

public class FluidBulletInfo
{
  // internal variables for fluent coding management
  private static int nextAutoId = 0;  // next id to automatically assign
  private static LinkedList<FluidBulletInfo> lastLayer = new LinkedList<FluidBulletInfo>(); // list of scripts in last layer of method chain
  private static LinkedList<FluidBulletInfo> curLayer  = new LinkedList<FluidBulletInfo>(); // list of scripts in current layer of method chain
  private static List<FluidBulletInfo> theChain        = new List<FluidBulletInfo>(); // all scripts in the method chain

  public readonly int id;           // internal id used by the script
  public List<int> runsAfter;       // list of scripts that must complete before this script runs
  public List<int> runsBefore;      // list of scripts that are awaiting completiong of this script
  public bool finished;             // whether this script has run to completion
  public IEnumerator script;        // the actual IEnumerator we run
  public int waitFrames;            // how many frames we're current waiting to run

  private FluidBulletScript manager;// our manager

  // public constructor that ensures a fresh start
  public FluidBulletInfo(IEnumerator script, FluidBulletScript manager)
  {
    nextAutoId = 0;
    this.id    = nextAutoId++;
    Initialize(script, manager);
  }

  // private constructor so we don't tamper with ID chaining
  private FluidBulletInfo(IEnumerator script, FluidBulletInfo parent)
  {
    this.id = nextAutoId++;
    Initialize(script, parent.manager);
  }

  // shared initialization function for common code
  private void Initialize(IEnumerator script, FluidBulletScript manager)
  {
    this.script     = script;
    this.finished   = false;
    this.waitFrames = 1;
    this.runsAfter  = new List<int>();
    this.runsBefore = new List<int>();
    this.manager    = manager;
  }

  // Starts an inital run of scripts
  public static FluidBulletInfo Run(FluidBulletScript manager, IEnumerator script)
  {
    lastLayer = new LinkedList<FluidBulletInfo>();
    curLayer  = new LinkedList<FluidBulletInfo>();
    theChain  = new List<FluidBulletInfo>();
    FluidBulletInfo next = new FluidBulletInfo(script, manager);
    curLayer.AddLast(next);
    theChain.Add(next);
    return next;
  }

  // Run another script simultaneously with other scripts in the current layer
  public FluidBulletInfo And(IEnumerator script, int withDelay = 0)
  {
    if (nextAutoId != this.id + 1)  // if we're not chaining these together, we're causing problems
    {
      ETGModConsole.Log("HEY! Don't abuse Then() and And() outside of a fluent method chain!");
      return null;
    }
    FluidBulletInfo next = new FluidBulletInfo(script, this);
    for (var node = lastLayer.First; node != null; node = node.Next) {
      node.Value.runsBefore.Add(next.id);
      next.runsAfter.Add(node.Value.id);
    }
    next.waitFrames = 1 + withDelay;

    curLayer.AddLast(next);
    theChain.Add(next);
    return next;
  }

  // Run another script sequentially after all scripts in the previous layer
  public FluidBulletInfo Then(IEnumerator script, int withDelay = 0)
  {
    lastLayer = curLayer;
    curLayer = new LinkedList<FluidBulletInfo>();

    return And(script, withDelay);
  }

  // Finish up the call chain and return it
  public List<FluidBulletInfo> Finish()
  {
    return theChain;
  }

}

public abstract class FluidBulletScript : Script
{
  // Must be overridden by anyone wanting to make use of our script
  protected abstract List<FluidBulletInfo> BuildChain();

  // Entry point for running our first script
  public FluidBulletInfo Run(IEnumerator script)
  {
    return FluidBulletInfo.Run(this,script);
  }

  // Where all of the actual magic happens
  public sealed override IEnumerator Top()
  {
    // load all of our scripts from our BuildChain()
    List<FluidBulletInfo> allScripts = BuildChain();

    // add scripts with no dependencies to activeScripts
    LinkedList<int> activeScripts = new LinkedList<int>();
    for (int i = 0; i < allScripts.Count; ++i)
    {
      if (allScripts[i].id != i)
      {
        ETGModConsole.Log("HEY! You have a bad ID, do it right next time!");
        yield break;
      }
      if (allScripts[i].runsAfter.Count == 0)
        activeScripts.AddLast(i);
    }

    // while we still have scripts to run
    while (activeScripts.Count > 0)
    {
      // loop over all of our active scripts
      for(var node = activeScripts.First; node != null; node = node.Next)
      {
        // if the script has an active timeout, decrement it and continue
        FluidBulletInfo fluid = allScripts[node.Value];
        if (--fluid.waitFrames > 0)
          continue;

        // if the script is still doing stuff, get the time out and move on to the next active script
        if (fluid.script.MoveNext())
        {
          fluid.waitFrames = (int)fluid.script.Current;
          continue;
        }

        // check all other scripts that depend on us finishing
        foreach(int otherId in fluid.runsBefore)
        {
          // remove ourselves from their list of dependencies
          allScripts[otherId].runsAfter.Remove(fluid.id);
          // if they don't have any more depencies, add them to the active list
          if (allScripts[otherId].runsAfter.Count == 0)
            activeScripts.AddLast(otherId);
        }

        // mark ourselves as finished internally
        fluid.finished = true;

        // remove ourselves from the active scripts
        activeScripts.Remove(node);
      }
      yield return Wait(1);
    }
    yield break;
  }
}
