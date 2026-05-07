using System.Collections.Generic;
using Pose.DetailedVisualizer;
using UnityEngine;
using static Constant;

public class HandRecognizer
{
    private HandVisualizer _hand;
    private GameObject _leftHandObj;
    private GameObject _rightHandObj;

    private const int FalseFrameNeeded = 20;

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
        
        _leftHandObj.SetActive(_leftStable);
        _rightHandObj.SetActive(_rightStable);
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

        bool sameLeftRightPose = (left.index == right.index)
                                 && (left.middle == right.middle)
                                 && (left.ring == right.ring)
                                 && (left.pinky == right.pinky);

        if (!sameLeftRightPose) return HandPose.Unknown;

        if (left.AllExtended && touches.AllTouches) return HandPose.Wind;
        if (left.AllFolded && touches.OnlyIndexPip) return HandPose.Land;
        if (left.AllExtended && touches.OnlyIndexTip) return HandPose.Water;
        if (left.OnlyIndex && touches.OnlyIndexTip) return HandPose.Fire;

        if (touches.NoneTouched)
        {
            if (left.AllFolded) return HandPose.Attack;
            if (left.AllExtended) return HandPose.Defense;
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
