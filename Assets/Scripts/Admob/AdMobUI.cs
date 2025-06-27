using UnityEngine;
using UnityEngine.UI;

public class AdMobUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button showBannerButton;
    [SerializeField] private Button hideBannerButton;
    [SerializeField] private Button showInterstitialButton;
    [SerializeField] private Button showRewardedButton;
    
    [Header("Reward Settings")]
    [SerializeField] private int rewardCoins = 100;
    
    private void Start()
    {
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        if (showBannerButton != null)
        {
            showBannerButton.onClick.AddListener(ShowBannerAd);
        }
        
        if (hideBannerButton != null)
        {
            hideBannerButton.onClick.AddListener(HideBannerAd);
        }
        
        if (showInterstitialButton != null)
        {
            showInterstitialButton.onClick.AddListener(ShowInterstitialAd);
        }
        
        if (showRewardedButton != null)
        {
            showRewardedButton.onClick.AddListener(ShowRewardedAd);
        }
        
        // リワード広告完了時のイベントを登録
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.OnRewardedAdCompleted += OnRewardedAdCompleted;
        }
    }
    
    private void ShowBannerAd()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowBannerAd();
        }
    }
    
    private void HideBannerAd()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.HideBannerAd();
        }
    }
    
    private void ShowInterstitialAd()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowInterstitialAd();
        }
    }
    
    private void ShowRewardedAd()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowRewardedAd();
        }
    }
    
    private void OnRewardedAdCompleted()
    {
        // リワード付与の処理
        Debug.Log($"Player earned {rewardCoins} coins!");
        
        // ここでゲーム内通貨やアイテムを付与
        // 例: GameManager.Instance.AddCoins(rewardCoins);
    }
    
    private void OnDestroy()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.OnRewardedAdCompleted -= OnRewardedAdCompleted;
        }
    }
} 