using System.Collections;
using Hovl;
using UnityEngine;
using static Constant;

public class CasterMagicVfx : MonoBehaviour
{
    [Header("Charging VFX (양손)")]
    [SerializeField] private ParticleSystem leftHandVfx;
    [SerializeField] private ParticleSystem rightHandVfx;

    [Header("Element Projectile Prefabs")]
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private GameObject waterProjectilePrefab;
    [SerializeField] private GameObject windProjectilePrefab;
    [SerializeField] private GameObject landProjectilePrefab;

    [Header("Shield Prefabs")]
    [SerializeField] private GameObject fireShieldPrefab;
    [SerializeField] private GameObject waterShieldPrefab;
    [SerializeField] private GameObject windShieldPrefab;
    [SerializeField] private GameObject landShieldPrefab;

    [Header("Counter Prefabs")]
    [SerializeField] private GameObject fireCounterPrefab;
    [SerializeField] private GameObject waterCounterPrefab;
    [SerializeField] private GameObject windCounterPrefab;
    [SerializeField] private GameObject landCounterPrefab;

    [Header("Projectile Settings")]
    [SerializeField] private Transform projectileStartPos;
    [SerializeField] private Transform projectileTarget;
    [SerializeField] private float projectileFlightTime = 1.2f;
    [SerializeField] private float hitEffectDuration = 2.5f;
    [SerializeField] private float targetHeightOffset = 1.4f;

    [Header("Shield / Counter Spawn")]
    [SerializeField] private Transform casterBasePos;        // 캐스터 자신의 발 위치 Transform
    [SerializeField] private float counterHeightOffset  = 1.0f; // Counter 높이 (Inspector에서 조정)
    [SerializeField] private float counterForwardOffset = 0.6f; // Counter를 상대 방향으로 얼마나 앞에 둘지

    private static readonly Color FireColor = new Color(1.00f, 0.42f, 0.21f);
    private static readonly Color WaterColor = new Color(0.31f, 0.80f, 0.77f);
    private static readonly Color WindColor = new Color(0.91f, 0.91f, 0.91f);
    private static readonly Color LandColor = new Color(0.55f, 0.44f, 0.28f);

    private Coroutine _projectileCoroutine;
    private Coroutine _counterCoroutine;
    private GameObject _activeProjectile;
    private GameObject _activeShield;
    private GameObject _activeCounter;

    // GameManager에서 카메라 지속 시간 계산에 사용
    public float ProjectileFlightTime => projectileFlightTime;

    // ── 색상 유틸 ────────────────────────────────────────────────────

    public static Color GetElementColor(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire: return FireColor;
            case HandPose.Water: return WaterColor;
            case HandPose.Wind: return WindColor;
            case HandPose.Land: return LandColor;
            default: return Color.white;
        }
    }

    // ── 충전 VFX ─────────────────────────────────────────────────────

    private void Awake() => StopCharging();

    public void StartCharging(HandPose element)
    {
        Color c = GetElementColor(element);
        SetParticleColor(leftHandVfx, c);
        SetParticleColor(rightHandVfx, c);
        if (leftHandVfx != null) leftHandVfx.Play();
        if (rightHandVfx != null) rightHandVfx.Play();
    }

    public void StopCharging()
    {
        if (leftHandVfx != null) leftHandVfx.Stop();
        if (rightHandVfx != null) rightHandVfx.Stop();
    }

    // ── 투사체 발사 ───────────────────────────────────────────────────

    public void Fire(HandPose element, bool showHit = true)
    {
        StopCharging();
        LaunchProjectile(element, null, showHit);
    }

    // 쉴드를 향해 발사 — 도달 시 쉴드 히트 트리거 (자체 히트 이펙트는 안 나오게)
    public void FireAtShield(HandPose element, CasterMagicVfx shieldOwner)
    {
        StopCharging();
        LaunchProjectile(element, () => shieldOwner.TriggerShieldHit(), showHit: false);
    }

    private void LaunchProjectile(HandPose element, System.Action onArrive, bool showHit = true)
    {
        if (_projectileCoroutine != null) StopCoroutine(_projectileCoroutine);
        if (_activeProjectile != null) { Destroy(_activeProjectile); _activeProjectile = null; }
        _projectileCoroutine = StartCoroutine(ProjectileRoutine(element, onArrive, showHit));
    }

    private IEnumerator ProjectileRoutine(HandPose element, System.Action onArrive = null, bool showHit = true)
    {
        GameObject prefab = GetPrefabForElement(element);
        if (prefab == null || projectileStartPos == null || projectileTarget == null)
            yield break;

        Vector3 startPos = projectileStartPos.position;
        Vector3 endPos = projectileTarget.position + Vector3.up * targetHeightOffset;
        Vector3 direction = (endPos - startPos).normalized;

        Quaternion toTarget = direction != Vector3.zero
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;

        _activeProjectile = Instantiate(prefab, startPos, toTarget);

        // HS_ProjectileMover 비활성화 (rb.velocity 간섭 방지)
        var mover = _activeProjectile.GetComponent<HS_ProjectileMover>();
        if (mover != null) mover.enabled = false;

        // 물리 충돌 비활성화 (자기 충돌 방지)
        var rb = _activeProjectile.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (var col in _activeProjectile.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        float elapsed = 0f;
        while (elapsed < projectileFlightTime)
        {
            elapsed += Time.deltaTime;
            if (_activeProjectile == null) yield break;

            float t = Mathf.Clamp01(elapsed / projectileFlightTime);
            _activeProjectile.transform.position = Vector3.Lerp(startPos, endPos, t);
            _activeProjectile.transform.rotation = toTarget;
            yield return null;
        }

        if (_activeProjectile == null) yield break;

        _activeProjectile.transform.position = endPos;
        onArrive?.Invoke();           // 쉴드 히트 콜백 등
        if (showHit) TriggerHitEffects(_activeProjectile);

        Destroy(_activeProjectile, showHit ? hitEffectDuration : 0.2f);
        _activeProjectile = null;
        _projectileCoroutine = null;
    }

    // 원소 상쇄 — 중간 지점까지만 날아가고 히트 이펙트 후 소멸
    public void FireCancelled(HandPose element)
    {
        StopCharging();
        if (_projectileCoroutine != null) StopCoroutine(_projectileCoroutine);
        if (_activeProjectile    != null) { Destroy(_activeProjectile); _activeProjectile = null; }
        _projectileCoroutine = StartCoroutine(ProjectileCancelledRoutine(element));
    }

    private IEnumerator ProjectileCancelledRoutine(HandPose element)
    {
        GameObject prefab = GetPrefabForElement(element);
        if (prefab == null || projectileStartPos == null || projectileTarget == null)
            yield break;

        Vector3 startPos  = projectileStartPos.position;
        Vector3 endPos    = projectileTarget.position + Vector3.up * targetHeightOffset;
        Vector3 midPos    = Vector3.Lerp(startPos, endPos, 0.5f);
        Vector3 direction = (endPos - startPos).normalized;

        Quaternion toTarget = direction != Vector3.zero
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;

        _activeProjectile = Instantiate(prefab, startPos, toTarget);

        var mover = _activeProjectile.GetComponent<HS_ProjectileMover>();
        if (mover != null) mover.enabled = false;
        var rb = _activeProjectile.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (var col in _activeProjectile.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        float halfTime = projectileFlightTime * 0.5f;
        float elapsed  = 0f;
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            if (_activeProjectile == null) yield break;

            float t = Mathf.Clamp01(elapsed / halfTime);
            _activeProjectile.transform.position = Vector3.Lerp(startPos, midPos, t);
            _activeProjectile.transform.rotation = toTarget;
            yield return null;
        }

        if (_activeProjectile == null) yield break;

        _activeProjectile.transform.position = midPos;
        TriggerHitEffects(_activeProjectile);

        Destroy(_activeProjectile, hitEffectDuration);
        _activeProjectile    = null;
        _projectileCoroutine = null;
    }

    // ── 쉴드 ──────────────────────────────────────────────────────────

    public void ShowShield(HandPose element)
    {
        if (_activeShield != null) Destroy(_activeShield);

        var prefab = GetShieldPrefab(element);
        if (prefab == null || projectileStartPos == null || projectileTarget == null) return;

        // 캐스터 발 위치 기준으로 소환 (프리팹 내부 Y=1.0 오프셋으로 적절한 높이에 표시)
        Transform baseRef = casterBasePos != null ? casterBasePos : projectileStartPos;
        Vector3 pos = new Vector3(baseRef.position.x, 0f, baseRef.position.z);

        // 상대방을 향해 회전
        Vector3 toOpponent = projectileTarget.position - baseRef.position;
        toOpponent.y = 0f;
        Quaternion rot = toOpponent != Vector3.zero
            ? Quaternion.LookRotation(toOpponent.normalized)
            : Quaternion.identity;

        _activeShield = Instantiate(prefab, pos, rot);
        _activeShield.transform.localScale = Vector3.one * 1.4f;
        Destroy(_activeShield, projectileFlightTime + 0.3f);
    }

    // 상대 투사체가 도달했을 때 쉴드 히트 이펙트 재생
    public void TriggerShieldHit()
    {
        if (_activeShield == null) return;
        TriggerHitEffects(_activeShield);
    }

    // ── 카운터 ────────────────────────────────────────────────────────

    // Counter 성공: 상대 투사체 흡수 후 반사 발사
    public void ShowCounterAndReflect(HandPose element)
    {
        if (_counterCoroutine != null) StopCoroutine(_counterCoroutine);
        _counterCoroutine = StartCoroutine(CounterReflectRoutine(element));
    }

    private IEnumerator CounterReflectRoutine(HandPose element)
    {
        var prefab = GetCounterPrefab(element);
        if (prefab != null)
        {
            Transform baseRef = casterBasePos != null ? casterBasePos : projectileStartPos;
            Vector3 toOpponent = projectileTarget.position - baseRef.position;
            toOpponent.y = 0f;
            Vector3 forwardDir = toOpponent.normalized;
            Vector3 pos = new Vector3(baseRef.position.x, counterHeightOffset, baseRef.position.z)
                          + forwardDir * counterForwardOffset;
            Quaternion rot = forwardDir != Vector3.zero
                ? Quaternion.LookRotation(forwardDir)
                : Quaternion.identity;
            _activeCounter = Instantiate(prefab, pos, rot);
        }

        // 상대 투사체 비행 시간만큼 대기 (도달 타이밍에 맞춤)
        yield return new WaitForSeconds(projectileFlightTime);

        yield return new WaitForSeconds(0.3f);

        // 반사 투사체 발사 (캐스터 → 상대방), 카운터 오브젝트는 즉시 정리해서 히트 모션 안 보이게
        if (_activeCounter != null)
        {
            Destroy(_activeCounter);
            _activeCounter = null;
        }
        LaunchProjectile(element, null);

        if (_activeCounter != null)
        {
            Destroy(_activeCounter, hitEffectDuration);
            _activeCounter = null;
        }

        _counterCoroutine = null;
    }

    // Counter 실패: 짧은 플래시 후 소멸
    public void ShowCounterFail(HandPose element)
    {
        if (_counterCoroutine != null) StopCoroutine(_counterCoroutine);
        _counterCoroutine = StartCoroutine(CounterFailRoutine(element));
    }

    private IEnumerator CounterFailRoutine(HandPose element)
    {
        var prefab = GetCounterPrefab(element);
        if (prefab == null) yield break;

        Transform baseRef = casterBasePos != null ? casterBasePos : projectileStartPos;
        Vector3 pos = new Vector3(baseRef.position.x, counterHeightOffset, baseRef.position.z);
        Vector3 toOpponent = projectileTarget.position - baseRef.position;
        toOpponent.y = 0f;
        Quaternion rot = toOpponent != Vector3.zero
            ? Quaternion.LookRotation(toOpponent.normalized)
            : Quaternion.identity;
        _activeCounter = Instantiate(prefab, pos, rot);

        yield return new WaitForSeconds(0.4f);

        if (_activeCounter != null)
        {
            Destroy(_activeCounter);
            _activeCounter = null;
        }
        _counterCoroutine = null;
    }

    // ── 히트 이펙트 공통 ─────────────────────────────────────────────

    // playOnAwake=True  → 이동/대기 파티클: 중지
    // playOnAwake=False → 히트/폭발 파티클: 재생
    private void TriggerHitEffects(GameObject obj)
    {
        if (obj == null) return;
        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.main.playOnAwake)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else
                ps.Play(true);
        }
    }

    // ── 프리팹 선택 ──────────────────────────────────────────────────

    private GameObject GetPrefabForElement(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire: return fireProjectilePrefab;
            case HandPose.Water: return waterProjectilePrefab;
            case HandPose.Wind: return windProjectilePrefab;
            case HandPose.Land: return landProjectilePrefab;
            default: return fireProjectilePrefab;
        }
    }

    private GameObject GetShieldPrefab(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire: return fireShieldPrefab;
            case HandPose.Water: return waterShieldPrefab;
            case HandPose.Wind: return windShieldPrefab;
            case HandPose.Land: return landShieldPrefab;
            default: return fireShieldPrefab;
        }
    }

    private GameObject GetCounterPrefab(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire: return fireCounterPrefab;
            case HandPose.Water: return waterCounterPrefab;
            case HandPose.Wind: return windCounterPrefab;
            case HandPose.Land: return landCounterPrefab;
            default: return fireCounterPrefab;
        }
    }

    // 외부 호환용
    public void TriggerTargetHit(HandPose element) { }

    private void SetParticleColor(ParticleSystem ps, Color c)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = c;
    }
}
