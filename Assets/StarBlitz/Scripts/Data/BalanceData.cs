using UnityEngine;
namespace StarBlitz.Data {
[CreateAssetMenu(menuName="Star Blitz/Balance")]
public sealed class BalanceData : ScriptableObject {
 [Header("Playfield")]
 public float halfWidth = 3.8f, halfHeight = 7.6f, playerBottom = -5.8f, playerTop = 3.3f, spawnY = 8.1f, despawnY = -8.5f;
 public float playerStartY = -4.8f, hitInvulnerability = .65f, touchOffset = .7f;
 [Header("Power-ups: weights map Weapon / Shield / Bomb / Coin / Gem / Magnet / Multiplier")]
 public int[] dropWeights = {25,18,10,25,3,10,9};
 public Sprite[] pickupSprites;
 public float pickupFallSpeed = 1.2f, pickupRadius = .25f, pickupSize = .48f;
 public float weaponDuration = 12, shieldDuration = 5, magnetDuration = 9, multiplierDuration = 10;
 public float magnetRadius = 5, magnetSpeed = 8, collectRadius = .5f;
 public int coinPickup = 15, gemPickup = 1, scoreMultiplier = 2, bombCap = 9;
 [Header("Bomb")]
 public float bombDamage = 140, bombFlashDuration = .3f;
 [Header("Economy")]
 public int startingCoins = 0, upgradeBaseCost = 100, upgradeMaxLevel = 8;
 public float upgradeCostGrowth = 1.65f;
 public int dailyCoins = 150, dailyGems = 1, rewardedCoins = 100;
 public int scorePerCoin = 250;
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
