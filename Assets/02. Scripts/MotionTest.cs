using System.Collections.Generic;
using Pose.DetailedVisualizer;
using UnityEngine;
using static Constant;

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

    private HandPose _leftPose = HandPose.Unknown;
    private HandPose _rightPose = HandPose.Unknown;
    
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

        if (_leftStable)
            _leftPose = DetectHandPose(hand.GetLeftHandFilteredPositions());
        else
            _leftPose = HandPose.Unknown;

        if (_rightStable)
            _rightPose = DetectHandPose(hand.GetRightHandFilteredPositions());
        else
            _rightPose = HandPose.Unknown;
    }

    private HandPose DetectHandPose(IReadOnlyList<Vector3> handPos)
    {
        // bool thumb = IsFingerExtended(handPos, (int)HandJoint.ThumbTip, (int)HandJoint.ThumbMcp);
        bool index = IsFingerExtended(handPos, (int)HandJoint.IndexTip, (int)HandJoint.IndexMcp);
        bool middle = IsFingerExtended(handPos, (int)HandJoint.MiddleTip, (int)HandJoint.MiddleMcp);
        bool ring = IsFingerExtended(handPos, (int)HandJoint.RingTip, (int)HandJoint.RingMcp);
        bool pinky = IsFingerExtended(handPos, (int)HandJoint.PinkyTip, (int)HandJoint.PinkyMcp);

        if (!index && !middle && !ring && !pinky) return HandPose.Fist;
        if (index && !middle && !ring && !pinky) return HandPose.Pointing;
        if (!index && !middle && !ring && pinky) return HandPose.Pinky;
        if (index && middle && !ring && !pinky) return HandPose.Victory;
        if (index && !middle && !ring && pinky) return HandPose.RockSign;
        if (index && middle && ring && !pinky) return HandPose.Three;
        if (index && middle && ring && pinky) return HandPose.OpenHand;

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

        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Left Pose : {_leftPose}");
        y += lineHeight;

        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Right Pose : {_rightPose}");
    }
}