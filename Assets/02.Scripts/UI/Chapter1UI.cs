using TMPro;
using UnityEngine;

// 챕터1 UI — IChapterUI 구현.
public class Chapter1UI : MonoBehaviour, IChapterUI
{
    [SerializeField] private ChapterClearPopup clearPopup;
    [SerializeField] private TMP_Text moneyText;

    public void OnShow() { }
    public void OnHide() { }

    public void UpdateMoney(int amount) { }

    public void SetMoneyText(string text)
    {
        if (moneyText != null) moneyText.text = text;
    }

    public void ShowClearPopup() => clearPopup?.Show();
    public void HideClearPopup() => clearPopup?.Hide();
}
