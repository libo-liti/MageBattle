using System.Collections;
using UnityEngine;
using static Constant;

public class CasterMagicVfx : MonoBehaviour
{
    [Header("Charging VFX (양손)")]
    [SerializeField] private ParticleSystem leftHandVfx;
    [SerializeField] private ParticleSystem rightHandVfx;

    [Header("Projectile VFX (마법 발사)")]
    [SerializeField] private GameObject projectileObject;
    [SerializeField] private ParticleSystem projectileVfx;
    [SerializeField] private Transform projectileStartPos;
    [SerializeField] private Transform projectileTarget;     
    [SerializeField] private float projectileFlightTime = 0.6f;

    [Header("Target Hit VFX (상대가 맞을 때)")]
    [SerializeField] private GameObject targetHitObject;     
    [SerializeField] private ParticleSystem targetHitVfx;
    
    private static readonly Color FireColor  = new Color(1.00f, 0.42f, 0.21f);
    private static readonly Color WaterColor = new Color(0.31f, 0.80f, 0.77f);
    private static readonly Color WindColor  = new Color(0.91f, 0.91f, 0.91f);
    private static readonly Color LandColor  = new Color(0.55f, 0.44f, 0.28f);
    
    public static Color GetElementColor(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire:  return FireColor;
            case HandPose.Water: return WaterColor;
            case HandPose.Wind:  return WindColor;
            case HandPose.Land:  return LandColor;
            default:             return Color.white;
        }
    }
    
    private void Awake()
    {
        StopCharging();
        if (projectileObject != null) projectileObject.SetActive(false);
        if (targetHitObject != null) targetHitObject.SetActive(false);
    }
    
    public void StartCharging(HandPose element)
    {
        Color c = GetElementColor(element);
        SetParticleColor(leftHandVfx, c);
        SetParticleColor(rightHandVfx, c);
        leftHandVfx.Play();
        rightHandVfx.Play();
    }
    
    public void StopCharging()
    {
        if (leftHandVfx != null) leftHandVfx.Stop();
        if (rightHandVfx != null) rightHandVfx.Stop();
    }
    
    public void Fire(HandPose element, bool showHit = true)
    {
        StopCharging();
        StartCoroutine(ProjectileRoutine(element, showHit));
    }
    
    private IEnumerator ProjectileRoutine(HandPose element, bool showHit)
    {
        Color c = GetElementColor(element);
        
        projectileObject.SetActive(true);
        SetParticleColor(projectileVfx, c);
        projectileVfx.Play();
        
        Vector3 startPos = projectileStartPos.position;
        Vector3 endPos   = projectileTarget.position;
        
        float elapsed = 0f;
        while (elapsed < projectileFlightTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / projectileFlightTime;
            projectileObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        if(showHit)
            TriggerTargetHit(element);
        
        projectileVfx.Stop();
        projectileObject.SetActive(false);
        projectileObject.transform.position = startPos;
    }

    public void TriggerTargetHit(HandPose element)
    {
        Color c = GetElementColor(element);
        targetHitObject.SetActive(true);
        SetParticleColor(targetHitVfx, c);
        targetHitVfx.Stop();
        targetHitVfx.Clear();
        targetHitVfx.Play();
        
        StartCoroutine(DeactivateAfter(targetHitObject, 1.5f));
    }

    private IEnumerator DeactivateAfter(GameObject obj, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        obj.SetActive(false);
    }

    private void SetParticleColor(ParticleSystem ps, Color c)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = c;
    }
}
