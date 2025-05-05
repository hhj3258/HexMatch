using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BoardManager
{
    private static readonly Vector2Int[] s_downDirections = new Vector2Int[]
    {
        new Vector2Int(-1, 1), // 아래쪽
        new Vector2Int(0, 1),  // 오른쪽 아래
        new Vector2Int(-1, 0), // 왼쪽 아래
    };

    public enum State
    {
        Idle,
        Dropping,
        Spawning,
        Matching,

        Wait, // 다음 스테이트 대기

        Error,
    }

    private State _boardState = State.Idle;

    private Block[,] _grid;

    private RectTransform _boardRootRT;

    private bool _isInputEnabled = true;

    private GameConfig GameConfig => App.GameMgr.Config;

    public static void Destroy(ref BoardManager boardManager)
    {
        boardManager?.Destroy();
        boardManager = null;
    }

    private void Destroy()
    {
        foreach (var block in _grid)
        {
            if (block != null)
            {
                var item = block;
                Block.Destroy(ref item);
            }
        }
    }

    public static bool Create(out BoardManager outBoardMgr)
    {
        outBoardMgr = new BoardManager();
        return outBoardMgr.OnCreate();
    }

    private bool OnCreate()
    {
        _grid = new Block[this.GameConfig.Width, this.GameConfig.Height];

        if (!Util.TryFindComponent(out _boardRootRT, "block_root")) return false;

        CreateBoard();

        EnterState(State.Matching);
        return true;
    }

    private void CreateBoard()
    {
        for (int q = 0; q < this.GameConfig.Width; q++)
        {
            for (int r = 0; r < this.GameConfig.Height; r++)
            {
                if (!GUtil.IsValidCell(q, r, this.GameConfig.BoardShape))
                    continue;

                if (IsNeedSpawnSpinningTopBlock(q, r))
                {
                    CreateBlock(q, r, BlockType.Special_SpinningTop);
                }
                else
                {
                    CreateBlock(q, r, GetRandomNormalBlockType());
                }
            }
        }


    }

    private bool IsNeedSpawnSpinningTopBlock(int q, int r)
    {
        var spinBoard = this.GameConfig.SpinningTopSpawnBoard;
        return GUtil.IsValidCell(q, r, spinBoard) && spinBoard[q].Cells[r];
    }

    private bool CreateBlock(int q, int r, BlockType blockType)
    {
        Vector2Int axial = new Vector2Int(q, r);

        if (!Block.Create(out Block outBlock, _boardRootRT.gameObject, blockType, axial))
            return false;

        _grid[q, r] = outBlock;
        return true;
    }

    public void Update()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        switch (_boardState)
        {
            case State.Dropping:
                if (TryFindCanMoveBlock(out Block outCanMoveBlock))
                {
                    TryMoveBlock(outCanMoveBlock.Axial.x, outCanMoveBlock.Axial.y);
                }
                else
                {
                    EnterState(State.Spawning, this.GameConfig.BlockSpawnDuration);
                }
                break;

            case State.Spawning:

                Vector2Int spawnAxial = this.GameConfig.BlockSpawnAxial;

                if (IsEmptyCell(spawnAxial.x, spawnAxial.y))
                {
                    TryMakeBlock(out _, spawnAxial.x, spawnAxial.y);

                    EnterState(State.Dropping);
                }
                else
                {
                    EnterState(State.Matching);
                }
                break;

            case State.Matching:
                if (IsAllBlocksStopped())
                {
                    if (TryFindMatchingBlocks(out List<Block> outMatchingBlocks))
                    {
                        CheckAndDestroyBlocks(outMatchingBlocks);
                    }
                    else if (IsInAnyEmptyCell())
                    {
                        EnterState(State.Dropping);
                    }
                    else
                    {
                        EnterState(State.Idle);
                    }
                }
                break;

            case State.Idle:
            case State.Wait:
            case State.Error:
                break;

            default:
                Debug.LogError($"No handling boardState({_boardState})");
                EnterState(State.Error);
                break;
        }
    }

    private void RefreshAllBlockAnim()
    {
        foreach (var block in _grid)
        {
            block?.RefreshAnim();
        }
    }

    public void EnterState(State nextState, float waitDuration)
    {
        EnterState(State.Wait);
        DOVirtual.DelayedCall(waitDuration, () => EnterState(nextState));
    }

    public void EnterState(State nextState)
    {
        if (nextState == _boardState) // 동일 스테이트에 중복 진입하면 흐름에 문제가 있음
        {
            Debug.LogError($"Enter Same BoardState({nextState})");
            return;
        }

        _boardState = nextState;
        Debug.Log($"Enter BoardState({nextState})");

        switch (nextState)
        {
            case State.Dropping:
                DropBlocks();
                break;

            case State.Error:
                App.GameMgr.ReloadGame();
                break;

            case State.Idle:
                if (IsAllBlocksStopped())
                    RefreshAllBlockAnim();
                break;

            case State.Spawning:
            case State.Matching:
            case State.Wait:
                break;

            default:
                Debug.LogError($"No handling boardState({_boardState})");
                EnterState(State.Error);
                break;
        }
    }

    private BlockType GetRandomNormalBlockType()
    {
        int typeCount = this.GameConfig.NormalBlockTypeCount;
        return (BlockType)Random.Range(0, typeCount);
    }

    public bool IsInputEnabled()
    {
        return _isInputEnabled && _boardState == State.Idle;
    }

    private void SetInputEnabled(bool isEnabled)
    {
        _isInputEnabled = isEnabled;
    }

    public void TryMoveBlockByPlayer(Block selectedBlock, Vector2Int moveDir)
    {
        if (selectedBlock.Movability == BlockMovability.Immovable)
            return; // 이동 불가 블록이면 무시

        Vector2Int fromAxial = selectedBlock.Axial;
        Vector2Int toAxial = fromAxial + moveDir;

        if (!GUtil.IsValidCell(toAxial.x, toAxial.y, this.GameConfig.BoardShape))
            return;

        Block targetBlock = _grid[toAxial.x, toAxial.y];
        if (targetBlock == null)
            return;

        SetInputEnabled(false); // 스왑 시작 시 입력 잠금

        SwapBlock(selectedBlock, targetBlock);

        // 2. 매칭 검사
        HashSet<Block> matchingBlocks = new();

        List<Block> selectedBlocks = GUtil.GetNearbyBlocksRecursive(selectedBlock, selectedBlock.Type, _grid, this.GameConfig.BoardShape);
        foreach (Block block in selectedBlocks)
        {
            var matching = GUtil.FindMatchingBlocks(block, _grid, this.GameConfig.BoardShape);
            matchingBlocks.AddRange(matching);
        }

        List<Block> targetBlocks = GUtil.GetNearbyBlocksRecursive(targetBlock, targetBlock.Type, _grid, this.GameConfig.BoardShape);
        foreach (Block block in targetBlocks)
        {
            var matching = GUtil.FindMatchingBlocks(block, _grid, this.GameConfig.BoardShape);
            matchingBlocks.AddRange(matching);
        }

        var onCompleteFunc = Util.WaitUntilTrue(() => !selectedBlock.IsMoving && !targetBlock.IsMoving, () =>
        {
            OnCompleteMoveByPlayer(selectedBlock, targetBlock, matchingBlocks);
        });
        App.GameMgr.CoroutineMgr.Run(onCompleteFunc);
    }

    private void OnCompleteMoveByPlayer(Block selectedBlock, Block targetBlock, HashSet<Block> matchingBlocks)
    {
        if (matchingBlocks.Count >= this.GameConfig.MinMatchCount)
        {
            // 3. 매칭 성공: 매칭 흐름으로 넘어간다
            EnterState(State.Matching);
        }
        else
        {
            // 4. 매칭 실패: 다시 스왑 복구
            SwapBlock(selectedBlock, targetBlock);
        }

        SetInputEnabled(true);
    }

    private void SwapBlock(Block srcBlock, Block targetBlock)
    {
        Vector2Int srcBlockAxial = srcBlock.Axial;
        Vector2Int tarBlockAxial = targetBlock.Axial;

        _grid[srcBlockAxial.x, srcBlockAxial.y] = targetBlock;
        _grid[tarBlockAxial.x, tarBlockAxial.y] = srcBlock;

        srcBlock.SetPos(tarBlockAxial, this.GameConfig.BlockMoveSpeed);
        targetBlock.SetPos(srcBlockAxial, this.GameConfig.BlockMoveSpeed);
    }

    private void CheckAndDestroyBlocks(List<Block> blocks)
    {
        foreach (var block in blocks)
        {
            if (block == null)
                continue;

            block.DecreaseHP();

            if (block.HP <= 0)
            {
                DestroyBlock(block);
            }
        }
    }

    private void DestroyBlock(Block block)
    {
        Vector2Int pos = block.Axial;
        Object.Destroy(block.gameObject);
        _grid[pos.x, pos.y] = null;

        App.GameMgr.onDestroyBlock.Invoke(block.Type);
    }

    private void DropBlocks()
    {
        for (int r = this.GameConfig.Height - 1; r >= 0; r--)
        {
            for (int q = 0; q < this.GameConfig.Width; q++)
            {
                if (_grid[q, r] != null)
                    continue;

                FillCell(q, r);
            }
        }
    }

    private void FillCell(int q, int r)
    {
        if (!GUtil.IsValidCell(q, r, this.GameConfig.BoardShape))
            return;

        if (_grid[q, r] != null)
            return;

        int searchQ = q;
        int searchR = r;

        while (true)
        {
            searchQ += 1;
            searchR -= 1;

            if (!GUtil.IsValidCell(searchQ, searchR, this.GameConfig.BoardShape))
                break;

            if (_grid[searchQ, searchR] != null)
            {
                TryMoveBlock(searchQ, searchR);
                return;
            }
        }
    }

    private bool CanMoveBlock(int fromQ, int fromR, int toQ, int toR)
    {
        if (IsEmptyCell(fromQ, fromR))
            return false;
        if (!IsEmptyCell(toQ, toR))
            return false;

        return true;
    }

    private bool TryFindCanMoveBlock(out Block outBlock)
    {
        outBlock = null;

        for (int q = 0; q < this.GameConfig.Width; q++)
        {
            for (int r = 0; r < this.GameConfig.Height; r++)
            {
                Block block = _grid[q, r];
                if (block == null)
                    continue;

                foreach (var downDir in s_downDirections)
                {
                    int downQ = q + downDir.x;
                    int downR = r + downDir.y;

                    if (CanMoveBlock(q, r, downQ, downR))
                    {
                        outBlock = block;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryMoveBlock(int q, int r)
    {
        Block movingBlock = _grid[q, r];
        if (movingBlock == null)
            return false;

        Vector2Int[] randDirections = s_downDirections.ToArray();
        randDirections.Shuffle(1, randDirections.Length - 1); // 좌, 우만 랜덤

        foreach (var downDir in randDirections)
        {
            int downQ = q + downDir.x;
            int downR = r + downDir.y;

            if (!CanMoveBlock(q, r, downQ, downR))
                continue;

            _grid[downQ, downR] = movingBlock;
            _grid[q, r] = null;

            Vector2Int toAxial = new Vector2Int(downQ, downR);
            movingBlock.SetPos(toAxial, this.GameConfig.BlockMoveSpeed);
            return true;
        }

        return false;
    }

    private bool IsAllBlocksStopped()
    {
        foreach (var block in _grid)
        {
            if (block != null && block.IsMoving)
                return false;
        }

        return true;
    }

    private bool IsInAnyEmptyCell()
    {
        for (int q = 0; q < this.GameConfig.Width; q++)
        {
            for (int r = 0; r < this.GameConfig.Height; r++)
            {
                if (IsEmptyCell(q, r))
                    return true;
            }
        }
        return false;
    }

    private bool IsEmptyCell(int q, int r)
    {
        if (!GUtil.IsValidCell(q, r, this.GameConfig.BoardShape))
            return false;

        return _grid[q, r] == null;
    }

    private bool TryMakeBlock(out Block outBlock, int q, int r)
    {
        outBlock = null;

        if (!IsEmptyCell(q, r))
            return false;

        BlockType blockType = GetRandomNormalBlockType();
        Vector2Int firstAxial = new Vector2Int(q + 1, r - 1);

        if (!Block.Create(out outBlock, _boardRootRT.gameObject, blockType, firstAxial))
            return false;

        _grid[q, r] = outBlock;
        outBlock.SetPos(new Vector2Int(q, r), this.GameConfig.BlockMoveSpeed);

        return true;
    }

    private bool TryFindMatchingBlocks(out List<Block> outBlocks)
    {
        outBlocks = new List<Block>();
        HashSet<Block> matchedBlocksSet = new();

        for (int q = 0; q < this.GameConfig.Width; q++)
        {
            for (int r = 0; r < this.GameConfig.Height; r++)
            {
                Block block = _grid[q, r];
                if (block == null)
                    continue;

                List<Block> matchingBlocks = GUtil.FindMatchingBlocks(block, _grid, this.GameConfig.BoardShape);
                if (matchingBlocks.Count < this.GameConfig.MinMatchCount)
                    continue;

                matchedBlocksSet.AddRange(matchingBlocks);
            }
        }

        if (matchedBlocksSet.Count <= 0)
            return false;

        outBlocks = matchedBlocksSet.ToList();

        List<Block> spinningBlocks = GUtil.GetNearbySpinningTopBlocks(outBlocks, _grid, this.GameConfig.BoardShape);
        outBlocks.AddRange(spinningBlocks);

        return true;
    }

}
