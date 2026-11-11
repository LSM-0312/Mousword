using UnityEngine;

public static class CommonState
{
    public class DieState : BaseState
    {
        private Monster monster;
        public DieState(Monster monster) : base(monster)
        {
            this.monster = monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            monster.moveSpeed = 0f;
            if (monster.anim != null)
            {
                monster.anim.SetTrigger("Die");
            }
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {

        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {

        }

    }
}
