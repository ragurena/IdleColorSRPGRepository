using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

// 報酬型広告の同意、初期化、読み込み、表示。生産枠の増減は呼び出し元が行う。
public static class AdService
{
    const string AndroidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    const string IosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

    static bool _beginStarted;
    static bool _initialized;
    static bool _showing;
    static RewardedAd _rewarded;

    public static void Begin()
    {
        if (_beginStarted)
            return;
        _beginStarted = true;
#if UNITY_ANDROID || UNITY_EDITOR
        if (!CanLoadRewardedAds())
        {
            Debug.Log("本番の広告ユニットIDが未設定のため、広告は読み込みません。");
            return;
        }
        MobileAdsEventExecutor.Initialize();
        ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
        {
            RunOnMain(() => OnConsentUpdated(updateError));
        });
#else
        Debug.Log("広告の初期化はAndroidで行います。");
#endif
    }

    public static bool TryShowRewarded(Action onRewarded, Action onClosedWithoutReward)
    {
        if (_showing)
            return false;
#if !(UNITY_ANDROID || UNITY_EDITOR)
        return false;
#else
        if (!_initialized || !CanLoadRewardedAds())
        {
            Debug.LogWarning("広告の準備ができていません。");
            return false;
        }
        _showing = true;
        if (_rewarded != null && _rewarded.CanShowAd())
        {
            Present(_rewarded, onRewarded, onClosedWithoutReward);
            _rewarded = null;
            return true;
        }
        LoadRewarded(loaded =>
        {
            if (!_showing)
                return;
            if (loaded == null || !loaded.CanShowAd())
            {
                FinishWithoutReward(onClosedWithoutReward);
                return;
            }
            Present(loaded, onRewarded, onClosedWithoutReward);
        });
        return true;
#endif
    }

    static void OnConsentUpdated(FormError updateError)
    {
        if (updateError != null)
        {
            Debug.LogWarning("同意情報を更新できませんでした。 " + updateError.Message);
            TryInitialize();
            return;
        }
        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            RunOnMain(() =>
            {
                if (formError != null)
                    Debug.LogWarning("同意フォームを表示できませんでした。 " + formError.Message);
                TryInitialize();
            });
        });
    }

    static void TryInitialize()
    {
        if (_initialized)
            return;
        if (!ConsentInformation.CanRequestAds())
        {
            Debug.Log("同意が得られていないため、広告は初期化しません。");
            return;
        }
        MobileAds.Initialize(status =>
        {
            RunOnMain(() =>
            {
                if (status == null)
                {
                    Debug.LogWarning("広告を初期化できませんでした。");
                    return;
                }
                _initialized = true;
                Debug.Log("広告を初期化しました。");
                LoadRewarded(null);
            });
        });
    }

    static void LoadRewarded(Action<RewardedAd> onLoaded)
    {
        string adUnitId = ActiveRewardedAdUnitId();
        if (string.IsNullOrEmpty(adUnitId))
        {
            if (onLoaded != null)
                onLoaded(null);
            return;
        }
        RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            RunOnMain(() =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning("広告を読み込めませんでした。 " + (error != null ? error.GetMessage() : ""));
                    if (onLoaded != null)
                        onLoaded(null);
                    return;
                }
                if (onLoaded != null)
                    onLoaded(ad);
                else
                    ReplacePreloaded(ad);
            });
        });
    }

    static void Present(RewardedAd ad, Action onRewarded, Action onClosedWithoutReward)
    {
        bool granted = false;
        bool finished = false;
        ad.OnAdFullScreenContentFailed += error =>
        {
            RunOnMain(() =>
            {
                Debug.LogWarning("広告を表示できませんでした。 " + (error != null ? error.GetMessage() : ""));
                Finish(false);
            });
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            RunOnMain(() => Finish(false));
        };
        ad.Show(reward =>
        {
            RunOnMain(() => Finish(true));
        });

        void Finish(bool rewardEarned)
        {
            if (finished)
                return;
            if (rewardEarned)
            {
                if (granted)
                    return;
                granted = true;
                if (onRewarded != null)
                    onRewarded();
                return;
            }
            finished = true;
            _showing = false;
            ad.Destroy();
            if (!granted && onClosedWithoutReward != null)
                onClosedWithoutReward();
            LoadRewarded(null);
        }
    }

    static void FinishWithoutReward(Action onClosedWithoutReward)
    {
        _showing = false;
        if (onClosedWithoutReward != null)
            onClosedWithoutReward();
    }

    static void ReplacePreloaded(RewardedAd ad)
    {
        if (_rewarded != null)
            _rewarded.Destroy();
        _rewarded = ad;
    }

    static bool CanLoadRewardedAds()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return true;
#else
        return !string.IsNullOrEmpty(ProductionRewardedAdUnitId());
#endif
    }

    static string ActiveRewardedAdUnitId()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Application.platform == RuntimePlatform.IPhonePlayer)
            return IosTestRewardedAdUnitId;
        return AndroidTestRewardedAdUnitId;
#else
        return ProductionRewardedAdUnitId();
#endif
    }

    static string ProductionRewardedAdUnitId()
    {
#if UNITY_IOS
        return GameConfig.IosRewardedAdUnitId;
#else
        return GameConfig.AndroidRewardedAdUnitId;
#endif
    }

    static void RunOnMain(Action action)
    {
        if (action == null)
            return;
        MobileAdsEventExecutor.ExecuteInUpdate(action);
    }
}
