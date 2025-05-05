using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public readonly string GameSceneName = "GamePhase";

    public GameConfig Config { get; private set; }
    public CoroutineManager CoroutineMgr { get; private set; }
    public BoardManager BoardMgr { get; private set; }

    public UnityEvent<BlockType> onDestroyBlock { get; private set; } = new();

    public int GameScore { get; private set; } = 0;

    private Button _restartBtn;

    private ClearGamePanel _clearGamePanel;

    private bool _isCreateComplete = false;

    private void OnDestroy()
    {
        if (!_isCreateComplete)
            return;

        _isCreateComplete = false;

        this.onDestroyBlock.RemoveAllListeners();
        _restartBtn.onClick.RemoveAllListeners();

        this.Config = null;

        var coroutineMgr = this.CoroutineMgr;
        CoroutineManager.Destroy(ref coroutineMgr);

        var boardMgr = this.BoardMgr;
        BoardManager.Destroy(ref boardMgr);

        ClearGamePanel.Destroy(ref _clearGamePanel);

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Awake()
    {
        if (!Util.TryLoadResource(out GameConfig outConfig, "game_config")) return;
        this.Config = outConfig;

        if (!CoroutineManager.Create(out CoroutineManager coroutineMgr)) return;
        this.CoroutineMgr = coroutineMgr;

        if (!BoardManager.Create(out BoardManager boardMgr)) return;
        this.BoardMgr = boardMgr;

        if (!Util.TryFindComponent(out _restartBtn, "reset_button")) return;
        _restartBtn.onClick.AddListener(ReloadGame);

        if (!Util.TryFindGameObject(out GameObject outCanvasGo, "canvas")) return;
        if (!ClearGamePanel.Create(out _clearGamePanel, outCanvasGo)) return;

        this.onDestroyBlock.AddListener(OnDestroyBlock);

        SetFPS(this.Config.FPS);

        SceneManager.sceneUnloaded += OnSceneUnloaded;

        _isCreateComplete = true;
    }

    private void OnDestroyBlock(BlockType blockType)
    {
        if (blockType != BlockType.Special_SpinningTop)
            return;

        this.GameScore++;

        if (this.GameScore == this.Config.GameClearScore)
        {
            ClearGame();
        }
    }

    private void Update()
    {
        if (!_isCreateComplete)
            return;

        this.BoardMgr.Update();

        UpdateDebug();
    }

    [Conditional("DEBUG")]
    private void UpdateDebug()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ReloadGame();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            this.BoardMgr.EnterState(BoardManager.State.Matching);
        }
    }

    private void ClearGame()
    {
        _clearGamePanel.Show();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        UnityEngine.Debug.Log($"Scene Unloaded. scene({scene.name})");

        if (scene.name == this.GameSceneName)
        {
            OnDestroy();
        }
    }

    public void ReloadGame()
    {
        SceneManager.LoadScene(this.GameSceneName);
    }

    private void SetFPS(int fps)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }



}

