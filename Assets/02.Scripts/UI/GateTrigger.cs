using System;
using System.Collections;
using UnityEngine;

// Gate 오브젝트에 부착.
// 플레이어 진입 시 WorldUI 표시 → holdDuration 초 대기 → FadeOut → 텔레포트 → FadeIn
[DisallowMultipleComponent]
public class GateTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject worldUI;

    [Header("텔레포트")]
    [SerializeField] private Transform teleportTarget;
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

        PlayerAgent player = other.GetComponent<PlayerAgent>();
        if (player == null) return;

        _triggered = true;
        StartCoroutine(GateSequence(player));
    }

    private IEnumerator GateSequence(PlayerAgent player)
    {
        // WorldUI 표시
        if (worldUI != null)
            worldUI.SetActive(true);

        // holdDuration 초 대기
        yield return new WaitForSeconds(holdDuration);

        // FadeOut
        yield return UIManager.Instance.FadeOut();

        // 텔레포트
        if (teleportTarget != null)
            player.transform.position = teleportTarget.position;

        // WorldUI 숨김
        if (worldUI != null)
            worldUI.SetActive(false);

        // FadeIn
        yield return UIManager.Instance.FadeIn();

        OnGatePassed?.Invoke(player);
        gameObject.SetActive(false);
    }

}
