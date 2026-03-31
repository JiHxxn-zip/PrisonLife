using UnityEngine;

// 피격 가능한 대상이 구현하는 인터페이스.
// Monster 외 오브젝트(구조물, 보스 등)도 동일 인터페이스로 확장 가능.
public interface IAttackable
{
    // hitFrom : 공격이 발생한 월드 위치 (넉백 방향 계산에 사용)
    void TakeDamage(int damage, Vector3 hitFrom);
}
