using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdMobManager : MonoBehaviour
{
    [Header("AdMob App IDs")]
    [SerializeField] private string androidAppId = "ca-app-pub-1142801310983686~9710648064";
    [SerializeField] private string iosAppId = "ca-app-pub-1142801310983686~5472020152";
    
    [Header("Banner Ad Unit IDs")]
    [SerializeField] private string androidBannerAdUnitId = "ca-app-pub-1142801310983686/9954190641";
    [SerializeField] private string iosBannerAdUnitId = "ca-app-pub-1142801310983686/7519598995";
    
    [Header("Interstitial Ad Unit IDs")]
    [SerializeField] private string androidInterstitialAdUnitId = "ca-app-pub-1142801310983686/1776317723";
    [SerializeField] private string iosInterstitialAdUnitId = "ca-app-pub-1142801310983686/3540260888";
    
    [Header("Rewarded Ad Unit IDs")]
    [SerializeField] private string androidRewardedAdUnitId = "ca-app-pub-1142801310983686/7283795279";
    [SerializeField] private string iosRewardedAdUnitId = "ca-app-pub-1142801310983686/5603882095";

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    
    public static AdMobManager Instance { get; private set; }
    
    public event Action OnRewardedAdCompleted;

    // 現在のプラットフォームとIDを取得するプロパティ
    public string CurrentPlatform => GetCurrentPlatform();
    public string CurrentAppId => GetCurrentAppId();
    public string CurrentBannerAdUnitId => GetCurrentBannerAdUnitId();
    public string CurrentInterstitialAdUnitId => GetCurrentInterstitialAdUnitId();
    public string CurrentRewardedAdUnitId => GetCurrentRewardedAdUnitId();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        MobileAds.Initialize(initStatus => {
            Debug.Log("AdMob initialized");
            LoadBannerAd();
            LoadInterstitialAd();
            LoadRewardedAd();
        });
    }

    #region Platform-specific ID getters
    private string GetCurrentPlatform()
    {
        #if UNITY_ANDROID
            return "Android";
        #elif UNITY_IOS
            return "iOS";
        #else
            return "Unknown";
        #endif
    }

    private string GetCurrentAppId()
    {
        #if UNITY_ANDROID
            return androidAppId;
        #elif UNITY_IOS
            return iosAppId;
        #else
            return "unused";
        #endif
    }

    private string GetCurrentBannerAdUnitId()
    {
        #if UNITY_ANDROID
            return androidBannerAdUnitId;
        #elif UNITY_IOS
            return iosBannerAdUnitId;
        #else
            return "unused";
        #endif
    }

    private string GetCurrentInterstitialAdUnitId()
    {
        #if UNITY_ANDROID
            return androidInterstitialAdUnitId;
        #elif UNITY_IOS
            return iosInterstitialAdUnitId;
        #else
            return "unused";
        #endif
    }

    private string GetCurrentRewardedAdUnitId()
    {
        #if UNITY_ANDROID
            return androidRewardedAdUnitId;
        #elif UNITY_IOS
            return iosRewardedAdUnitId;
        #else
            return "unused";
        #endif
    }
    #endregion

    #region Banner Ad
    public void LoadBannerAd()
    {
        string adUnitId = GetCurrentBannerAdUnitId();
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
        bannerView.OnBannerAdLoaded += () => Debug.Log("Banner loaded");
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) => Debug.LogError($"Banner failed: {error.GetMessage()}");
        bannerView.OnAdFullScreenContentOpened += () => Debug.Log("Banner full screen opened");
        bannerView.OnAdFullScreenContentClosed += () => Debug.Log("Banner full screen closed");
        bannerView.LoadAd(new AdRequest());
    }

    public void ShowBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Show();
        }
    }

    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }
    #endregion

    #region Interstitial Ad
    public void LoadInterstitialAd()
    {
        string adUnitId = GetCurrentInterstitialAdUnitId();
        InterstitialAd.Load(adUnitId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"Interstitial failed to load: {error?.GetMessage()}");
                return;
            }
            interstitialAd = ad;
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial closed");
                LoadInterstitialAd();
            };
            interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"Interstitial full screen failed: {error.GetMessage()}");
                LoadInterstitialAd();
            };
            interstitialAd.OnAdImpressionRecorded += () => Debug.Log("Interstitial impression");
            interstitialAd.OnAdClicked += () => Debug.Log("Interstitial clicked");
            Debug.Log("Interstitial loaded");
        });
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("Interstitial not loaded");
        }
    }
    #endregion

    #region Rewarded Ad
    public void LoadRewardedAd()
    {
        string adUnitId = GetCurrentRewardedAdUnitId();
        RewardedAd.Load(adUnitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"Rewarded failed to load: {error?.GetMessage()}");
                return;
            }
            rewardedAd = ad;
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Rewarded closed");
                LoadRewardedAd();
            };
            rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"Rewarded full screen failed: {error.GetMessage()}");
                LoadRewardedAd();
            };
            rewardedAd.OnAdImpressionRecorded += () => Debug.Log("Rewarded impression");
            rewardedAd.OnAdClicked += () => Debug.Log("Rewarded clicked");
            Debug.Log("Rewarded loaded");
        });
    }

    public void ShowRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log($"Rewarded ad completed: {reward.Amount} {reward.Type}");
                OnRewardedAdCompleted?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("Rewarded not loaded");
        }
    }
    #endregion

    // デバッグ用：現在の設定を表示
    [ContextMenu("Log Current Settings")]
    public void LogCurrentSettings()
    {
        Debug.Log($"=== AdMob Settings for {GetCurrentPlatform()} ===");
        Debug.Log($"App ID: {GetCurrentAppId()}");
        Debug.Log($"Banner Ad Unit ID: {GetCurrentBannerAdUnitId()}");
        Debug.Log($"Interstitial Ad Unit ID: {GetCurrentInterstitialAdUnitId()}");
        Debug.Log($"Rewarded Ad Unit ID: {GetCurrentRewardedAdUnitId()}");
        Debug.Log("==========================================");
    }

    private void OnDestroy()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
        }
    }
} 