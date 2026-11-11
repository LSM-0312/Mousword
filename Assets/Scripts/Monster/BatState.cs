using UnityEngine;

public static class BatState
{
    public class IdleState : BaseState
    {
        private Bat bat;
        public IdleState(Monster monster) : base(monster)
        {
            bat = (Bat)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            bat.startPosition = bat.transform.position;
            bat.moveSpeed = 0;
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {

        }

    }
    public class AttackState : BaseState
    {
        private Bat bat;
        public AttackState(Monster monster) : base(monster)
        {
            bat = (Bat)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            bat.startPosition = bat.transform.position;
            bat.curMoveDistance = 0f;
            bat.curWaitTime = 0f;
            bat.rotate();
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            if (!bat.isWait)
            {
                bat.moveSpeed = bat.constMoveSpeed;
                if (bat.anim != null) bat.anim.SetBool("Attack", true);

                if (bat.curMoveDistance >= bat.attackMoveDistance || bat.isGround)
                {
                    bat.isWait = true;             
                    bat.curMoveDistance = 0f;
                    bat.moveSpeed = 0f; 

                    bat.startPosition = bat.transform.position; // 자기 위치 저장
                    bat.rotate();                   // 플레이어 바라봄
                }
            }
            else
            {
                if (bat.anim != null) bat.anim.SetBool("Attack", false);

                if (bat.curWaitTime < bat.maxWaitTime)
                {
                    bat.curWaitTime += Time.deltaTime;
                }
                else
                {
                    bat.rotate();
                    bat.startPosition = bat.transform.position; 
                    bat.isWait = false;                        
                    bat.curWaitTime = 0f;                       
                    bat.curWaitTime = 0f;                       
                }
            }
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {
            if (bat.anim != null)
            {
                bat.anim.SetBool("Attack", false);      //공격 애니메이션 끔
            }
        }

    }
}
