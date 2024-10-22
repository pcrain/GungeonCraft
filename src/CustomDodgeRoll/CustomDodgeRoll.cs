namespace CwaffingTheGungy;

public class CustomDodgeRoll : MonoBehaviour
{
    private Coroutine _activeDodgeRoll = null;

    /// <summary>The last time a dodge roll input was buffered.</summary>
    internal float _bufferTime { get; set; }

    /// <summary>Whether <see cref="ContinueDodgeRoll()"/> is currently running.</summary>
    protected internal bool _isDodging         { get; private set; }
    /// <summary>Whether the player is currently holding the dodge button.</summary>
    protected internal bool _dodgeButtonHeld   { get; internal set; }
    /// <summary>The PlayerController owner of this dodge roll.</summary>
    protected internal PlayerController _owner { get; internal set; }

    /// <summary>How many seconds in advance the dodge roll can be buffered. Set to 0 to disable buffering.</summary>
    public virtual float bufferWindow    => 0.0f;
    /// <summary>Custom logic imposing additional restrictions on whether the player can dodge roll.</summary>
    public virtual bool  canDodge        => true;
    /// <summary>Whether this dodge roll can be initiated again while it's already in progress. Incompatible with <see cref="bufferWindow"/> > 0.</summary>
    public virtual bool  canMultidodge   => false;
    /// <summary>Whether this dodge roll can be initiated while the player is not moving.</summary>
    public virtual bool  canDodgeInPlace => false;
    /// <summary>Percent by which the player's fire meter is reduced upon initiating the dodge roll (vanilla dodge rolls reduce it by 50%).</summary>
    public virtual float fireReduction   => 0.5f;

    /// <summary>Called when the player successfully begins the custom dodge roll.</summary>
    /// <param name="direction">The direction the player initiated the dodge roll in.</param>
    /// <param name="buffered">Whether the dodge roll was buffered.</param>
    /// <param name="wasAlreadyDodging">
    /// Whether we were already dodging prior to beginning this custom dodge roll. Only possible if <see cref="canMultidodge"/> returns true.
    /// </param>
    protected virtual void BeginDodgeRoll(Vector2 direction, bool buffered, bool wasAlreadyDodging)
    {
        // any dodge setup code should be here
    }

    /// <summary>
    /// Called after <see cref="BeginDodgeRoll"/> while the custom dodge roll is active.
    /// If this coroutine finishes naturally, <see cref="FinishDodgeRoll"/> is called with aborted == false.
    /// If this coroutine was aborted before finishing, <see cref="FinishDodgeRoll"/> is called with aborted == true.
    /// </summary>
    protected virtual IEnumerator ContinueDodgeRoll()
    {
        // code to execute while dodge rolling should be here
        yield break;
    }

    /// <summary>Called when the player completes the custom dodge roll (i.e., after <see cref="ContinueDodgeRoll"/> finishes).</summary>
    /// <param name="aborted">Whether <see cref="ContinueDodgeRoll"/> was ended early for any reason (multidodge, cutscene, opening a chest, etc.).</param>
    protected virtual void FinishDodgeRoll(bool aborted)
    {
        // any succesful (or aborted) dodge cleanup code should be here
    }

    /// <summary>If the custom dodge roll is active, immediately ends <see cref="ContinueDodgeRoll"/> and calls <see cref="FinishDodgeRoll"/> with aborted == true.</summary>
    public void AbortDodgeRoll()
    {
        if (!_isDodging)
            return;
        FinishDodgeRoll(aborted: true);
        if (_activeDodgeRoll != null)
        {
            _owner.StopCoroutine(_activeDodgeRoll);
            _activeDodgeRoll = null;
        }
        _isDodging = false;
    }

    internal bool TryBeginDodgeRoll(Vector2 direction, bool buffered)
    {
        if (!_owner || !canDodge || (_isDodging && !canMultidodge) || (!canDodgeInPlace && direction == Vector2.zero))
            return false;
        BeginDodgeRollInternal(direction, buffered);
        return true;
    }

    private void BeginDodgeRollInternal(Vector2 direction, bool buffered)
    {
        bool wasAlreadyDodging = _isDodging; // check if we are already in the middle of a dodge roll
        AbortDodgeRoll(); // clean up any extant dodge roll in case we are multidodging
        BeginDodgeRoll(direction, buffered, wasAlreadyDodging);
        _isDodging = true;

        // by default, we want to make sure we can put out fires at the beginning of our dodge roll
        if (fireReduction > 0.0f && _owner.CurrentFireMeterValue > 0f)
        {
            _owner.CurrentFireMeterValue = Mathf.Max(0f, _owner.CurrentFireMeterValue - fireReduction);
            if (_owner.CurrentFireMeterValue == 0f)
                _owner.IsOnFire = false;
        }

        _activeDodgeRoll = _owner.StartCoroutine(DoDodgeRollWrapper());
    }

    private IEnumerator DoDodgeRollWrapper()
    {
        IEnumerator script = ContinueDodgeRoll();
        while(_isDodging && script.MoveNext())
            yield return script.Current;
        if (_isDodging)
        {
            FinishDodgeRoll(aborted: false);
            _isDodging = false;
        }
        yield break;
    }
}
