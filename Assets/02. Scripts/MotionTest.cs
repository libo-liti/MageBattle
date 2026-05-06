using System;
using System.Collections.Generic;
using Pose.DetailedVisualizer;
using UnityEngine;
using static Constant;

public class MotionTest : MonoBehaviour
{
    private const float REQUIRED_HOLD = 1.0f;
    private const float TOTAL_DURATION = 10.0f;
    
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

    private CasterContext _player = new CasterContext();
    private AICaster _ai = new AICaster();
    private RoundManager _roundManager;

    private BattleState _prevPlayerState = BattleState.Idle;

    private void Awake()
    {
        _roundManager = new RoundManager(_player, _ai);
    }

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

        UpdateCaster(_player, _pose);

        if (_prevPlayerState == BattleState.Idle && _player.state == BattleState.ElementCharging)
        {
            _ai.StartRound();
            _roundManager.StartRound();
        }
        _prevPlayerState = _player.state;
        
        _ai.Update(Time.deltaTime);
        _roundManager.Update();
    }

    private void UpdateCaster(CasterContext ctx, HandPose pose)
    {
        switch (ctx.state)
        {
            case BattleState.Idle:
                if (IsElement(pose))
                {
                    ctx.state = BattleState.ElementCharging;
                    ctx.chargingElement = pose;
                    ctx.holdTime = 0f;
                    ctx.totalTime = 0;
                }
                break;
            case BattleState.ElementCharging:
                ctx.totalTime += Time.deltaTime;

                if (ctx.totalTime >= TOTAL_DURATION)
                {
                    ctx.state = BattleState.Idle;
                    ctx.holdTime = 0f;
                    ctx.totalTime = 0f;
                    ctx.chargingElement = HandPose.Unknown;
                    break;
                }

                if (IsElement(pose))
                {
                    if (pose == ctx.chargingElement)
                    {
                        ctx.holdTime += Time.deltaTime;
                        if (ctx.holdTime >= REQUIRED_HOLD)
                        {
                            ctx.confirmedElement = ctx.chargingElement;
                            ctx.state = BattleState.FormCharging;
                            ctx.chargingForm = HandPose.Unknown;
                            ctx.holdTime = 0f;
                        }
                    }
                    else
                    {
                        ctx.chargingElement = pose;
                        ctx.holdTime = 0;
                    }
                }
                break;
            case BattleState.FormCharging:
                ctx.totalTime += Time.deltaTime;

                if (ctx.totalTime >= TOTAL_DURATION)
                {
                    ctx.state = BattleState.Idle;
                    ctx.chargingElement = HandPose.Unknown;
                    ctx.chargingForm = HandPose.Unknown;
                    ctx.holdTime = 0f;
                    ctx.totalTime = 0f;
                    break;
                }

                if (IsElement(pose) && pose != ctx.confirmedElement)
                {
                    ctx.state = BattleState.ElementCharging;
                    ctx.chargingElement = pose;
                    ctx.confirmedElement = HandPose.Unknown;
                    ctx.chargingForm = HandPose.Unknown;
                    ctx.holdTime = 0f;
                    break;
                }
                
                if (IsForm(pose))
                {
                    if (ctx.chargingForm == HandPose.Unknown)
                    {
                        ctx.chargingForm = pose;
                        ctx.holdTime = 0;
                    }
                    else if (pose == ctx.chargingForm)
                    {
                        ctx.holdTime += Time.deltaTime;
                        if (ctx.holdTime >= REQUIRED_HOLD)
                        {
                            ctx.confirmedForm = pose;
                            ctx.state = BattleState.Casting;
                            ctx.holdTime = 0f;
                        }
                    }
                    else
                    {
                        ctx.chargingForm = pose;
                        ctx.holdTime = 0f;
                    }
                }
                else
                {
                    ctx.chargingForm = HandPose.Unknown;
                    ctx.holdTime = 0f;
                }
                break;
            case BattleState.Casting:
                break;
        }
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

    private bool IsElement(HandPose pose)
    {
        return pose == HandPose.Fire || pose == HandPose.Water || pose == HandPose.Wind || pose == HandPose.Land;
    }

    private bool IsForm(HandPose pose)
    {
        return pose == HandPose.Attack || pose == HandPose.Defense || pose == HandPose.Special;
    }
    
    private void OnGUI()
    {
        float boxX = 10, boxY = 10;
        float boxW = 300, boxH = 400;
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
        y += lineHeight;

        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"State : {_player.state}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Hold : {_player.holdTime:F2} / 1.00s");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Element : {_player.confirmedElement}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Form : {_player.confirmedForm}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"TotalTime : {_player.totalTime:F2}/10.00s");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"--- AI ---");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI State : {_ai.ctx.state}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI Element : {_ai.ctx.confirmedElement}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI Form : {_ai.ctx.confirmedForm}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI Time : {_ai.ctx.totalTime:F2}");
        y += lineHeight + 5;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"--- HP ---");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Player HP: {_roundManager.PlayerHp}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI HP: {_roundManager.AiHp}");
        
        if (_roundManager.GameOver)
        {
            y += lineHeight;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"=== GAME OVER ===");
        }
    }
}