using UnityEngine;

public static class SlimeState
{
    public class IdleState : BaseState
    {
        private Slime slime;
        public IdleState(Monster monster) : base(monster)
        {
            slime = (Slime)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            slime.startPosition = slime.transform.position;
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            slime.moveSpeed = slime.constMoveSpeed;

            if (slime.curMoveDistance > slime.maxIdleMoveDistance)
            {
                slime.isWait = true; //대기상태 갱신
                slime.curMoveDistance = 0f; //이동 거리 초기화

                slime.transform.position = new Vector2(slime.startPosition.x + slime.moveDirection * slime.maxIdleMoveDistance, slime.transform.position.y);
            }

            if (slime.isWait)
            {
                if (slime.curWaitTime < slime.maxWaitTime)
                {
                    slime.isWait = true;
                    slime.curWaitTime += Time.deltaTime;
                }
                else if (slime.curWaitTime >= slime.maxWaitTime || !slime.isGround)
                {
                    slime.moveDirection = (int)(slime.startPosition.x) <= (int)(slime.transform.position.x) ? -1 : 1; //이동 방향 반전
                    slime.flip(slime.moveDirection);                                             //좌우 반전
                    slime.isWait = false;
                    slime.curWaitTime = 0f;
                }
            }
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {
            
        }

    }

    public class NoticeState : BaseState
    {
        private Slime slime;
        public NoticeState(Monster monster) : base(monster)
        {
            slime = (Slime)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            slime.flip(slime.player.position.x > slime.transform.position.x ? 1 : -1);
            slime.startPosition = slime.transform.position;
            slime.curMoveDistance = 0f; // 이동 거리 초기화
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            slime.moveSpeed = slime.constMoveSpeed * 1.2f;

            if (slime.curMoveDistance > slime.noticeMoveDistance)
            {
                slime.isWait = true; //대기상태 갱신
                slime.curMoveDistance = 0f; // 이동 거리 초기화

                slime.transform.position = new Vector2(slime.startPosition.x + slime.moveDirection * slime.noticeMoveDistance, slime.transform.position.y);
            }

            // 플레이어 방향으로 이동
            //아주 잠깐 멈췄다가 플레이어를 향해 일정 거리를 전진하는 방식
            if (slime.isWait)
            {
                slime.moveDirection = slime.player.position.x > slime.transform.position.x ? 1 : -1; //플레이어 방향으로 이동
                slime.startPosition = slime.transform.position;
                slime.flip(slime.moveDirection);
                if (slime.isGround) { slime.isWait = false; }
            }
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {

        }

    }

    public class AttackState : BaseState
    {
        private Slime slime;
        public AttackState(Monster monster) : base(monster)
        {
            slime = (Slime)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            slime.moveSpeed = 0f;
            if (slime.anim != null)
            {
                slime.anim.SetBool("Attack", true);       //에니메이션에 히트박스 이벤트 설정이 되어있어서 별도로 히트박스 이벤트 작성할 필요 없음
                                                          //slime.isAnimeEnd = false;
            }
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            if (slime.isAnimeEnd) { slime.flip(slime.player.position.x > slime.transform.position.x ? 1 : -1);}
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {
            if (slime.anim != null)
            {
                slime.anim.SetBool("Attack", false);      //공격 애니메이션 끔
                slime.isAnimeEnd = true;
            }
        }

    }
}
