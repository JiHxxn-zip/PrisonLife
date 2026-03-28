using System.Collections.Generic;
using UnityEngine;

// 무기마다 하나씩 붙이는 투사체 풀.
// 시작 시 initialSize개 미리 생성, 부족 시 자동 확장.
public class BulletPool : MonoBehaviour
{
    [SerializeField] private BulletBase prefab;
    [SerializeField] private int initialSize = 5;

    private readonly Queue<BulletBase> _pool = new Queue<BulletBase>();

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
            Prewarm();
    }

    private void Prewarm()
    {
        BulletBase bullet = Instantiate(prefab, transform);
        bullet.gameObject.SetActive(false);
        _pool.Enqueue(bullet);
    }

    public BulletBase Get()
    {
        if (_pool.Count == 0)
            Prewarm();

        return _pool.Dequeue();
    }

    public void Return(BulletBase bullet)
    {
        bullet.gameObject.SetActive(false);
        bullet.transform.SetParent(transform);
        _pool.Enqueue(bullet);
    }
}
