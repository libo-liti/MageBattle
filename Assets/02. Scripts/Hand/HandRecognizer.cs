using System.Collections.Generic;
using Pose.DetailedVisualizer;
using UnityEngine;
using static Constant;

public class HandRecognizer
{
    private HandVisualizer _hand;
    private GameObject _leftHandObj;
    private GameObject _rightHandObj;

    public int FalseFrameNeeded = 20;
    public bool ShowFeedback = true;

    private bool _leftStable;
    private int _leftConsecutiveFalse;
    private bool _rightStable;
    private int _rightConsecutiveFalse;

    private Vector3 _leftWristCached;
    private Vector3 _rightWristCached;
    private float _wristDistance;
    private HandPose _pose = HandPose.Unknown;

    public HandPose CurrentPose => _pose;
    public bool LeftStable => _leftStable;
    public bool RightStable => _rightStable;
    public bool BothStable => _leftStable && _rightStable;
    public Vector3 LeftWrist => _leftWristCached;
    public Vector3 RightWrist => _rightWristCached;
    public float WristDistance => _wristDistance;

    // [포트폴리오용 추가] 시각화 컴포넌트에 관절 전체 좌표를 노출
    // (Pose 폴더에 직접 의존하지 않고 HandRecognizer만 바라보도록 패스스루)
    public IReadOnlyList<Vector3> LeftHandJoints => _hand.GetLeftHandFilteredPositions();
    public IReadOnlyList<Vector3> RightHandJoints => _hand.GetRightHandFilteredPositions();

    public HandRecognizer(HandVisualizer hand, GameObject leftObj, GameObject rightObj)
    {
        _hand = hand;
        _leftHandObj = leftObj;
        _rightHandObj = rightObj;
    }

    public void Update()
    {
        UpdateStability();
        UpdateWristCache();
        UpdatePose();
    }

    private void UpdateStability()
    {
        var leftRaw = _hand.IsLeftHandDetected();
        var rightRaw = _hand.IsRightHandDetected();

        if (leftRaw)
        {
            _leftStable = true;
            _leftConsecutiveFalse = 0;
        }
        else
        {
            _leftConsecutiveFalse += 1;
            if (_leftConsecutiveFalse >= FalseFrameNeeded)
                _leftStable = false;
        }

        if (rightRaw)
        {
            _rightStable = true;
            _rightConsecutiveFalse = 0;
        }
        else
        {
            _rightConsecutiveFalse += 1;
            if (_rightConsecutiveFalse >= FalseFrameNeeded)
                _rightStable = false;
        }
        
        _leftHandObj.SetActive(ShowFeedback && _leftStable);
        _rightHandObj.SetActive(ShowFeedback && _rightStable);
    }

    private void UpdateWristCache()
    {
        if (_leftStable && _rightStable)
        {
            _leftWristCached = _hand.GetLeftHandFilteredPositions()[(int)HandJoint.Wrist];
            _rightWristCached = _hand.GetRightHandFilteredPositions()[(int)HandJoint.Wrist];
            _wristDistance = Vector3.Distance(_leftWristCached, _rightWristCached);
        }
        else
        {
            _wristDistance = -1f;
        }
    }

    private void UpdatePose()
    {
        if (_leftStable && _rightStable)
        {
            _pose = DetectHandPose(_hand.GetLeftHandFilteredPositions(), _hand.GetRightHandFilteredPositions());
        }
        else
            _pose = HandPose.Unknown;
    }

    private HandPose DetectHandPose(IReadOnlyList<Vector3> leftPos, IReadOnlyList<Vector3> rightPos)
    {
        FingerStates left = GetFingerStates(leftPos);
        FingerStates right = GetFingerStates(rightPos);
        FingerTouches touches = GetFingerTouches(leftPos, rightPos);

        // ── Land: 주먹 마주보고 닿기 ─────────────────────────────────────
        // sameLeftRightPose 게이트보다 먼저 확인해야 함.
        // 주먹을 마주보게 들면 카메라 앵글이 좌우 비대칭해져서 MediaPipe가
        // 두 손의 손가락 상태를 다르게 인식 → sameLeftRightPose = false 됨.
        // 그러면 기존 구조에서는 Land 체크에 절대 도달 못함.

        // 손목 거리 (너클 뒤쪽이라 약간 더 멀게 나옴)
        float wristDist = Vector2.Distance(
            new Vector2(leftPos[(int)HandJoint.Wrist].x, leftPos[(int)HandJoint.Wrist].y),
            new Vector2(rightPos[(int)HandJoint.Wrist].x, rightPos[(int)HandJoint.Wrist].y)
        );
        // 너클(IndexMcp) 거리: 주먹을 마주보고 닿힐 때 가장 가까워지는 지점
        float knuckleDist = Vector2.Distance(
            new Vector2(leftPos[(int)HandJoint.IndexMcp].x, leftPos[(int)HandJoint.IndexMcp].y),
            new Vector2(rightPos[(int)HandJoint.IndexMcp].x, rightPos[(int)HandJoint.IndexMcp].y)
        );

        // mostlyFolded: 손 겹침 오클루전으로 ring/pinky가 오인식돼도
        // index·middle은 상대적으로 신뢰할 수 있음
        bool leftMostlyFolded  = !left.index  && !left.middle;
        bool rightMostlyFolded = !right.index && !right.middle;
        bool fistsTouching     = wristDist < 0.30f || knuckleDist < 0.20f;

        // 조건 A: 엄밀 판정 — 손 약간 떨어진 경우, IndexPip 닿음으로 확인
        if (left.AllFolded && right.AllFolded && touches.OnlyIndexPip) return HandPose.Land;
        // 조건 B: 너그러운 판정 — 주먹 맞닿을 때 오클루전 대응
        if (leftMostlyFolded && rightMostlyFolded && fistsTouching)    return HandPose.Land;

        // ── 나머지 포즈: 양손 대칭 필요 ────────────────────────────────
        bool sameLeftRightPose = (left.index == right.index)
                                 && (left.middle == right.middle)
                                 && (left.ring == right.ring)
                                 && (left.pinky == right.pinky);
        if (!sameLeftRightPose) return HandPose.Unknown;

        if (left.AllExtended && touches.AllTouches)  return HandPose.Wind;
        if (left.AllExtended && touches.OnlyIndexTip) return HandPose.Water;
        if (left.OnlyIndex   && touches.OnlyIndexTip) return HandPose.Fire;

        if (touches.NoneTouched)
        {
            if (left.AllFolded)    return HandPose.Attack;
            if (left.AllExtended)  return HandPose.Defense;
            if (left.ThreeFingers) return HandPose.Special;
        }

        return HandPose.Unknown;
    }
    private bool IsFingerExtended(IReadOnlyList<Vector3> handPos, int tipJoint, int mcpJoint)
    {
        if(handPos[tipJoint].y >= handPos[mcpJoint].y)
            return false;

        float palmLength = GetPalmLength(handPos);
        float fingerExtension = Vector3.Distance(handPos[tipJoint], handPos[mcpJoint]);
        return fingerExtension > palmLength * 0.6f;
    }

    private float GetPalmLength(IReadOnlyList<Vector3> handPos)
    {
        Vector3 wrist = handPos[(int)HandJoint.Wrist];
        Vector3 indexMcp = handPos[(int)HandJoint.IndexMcp];
        return Vector3.Distance(wrist, indexMcp);
    }

    private FingerStates GetFingerStates(IReadOnlyList<Vector3> handPos)
    {
        return new FingerStates
        (
            IsFingerExtended(handPos, (int)HandJoint.IndexTip, (int)HandJoint.IndexMcp),
            IsFingerExtended(handPos, (int)HandJoint.MiddleTip, (int)HandJoint.MiddleMcp),
            IsFingerExtended(handPos, (int)HandJoint.RingTip, (int)HandJoint.RingMcp),
            IsFingerExtended(handPos, (int)HandJoint.PinkyTip, (int)HandJoint.PinkyMcp)
        );
    }

    private FingerTouches GetFingerTouches(IReadOnlyList<Vector3> leftPos, IReadOnlyList<Vector3> rightPos)
    {
        return new FingerTouches
        (
            Vector2.Distance(leftPos[(int)HandJoint.IndexTip], rightPos[(int)HandJoint.IndexTip]) < 0.1f,
            Vector2.Distance(leftPos[(int)HandJoint.MiddleTip], rightPos[(int)HandJoint.MiddleTip]) < 0.1f,
            Vector2.Distance(leftPos[(int)HandJoint.RingTip], rightPos[(int)HandJoint.RingTip]) < 0.1f,
            Vector2.Distance(leftPos[(int)HandJoint.PinkyTip], rightPos[(int)HandJoint.PinkyTip]) < 0.1f,
            Vector2.Distance(leftPos[(int)HandJoint.IndexPip], rightPos[(int)HandJoint.IndexPip]) < 0.1f
        );
    }
}
