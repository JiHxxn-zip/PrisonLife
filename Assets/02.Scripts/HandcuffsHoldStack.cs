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
}

