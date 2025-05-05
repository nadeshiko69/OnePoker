using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour
{
    // ライフ関連
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;
    private const int MAX_LIFE = 40;
    private const int MIN_LIFE = 0;
    private int playerLife = 20;  // 初期ライフを20に変更
    private int opponentLife = 20;  // 初期ライフを20に変更

    // ゲーム回数管理
    private const int MAX_GAMES = 15;
    private int currentGameCount = 0;

    // UI関連
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;

    // 参照
    private GameManager gameManager;
    private RandomChoiceCard randomChoiceCard;

    public int PlayerLife => playerLife;
    public int OpponentLife => opponentLife;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        randomChoiceCard = FindObjectOfType<RandomChoiceCard>();
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartMatch);
        }

        UpdateLifeUI();
    }

    void Update()
    {
        // マッチの終了条件を確認
        if (IsMatchOver())
        {
            ShowGameOver();
        }

        // まだ終わっていない場合、Gameが終了したら次のGameを開始する

    }

    private void UpdateLifeUI()
    {
        if (playerLifeText != null)
        {
            playerLifeText.text = $"Life: {playerLife}";
        }
        if (opponentLifeText != null)
        {
            opponentLifeText.text = $"Life: {opponentLife}";
        }
    }

    public void UpdatePlayerLife(int amount)
    {
        playerLife = Mathf.Clamp(playerLife + amount, MIN_LIFE, MAX_LIFE);
        UpdateLifeUI();
        CheckMatchEnd();
    }

    public void UpdateOpponentLife(int amount)
    {
        opponentLife = Mathf.Clamp(opponentLife + amount, MIN_LIFE, MAX_LIFE);
        UpdateLifeUI();
        CheckMatchEnd();
    }

    // 1回のゲームが終了したときの処理
    public void OnGameComplete()
    {
        currentGameCount++;
        Debug.Log($"Game {currentGameCount} completed. Player Life: {playerLife}, Opponent Life: {opponentLife}");

        // マッチの終了条件を確認
        if (IsMatchOver())
        {
            ShowGameOver();
        }
        else
        {
            // 次のゲームの準備
            PrepareNextGame();
        }
    }

    private bool IsMatchOver()
    {
        return currentGameCount >= MAX_GAMES || playerLife <= MIN_LIFE || opponentLife <= MIN_LIFE;
    }

    private void CheckMatchEnd()
    {
        if (IsMatchOver())
        {
            ShowGameOver();
        }
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            string result;
            if (currentGameCount >= MAX_GAMES)
            {
                if (playerLife > opponentLife)
                    result = "MATCH WIN!";
                else if (opponentLife > playerLife)
                    result = "MATCH LOSE...";
                else
                    result = "DRAW";
            }
            else
            {
                if (playerLife <= MIN_LIFE)
                    result = "MATCH LOSE...";
                else
                    result = "MATCH WIN!";
            }

            if (gameOverText != null)
            {
                gameOverText.text = result;
            }
        }
    }

    private void PrepareNextGame()
    {
        // カードをシャッフル
        if (randomChoiceCard != null)
        {
            randomChoiceCard.ShuffleCard();
        }
        else
        {
            Debug.LogError("RandomChoiceCard not found!");
        }

        // 画面のリセット
        if (gameManager != null)
        {
            gameManager.ClearPlayerCards();
        }
        else
        {
            Debug.LogError("GameManager not found!");
        }
    }

    public void RestartMatch()
    {
        // 全ての値を初期状態に戻す
        playerLife = 20;  // 初期ライフを20に変更
        opponentLife = 20;  // 初期ライフを20に変更
        currentGameCount = 0;
        
        UpdateLifeUI();
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 新しいマッチを開始
        PrepareNextGame();
    }
} 