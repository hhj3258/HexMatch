using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameConfig))]
public class GameConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameConfig config = (GameConfig)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Board Shape Editor (게임상 표기는 반시계방향 30도 돌아가 있음)", EditorStyles.boldLabel);

        // 일반 보드 셰이프
        DrawBoardShapeEditor(config.BoardShape, nameof(config.BoardShape), config, true);

        EditorGUILayout.Space();

        // 팽이 블럭 스폰 보드 셰이프 (크기 자동 동기화)
        SyncSpinningTopSpawnBoardSize(config);
        DrawBoardShapeEditor(config.SpinningTopSpawnBoard, nameof(config.SpinningTopSpawnBoard), config, false);
    }

    private void DrawBoardShapeEditor(List<BoardRow> boardShape, string propertyName, GameConfig config, bool isUseResizeButton)
    {
        if (boardShape == null)
        {
            EditorGUILayout.HelpBox($"{propertyName}가 비어 있습니다.", MessageType.Info);
            return;
        }

        SerializedProperty boardShapeProperty = serializedObject.FindProperty(propertyName);
        EditorGUILayout.PropertyField(boardShapeProperty, includeChildren: true);

        if (isUseResizeButton)
        {
            if (GUILayout.Button($"Resize {propertyName}"))
            {
                ResizeBoardShape(boardShape, config.Width, config.Height);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        int boardWidth = boardShape.Count;
        int boardHeight = 0;
        foreach (var row in boardShape)
        {
            if (row != null && row.Cells != null)
                boardHeight = Mathf.Max(boardHeight, row.Cells.Count);
        }

        for (int r = 0; r < boardHeight; r++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int q = 0; q < boardWidth; q++)
            {
                if (q >= boardShape.Count)
                    break;

                var row = boardShape[q];
                if (row == null)
                {
                    row = new BoardRow();
                    boardShape[q] = row;
                }
                if (row.Cells == null)
                    row.Cells = new List<bool>();

                while (row.Cells.Count <= r)
                    row.Cells.Add(true);

                bool value = row.Cells[r];
                row.Cells[r] = GUILayout.Toggle(value, "", GUILayout.Width(20), GUILayout.Height(20));
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(config);
        }
    }

    private void ResizeBoardShape(List<BoardRow> boardShape, int width, int height)
    {
        if (boardShape == null)
            return;

        while (boardShape.Count < width)
            boardShape.Add(new BoardRow());
        while (boardShape.Count > width)
            boardShape.RemoveAt(boardShape.Count - 1);

        for (int q = 0; q < boardShape.Count; q++)
        {
            if (boardShape[q].Cells == null)
                boardShape[q].Cells = new List<bool>();

            var cells = boardShape[q].Cells;
            while (cells.Count < height)
                cells.Add(true);
            while (cells.Count > height)
                cells.RemoveAt(cells.Count - 1);
        }
    }

    /// <summary>
    /// BoardShape 기준으로 SpinningTopSpawnBoard의 크기를 항상 맞춰준다.
    /// </summary>
    private void SyncSpinningTopSpawnBoardSize(GameConfig config)
    {
        if (config.BoardShape == null || config.SpinningTopSpawnBoard == null)
            return;

        int width = config.BoardShape.Count;
        int height = 0;
        foreach (var row in config.BoardShape)
        {
            if (row != null && row.Cells != null)
                height = Mathf.Max(height, row.Cells.Count);
        }

        ResizeBoardShape(config.SpinningTopSpawnBoard, width, height);
    }
}
