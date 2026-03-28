using System;
using UnityEngine;

// 구역 내 몬스터를 관리한다.
// Activate() 호출 시 몬스터를 모두 활성화하고,
// 전원 처치 시 OnAllMonstersDefeated 이벤트를 발동한다.
[DisallowMultipleComponent]
public class MonsterZone : MonoBehaviour
{
    [SerializeField] private MonsterBase[] monsters;

    public event Action OnAllMonstersDefeated;

    private int _aliveCount;

    private void Awake()
    {
        foreach (MonsterBase m in monsters)
            if (m != null) m.gameObject.SetActive(false);
    }

    public void Activate()
    {
        _aliveCount = 0;

        foreach (MonsterBase m in monsters)
        {
            if (m == null) continue;
            m.OnDied += OnMonsterDied;
            m.gameObject.SetActive(true);
            _aliveCount++;
        }

        // 몬스터가 하나도 없으면 즉시 완료
        if (_aliveCount == 0)
            OnAllMonstersDefeated?.Invoke();
    }

    private void OnMonsterDied()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);

        if (_aliveCount == 0)
            OnAllMonstersDefeated?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (MonsterBase m in monsters)
            if (m != null) m.OnDied -= OnMonsterDied;
    }

    public int AliveCount => _aliveCount;
    public int TotalCount => monsters.Length;
}
