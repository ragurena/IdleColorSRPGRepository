using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

// 報酬型広告の同意、初期化、読み込み、表示。生産枠の増減は呼び出し元が行う。
public static class AdService
{
    public enum AdFailReason
    {
        Preparing,
        Consent,
        Load,
        Show,
        Unsupported
    }

    const string AndroidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    const string IosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    const int ConsentRetryMax = 3;
    const int InitRetryMax = 3;
    const int BackgroundLoadMax = 4;
    const float ConsentRetryDelay = 2f;
    const float InitTimeoutSeconds = 12f;

    static bool _beginStarted;
    static bool _initialized;
    static bool _startupBusy;
    static bool _showing;
    static bool _showWaiting;
    static bool _loadInFlight;
    static int _consentAttempts;
    static int _initAttempts;
    static int _initEpoch;
    static int _backgroundTries;
    static float _retryLeft;
    static Action _retryAction;
    static Action _pendingRewarded;
    static Action _pendingClosed;
    static Action<RewardedAd> _loadWaiter;
    static RewardedAd _rewarded;

    // 再生を始められない、または表示に失敗したとき。自分で閉じたときは呼ばない。
    public static event Action<AdFailReason> PlaybackFailed;

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
        RequestConsent();
#else
        Debug.Log("広告の初期化はAndroidで行います。");
#endif
    }

    public static void Tick(float unscaledDelta)
    {
        if (_retryAction == null)
            return;
        _retryLeft -= unscaledDelta;
        if (_retryLeft > 0f)
            return;
        Action action = _retryAction;
        _retryAction = null;
        action();
    }

    public static bool TryShowRewarded(Action onRewarded, Action onClosedWithoutReward)
    {
        if (_showing || _showWaiting)
            return false;
#if !(UNITY_ANDROID || UNITY_EDITOR)
        Notify(AdFailReason.Unsupported);
        return false;
#else
        if (!CanLoadRewardedAds())
        {
            Notify(AdFailReason.Unsupported);
            return false;
        }
        if (!_initialized)
        {
            _pendingRewarded = onRewarded;
            _pendingClosed = onClosedWithoutReward;
            _showWaiting = true;
            if (!_startupBusy)
            {
                _consentAttempts = 0;
                _initAttempts = 0;
                RequestConsent();
            }
            return true;
        }
        return ShowNow(onRewarded, onClosedWithoutReward);
#endif
    }

    static void RequestConsent()
    {
        _startupBusy = true;
        _consentAttempts++;
        ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
        {
            RunOnMain(() => OnConsentUpdated(updateError));
        });
    }

    static void OnConsentUpdated(FormError updateError)
    {
        if (updateError != null)
            Debug.LogWarning("同意情報を更新できませんでした。 " + updateError.Message);
        if (updateError != null && !ConsentInformation.CanRequestAds())
        {
            RetryConsentOrGiveUp();
            return;
        }
        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            RunOnMain(() =>
            {
                if (formError != null)
                    Debug.LogWarning("同意フォームを表示できませんでした。 " + formError.Message);
                if (!ConsentInformation.CanRequestAds())
                    RetryConsentOrGiveUp();
                else
                    TryInitialize();
            });
        });
    }

    static void RetryConsentOrGiveUp()
    {
        if (_consentAttempts < ConsentRetryMax)
            RetryAfter(ConsentRetryDelay, RequestConsent);
        else
            GiveUp(AdFailReason.Consent);
    }

    static void TryInitialize()
    {
        if (_initialized)
        {
            _startupBusy = false;
            StartPendingShow();
            return;
        }
        if (!ConsentInformation.CanRequestAds())
        {
            Debug.Log("同意が得られていないため、広告は初期化しません。");
            RetryConsentOrGiveUp();
            return;
        }
        if (_initAttempts >= InitRetryMax)
        {
            GiveUp(AdFailReason.Preparing);
            return;
        }
        _initAttempts++;
        int epoch = ++_initEpoch;
        MobileAds.Initialize(status =>
        {
            RunOnMain(() => OnInitialized(epoch, status));
        });
        RetryAfter(InitTimeoutSeconds, () => OnInitializeTimedOut(epoch));
    }

    static void OnInitialized(int epoch, InitializationStatus status)
    {
        if (epoch != _initEpoch)
            return;
        CancelRetry();
        if (status == null)
        {
            Debug.LogWarning("広告を初期化できませんでした。");
            _initEpoch++;
            if (_initAttempts < InitRetryMax)
                RetryAfter(ConsentRetryDelay, TryInitialize);
            else
                GiveUp(AdFailReason.Preparing);
            return;
        }
        if (_initialized)
            return;
        _initialized = true;
        _startupBusy = false;
        Debug.Log("広告を初期化しました。");
        _backgroundTries = 0;
        LoadRewarded(null);
        StartPendingShow();
    }

    static void OnInitializeTimedOut(int epoch)
    {
        if (_initialized || epoch != _initEpoch)
            return;
        Debug.LogWarning("広告の初期化が完了しませんでした。");
        _initEpoch++;
        if (_initAttempts < InitRetryMax)
            TryInitialize();
        else
            GiveUp(AdFailReason.Preparing);
    }

    static void StartPendingShow()
    {
        if (!_showWaiting || !_initialized)
            return;
        Action rewarded = _pendingRewarded;
        Action closed = _pendingClosed;
        _pendingRewarded = null;
        _pendingClosed = null;
        _showWaiting = false;
        ShowNow(rewarded, closed);
    }

    static void GiveUp(AdFailReason reason)
    {
        _startupBusy = false;
        CancelRetry();
        if (!_showWaiting)
            return;
        _showWaiting = false;
        Action closed = _pendingClosed;
        _pendingRewarded = null;
        _pendingClosed = null;
        Notify(reason);
        if (closed != null)
            closed();
    }

    static bool ShowNow(Action onRewarded, Action onClosedWithoutReward)
    {
        _showing = true;
        if (_rewarded != null && _rewarded.CanShowAd())
        {
            RewardedAd ready = _rewarded;
            _rewarded = null;
            Present(ready, onRewarded, onClosedWithoutReward);
            return true;
        }
        if (_rewarded != null)
        {
            _rewarded.Destroy();
            _rewarded = null;
        }
        LoadRewarded(loaded => OnShowLoaded(loaded, onRewarded, onClosedWithoutReward));
        return true;
    }

    static void OnShowLoaded(RewardedAd loaded, Action onRewarded, Action onClosedWithoutReward)
    {
        if (!_showing)
        {
            if (loaded != null)
                ReplacePreloaded(loaded);
            return;
        }
        if (loaded != null && loaded.CanShowAd())
        {
            Present(loaded, onRewarded, onClosedWithoutReward);
            return;
        }
        if (loaded != null)
            loaded.Destroy();
        _showing = false;
        Notify(AdFailReason.Load);
        if (onClosedWithoutReward != null)
            onClosedWithoutReward();
        _backgroundTries = 0;
        ScheduleBackgroundLoad();
    }

    static void LoadRewarded(Action<RewardedAd> onLoaded)
    {
        if (_loadInFlight)
        {
            if (onLoaded != null)
                _loadWaiter = onLoaded;
            return;
        }
        string adUnitId = ActiveRewardedAdUnitId();
        if (string.IsNullOrEmpty(adUnitId))
        {
            if (onLoaded != null)
                onLoaded(null);
            return;
        }
        _loadInFlight = true;
        _loadWaiter = onLoaded;
        RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            RunOnMain(() =>
            {
                _loadInFlight = false;
                Action<RewardedAd> waiter = _loadWaiter;
                _loadWaiter = null;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("広告を読み込めませんでした。 " + (error != null ? error.GetMessage() : ""));
                    if (waiter != null)
                        waiter(null);
                    else
                        ScheduleBackgroundLoad();
                    return;
                }
                if (waiter != null)
                    waiter(ad);
                else
                {
                    ReplacePreloaded(ad);
                    _backgroundTries = 0;
                }
            });
        });
    }

    static void ScheduleBackgroundLoad()
    {
        if (_backgroundTries >= BackgroundLoadMax)
            return;
        if (string.IsNullOrEmpty(ActiveRewardedAdUnitId()))
            return;
        _backgroundTries++;
        float delay = 3f;
        for (int i = 1; i < _backgroundTries; i++)
            delay *= 2f;
        if (delay > 24f)
            delay = 24f;
        RetryAfter(delay, BackgroundLoad);
    }

    static void BackgroundLoad()
    {
        if (!_initialized || _showing || _showWaiting || _loadInFlight)
            return;
        if (_rewarded != null && _rewarded.CanShowAd())
        {
            _backgroundTries = 0;
            return;
        }
        LoadRewarded(null);
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
                Finish(false, true);
            });
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            RunOnMain(() => Finish(false, false));
        };
        ad.Show(reward =>
        {
            RunOnMain(() => Finish(true, false));
        });

        void Finish(bool rewardEarned, bool failed)
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
            if (failed && !granted)
            {
                Notify(AdFailReason.Show);
                _backgroundTries = 0;
            }
            if (!granted && onClosedWithoutReward != null)
                onClosedWithoutReward();
            LoadRewarded(null);
        }
    }

    static void Notify(AdFailReason reason)
    {
        Action<AdFailReason> handler = PlaybackFailed;
        if (handler != null)
            handler(reason);
    }

    static void RetryAfter(float seconds, Action action)
    {
        _retryLeft = seconds;
        _retryAction = action;
    }

    static void CancelRetry()
    {
        _retryAction = null;
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
