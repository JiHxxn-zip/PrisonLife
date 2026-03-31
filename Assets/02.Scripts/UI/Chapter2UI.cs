using TMPro;
using UnityEngine;

// 챕터2 UI — IChapterUI 구현.
// Money 텍스트 갱신 + 클리어 팝업을 챕터2 전용으로 처리한다.
public class Chapter2UI : MonoBehaviour, IChapterUI
{
    [SerializeField] private TextMeshProUGUI   moneyText;
    [SerializeField] private ChapterClearPopup clearPopup;

    public void OnShow() { }
    public void OnHide() { }

    public void UpdateMoney(int amount)
    {
        if (moneyText != null)
            moneyText.text = amount.ToString();
    }

    public void ShowClearPopup() => clearPopup?.Show();
    public void HideClearPopup() => clearPopup?.Hide();
}
