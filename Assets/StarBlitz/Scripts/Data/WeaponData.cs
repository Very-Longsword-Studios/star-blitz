using UnityEngine;
namespace StarBlitz.Data {
[CreateAssetMenu(menuName="Star Blitz/Weapon")]
public sealed class WeaponData : ScriptableObject {
 public Sprite projectile;
 [Min(.01f)] public float interval = .18f;
 [Min(1)] public float damage = 12, speed = 15;
 [Min(.01f)] public float radius = .10f, visualSize = .25f;
 [Range(0,45)] public float spreadAngle = 12;
 [Min(1)] public int maximumTier = 3;
 public Color tint = new Color(.3f,1,1);
}
}
