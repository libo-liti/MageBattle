using UnityEngine;

[CreateAssetMenu(fileName = "TutorialConfig", menuName = "Game/Tutorial/Config")]
public class TutorialConfig : ScriptableObject
{
    [Tooltip("순서대로 진행될 미션 목록")]
    public TutorialMissionData[] missions;
}