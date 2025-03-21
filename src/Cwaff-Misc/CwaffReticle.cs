namespace CwaffingTheGungy;

using static CwaffReticle.Visibility;

// dummy classes to help differentiate for weapons that use multiple types (e.g., Maestro)
public class CwaffEnemyReticle : CwaffReticle {}
public class CwaffProjectileReticle : CwaffReticle {}

public class CwaffReticle : MonoBehaviour
{
  public enum Visibility
  {
    DEFAULT, // reticle is always visible while the gun's default reticle is active
    CONTROLLER, // reticle is always visible on controller while the gun's default reticle is active, and is invisible on mouse and keyboard
    CHARGING, // reticle is only visible when charging
    WITHTARGET, // reticle is only visible when it has a valid target GameObject
    ALWAYS, // reticle is always visible while the gun is active, regardless of whether the default reticle is active
  }

  public GameObject reticleVFX          = null;  // prefab VFX used for displaying the reticle
  public float      reticleAlpha        = 1f;    // alpha of the reticle when fully visible
  public float      fadeInTime          = 0.0f;  // the amount of time we spend fading in when spawned (0.0 == instant)
  public float      fadeOutTime         = 0.0f;  // the amount of time we spend fading out when despawned (0.0 == instant)
  public bool       smoothLerp          = false; // whether the reticle smoothly lerps to its target
  public bool       aimFromPlayerCenter = false; // if true, aim is relative to player center; if false, aim is relative to barrel offset
  public bool       background          = false; // whether the reticle draws in the background
  public float      maxDistance         = -1f;   // maximum distance the reticle can be from the player (-1 == no max)
  public float      controllerScale     = 1f;    // on controller, determines how the reticle scales with aim distance
  public float      rotateSpeed         = 0f;    // how quickly the reticle rotates
  public Visibility visibility          = Visibility.DEFAULT; // when the reticle is visible
  public Func<CwaffReticle, GameObject> targetObjFunc = null; // optional custom object targeting function; if null, uses targetPosFunc instead
  public Func<CwaffReticle, Vector2> targetPosFunc = null; // optional custom position targeting function; if null, follows the mouse / controller aim direction

  private GameObject _extantVfx     = null; // spawned-in vfx for our reticle
  private CwaffGun _gun             = null; // the gun we belong to
  private PlayerController _player  = null; // the player we belong to
  private Vector2 _currentPos       = Vector2.zero; // where the reticle is currently at
  private GameObject _targetObject  = null; // the GameObject the reticle is targeting
  private Vector2 _targetPos        = Vector2.zero; // where the reticle is moving towards
  private Vector2 _clampedTargetPos = Vector2.zero; // the target position, clamped to the max distance
  private bool _visible = false; // whether the reticle is currently visible
  private float _fadeProgress = 0.0f; // timer that counts upward while fading in and down while fading out

  private void Start()
  {
    this._gun = base.gameObject.GetComponent<CwaffGun>();
    this._player = this._gun.PlayerOwner;
  }

  private void LateUpdate()
  {
    if (!this._gun || !this._gun.PlayerOwner)
      return;

    this._player = this._gun.PlayerOwner;
    HandleVisibility();
    HandleTargeting();
    HandlePositioning();
  }

  private void OnDestroy()
  {
    if (this._extantVfx)
      UnityEngine.Object.Destroy(this._extantVfx);
  }

  public void HideImmediately()
  {
    this._visible = false;
    if (!this._extantVfx)
      return;
    this._extantVfx.SetAlphaImmediate(0.0f);
    if (this._gun && this._gun.PlayerOwner)
      this._extantVfx.transform.position = this._gun.PlayerOwner.CenterPosition;
  }

  public Vector2 GetTargetPos() => this._targetPos;
  public bool IsVisible() => this._visible;

  private void HandleVisibility()
  {
    if (!this._extantVfx)
    {
      this._extantVfx = UnityEngine.Object.Instantiate(this.reticleVFX, this._gun.gun.barrelOffset.transform.position, Quaternion.identity);
      if (this.background)
        this._extantVfx.SetLayerRecursively(LayerMask.NameToLayer("BG_Critical"));
      this._extantVfx.SetAlphaImmediate(0.0f);
    }

    if (!this._player.AcceptingNonMotionInput)
      this._visible = false;
    else switch (this.visibility)
    {
      case DEFAULT:    this._visible = this._player.IsKeyboardAndMouse() ? true  : this._player.m_activeActions.Aim.Vector.sqrMagnitude > 0.02f; break;
      case CONTROLLER: this._visible = this._player.IsKeyboardAndMouse() ? false : this._player.m_activeActions.Aim.Vector.sqrMagnitude > 0.02f; break;
      case CHARGING:   this._visible = this._gun.gun.IsCharging || this._gun.gun.IsFiring; break;
      case WITHTARGET: this._visible = this._targetObject != null; break;
      case ALWAYS:     this._visible = true; break;
    }

    if (this._visible)
    {
      if (this.fadeInTime > 0f && this._fadeProgress < 1f)
        this._fadeProgress = Mathf.Min(this._fadeProgress + BraveTime.DeltaTime / this.fadeInTime, 1f);
      else
        this._fadeProgress = 1f;
    }
    else
    {
      if (this.fadeOutTime > 0f && this._fadeProgress > 0f)
        this._fadeProgress = Mathf.Max(this._fadeProgress - BraveTime.DeltaTime / this.fadeOutTime, 0f);
      else
        this._fadeProgress = 0f;
    }
    this._extantVfx.SetAlpha(this.reticleAlpha * this._fadeProgress);
  }

  private void HandleTargeting()
  {
    if (!this._extantVfx || !this._player.AcceptingNonMotionInput)
      return;

    if (this.rotateSpeed > 0)
      this._extantVfx.transform.localRotation = (this.rotateSpeed * BraveTime.ScaledTimeSinceStartup).EulerZ();

    if (this.targetObjFunc != null)
    {
      this._targetObject = this.targetObjFunc(this);
      if (this._targetObject)
      {
        if (this._targetObject.GetComponent<GameActor>() is GameActor actor)
          this._targetPos = actor.CenterPosition;
        else if (this._targetObject.GetComponent<Projectile>() is Projectile proj)
          this._targetPos = proj.SafeCenter;
        else
          this._targetPos = this._targetObject.transform.position;
      }
      else if (this.targetPosFunc != null)  // use targetPosFunc as fallback when targetObjFunc fails
        this._targetPos = this.targetPosFunc(this);
    }
    else if (this.targetPosFunc != null)
      this._targetPos = this.targetPosFunc(this);
    else if (!this._player.IsKeyboardAndMouse())  //WARNING: this has caused a null dereference, but not sure how
      this._targetPos = this._player.CenterPosition + this.controllerScale * this._player.m_activeActions.Aim.Vector;
    else
      this._targetPos = this._player.unadjustedAimPoint.XY();

    if (this.maxDistance > 0)
    {
      Vector2 centerPos = this.aimFromPlayerCenter ? this._player.CenterPosition : this._gun.gun.barrelOffset.transform.position;
      Vector2 delta = (this._targetPos - centerPos);
      if (delta.sqrMagnitude > (this.maxDistance * this.maxDistance))
        this._targetPos = centerPos + this.maxDistance * delta.normalized;
    }
  }

  private void HandlePositioning()
  {
    if (!this._visible && (this._fadeProgress <= 0.0f))
      this._targetPos = this._player.CenterPosition; // reset position when invisible
    this._currentPos = this.smoothLerp ? Lazy.SmoothestLerp(this._currentPos, this._targetPos, 16f) : this._targetPos;
    this._extantVfx.transform.position = this._currentPos;
  }
}
