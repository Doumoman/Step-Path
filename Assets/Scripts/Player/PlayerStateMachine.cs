public interface IPlayerState
{
    void Enter();
    void Tick();
    void Exit();
}

public class PlayerStateMachine
{
    private IPlayerState curState;
    private IPlayerState previousState;

    public void ChangeState(IPlayerState newState)
    {
        if (curState != null)
        {
            previousState = curState;
            curState.Exit();
        }

        curState = newState;
        curState?.Enter();
    }

    public void RestorePreviousState()
    {
        if (previousState != null)
        {
            ChangeState(previousState);
        }
    }

    public IPlayerState GetCurrentState() => curState;
    public IPlayerState GetPreviousState() => previousState;

    public void Update()
    {
        curState?.Tick();
    }
}