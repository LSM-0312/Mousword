using UnityEngine;

public static class GoblinState
{
    public class IdleState : BaseState
    {
        private Goblin goblin;
        public IdleState(Monster monster) : base(monster)
        {
            goblin = (Goblin)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            goblin.startPosition = goblin.transform.position;

            if (goblin.anim != null)
            {
                goblin.anim.SetBool("Idle", true);
            }
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            goblin.moveSpeed = 0f;
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {
            if (goblin.anim != null)
            {
                goblin.anim.SetBool("Idle", false);
            }
        }

    }

    public class NoticeState : BaseState
    {
        private Goblin goblin;
        public NoticeState(Monster monster) : base(monster)
        {
            goblin = (Goblin)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            goblin.moveDirection = goblin.player.position.x > goblin.transform.position.x ? 1 : -1; //플레이어 방향으로
            goblin.flip(-goblin.moveDirection);
            goblin.startPosition = goblin.transform.position;
            goblin.moveSpeed = goblin.constMoveSpeed;
            goblin.anim.SetBool("Run", true);
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            if (goblin.curMoveDistance > goblin.noticeMoveDistance)
            {
                goblin.isWait = true; //대기상태 갱신
                goblin.curMoveDistance = 0f; // 이동 거리 초기화

                goblin.transform.position = new Vector2(goblin.startPosition.x + goblin.moveDirection * goblin.noticeMoveDistance, goblin.transform.position.y);
            }

            // 플레이어 방향으로 이동
            //아주 잠깐 멈췄다가 플레이어를 향해 일정 거리를 전진하는 방식
            if (goblin.isWait)
            {
                goblin.moveDirection = goblin.player.position.x > goblin.transform.position.x ? 1 : -1; //플레이어 방향으로 이동
                goblin.startPosition = goblin.transform.position;
                goblin.flip(-goblin.moveDirection);
                if (goblin.isGround) { goblin.isWait = false; }
            }
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {
            goblin.anim.SetBool("Run", false);
        }

    }

    public class AttackState : BaseState
    {
        private Goblin goblin;

        public AttackState(Monster monster) : base(monster)
        {
            goblin = (Goblin)monster;
        }

        public override void stateEnter() //상태에 들어갈 때 실행되는 코드
        {
            
        }

        public override void stateUpdate() //상태에 들어갈 때 계속 실행되는 코드
        {
            if (goblin.isAnimeEnd)
            {
                goblin.groundCheck.transform.localPosition = new Vector2((goblin.player.position.x > goblin.transform.position.x ? -1 : 1)
                    * Mathf.Abs(goblin.groundCheck.transform.localPosition.x), goblin.groundCheck.transform.localPosition.y);

                if (goblin.isAttack)
                {
                    goblin.anim.SetBool("Attack", false);
                    if (goblin.distanceToPlayer < goblin.attackRadius - 0.5f && goblin.isGround)
                    {
                        goblin.moveSpeed = goblin.constMoveSpeed;
                        goblin.moveDirection = goblin.player.position.x > goblin.transform.position.x ? -1 : 1; //플레이어 반대 방향으로 카이팅
                        goblin.flip(-goblin.moveDirection);
                        goblin.anim.SetBool("Run", true);
                        goblin.anim.SetBool("Idle", false);
                    }
                    else if (goblin.distanceToPlayer > goblin.attackRadius - 0.5f || !goblin.isGround)
                    {
                        goblin.moveSpeed = 0f;
                        goblin.moveDirection = goblin.player.position.x > goblin.transform.position.x ? 1 : -1; //플레이어 방향으로 회전
                        goblin.flip(-goblin.moveDirection);
                        goblin.anim.SetBool("Run", false);
                        goblin.anim.SetBool("Idle", true);
                    }
                }
                else
                {
                    goblin.moveSpeed = 0f;
                    goblin.moveDirection = goblin.player.position.x > goblin.transform.position.x ? 1 : -1; //플레이어 방향으로 회전
                    goblin.flip(-goblin.moveDirection);
                    goblin.isAnimeEnd = false;
                    goblin.anim.SetBool("Idle", true);
                    goblin.anim.SetBool("Attack", true);       //에니메이션에 히트박스 이벤트 설정이 되어있어서 별도로 히트박스 이벤트 작성할 필요 없음
                    goblin.isAttack = true;
                }
            }
        }

        public override void stateExit() //상태에 나갈 때 실행되는 코드
        {
            goblin.anim.SetBool("Run", false);
            goblin.anim.SetBool("Attack", false);
            goblin.startPosition = goblin.transform.position;
        }

    }
}
