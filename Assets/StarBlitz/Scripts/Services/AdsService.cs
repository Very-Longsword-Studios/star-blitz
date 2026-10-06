using System;
using System.Collections.Concurrent;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using StarBlitz.Data;

namespace StarBlitz.Services {
/// <summary>Google Mobile Ads adapter. Ads are requested only when UMP permits them.</summary>
public sealed class AdsService : MonoBehaviour {
 const string AndroidTestInterstitial = "ca-app-pub-3940256099942544/1033173712";
 const string AndroidTestRewarded = "ca-app-pub-3940256099942544/5224354917";
 readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
 AdsData data;
 string rewardedId, interstitialId;
 RewardedAd rewardedAd;
 InterstitialAd interstitialAd;
 Action<bool> rewardCompletion;
 Coroutine rewardedRetry, interstitialRetry;
 bool sdkInitialized, rewardLoading, interstitialLoading, busy, rewardEarned;
 int runs;
 float lastInterstitial;

 public bool RewardReady => !busy && rewardedAd != null && rewardedAd.CanShowAd();
 public bool PrivacyOptionsRequired =>
  ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

 public void Initialize(AdsData settings) {
  data = settings;
  if (!data || !data.enabledAds) return;
#if UNITY_ANDROID && !UNITY_EDITOR
  rewardedId = SelectAdUnit(data.androidRewarded, AndroidTestRewarded);
  interstitialId = SelectAdUnit(data.androidInterstitial, AndroidTestInterstitial);
  if (string.IsNullOrWhiteSpace(rewardedId) || string.IsNullOrWhiteSpace(interstitialId)) {
   Debug.LogWarning("AdMob is enabled, but one or more Android ad unit IDs are missing.");
   return;
  }
  lastInterstitial = Time.realtimeSinceStartup;
  RequestConsent();
#elif UNITY_IOS && !UNITY_EDITOR
  Debug.LogWarning("AdMob iOS is not configured. Add an iOS app ID and iOS ad unit IDs before enabling iOS ads.");
#endif
 }

 string SelectAdUnit(string productionId, string testId) {
#if DEVELOPMENT_BUILD
  return testId;
#else
  return data.testMode ? testId : productionId;
#endif
 }

 void RequestConsent() {
  ConsentInformation.Update(new ConsentRequestParameters(), error => Enqueue(() => {
   if (error != null) Debug.LogWarning("AdMob consent update: " + error.Message);
   if (error != null) {
    TryInitializeAds();
    return;
   }
   ConsentForm.LoadAndShowConsentFormIfRequired(formError => Enqueue(() => {
    if (formError != null) Debug.LogWarning("AdMob consent form: " + formError.Message);
    TryInitializeAds();
   }));
  }));
 }

 void TryInitializeAds() {
  if (sdkInitialized || !ConsentInformation.CanRequestAds()) return;
  sdkInitialized = true;
  MobileAds.Initialize(_ => Enqueue(() => {
   LoadRewarded();
   LoadInterstitial();
  }));
 }

 public void ShowPrivacyOptions() {
  if (!PrivacyOptionsRequired) return;
  ConsentForm.ShowPrivacyOptionsForm(error => Enqueue(() => {
   if (error != null) Debug.LogWarning("AdMob privacy options: " + error.Message);
   if (ConsentInformation.CanRequestAds()) {
    TryInitializeAds();
    LoadRewarded();
    LoadInterstitial();
   } else {
    ClearAds();
   }
  }));
 }

 public void ShowRewarded(Action<bool> result) {
  if (!RewardReady) {
   result?.Invoke(false);
   return;
  }
  var ad = rewardedAd;
  rewardedAd = null;
  rewardCompletion = result;
  rewardEarned = false;
  busy = true;
  ad.OnAdFullScreenContentClosed += () => Enqueue(() => FinishReward(ad));
  ad.OnAdFullScreenContentFailed += error => Enqueue(() => {
   Debug.LogWarning("Rewarded ad failed to open: " + error.GetMessage());
   FinishReward(ad);
  });
  ad.Show(_ => Enqueue(() => rewardEarned = true));
 }

 public void RunFinished() {
  runs++;
  if (!data || !data.enabledAds || busy || interstitialAd == null || !interstitialAd.CanShowAd() ||
      runs < data.runsBetweenInterstitials ||
      Time.realtimeSinceStartup - lastInterstitial < data.minimumInterstitialSeconds) return;

  runs = 0;
  lastInterstitial = Time.realtimeSinceStartup;
  busy = true;
  var ad = interstitialAd;
  interstitialAd = null;
  ad.OnAdFullScreenContentClosed += () => Enqueue(() => FinishInterstitial(ad));
  ad.OnAdFullScreenContentFailed += error => Enqueue(() => {
   Debug.LogWarning("Interstitial ad failed to open: " + error.GetMessage());
   FinishInterstitial(ad);
  });
  ad.Show();
 }

 void LoadRewarded() {
  if (!sdkInitialized || rewardLoading || rewardedAd != null || !ConsentInformation.CanRequestAds()) return;
  rewardLoading = true;
  RewardedAd.Load(rewardedId, new AdRequest(), (ad, error) => Enqueue(() => {
   rewardLoading = false;
   if (error != null || ad == null) {
    Debug.LogWarning("Rewarded ad failed to load: " + (error == null ? "no ad returned" : error.GetMessage()));
    ScheduleRewardedRetry();
    return;
   }
   rewardedAd = ad;
  }));
 }

 void LoadInterstitial() {
  if (!sdkInitialized || interstitialLoading || interstitialAd != null || !ConsentInformation.CanRequestAds()) return;
  interstitialLoading = true;
  InterstitialAd.Load(interstitialId, new AdRequest(), (ad, error) => Enqueue(() => {
   interstitialLoading = false;
   if (error != null || ad == null) {
    Debug.LogWarning("Interstitial ad failed to load: " + (error == null ? "no ad returned" : error.GetMessage()));
    ScheduleInterstitialRetry();
    return;
   }
   interstitialAd = ad;
  }));
 }

 void ScheduleRewardedRetry() {
  if (rewardedRetry == null && isActiveAndEnabled && ConsentInformation.CanRequestAds())
   rewardedRetry = StartCoroutine(RetryRewarded());
 }

 IEnumerator RetryRewarded() {
  yield return new WaitForSecondsRealtime(30);
  rewardedRetry = null;
  LoadRewarded();
 }

 void ScheduleInterstitialRetry() {
  if (interstitialRetry == null && isActiveAndEnabled && ConsentInformation.CanRequestAds())
   interstitialRetry = StartCoroutine(RetryInterstitial());
 }

 IEnumerator RetryInterstitial() {
  yield return new WaitForSecondsRealtime(30);
  interstitialRetry = null;
  LoadInterstitial();
 }

 void FinishReward(RewardedAd ad) {
  ad.Destroy();
  busy = false;
  var callback = rewardCompletion;
  rewardCompletion = null;
  callback?.Invoke(rewardEarned);
  rewardEarned = false;
  LoadRewarded();
 }

 void FinishInterstitial(InterstitialAd ad) {
  ad.Destroy();
  busy = false;
  LoadInterstitial();
 }

 void ClearAds() {
  rewardedAd?.Destroy();
  interstitialAd?.Destroy();
  rewardedAd = null;
  interstitialAd = null;
 }

 void Enqueue(Action action) {
  if (action != null) mainThreadActions.Enqueue(action);
 }

 void Update() {
  while (mainThreadActions.TryDequeue(out var action)) action();
 }

 void OnDestroy() {
  rewardedRetry = null;
  interstitialRetry = null;
  ClearAds();
  var callback = rewardCompletion;
  rewardCompletion = null;
  callback?.Invoke(false);
 }
}
}
