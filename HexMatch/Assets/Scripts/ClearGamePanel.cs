using DG.Tweening;
using UnityEngine;

public class ClearGamePanel : MonoBehaviour
{
    [SerializeField]
    private RectTransform _imgRect;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    public static void Destroy(ref ClearGamePanel clearGamePanel)
    {
        clearGamePanel?.Destroy();
        clearGamePanel = null;
    }

    private void Destroy()
    {
        _imgRect = null;
        _canvasGroup = null;

        if (this != null)
            Destroy(this.gameObject);
    }

    public static bool Create(out ClearGamePanel outInstance, GameObject rootGO)
    {
        outInstance = null;

        if (!Util.TryLoadResource(out GameObject clearGamePanelRes, "prefabs/clear_game_panel"))
            return false;

        GameObject go = Object.Instantiate(clearGamePanelRes, rootGO.transform);

        if (!Util.TryGetComponent(out outInstance, go))
            return false;

        return outInstance.OnCreate();
    }

    private bool OnCreate()
    {
        // 초기 상태 숨김
        _imgRect.localScale = Vector3.zero;
        _canvasGroup.alpha = 0f;
        _imgRect.anchoredPosition = new Vector2(0, -200f); // 아래에서 올라오게

        this.gameObject.SetActive(false);
        return true;
    }

    private void PlayShowAnim()
    {
        Sequence seq = DOTween.Sequence();

        seq.AppendCallback(() =>
        {
            _imgRect.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;
            _imgRect.anchoredPosition = new Vector2(0, -200f);
        });

        seq.Append(_canvasGroup.DOFade(1f, 0.3f));
        seq.Join(_imgRect.DOAnchorPosY(0f, 0.4f).SetEase(Ease.OutBack));
        seq.Join(_imgRect.DOScale(1.1f, 0.4f).SetEase(Ease.OutBack));

        seq.Append(_imgRect.DOScale(1f, 0.15f).SetEase(Ease.InOutSine));
    }

    public void Show()
    {
        this.gameObject.SetActive(true);

        PlayShowAnim();
    }
}
