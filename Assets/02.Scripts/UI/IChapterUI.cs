// 챕터별 UI가 구현하는 인터페이스.
// UIManager는 이 타입의 변수 하나로 현재 챕터 UI를 참조하며,
// 플레이어·게임 매니저의 요청을 구현체에 그대로 위임한다.
public interface IChapterUI
{
    void OnShow();             // 챕터 활성화 시 호출
    void OnHide();             // 챕터 비활성화 시 호출
    void UpdateMoney(int amount);
    void ShowClearPopup();
    void HideClearPopup();
}
