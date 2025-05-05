using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Game Util
/// 게임 전용 유틸리티 함수 모음 (퍼즐 로직, 보드 탐색 등)
/// </summary>
public static class GUTIL
{
    private static readonly Vector2Int[] s_nearbyDirections = new Vector2Int[]
    {
        new (1, -1), // 위쪽
        new (1, 0),  // 오른쪽 위
        new (0, 1),  // 오른쪽 아래
        new (-1, 1), // 아래쪽
        new (-1, 0), // 왼쪽 아래
        new (0, -1), // 왼쪽 위
    };

    // 30도 기울어진 육각형 기준 6방향 정의
    private static readonly Vector2[] s_dirVectors = new Vector2[]
    {
        new (0f, 1f),     // 아래로
        new (0.866f, 0.5f),  // 오른쪽 아래
        new (0.866f, -0.5f), // 오른쪽 위 (cos30, -sin30)
        new (0f, -1f),    // 위로
        new (-0.866f, -0.5f), // 왼쪽 위
        new (-0.866f, 0.5f), // 왼쪽 아래
    };

    // 인접한 같은 타입의 블록을 모두 찾는다
    public static List<Block> GetNearbyBlocksRecursive(Block block, BlockType blockType, Block[,] grid, List<BoardRow> boardShape)
    {
        HashSet<Block> visited = new HashSet<Block>();
        List<Block> connectedBlocks = new List<Block>();

        RecursiveFind(block);

        return connectedBlocks;

        void RecursiveFind(Block startBlock)
        {
            if (startBlock == null || visited.Contains(startBlock))
                return;

            visited.Add(startBlock);
            connectedBlocks.Add(startBlock);

            var nearbyBlocks = GetNearbyBlocks(block, blockType, grid, boardShape);
            foreach (var nearbyBlock in nearbyBlocks)
            {
                RecursiveFind(nearbyBlock);
            }
        }
    }

    public static List<Block> GetNearbyBlocks(Block startBlock, BlockType targetBlockType, Block[,] grid, List<BoardRow> boardShape)
    {
        List<Block> connectedBlocks = new List<Block>();

        foreach (var dir in s_nearbyDirections)
        {
            Vector2Int nearbyAxial = startBlock.Axial + dir;

            if (!IsValidCell(nearbyAxial.x, nearbyAxial.y, boardShape))
                continue;

            Block nearbyBlock = grid[nearbyAxial.x, nearbyAxial.y];
            if (nearbyBlock == null)
                continue;
            if (nearbyBlock.Type != targetBlockType)
                continue;

            connectedBlocks.Add(nearbyBlock);
        }

        return connectedBlocks;
    }

    public static List<Block> GetNearbySpinningTopBlocks(List<Block> blocks, Block[,] grid, List<BoardRow> boardShape)
    {
        HashSet<Block> spinBlockSet = new();
        foreach (var block in blocks)
        {
            var nearbySpinBlocks = GetNearbyBlocks(block, BlockType.Special_SpinningTop, grid, boardShape);
            foreach (var nearbyBlock in nearbySpinBlocks)
            {
                spinBlockSet.Add(nearbyBlock);
            }
        }

        return spinBlockSet.ToList();
    }

    /// 주어진 Axial 좌표(q, r)가 보드 범위 안에 존재하며, 활성화(true) 상태인지를 체크
    public static bool IsValidCell(int q, int r, List<BoardRow> boardShape)
    {
        if (boardShape == null)
            return false;
        if (q < 0 || q >= boardShape.Count)
            return false;
        if (boardShape[q]?.Cells == null)
            return false;
        if (r < 0 || r >= boardShape[q].Cells.Count)
            return false;

        return boardShape[q].Cells[r] == true;
    }

    /// <summary>
    /// 특정 블록을 기준으로 다양한 매칭 패턴(직선, 사각형 등) 탐색.
    /// </summary>
    public static List<Block> FindMatchingBlocks(Block startBlock, Block[,] grid, List<BoardRow> boardShape)
    {
        if (startBlock == null)
            return new List<Block>();

        if (!IsMatchingBlockType(startBlock.Type))
            return new List<Block>();

        HashSet<Block> matchedBlocks = new HashSet<Block>();

        // 매칭 규칙들을 순서대로 적용
        CheckLineMatch(startBlock, grid, boardShape, matchedBlocks);
        CheckSquareMatch(startBlock, grid, boardShape, matchedBlocks);

        if (matchedBlocks.Count < APP.GameMgr.Config.MinMatchCount)
            return new List<Block>();

        return new List<Block>(matchedBlocks);
    }

    /// <summary>
    /// 직선 방향으로 같은 블록이 3개 이상 연결된 경우 매칭.
    /// </summary>
    private static void CheckLineMatch(Block startBlock, Block[,] grid, List<BoardRow> boardShape, HashSet<Block> matchedBlocks)
    {
        BlockType targetType = startBlock.Type;

        Vector2Int[] patterns = new Vector2Int[]
        {
            new (1, -1), // 위쪽
            new (1, 0),  // 오른쪽 위
            new (0, 1),  // 오른쪽 아래
        };

        foreach (Vector2Int dir in patterns)
        {
            List<Block> lineGroup = new List<Block> { startBlock };

            // 양방향 모두 탐색 (정방향 + 역방향)
            foreach (int sign in new int[] { 1, -1 })
            {
                Vector2Int curAxial = startBlock.Axial;

                while (true)
                {
                    curAxial += dir * sign;

                    if (!GUTIL.IsValidCell(curAxial.x, curAxial.y, boardShape))
                        break;

                    Block nextBlock = grid[curAxial.x, curAxial.y];
                    if (nextBlock == null || nextBlock.Type != targetType)
                        break;

                    lineGroup.Add(nextBlock);
                }
            }

            if (lineGroup.Count >= APP.GameMgr.Config.MinMatchCount)
            {
                foreach (var block in lineGroup)
                    matchedBlocks.Add(block);
            }
        }
    }

    // 4개의 블록이 사각형(ㅁ) 형태로 연결된 경우 매칭
    private static void CheckSquareMatch(Block startBlock, Block[,] grid, List<BoardRow> boardShape, HashSet<Block> matchedBlocks)
    {
        BlockType targetType = startBlock.Type;

        Vector2Int[][] squarePatterns = new Vector2Int[][]
        {
            new Vector2Int[] { new (-1, 0), new (-1, 1), new (0, 1) },
            new Vector2Int[] { new (1, -1), new (1, 0), new (0, -1) },
            new Vector2Int[] { new (1, -1), new (1, 0), new (0, 1) },
            new Vector2Int[] { new (1, 0), new (0, 1), new (-1, 1) },
        };

        foreach (var pattern in squarePatterns)
        {
            Vector2Int check1 = startBlock.Axial + pattern[0];
            Vector2Int check2 = startBlock.Axial + pattern[1];
            Vector2Int check3 = startBlock.Axial + pattern[2];

            if (!GUTIL.IsValidCell(check1.x, check1.y, boardShape)) continue;
            if (!GUTIL.IsValidCell(check2.x, check2.y, boardShape)) continue;
            if (!GUTIL.IsValidCell(check3.x, check3.y, boardShape)) continue;

            Block block1 = grid[check1.x, check1.y];
            Block block2 = grid[check2.x, check2.y];
            Block block3 = grid[check3.x, check3.y];

            if (block1 == null || block2 == null || block3 == null)
                continue;

            if (block1.Type == targetType && block2.Type == targetType && block3.Type == targetType)
            {
                matchedBlocks.Add(startBlock);
                matchedBlocks.Add(block1);
                matchedBlocks.Add(block2);
                matchedBlocks.Add(block3);
            }
        }
    }

    public static Vector2Int GetAxialMoveDir(Vector2 dragDir)
    {
        int bestIdx = 0;
        float bestDot = -1f;

        for (int i = 0; i < s_dirVectors.Length; i++)
        {
            float dot = Vector2.Dot(dragDir.normalized, s_dirVectors[i]);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestIdx = i;
            }
        }

        return s_nearbyDirections[bestIdx];
    }

    public static Vector2Int[] GetNearbyDirections()
    {
        return s_nearbyDirections;
    }

    public static BlockMovability GetBlockMovableType(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.Normal_Blue:
            case BlockType.Normal_Green:
            case BlockType.Normal_Orange:
            case BlockType.Normal_Purple:
            case BlockType.Normal_Red:
            case BlockType.Normal_Yellow:
            case BlockType.Special_SpinningTop:
                return BlockMovability.Movable;

            default:
                Debug.LogError($"No handling blockType({blockType})");
                return BlockMovability.Movable;
        }
    }

    public static int GetBlockMaxHP(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.Normal_Blue:
            case BlockType.Normal_Green:
            case BlockType.Normal_Orange:
            case BlockType.Normal_Purple:
            case BlockType.Normal_Red:
            case BlockType.Normal_Yellow:
                return APP.GameMgr.Config.NormalBlockHP;

            case BlockType.Special_SpinningTop:
                return APP.GameMgr.Config.SpinningTopBlockHP;

            default:
                Debug.LogError($"No handling blockType({blockType})");
                return APP.GameMgr.Config.NormalBlockHP;
        }
    }

    private static bool IsMatchingBlockType(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.Normal_Blue:
            case BlockType.Normal_Green:
            case BlockType.Normal_Orange:
            case BlockType.Normal_Purple:
            case BlockType.Normal_Red:
            case BlockType.Normal_Yellow:
                return true;

            case BlockType.Special_SpinningTop:
                return false;

            default:
                Debug.LogError($"No handling blockType({blockType})");
                return false;
        }
    }
}
