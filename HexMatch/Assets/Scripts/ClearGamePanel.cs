using DG.Tweening;
using UnityEngine;

public class ClearGamePanel : MonoBehaviour
{
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private CanvasGroup canvasGroup;

    public static bool Create(out ClearGamePanel outInstance, GameObject rootGO)
    {
        outInstance = null;

        if (!UTIL.TryLoadResource(out GameObject clearGamePanelRes, "prefabs/clear_game_panel"))
            return false;

        GameObject go = Object.Instantiate(clearGamePanelRes, rootGO.transform);

        if (!UTIL.TryGetComponent(out outInstance, go))
            return false;

        return outInstance.OnCreate();
    }

    private bool OnCreate()
    {
        // 초기 상태 숨김
        popupRect.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        popupRect.anchoredPosition = new Vector2(0, -200f); // 아래에서 올라오게

        this.gameObject.SetActive(false);
        return true;
    }

    public void PlayPopupAnimation()
    {
        this.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.AppendCallback(() =>
        {
            popupRect.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
            popupRect.anchoredPosition = new Vector2(0, -200f);
        });

        // 투명도 & 위치 & 스케일 애니메이션 병렬 실행
        seq.Append(canvasGroup.DOFade(1f, 0.3f));
        seq.Join(popupRect.DOAnchorPosY(0f, 0.4f).SetEase(Ease.OutBack));
        seq.Join(popupRect.DOScale(1.1f, 0.4f).SetEase(Ease.OutBack));

        // 약간의 튕김 효과
        seq.Append(popupRect.DOScale(1f, 0.15f).SetEase(Ease.InOutSine));

        // (선택) 자동 사라짐
        // seq.AppendInterval(1.5f);
        // seq.Append(canvasGroup.DOFade(0f, 0.3f));
    }

    public void Show()
    {
        this.gameObject.SetActive(true);

        PlayPopupAnimation();
    }
}
