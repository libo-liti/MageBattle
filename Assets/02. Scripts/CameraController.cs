using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera firstPersonCam;
    [SerializeField] private CinemachineCamera thirdPersonCam;
    
    [Header("Priority Settings")]
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;
    
    [Header("Third Person Hold Time")]
    [SerializeField] private float thirdPersonDuration = 2.0f;

    private void Start()
    {
        SwitchToFirstPerson();
    }

    public void SwitchToFirstPerson()
    {
        firstPersonCam.Priority = activePriority;
        thirdPersonCam.Priority = inactivePriority;
    }

    public void SwitchToThirdPerson()
    {
        firstPersonCam.Priority = inactivePriority;
        thirdPersonCam.Priority = activePriority;
    }

    public void ShowThirdPersonBriefly()
    {
        SwitchToThirdPerson();
        Invoke(nameof(SwitchToFirstPerson), thirdPersonDuration);
    }
}
