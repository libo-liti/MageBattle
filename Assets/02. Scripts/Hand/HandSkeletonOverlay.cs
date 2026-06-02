using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손 인식 디버그 오버레이 (포트폴리오 시연용).
///
/// 설계 의도:
///  - Pose 폴더(비공개)에 직접 의존하지 않고, HandRecognizer가 노출하는
///    LeftHandJoints / RightHandJoints (관절 21개 좌표)만 받아서 그린다.
///  - 기존 UI 패널(WebcamArea)의 화면 영역 안에 정확히 맞춰 그려, UI와 겹치지 않게 한다.
///    (자체 배경을 그리지 않고 WebcamArea의 RectTransform 영역을 그대로 사용)
///  - GameManager.IsInGame이 true일 때(전투/튜토리얼 중)에만 표시한다.
///  - 좌표는 0~1 정규화로 보고 패널 영역에 매핑, 벗어나는 값은 클램프.
///  - 기존 게임 로직은 전혀 건드리지 않는 읽기 전용 오버레이.
/// </summary>
public class HandSkeletonOverlay : MonoBehaviour
{
    [Header("그릴 대상 UI 패널 (없으면 이름으로 자동 탐색)")]
    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private string targetPanelName = "WebcamArea [Image]";

    [Header("표현")]
    [SerializeField] private float padding = 14f;
    [SerializeField] private float jointRadius = 4f;
    [SerializeField] private Color leftColor = new Color(0.30f, 0.80f, 1f, 1f);
    [SerializeField] private Color rightColor = new Color(1f, 0.55f, 0.30f, 1f);
    [SerializeField] private Color boneColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private bool flipX = true;
    [SerializeField] private bool flipY = false;

    private HandRecognizer _recognizer;
    private GameManager _gameManager;
    private Texture2D _dot;

    private static readonly int[,] Bones = new int[,]
    {
        {0,1},{1,2},{2,3},{3,4},
        {0,5},{5,6},{6,7},{7,8},
        {0,9},{9,10},{10,11},{11,12},
        {0,13},{13,14},{14,15},{15,16},
        {0,17},{17,18},{18,19},{19,20},
        {5,9},{9,13},{13,17}
    };

    public void SetRecognizer(HandRecognizer recognizer)
    {
        _recognizer = recognizer;
    }

    private void Awake()
    {
        _dot = new Texture2D(1, 1);
        _dot.SetPixel(0, 0, Color.white);
        _dot.Apply();
    }

    private bool TryGetPanelRectInScreen(out Rect screenRect)
    {
        screenRect = default;
        if (targetPanel == null && !string.IsNullOrEmpty(targetPanelName))
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene.IsValid() && go.name == targetPanelName)
                {
                    targetPanel = go.GetComponent<RectTransform>();
                    break;
                }
            }
        }
        if (targetPanel == null) return false;

        // RectTransform의 월드 코너 → 스크린(GUI는 y가 위에서 아래) 좌표로 변환
        Vector3[] corners = new Vector3[4];
        targetPanel.GetWorldCorners(corners); // 0:좌하 1:좌상 2:우상 3:우하 (ScreenSpaceOverlay는 픽셀)
        float xMin = corners[0].x;
        float xMax = corners[2].x;
        float yMinBottom = corners[0].y; // 화면 아래 기준 y(작을수록 아래)
        float yMaxTop = corners[1].y;
        // GUI 좌표(위가 0)로 변환
        float guiTop = Screen.height - yMaxTop;
        float height = yMaxTop - yMinBottom;
        screenRect = new Rect(xMin, guiTop, xMax - xMin, height);
        return true;
    }

    private bool ShouldDraw()
    {
        if (_recognizer == null) return false;
        if (_gameManager == null) _gameManager = FindObjectOfType<GameManager>();
        return _gameManager != null && _gameManager.IsInGame;
    }

    private void OnGUI()
    {
        if (!ShouldDraw()) return;
        if (!TryGetPanelRectInScreen(out Rect panel)) return;

        // 자체 배경은 그리지 않음 (WebcamArea Image가 배경 역할)
        DrawHand(_recognizer.LeftHandJoints, leftColor, panel, _recognizer.LeftStable);
        DrawHand(_recognizer.RightHandJoints, rightColor, panel, _recognizer.RightStable);
    }

    private void DrawHand(IReadOnlyList<Vector3> joints, Color color, Rect panel, bool stable)
    {
        if (joints == null || joints.Count < 21) return;

        bool anyValid = false;
        for (int i = 0; i < 21; i++)
            if (Mathf.Abs(joints[i].x) > 0.0001f || Mathf.Abs(joints[i].y) > 0.0001f) { anyValid = true; break; }
        if (!anyValid) return;

        Rect inner = new Rect(panel.x + padding, panel.y + padding,
                              panel.width - padding * 2, panel.height - padding * 2);

        Vector2 ToScreen(Vector3 p)
        {
            float nx = Mathf.Clamp01(p.x);
            float ny = Mathf.Clamp01(p.y);
            if (flipX) nx = 1f - nx;
            if (flipY) ny = 1f - ny;
            return new Vector2(inner.x + nx * inner.width, inner.y + ny * inner.height);
        }

        var lineColor = stable ? boneColor : new Color(boneColor.r, boneColor.g, boneColor.b, 0.35f);
        for (int b = 0; b < Bones.GetLength(0); b++)
            DrawLine(ToScreen(joints[Bones[b, 0]]), ToScreen(joints[Bones[b, 1]]), lineColor, 2f);

        var jc = stable ? color : new Color(color.r, color.g, color.b, 0.4f);
        for (int i = 0; i < 21; i++)
            DrawDot(ToScreen(joints[i]), jointRadius, jc);
    }

    private void DrawDot(Vector2 center, float radius, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), _dot);
        GUI.color = prev;
    }

    private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 0.01f) return;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        var prevColor = GUI.color;
        var prevMatrix = GUI.matrix;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width / 2f, len, width), _dot);
        GUI.matrix = prevMatrix;
        GUI.color = prevColor;
    }
}
