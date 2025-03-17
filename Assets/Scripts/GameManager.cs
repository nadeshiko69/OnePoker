
// GameManager.cs
// カードゲームの管理を行うクラス
// プレイヤーとCPUが交互にカードを引き、数字の大小を競うシンプルなゲーム

// 主な機能:
// - デッキの初期化と管理 (1-13の数字カード)
// - カードのシャッフル
// - ゲームの状態管理 (Ready, Deal, PlayerTurn, CPUTurn, Result)
// - プレイヤーとCPUのカード管理

// 使用方法:
// 1. StartGame()でゲームを開始
// 2. プレイヤーのターン後、CPUが自動的にカードを引く
// 3. 結果を表示して次のラウンドへ

using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    
    private List<int> deck = new List<int>();
    private int playerCard;
    private int cpuCard;
    
    private GameState currentState = GameState.Ready;
    
    private enum GameState
    {
        Ready,
        Deal,
        PlayerTurn,
        CPUTurn,
        Result
    }
    
    private void Start()
    {
        InitializeDeck();
    }
    
    private void InitializeDeck()
    {
        deck.Clear();
        for (int i = 1; i <= 13; i++)
        {
            deck.Add(i);
        }
    }
    
    private void ShuffleDeck()
    {
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            int temp = deck[k];
            deck[k] = deck[n];
            deck[n] = temp;
        }
    }
    
    public void StartGame()
    {
        InitializeDeck();
        ShuffleDeck();
        DealCards();
        currentState = GameState.PlayerTurn;
    }
    
    private void DealCards()
    {
        playerCard = DrawCard();
        cpuCard = DrawCard();
    }
    
    private int DrawCard()
    {
        if (deck.Count == 0) return -1;
        
        int card = deck[0];
        deck.RemoveAt(0);
        return card;
    }
}
