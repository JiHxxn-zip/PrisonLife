using System;
using UnityEngine;

// 챕터2 베이스캠프 도착 존.
// 플레이어 진입 시 objectsToActivate 목록을 활성화하고 OnPlayerArrived 이벤트를 발생시킨다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Ch2BaseZone : MonoBehaviour
{
    [Header("도착 시 활성화할 오브젝트")]
    [SerializeField] private GameObject[] objectsToActivate;

    public event Action OnPlayerArrived;

    private bool _triggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnEnable()
    {
        _triggered = false;
        foreach (GameObject obj in objectsToActivate)
            if (obj != null) obj.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (other.GetComponentInParent<PlayerCombat>() == null) return;

        _triggered = true;

        foreach (GameObject obj in objectsToActivate)
            if (obj != null) obj.SetActive(true);

        RestorePlayerHp(other);
        OnPlayerArrived?.Invoke();
    }

    private void RestorePlayerHp(Collider playerCollider)
    {
        HpComponent hp = playerCollider.GetComponentInParent<HpComponent>();
        if (hp == null || hp.CurrentHp >= hp.MaxHp) return;
        hp.Heal(hp.MaxHp - hp.CurrentHp);
    }
}
