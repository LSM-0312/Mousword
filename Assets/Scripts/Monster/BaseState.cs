using UnityEngine;

public abstract class BaseState
{
    protected Monster monster;

    protected BaseState(Monster monster)
    {
        this.monster = monster;
    }

    public abstract void stateEnter();
    public abstract void stateUpdate();
    public abstract void stateExit();
}
