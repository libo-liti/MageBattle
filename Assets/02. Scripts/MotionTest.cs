using System.Collections.Generic;
using Pose.DetailedVisualizer;
using UnityEngine;
using static Constant;

struct FingerStates
{
    public readonly bool index, middle, ring, pinky;
    public bool OnlyIndex => index && !middle && !ring && !pinky;
    public bool AllExtended => index && middle && ring && pinky;
    public bool AllFolded => !index && !middle && !ring && !pinky;
    
    public FingerStates(bool index, bool middle, bool ring, bool pinky)
    {
        this.index = index;
        this.middle = middle;
        this.ring = ring;
        this.pinky = pinky;
    }
}

struct FingerTouches
{
    private readonly bool _index, _middle, _ring, _pinky, _indexPip;
    public bool OnlyIndexTip => _index && !_middle && !_ring && !_pinky && !_indexPip;
    public bool OnlyIndexPip => !_index && !_middle && !_ring && !_pinky && _indexPip;
    public bool AllTouches => _index && _middle && _ring && _pinky;

    public FingerTouches(bool index, bool middle, bool ring, bool pinky, bool indexPip)
    {
        _index = index;
        _middle = middle;
        _ring = ring;
        _pinky = pinky;
        _indexPip = indexPip;
    }
}

public class MotionTest : MonoBehaviour
{
    [SerializeField] private HandVisualizer hand;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;

    private Vector3 _leftWristCached;
    private Vector3 _rightWristCached;
    private float _wristDistance;
    
    public int falseFrameNeeded = 20;

    private bool _leftStable = false;
    private int _leftConsecutiveFalse = 0;
    
    private bool _rightStable = false;
    private int _rightConsecutiveFalse = 0;

    private HandPose _pose = HandPose.Unknown;
    
    private void Update()
    {
        var leftRaw = hand.IsLeftHandDetected();
        var rightRaw = hand.IsRightHandDetected();

        if (leftRaw)
        {
            _leftStable = true;
            _leftConsecutiveFalse = 0;
        }
        else
        {
            _leftConsecutiveFalse += 1;
            if (_leftConsecutiveFalse >= falseFrameNeeded)
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
            if (_rightConsecutiveFalse >= falseFrameNeeded)
                _rightStable = false;
        }
        
        leftHand.SetActive(_leftStable);
        rightHand.SetActive(_rightStable);
        
        if (_leftStable && _rightStable)
        {
            _leftWristCached = hand.GetLeftHandFilteredPositions()[(int)HandJoint.Wrist];
            _rightWristCached = hand.GetRightHandFilteredPositions()[(int)HandJoint.Wrist];
            _wristDistance = Vector3.Distance(_leftWristCached, _rightWristCached);
        }
        else
        {
            _wristDistance = -1f;
        }
        
        if (_leftStable && _rightStable)
        {
            _pose = DetectHandPose(hand.GetLeftHandFilteredPositions(), hand.GetRightHandFilteredPositions());
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
    
    private void OnGUI()
    {
        float boxX = 10, boxY = 10;
        float boxW = 300, boxH = 200;
        GUI.Box(new Rect(boxX, boxY, boxW, boxH), "Hand Debug");

        float x = boxX + 10;
        float y = boxY + 25;
        float lineHeight = 22;
        float labelWidth = boxW - 20;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Left : {_leftWristCached.x:F2}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"right : {_rightWristCached.x:F2}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Distance : {(_wristDistance >= 0 ? _wristDistance.ToString("F2") : "N/A")}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Stable : L={_leftStable} R={_rightStable}");
        y += lineHeight;

        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Pose : {_pose}");
    }
}