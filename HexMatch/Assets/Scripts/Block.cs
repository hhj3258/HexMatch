using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Block : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    public BlockType Type { get; private set; }

    public Vector2Int Axial { get; private set; } // (q, r)

    public RectTransform RectTransform { get; private set; }

    public bool IsMoving { get; private set; }

    public int HP { get; private set; }

    public BlockMovability Movability { get; private set; }

    private Tween _spinAnimTween;

    private Vector3 _originEulerAngles;

    private Vector2 _dragStartPos;
    private bool _isDragging = false;

    private GameConfig GameConfig => App.GameMgr.Config;

    public static void Destroy(ref Block block)
    {
        block?.Destroy();
        block = null;
    }

    private void Destroy()
    {
        _spinAnimTween.Kill();
        _spinAnimTween = null;

        this.IsMoving = false;

        Destroy(this.gameObject);
    }

    public static bool Create(out Block outBlock, GameObject rootGO, BlockType type, Vector2Int axial)
    {
        outBlock = null;

        if (!Util.TryLoadResource(out GameObject blockRes, "Prefabs/block"))
            return false;

        GameObject go = Object.Instantiate(blockRes, rootGO.transform);

        if (!Util.TryGetComponent(out outBlock, go))
            return false;

        return outBlock.OnCreate(type, axial);
    }

    private bool OnCreate(BlockType type, Vector2Int axial)
    {
        this.Type = type;
        this.Axial = axial;

        if (!Util.TryGetComponent(out RectTransform outRect, this.gameObject)) return false;
        if (!Util.TryGetComponent(out Image outImg, this.gameObject)) return false;
        if (!TryGetBlockSprite(out Sprite outSprite, type)) return false;

        this.RectTransform = outRect;
        this.RectTransform.anchoredPosition = GetAnchoredPos(axial);

        outImg.sprite = outSprite;

        this.HP = GUtil.GetBlockMaxHP(this.Type);
        this.Movability = GUtil.GetBlockMovableType(this.Type);

        RefreshName();

        _originEulerAngles = this.transform.eulerAngles;

        return true;
    }

    private bool TryGetBlockSprite(out Sprite outSprite, BlockType type)
    {
        string blockSpriteName = type.ToString().ToLower();

        if (!Util.TryLoadResource(out outSprite, $"icons/{blockSpriteName}"))
        {
            Util.TryLoadResource(out outSprite, $"icons/normal_blue");
            return false;
        }

        return true;
    }

    public void SetPos(Vector2Int axial)
    {
        this.Axial = axial;

        // UI 위치 업데이트
        this.RectTransform.anchoredPosition = GetAnchoredPos(axial);

        RefreshName();

        this.IsMoving = false;
    }

    public void SetPos(Vector2Int axial, float moveSpeed)
    {
        if (moveSpeed <= 0f)
        {
            SetPos(axial);
            return;
        }

        Vector2 targetPos = GetAnchoredPos(axial);
        Vector2 currentPos = this.RectTransform.anchoredPosition;

        float distance = Vector2.Distance(currentPos, targetPos);
        float moveDuration = distance / moveSpeed; // 거리 / 속도 = 이동 시간

        this.IsMoving = true; // 이동 시작
        this.Axial = axial;
        RefreshName();

        this.RectTransform.DOKill();
        this.RectTransform.DOAnchorPos(targetPos, moveDuration)
            .SetEase(App.GameMgr.Config.BlockMoveGraph)
            .OnComplete(() =>
            {
                this.IsMoving = false;
            })
            .SetLink(this.gameObject);
    }

    private void RefreshName()
    {
#if UNITY_EDITOR
        this.gameObject.name = $"block_{this.Type}_{this.Axial}";
#endif
    }

    private Vector2 GetAnchoredPos(Vector2Int axial)
    {
        // sqrt(3) ≈ 1.732, 30도 기울어진 육각형에서 가로 방향 블록 간 거리 비율
        float sqrt3 = Mathf.Sqrt(3f);

        // (1) 육각형 블록 간 가로(x축) 이동 간격 계산
        // 블록 한 변 길이에 √3을 곱해 가로 이동 간격을 만든다.
        float xSpacing = this.GameConfig.BlockUnit * sqrt3;

        // (2) 육각형 블록 간 세로(y축) 이동 간격 계산
        // 블록 한 변 길이에 1.5를 곱해 세로 이동 간격을 만든다.
        float ySpacing = this.GameConfig.BlockUnit * 1.5f;

        float x = xSpacing * axial.x + (xSpacing / 2f) * axial.y;
        float y = ySpacing * axial.y;

        Vector2 rawPos = new Vector2(x, -y); // UI 반전

        // 전체 보드 중심 보정
        float totalWidth = (this.GameConfig.Width - 1) * xSpacing + (this.GameConfig.Height - 1) * (xSpacing / 2f);
        float totalHeight = (this.GameConfig.Height - 1) * ySpacing;
        Vector2 centerOffset = new Vector2(totalWidth / 2f, -totalHeight / 2f);

        return rawPos - centerOffset;
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (!App.GameMgr.BoardMgr.IsInputEnabled())
            return;

        _dragStartPos = eventData.position;
        _isDragging = true;
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        Vector2 dragDelta = eventData.position - _dragStartPos;

        if (dragDelta.magnitude < 10f) // 최소 드래그 거리 설정
            return;

        Vector2 direction = dragDelta.normalized;

        Vector2Int moveDir = GUtil.GetAxialMoveDir(direction);

        App.GameMgr.BoardMgr.TryMoveBlockByPlayer(this, moveDir);

        _isDragging = false;
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    public float GetEstimatedMoveDuration(Vector2Int targetAxial, float moveSpeed)
    {
        Vector2 targetPos = GetAnchoredPos(targetAxial);
        float distance = Vector2.Distance(RectTransform.anchoredPosition, targetPos);
        return distance / moveSpeed;
    }

    public void DecreaseHP()
    {
        if (this.HP > 0)
            this.HP--;
    }

    private bool IsUseSpinAnim()
    {
        return this.Type == BlockType.Special_SpinningTop && this.HP == 1;
    }

    private void PlaySpinAnim()
    {
        StopSpinAnim();

        float baseZ = this.transform.eulerAngles.z;

        baseZ = baseZ > 180f ? baseZ -= 360f : baseZ;

        _spinAnimTween = DOTween.Sequence()
            .Append(DOVirtual.Float(baseZ - 60f, baseZ, 0.5f, value =>
            {
                this.transform.localRotation = Quaternion.Euler(0, 0, value);
            }).SetEase(Ease.InOutSine))
            .Append(DOVirtual.Float(baseZ, baseZ - 60f, 0.5f, value =>
            {
                this.transform.localRotation = Quaternion.Euler(0, 0, value);
            }).SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetLink(this.gameObject); // 오브젝트 파괴되면 자동 종료
    }

    private void StopSpinAnim()
    {
        if (_spinAnimTween != null && _spinAnimTween.IsActive())
        {
            _spinAnimTween.Kill();
            _spinAnimTween = null;

            this.transform.eulerAngles = _originEulerAngles;
        }
    }

    public void RefreshAnim()
    {
        if (IsUseSpinAnim())
            PlaySpinAnim();
    }

}
