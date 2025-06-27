using UnityEngine;

[CreateAssetMenu(fileName = "AdMobConfig", menuName = "AdMob/AdMob Configuration")]
public class AdMobConfig : ScriptableObject
{
    [Header("App IDs")]
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

    // プロパティ
    public string AndroidAppId => androidAppId;
    public string IosAppId => iosAppId;
    public string AndroidBannerAdUnitId => androidBannerAdUnitId;
    public string IosBannerAdUnitId => iosBannerAdUnitId;
    public string AndroidInterstitialAdUnitId => androidInterstitialAdUnitId;
    public string IosInterstitialAdUnitId => iosInterstitialAdUnitId;
    public string AndroidRewardedAdUnitId => androidRewardedAdUnitId;
    public string IosRewardedAdUnitId => iosRewardedAdUnitId;

    // 現在のプラットフォームに応じたIDを取得
    public string GetCurrentAppId()
    {
        #if UNITY_ANDROID
            return androidAppId;
        #elif UNITY_IOS
            return iosAppId;
        #else
            return "unused";
        #endif
    }

    public string GetCurrentBannerAdUnitId()
    {
        #if UNITY_ANDROID
            return androidBannerAdUnitId;
        #elif UNITY_IOS
            return iosBannerAdUnitId;
        #else
            return "unused";
        #endif
    }

    public string GetCurrentInterstitialAdUnitId()
    {
        #if UNITY_ANDROID
            return androidInterstitialAdUnitId;
        #elif UNITY_IOS
            return iosInterstitialAdUnitId;
        #else
            return "unused";
        #endif
    }

    public string GetCurrentRewardedAdUnitId()
    {
        #if UNITY_ANDROID
            return androidRewardedAdUnitId;
        #elif UNITY_IOS
            return iosRewardedAdUnitId;
        #else
            return "unused";
        #endif
    }

    // 設定の検証
    public bool IsValid()
    {
        bool isValid = true;
        
        if (string.IsNullOrEmpty(androidAppId) || androidAppId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("Android App ID is not set properly");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(iosAppId) || iosAppId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("iOS App ID is not set properly");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(androidBannerAdUnitId) || androidBannerAdUnitId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("Android Banner Ad Unit ID is not set properly");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(iosBannerAdUnitId) || iosBannerAdUnitId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("iOS Banner Ad Unit ID is not set properly");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(androidInterstitialAdUnitId) || androidInterstitialAdUnitId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("Android Interstitial Ad Unit ID is not set properly");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(iosInterstitialAdUnitId) || iosInterstitialAdUnitId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("iOS Interstitial Ad Unit ID is not set properly");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(androidRewardedAdUnitId) || androidRewardedAdUnitId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("Android Rewarded Ad Unit ID is not set properly");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(iosRewardedAdUnitId) || iosRewardedAdUnitId.Contains("xxxxxxxx"))
        {
            Debug.LogWarning("iOS Rewarded Ad Unit ID is not set properly");
            isValid = false;
        }
        
        return isValid;
    }

    // 設定情報をログ出力
    [ContextMenu("Log Configuration")]
    public void LogConfiguration()
    {
        Debug.Log("=== AdMob Configuration ===");
        Debug.Log($"Android App ID: {androidAppId}");
        Debug.Log($"iOS App ID: {iosAppId}");
        Debug.Log($"Android Banner Ad Unit ID: {androidBannerAdUnitId}");
        Debug.Log($"iOS Banner Ad Unit ID: {iosBannerAdUnitId}");
        Debug.Log($"Android Interstitial Ad Unit ID: {androidInterstitialAdUnitId}");
        Debug.Log($"iOS Interstitial Ad Unit ID: {iosInterstitialAdUnitId}");
        Debug.Log($"Android Rewarded Ad Unit ID: {androidRewardedAdUnitId}");
        Debug.Log($"iOS Rewarded Ad Unit ID: {iosRewardedAdUnitId}");
        Debug.Log($"Configuration Valid: {IsValid()}");
        Debug.Log("===========================");
    }
} 