using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class StagePortal : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private string targetSpawnPointName;
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private ParticleSystem dustParticle;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [SerializeField] private float doorDropDuration = 0.25f;
    [SerializeField] private float waitAfterHit = 0.5f;

    private bool isActivated = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(GameManager.Instance != null && GameManager.Instance.IsTransitioning)
        {
            return;
        }

        if(collision.CompareTag("Player") && !isActivated)
        {
            isActivated = true;
            StartCoroutine(CinematicDoorSequence());
        }
    }

    private IEnumerator CinematicDoorSequence()
    {
        if(doorAnimator != null)
        {
            doorAnimator.SetFloat("Speed", 1f);
            doorAnimator.Play("RockDrop", 0, 0f);
        }

        yield return new WaitForSeconds(doorDropDuration);

        if(impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        if(dustParticle != null)
        {
            dustParticle.Play();
        }

        yield return new WaitForSeconds(waitAfterHit);

        GameManager.Instance.TransitionToNextScene(nextSceneName, targetSpawnPointName);
    }
}
