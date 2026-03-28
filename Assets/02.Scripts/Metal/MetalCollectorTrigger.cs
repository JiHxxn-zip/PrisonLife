using UnityEngine;

// 플레이어 자식의 메탈 전용 수집 콜라이더에 붙이는 마커.
// 이 컴포넌트가 있는 콜라이더는 MetalCollectionZone과 ItemPickup(Metal)만 반응하고,
// BaseZone / AccumulatedPaymentZone / MoneyZone 등 나머지 존은 무시한다.
[DisallowMultipleComponent]
public class MetalCollectorTrigger : MonoBehaviour { }
