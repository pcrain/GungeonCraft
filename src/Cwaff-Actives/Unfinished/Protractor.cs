namespace CwaffingTheGungy;

public class Protractor : CwaffActive
{
    public static string ItemName         = "Protractor";
    public static string ShortDescription = "Lines of Defense";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _RulerVFX = null; // dashed line between ruler points
    private List<Vector2> _vertices      = new();
    private List<GameObject> _lines      = new();

    public static void Init()
    {
        PlayerItem item   = Lazy.SetupActive<Protractor>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        item.consumable   = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 1f);

        // _RulerVFX = VFX.Create("pencil_sparkles", 12, loops: false, anchor: Anchor.MiddleCenter);
    }

    public override void OnPreDrop(PlayerController user)
    {
        base.OnPreDrop(user);
        Cleanup();
    }

    private void Cleanup()
    {
      foreach (GameObject line in this._lines)
        line.SafeDestroy();
      this._lines.Clear();
      this._vertices.Clear();
    }

    public override bool CanBeUsed(PlayerController user)
    {
        return user.IsInCombat && base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
      Vector2 ppos = user.CenterPosition;
      if (this._vertices.Count() > 0)
      {
        Vector2 lastPos = this._vertices.Last();
        GameObject line = FancyLine(lastPos, ppos, 0.1f);
        this._lines.Add(line);
      }
      this._vertices.Add(ppos);
      this.LastOwner.gameObject.Play("chekhovs_gun_place_sound");
    }

    private float _nextCheck = 0.0f;
    public override void Update()
    {
        base.Update();
        if (this._nextCheck > BraveTime.ScaledTimeSinceStartup)
          return;
        this._nextCheck = BraveTime.ScaledTimeSinceStartup + 0.1f;
        for (int i = 1; i < this._vertices.Count; ++i)
        {
          if (!Lazy.AnyEnemyInLineOfSight(this._vertices[i-1], this._vertices[i]))
            continue;
          Vector2 delta = (this._vertices[i-1] - this._vertices[i]);
          Projectile proj = SpawnManager.SpawnProjectile(PistolWhip._PistolWhipProjectile.gameObject, this._vertices[i-1], delta.EulerZ()
            ).GetComponent<Projectile>();
            proj.Owner = this.LastOwner;
            proj.collidesWithEnemies = true;
            proj.collidesWithPlayer = false;
            this.LastOwner.gameObject.Play("whip_crack_sound");
        }
    }

    // Draw a nice tiled sprite from start to target
    public static GameObject FancyLine(Vector2 start, Vector2 target, float width, int? spriteId = null)
    {
        Vector2 delta         = target - start;
        Quaternion rot        = delta.EulerZ();
        GameObject reticle    = UnityEngine.Object.Instantiate(new GameObject(), start, rot);
        tk2dSlicedSprite quad = reticle.AddComponent<tk2dSlicedSprite>();
        quad.SetSprite(VFX.Collection, spriteId ?? VFX.Collection.GetSpriteIdByName("fancy_line"));
        quad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude, width));
        quad.transform.localRotation = rot;
        quad.transform.position      = start + (-1.5f * width * delta.normalized.Rotate(-90f));
        return reticle;
    }

}
