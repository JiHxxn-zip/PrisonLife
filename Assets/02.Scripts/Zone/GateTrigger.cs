using System;
using System.Collections;
using UnityEngine;

// Gate 오브젝트에 부착.
// 플레이어 진입 시 WorldUI 표시 → holdDuration 초 대기 → FadeOut → 텔레포트 → FadeIn
[DisallowMultipleComponent]
public class GateTrigger : MonoBehaviour
{
    [Header("카메라")]
    [Tooltip("Gate 통과 후 카메라가 추적할 새 타겟 (Player2)")]
    [SerializeField] private Transform newCameraTarget;
    [SerializeField] private float holdDuration = 3f;

    public event Action<PlayerAgent> OnGatePassed;

    private bool _triggered;

    private void OnEnable()
    {
        _triggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        _triggered = true;
        StartCoroutine(GateSequence(player));
    }

    private IEnumerator GateSequence(PlayerAgent player)
    {
        // holdDuration 초 대기
        yield return new WaitForSeconds(holdDuration);

        // FadeOut
        yield return UIManager.Instance.FadeOut();

        // 화면이 검은 동안 — 챕터1 UI 숨김, 카메라 타겟 교체, WorldUI 숨김
        // 챕터2 UI 활성화는 TutorialManager.Chapter2WeaponCinematic 종료 시 처리
        UIManager.Instance.HideCurrentChapter();

        if (newCameraTarget != null)
            newCameraTarget.gameObject.SetActive(true);

        // FadeIn
        yield return UIManager.Instance.FadeIn();

        OnGatePassed?.Invoke(player);
        gameObject.SetActive(false);
    }

}
