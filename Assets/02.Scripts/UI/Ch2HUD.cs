using TMPro;
using UnityEngine;

// 챕터2 HUD — 몬스터 드롭 Money 획득 수를 추적하고 *10으로 UI에 표시한다.
// 챕터2 UI Root 하위 오브젝트에 부착하고 moneyText를 연결한다.
public class Ch2HUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    private int _moneyCount;

    private void OnEnable()
    {
        Ch2MoneyPickup.OnMoneyCollected += HandleMoneyCollected;
        Refresh();
    }

    private void OnDisable()
    {
        Ch2MoneyPickup.OnMoneyCollected -= HandleMoneyCollected;
    }

    private void HandleMoneyCollected()
    {
        _moneyCount++;
        Refresh();
    }

    private void Refresh()
    {
        if (moneyText != null)
            moneyText.text = (_moneyCount * 10).ToString();
    }
}
