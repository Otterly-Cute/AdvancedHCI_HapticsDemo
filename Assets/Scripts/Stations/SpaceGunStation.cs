using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using HapticsDemo;
using Oculus.Haptics;

public class SpaceGunStation : HapticStationBase
{
    [Header("Child Parts")]
    [SerializeField]
    XRGrabInteractable gun;

    [SerializeField]
    AudioSource fireAudio;

    [Header("Config")]
    [SerializeField]
    HapticConfig fireConfig;

    [Header("Projectile Settings")]
    [SerializeField]
    GameObject projectilePrefab;

    [SerializeField]
    Transform projectileSpawnPoint;

    [SerializeField]
    float projectileSpeed = 20f;

    HandTarget hand = HandTarget.Right;
    private HapticClipPlayer firePlayer;

    protected override void Awake()
    {
        base.Awake();
        if (fireConfig != null && fireConfig.advancedClip != null)
        {
            firePlayer = new HapticClipPlayer(fireConfig.advancedClip);
        }
    }

    void OnValidate()
    {
        if (!gun)
            gun = GetComponentInChildren<XRGrabInteractable>(true);
        if (!fireAudio)
            fireAudio = GetComponentInChildren<AudioSource>(true);

        if (!projectileSpawnPoint && gun)
            projectileSpawnPoint = gun.transform;
    }

    void OnEnable()
    {
        if (!gun)
            return;
        gun.selectEntered.AddListener(a => hand = HandFrom(a.interactorObject, HandTarget.Right));
        gun.activated.AddListener(OnFire);
    }

    void OnDisable()
    {
        if (!gun)
            return;
        gun.selectEntered.RemoveAllListeners();
        gun.activated.RemoveAllListeners();
    }

    void OnFire(ActivateEventArgs _)
    {
        PlayOneShot(fireAudio, fireConfig);
        if (UseAdvanced)
        {
            if (firePlayer != null)
            {
                firePlayer.amplitude = fireConfig.advancedAmplitude;
                firePlayer.Play(HapticPlayer.ToMeta(hand));
            }
        }
        else
        {
            PlayBasic(fireConfig, hand);
        }

        if (projectilePrefab && projectileSpawnPoint)
        {
            GameObject projectile = Instantiate(
                projectilePrefab,
                projectileSpawnPoint.position,
                projectileSpawnPoint.rotation
            );

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = projectileSpawnPoint.forward * projectileSpeed;
            }

            Destroy(projectile, 3f);
        }
    }
}
