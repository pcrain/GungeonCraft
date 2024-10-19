namespace CwaffingTheGungy;

public class CustomDodgeRoll : MonoBehaviour
{
    protected internal bool _isDodging         { get; private set; }
    protected internal bool _dodgeButtonHeld   { get; internal set; }
    protected internal PlayerController _owner { get; internal set; }

    public virtual bool canDodge        => true;
    public virtual bool canMultidodge   => false;
    public virtual bool canDodgeInPlace => false;
    public virtual bool putsOutFire     => true;

    protected virtual void BeginDodgeRoll(Vector2 direction)
    {
        // any dodge setup code should be here
    }

    protected virtual IEnumerator ContinueDodgeRoll()
    {
        // code to execute while dodge rolling should be here
        yield break;
    }

    protected virtual void FinishDodgeRoll(bool aborted = false)
    {
        // any succesful (or aborted) dodge cleanup code should be here
    }

    public void AbortDodgeRoll()
    {
        if (!_isDodging)
            return;
        FinishDodgeRoll(aborted: true);
        _isDodging = false;
    }

    private void BeginDodgeRollInternal(Vector2 direction)
    {
        BeginDodgeRoll(direction);
        _isDodging = true;

        // by default, we want to make sure we can put out fires at the beginning of our dodge roll
        if (this.putsOutFire && this._owner.CurrentFireMeterValue > 0f)
        {
            this._owner.CurrentFireMeterValue = Mathf.Max(0f, this._owner.CurrentFireMeterValue - 0.5f);
            if (this._owner.CurrentFireMeterValue == 0f)
                this._owner.IsOnFire = false;
        }

        _owner.StartCoroutine(DoDodgeRollWrapper());
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

    internal bool TryBeginDodgeRoll(Vector2 direction)
    {
        if (!_owner || !canDodge || (_isDodging && !canMultidodge) || (!canDodgeInPlace && direction == Vector2.zero))
            return false;
        BeginDodgeRollInternal(direction);
        return true;
    }
}
