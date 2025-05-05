using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameConfig Config { get; private set; }
    public CoroutineManager CoroutineMgr { get; private set; }
    public BoardManager BoardMgr { get; private set; }

    private bool _isCreateComplete = false;

    public UnityEvent OnClearGame { get; private set; } = new();

    private Button _restartButton;

    private ClearGamePanel _clearGamePanel;

    private void OnDestroy()
    {
        _restartButton.onClick.RemoveAllListeners();
        this.OnClearGame.RemoveAllListeners();

        this.Config = null;
        this.CoroutineMgr = null;
        this.BoardMgr = null;
        _clearGamePanel = null;
    }

    private void Awake()
    {
        //프레임 60
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (!UTIL.TryLoadResource(out GameConfig outConfig, "game_config")) return;
        this.Config = outConfig;

        if (!CoroutineManager.Create(out CoroutineManager coroutineMgr)) return;
        this.CoroutineMgr = coroutineMgr;

        if (!BoardManager.Create(out BoardManager boardMgr)) return;
        this.BoardMgr = boardMgr;

        if (!UTIL.TryFindComponent(out _restartButton, "reset_button")) return;
        _restartButton.onClick.AddListener(ReloadGame);

        if (!UTIL.TryFindGameObject(out GameObject outCanvasGo, "canvas")) return;
        if (!ClearGamePanel.Create(out _clearGamePanel, outCanvasGo)) return;

        this.OnClearGame.AddListener(ClearGame);

        _isCreateComplete = true;
    }


    private void Update()
    {
        if (!_isCreateComplete)
            return;

        this.BoardMgr.Update();

#if UNITY_EDITOR
        Update_Debug();
#endif
    }

    private void ClearGame()
    {
        _clearGamePanel.Show();
    }

    public void ReloadGame()
    {
        SceneManager.LoadScene("GamePhase");
    }

    private void Update_Debug()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ReloadGame();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            this.BoardMgr.EnterState(BoardManager.BoardState.Matching);
        }
#endif
    }

}

