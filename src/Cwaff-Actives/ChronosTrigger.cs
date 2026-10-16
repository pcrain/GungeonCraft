namespace CwaffingTheGungy;

public class ChronosTrigger : CwaffActive
{
    public static string ItemName         = "Chronos' Trigger";
    public static string ShortDescription = "A Timeless Classic";
    public static string LongDescription  = "Halts the flow of time for all enemy projectiles around the player for a short duration. Bullets cannot collide with the player while halted.";
    public static string Lore             = "A large, egg-shaped device with a revolver's trigger inexplicably formed into its core. Pulling the trigger doesn't fire any bullets, but does have a noticeable effect on bullets fired at you. Observing this effect leads you to the revolutionary conclusion that bullets are rather useless without the flow of time on their side.";

    private Material _renderPass = null;
    private Coroutine _activeTimestop = null;

    public static void Init()
    {
        PlayerItem item   = Lazy.SetupActive<ChronosTrigger>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.S;
        item.consumable   = false;
        item.usableDuringDodgeRoll = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 5f);
    }

    public override void DoEffect(PlayerController user)
    {
        this.IsCurrentlyActive = true;
        this._activeTimestop = base.StartCoroutine(StopTheWorld(user));
    }

    public override void OnPreDrop(PlayerController player)
    {
        Deactivate();
        base.OnPreDrop(player);
    }

    private void Deactivate()
    {
        HandleTimeStopInRadius(Vector2.zero, 0);
        if (this._activeTimestop != null)
          StopCoroutine(this._activeTimestop);
        this._activeTimestop = null;
        if (this._renderPass != null)
          Pixelator.Instance.DeregisterAdditionalRenderPass(this._renderPass);
        this._renderPass = null;
        this.IsCurrentlyActive = false;
    }

    private IEnumerator StopTheWorld(PlayerController user)
    {
        const float GROW_TIME = 3.0f;
        const float STAY_TIME = 4.0f;
        const float FADE_TIME = 3.0f;
        const float MAX_RADIUS = 8.0f;
        const float TOTAL_TIME = GROW_TIME + STAY_TIME + FADE_TIME;

        user.gameObject.Play("chronos_trigger_activate_sound");
        SpeculativeRigidbody body = user.gameObject.GetComponent<SpeculativeRigidbody>();
        CameraController cam = GameManager.Instance.MainCameraController;
        this._renderPass = new Material(CwaffShaders.RadialTimestopShader);
        Pixelator.Instance.RegisterAdditionalRenderPass(this._renderPass);

        for (float elapsed = 0f; elapsed < TOTAL_TIME; elapsed += BraveTime.DeltaTime)
        {
            float size = 0.0f;
            if (elapsed < GROW_TIME)
              size = elapsed / GROW_TIME;
            else if (elapsed > (TOTAL_TIME - FADE_TIME))
              size = (TOTAL_TIME - elapsed) / FADE_TIME;
            else
              size = 1.0f;
            float radius = Ease.OutQuad(size) * MAX_RADIUS;
            Vector2 center = body.UnitCenter;
            HandleTimeStopInRadius(center, radius);
            Vector2 camMin = cam.MinVisiblePoint;
            Vector2 camMax = cam.MaxVisiblePoint;
            this._renderPass.SetVector("_CamSize", new Vector4(camMin.x, camMin.y, (camMax.x - camMin.x), (camMax.y - camMin.y)));
            this._renderPass.SetVector("_WorldCenter", new Vector4(center.x, center.y, 0f, 0f));
            this._renderPass.SetFloat("_Radius", radius);
            yield return null;
        }
        Deactivate();
        yield break;
    }

    private static Vector2 SetVelocityToZero(Vector2 _) => Vector2.zero;

    private static void CantCollideWithPlayer(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (otherRigidbody.gameActor is PlayerController player)
          PhysicsEngine.SkipCollision = true;
    }

    private static void HandleTimeStopInRadius(Vector2 center, float radius)
    {
      const float MIN_SPEED = 5f;
      float sqrRadius = radius * radius;
      foreach (Projectile p in StaticReferenceManager.AllProjectiles)
      {
        if (!p || p.Owner is PlayerController player)
          continue;
        SpeculativeRigidbody body = p.specRigidbody;
        if (body)
          body.OnPreRigidbodyCollision -= CantCollideWithPlayer;
        p.ModifyVelocity -= SetVelocityToZero;
        if ((p.SafeCenter - center).sqrMagnitude >= sqrRadius)
          continue;
        if (p.IsBulletScript)
        {
          p.SendInDirection(p.braveBulletScript.bullet.Direction.ToVector(), false);
          p.SetSpeed(Mathf.Max(p.braveBulletScript.bullet.Speed, MIN_SPEED));
          p.RemoveBulletScriptControl();
        }
        p.ModifyVelocity += SetVelocityToZero;
        if (body)
          body.OnPreRigidbodyCollision += CantCollideWithPlayer;
      }
    }
}
