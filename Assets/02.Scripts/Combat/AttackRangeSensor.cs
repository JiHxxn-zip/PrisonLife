using System;
using UnityEngine;

// 공격 범위 전용 콜라이더 오브젝트에 부착.
// detectionMask에 포함된 레이어만 감지해 PlayerCombat에 전달한다.
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class AttackRangeSensor : MonoBehaviour
{
    [Tooltip("감지할 레이어 (Monster 레이어만 포함)")]
    [SerializeField] private LayerMask detectionMask = ~0;

    public event Action<Collider> OnEntered;
    public event Action<Collider> OnExited;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((detectionMask & (1 << other.gameObject.layer)) == 0) return;
        OnEntered?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if ((detectionMask & (1 << other.gameObject.layer)) == 0) return;
        OnExited?.Invoke(other);
    }
}
