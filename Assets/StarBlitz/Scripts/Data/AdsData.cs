using UnityEngine;
namespace StarBlitz.Data {
[CreateAssetMenu(menuName="Star Blitz/Ads")]
public sealed class AdsData : ScriptableObject {
 [Tooltip("Ads are requested only after the Google UMP consent flow allows them.")]
 public bool enabledAds = true;
 [Tooltip("Use Google test ad units in non-development builds as well.")]
 public bool testMode;
 public string androidRewarded = "ca-app-pub-4024174422625286/3988817594", iosRewarded = "";
 public string androidInterstitial = "ca-app-pub-4024174422625286/2226897950", iosInterstitial = "";
 [Min(1)] public int runsBetweenInterstitials = 3;
 [Min(0)] public float minimumInterstitialSeconds = 180;
}
}
