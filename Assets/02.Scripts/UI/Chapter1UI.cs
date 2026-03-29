using UnityEngine;

// 챕터1 UI — IChapterUI 구현.
// 챕터1은 Money UI가 없으므로 UpdateMoney는 no-op.
public class Chapter1UI : MonoBehaviour, IChapterUI
{
    [SerializeField] private ChapterClearPopup clearPopup;

    public void OnShow() { }
    public void OnHide() { }

    public void UpdateMoney(int amount) { }

    public void ShowClearPopup() => clearPopup?.Show();
    public void HideClearPopup() => clearPopup?.Hide();
}
