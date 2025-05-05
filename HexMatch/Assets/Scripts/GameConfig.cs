using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class BoardRow
{
    public List<bool> Cells = new List<bool>();
}

public class GameConfig : ScriptableObject
{
    [Header("보드 크기")]
    public int Width = 6;
    public int Height = 6;

    [SerializeField, Header("일반 블럭 종류 수")]
    private int _normalBlockTypeCnt = 6;

    [Header("블록 한 변 기준 길이")]
    public float BlockUnit = 30f;

    [Header("최소 매칭 갯수")]
    public int MinMatchCount = 3;

    [Header("블럭 이동 속도")]
    public float BlockMoveSpeed = 100f;

    [Header("블럭 이동 그래프")]
    public DG.Tweening.Ease BlockMoveGraph = DG.Tweening.Ease.Linear;

    [Header("블럭 스폰 간격")]
    public float BlockSpawnDuration = 0.2f;

    [Header("기본 블록 HP")]
    public int NormalBlockHP = 1;

    [Header("팽이 블록 HP")]
    public int SpinningTopBlockHP = 2;

    [HideInInspector]
    public List<BoardRow> BoardShape = new();

    [HideInInspector]
    public List<BoardRow> SpinningTopSpawnBoard = new();

    public int NormalBlockTypeCount
    {
        get
        {
            int maxNormalBlockTypeCnt = (int)BlockType.Normal_Yellow + 1;
            if (_normalBlockTypeCnt > maxNormalBlockTypeCnt)
            {
                Debug.LogError($"[GameConfig] _blockTypeCount({_normalBlockTypeCnt}) > BlockType.Purple({maxNormalBlockTypeCnt - 1}). 잘못된 설정입니다.");
            }

            return Mathf.Clamp(_normalBlockTypeCnt, 1, maxNormalBlockTypeCnt);
        }
    }

    public int GameClearScore
    {
        get
        {
            int clearScore = 0;
            foreach (var row in this.SpinningTopSpawnBoard)
            {
                foreach (var cell in row.Cells)
                {
                    if (cell == true)
                        clearScore++;
                }
            }

            return clearScore;
        }
    }


}

