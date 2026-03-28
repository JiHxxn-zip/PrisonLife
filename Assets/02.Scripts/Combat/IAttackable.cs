// 피격 가능한 대상이 구현하는 인터페이스.
// Monster 외 오브젝트(구조물, 보스 등)도 동일 인터페이스로 확장 가능.
public interface IAttackable
{
    void TakeDamage(int damage);
}
