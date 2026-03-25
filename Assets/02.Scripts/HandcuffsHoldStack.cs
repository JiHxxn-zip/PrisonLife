using System.Collections.Generic;
using UnityEngine;

// 플레이어 프리팹 내부의 HandcuffsHoldAnchor에 수갑을 무제한으로 적층
[DisallowMultipleComponent]
public class HandcuffsHoldStack : MonoBehaviour
{
    [Header("Anchor")]
    [SerializeField] private Transform handcuffsHoldAnchor;
    [SerializeField] private Vector3 holdLocalOffset = Vector3.zero;
    [SerializeField] private float holdSpacingY = 0.15f;

    private readonly List<GameObject> heldHandcuffs = new List<GameObject>();

    private void Awake()
    {
        if (handcuffsHoldAnchor == null)
        {
            handcuffsHoldAnchor = transform;
        }
    }

    public int Count => heldHandcuffs.Count;

    // Handcuffs를 판매(소모)할 때 사용
    // 반환값은 실제로 소모된 수량
    public int ConsumeAll()
    {
        int count = heldHandcuffs.Count;
        for (int i = heldHandcuffs.Count - 1; i >= 0; i--)
        {
            GameObject v = heldHandcuffs[i];
            if (v != null)
            {
                Destroy(v);
            }
        }

        heldHandcuffs.Clear();
        return count;
    }

    public int ConsumeLast(int amount)
    {
        int safe = Mathf.Max(0, amount);
        if (safe <= 0 || heldHandcuffs.Count <= 0)
        {
            return 0;
        }

        int toConsume = Mathf.Min(safe, heldHandcuffs.Count);
        int consumed = 0;
        for (int i = heldHandcuffs.Count - 1; i >= 0 && consumed < toConsume; i--)
        {
            GameObject v = heldHandcuffs[i];
            if (v != null)
            {
                Destroy(v);
            }
            consumed++;
        }

        // 마지막 toConsume개만 제거 후 리스트에서 제외
        heldHandcuffs.RemoveRange(heldHandcuffs.Count - consumed, consumed);
        return consumed;
    }

    public void AddExistingHandcuffVisual(GameObject handcuffVisual)
    {
        if (handcuffVisual == null)
        {
            return;
        }

        handcuffVisual.SetActive(true);
        handcuffVisual.transform.SetParent(handcuffsHoldAnchor, false);

        int index = heldHandcuffs.Count;
        handcuffVisual.transform.localPosition = holdLocalOffset + new Vector3(0f, holdSpacingY * index, 0f);
        handcuffVisual.transform.localRotation = Quaternion.identity;

        heldHandcuffs.Add(handcuffVisual);
    }

    public void AddRange(IEnumerable<GameObject> visuals)
    {
        if (visuals == null)
        {
            return;
        }

        foreach (GameObject v in visuals)
        {
            AddExistingHandcuffVisual(v);
        }
    }

    // 보유 중인 수갑 비주얼을 파괴하지 않고 리스트를 넘겨준다 (Zone으로 이동 용도)
    public List<GameObject> ReleaseAll()
    {
        List<GameObject> released = new List<GameObject>(heldHandcuffs);
        heldHandcuffs.Clear();
        return released;
    }
}

