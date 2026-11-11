using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private Collider2D doorCollider;

    [SerializeField] private string dropTriggerName = "Drop";
    [SerializeField] private string dropStateName = "RockDrop";
    [SerializeField] private string speedParameterName = "Speed";

    [SerializeField] private float openDuration = 0.5f;

    private List<GameObject> trackedMonsters = new List<GameObject>();
    private bool isClose = false;

    private void Awake()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
        }

        if (doorCollider == null)
        {
            doorCollider = GetComponent<Collider2D>();
        }
    }



    public void CloseDoor()
    {
        if (isClose)
        {
            return;
        }

        isClose = true;

        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(speedParameterName, 1f);
            doorAnimator.Play(dropStateName, 0, 0.0f);
        }

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }
    }

    public void TrackMonsters(List<GameObject> monsters)
    {
        trackedMonsters.AddRange(monsters);
        StartCoroutine(MonitorMonsterRoutine());
    }

    private IEnumerator MonitorMonsterRoutine()
    {
        while (trackedMonsters.Count > 0)
        {
            yield return new WaitForSeconds(0.3f);

            trackedMonsters.RemoveAll(monster => monster == null);
        }

        OpenDoor();
    }

    public void OpenDoor()
    {
        isClose = false;

        if (doorAnimator != null)
        {
            doorAnimator.ResetTrigger(dropTriggerName);
            doorAnimator.SetFloat(speedParameterName, -1f);
            doorAnimator.Play(dropStateName, 0, 1.0f);
        }

        if (doorCollider != null)
        {
            StartCoroutine(DisableCollider(openDuration));
        }
    }

    private IEnumerator DisableCollider(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }
    }
}