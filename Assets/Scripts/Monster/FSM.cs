using UnityEngine;

public class FSM
{
    public FSM(BaseState initialState)
    {
        curState = initialState;
        changeState(curState);
    }

    protected BaseState curState;

    public void changeState(BaseState newState)
    {
        if (newState.GetType() == curState.GetType()) { return; }

        if (curState != null) { curState.stateExit(); }

        curState = newState;
        curState.stateEnter();
    }

    public void stateUpdate()
    {
        if (curState != null) curState.stateUpdate();
    }
}
