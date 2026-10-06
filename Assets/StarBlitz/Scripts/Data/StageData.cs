using System;
using UnityEngine;
namespace StarBlitz.Data {
[Serializable] public sealed class Wave {
 public EnemyData enemy;
 [Min(1)] public int count = 6;
 [Min(.05f)] public float spawnInterval = .5f;
 [Min(0)] public float delay = 2;
 [Range(.2f,3.5f)] public float width = 2.6f;
 public bool alternateSides;
 public int formation;
}
[CreateAssetMenu(menuName="Star Blitz/Stage")]
public sealed class StageData : ScriptableObject {
 public string title = "FIRST CONTACT", sector = "THE OUTER RIM";
 public Color accent = new Color(.2f,.85f,1), background = new Color(.018f,.025f,.075f);
 [Min(.1f)] public float healthScale = 1, speedScale = 1, fireRateScale = 1;
 [Min(0)] public int clearCoins = 100;
 public Wave[] waves;
 public EnemyData boss;
 public float targetSeconds = 150;
 [Min(0)] public float bossDelay = 3;
}
}
