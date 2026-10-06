using UnityEngine;
namespace StarBlitz.Data {
[CreateAssetMenu(menuName="Star Blitz/Ship")]
public sealed class ShipData : ScriptableObject {
 public string displayName = "VANGUARD";
 public string role = "Balanced interceptor", description = "Reliable firepower and responsive handling.";
 public int unlockCost;
 public Color engineColor = new Color(.2f,.9f,1);
 public Sprite sprite;
 public WeaponData weapon;
 [Min(1)] public float hull = 100, shield = 60;
 [Min(0)] public float shieldRegen = 6, shieldRegenDelay = 3;
 [Min(.1f)] public float moveSpeed = 9, radius = .22f, visualSize = 1.1f;
 [Min(0)] public int bombs = 2;
 public float damagePerLevel = .15f, fireRatePerLevel = .10f, speedPerLevel = .08f, shieldPerLevel = .20f;
}
}
