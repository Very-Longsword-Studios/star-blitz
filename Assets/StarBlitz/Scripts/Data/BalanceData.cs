using UnityEngine;
namespace StarBlitz.Data {
[CreateAssetMenu(menuName="Star Blitz/Balance")]
public sealed class BalanceData : ScriptableObject {
 [Header("Playfield")]
 public float halfWidth = 3.8f, halfHeight = 7.6f, playerBottom = -5.8f, playerTop = 3.3f, spawnY = 8.1f, despawnY = -8.5f;
 public float playerStartY = -4.8f, hitInvulnerability = .65f;
 [Header("Power-ups: weights map Weapon / Shield / Ray Gun / Coin / Gem / Magnet / Multiplier")]
 public int[] dropWeights = {25,18,10,25,3,10,9};
 public Sprite[] pickupSprites;
 public float pickupFallSpeed = 1.2f, pickupRadius = .25f, pickupSize = .48f;
 public float weaponDuration = 12, shieldDuration = 5, magnetDuration = 9, multiplierDuration = 10;
 public float magnetRadius = 5, magnetSpeed = 8, collectRadius = .5f;
 public int gemPickup = 1, scoreMultiplier = 2, bombCap = 9;
 [Header("Combo")]
 public float comboWindow = 2.5f, comboScoreBonus = .1f;
 public int comboMax = 25;
 [Header("Particle Ray Gun")]
 public float rayDuration = 3.5f, rayWidth = 1.25f, rayDamagePerSecond = 240f, rayParticleRate = 90f;
 public float bombFlashDuration = .3f;
 [Header("Boss combat")]
 public float bossFightSeconds = 38, bossMaxFightSeconds = 55, bossEnrageSeconds = 42;
 public float bossBulletDamage = 18, bossContactDamage = 35, bossBeamDamage = 35;
 public int bossWeaponTier = 3;
 public float bossBeamTelegraph = 1.1f, bossBeamDuration = .75f, bossBeamWidth = 1.15f, bossBeamInterval = 9, bossBeamDelay = 6, bossBeamTrackSpeed = 1.7f;
 public float bossPhaseDescent = .35f, bossMoveSpeed = 2.2f, bossPlayerClearance = 1.3f;
 public float bossEnrageFireScale = .72f, bossEnrageMoveScale = 1.25f;
 [Header("Economy")]
 public int startingCoins = 0, upgradeBaseCost = 100, upgradeMaxLevel = 8;
 public float upgradeCostGrowth = 1.65f;
 public int dailyCoins = 150, dailyGems = 1;
 public int gemExchangeCost = 3, gemExchangeCoins = 250, ngPlusRewardGems = 2;
 [Range(0,1)] public float threeStarHull = .75f, twoStarHull = .35f;
 [Header("Pools and VFX")]
 public int bulletPool = 320, enemyPool = 60, particlePool = 240, pickupPool = 50;
 public int explosionParticles = 18, hitParticles = 5;
 public float particleLife = .5f, particleSpeed = 4, particleSize = .12f, shakeDuration = .18f, shakeStrength = .12f;
 public float starSpeed = .55f;
 public int starCount = 100;
 public int UpgradeCost(int level) => Mathf.RoundToInt(upgradeBaseCost * Mathf.Pow(upgradeCostGrowth, level));
}
}
