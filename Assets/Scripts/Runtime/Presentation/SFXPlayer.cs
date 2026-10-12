using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXPlayer : MonoBehaviour
{
    [SerializeField] AudioClip landingSound;
    [SerializeField] AudioClip resolveSound;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayLanding()
    {
        if (landingSound != null)
            audioSource.PlayOneShot(landingSound);
    }

    public void PlayResolve()
    {
        if (resolveSound != null)
            audioSource.PlayOneShot(resolveSound);
    }
}