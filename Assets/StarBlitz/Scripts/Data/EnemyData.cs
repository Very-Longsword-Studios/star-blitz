using UnityEngine;
namespace StarBlitz.Data {
public enum Flight { Grunt, Weaver, Turret, Kamikaze, Boss }
public enum BossPattern { Fan, Spiral, TwinSweep, Ring, Crossfire, Alternating, Rain, Vortex }
[CreateAssetMenu(menuName="Star Blitz/Enemy")]
public sealed class EnemyData : ScriptableObject {
 public string displayName;
 public BossPattern pattern;
 public Flight flight;
 public Sprite sprite;
 public Color tint = Color.white;
 [Min(.1f)] public float health = 35, speed = 1.3f, radius = .35f, visualSize = 1.1f;
 [Min(0)] public float contactDamage = 25;
 public WeaponData weapon;
 [Min(.1f)] public float fireInterval = 2;
 [Min(1)] public int shotCount = 1;
 [Min(0)] public float spreadAngle = 18, sineAmplitude = .7f, sineFrequency = 2;
 [Min(0)] public int score = 100, coins = 3;
 [Range(0,1)] public float dropChance = .2f;
 [Range(.1f,.9f)] public float bossPhaseThreshold = .5f;
 [Min(.1f)] public float bossPhaseFireMultiplier = .60f;
 public int bossPhaseExtraShots = 4;
 public float holdY = 4.7f;
}
}
