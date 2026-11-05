using UnityEngine;
namespace StarBlitz.Data {
/// <summary>
/// Deterministic boss maths shared by the runtime, the content generators and the editor tests.
/// Reference gear is a tier-3 weapon plus the permanent upgrades a player is expected to own
/// when they reach a world. The result is a fight that stays near 38 seconds without making
/// under-geared players wait forever (the runtime caps the fight at <see cref="MaxFightSeconds"/>).
/// </summary>
public static class BossTuning {
 public const int ReferenceWeaponTier = 3;
 public const float FightSeconds = 38f;
 public const float MaxFightSeconds = 55f;

 public static int WorldOf(int missionIndex) => Mathf.Clamp(missionIndex/36,0,7);
 public static int LocalOf(int missionIndex) => Mathf.Clamp(missionIndex%36,0,35);

 /// <summary>Permanent upgrade level expected in each world (0 in world 1, 7 in world 8).</summary>
 public static int ReferenceUpgradeLevel(int world) => Mathf.Clamp(world,0,8);

 /// <summary>Damage x fire-rate multiplier for the reference gear in a world.</summary>
 public static float GearMultiplier(int world,float damagePerLevel=.15f,float fireRatePerLevel=.10f) {
  int level=ReferenceUpgradeLevel(world);
  return (1f+damagePerLevel*level)*(1f+fireRatePerLevel*level);
 }

 /// <summary>DPS a tier-3 reference fighter brings to a world, including expected upgrades.</summary>
 public static float ReferenceDps(ShipData ship,int world) {
  if(!ship||!ship.weapon)return 1f;
  var weapon=ship.weapon;
  int tier=Mathf.Clamp(ReferenceWeaponTier,1,weapon.maximumTier);
  return tier*weapon.damage*GearMultiplier(world,ship.damagePerLevel,ship.fireRatePerLevel)/Mathf.Max(.01f,weapon.interval);
 }

 /// <summary>Authored HP for one mission at the reference ship's DPS. The per-world curve is already included.</summary>
 public static float Health(int missionIndex,float referenceDps,float fightSeconds) {
  int local=LocalOf(missionIndex);
  return Mathf.Round(referenceDps*fightSeconds*(1f+local*.004f));
 }

 /// <summary>
 /// Scales authored HP to the active ship's reference DPS so every hull gets the same fight
 /// length. The clamp keeps a bad asset or an unusual ship from producing an absurd boss.
 /// </summary>
 public static float HealthForShip(float authoredHealth,float authoredDps,ShipData ship,int world) {
  float activeDps=ReferenceDps(ship,world);
  if(authoredDps>0f&&activeDps>0f)authoredHealth*=Mathf.Clamp(activeDps/authoredDps,.25f,4f);
  return authoredHealth;
 }

 /// <summary>Per-pattern movement speed multiplier. Each boss owns a distinct patrol shape.</summary>
 public static float MotionSpeed(BossPattern pattern) {
  switch(pattern){
   case BossPattern.Fan:return .7f;
   case BossPattern.Spiral:return 1f;
   case BossPattern.TwinSweep:return 1.25f;
   case BossPattern.Ring:return .55f;
   case BossPattern.Crossfire:return 1.4f;
   case BossPattern.Alternating:return .95f;
   case BossPattern.Rain:return .6f;
   case BossPattern.Vortex:return .9f;
   default:return 1f;
  }
 }

 /// <summary>Beam width multiplier. Twin Seraph fires the widest lance; Helix the narrowest.</summary>
 public static float BeamWidthScale(BossPattern pattern) {
  switch(pattern){
   case BossPattern.Fan:return 1f;
   case BossPattern.Spiral:return .85f;
   case BossPattern.TwinSweep:return 1.45f;
   case BossPattern.Ring:return 1.2f;
   case BossPattern.Crossfire:return .9f;
   case BossPattern.Alternating:return 1.1f;
   case BossPattern.Rain:return 1.3f;
   case BossPattern.Vortex:return .95f;
   default:return 1f;
  }
 }

 /// <summary>Beam warning multiplier. Faster bosses warn for less time but fire narrower lances.</summary>
 public static float BeamTelegraphScale(BossPattern pattern) {
  switch(pattern){
   case BossPattern.Fan:return 1f;
   case BossPattern.Spiral:return .9f;
   case BossPattern.TwinSweep:return 1.15f;
   case BossPattern.Ring:return 1.05f;
   case BossPattern.Crossfire:return .95f;
   case BossPattern.Alternating:return 1f;
   case BossPattern.Rain:return 1.1f;
   case BossPattern.Vortex:return .95f;
   default:return 1f;
  }
 }

 /// <summary>Beam cadence multiplier, applied on top of the global boss beam interval.</summary>
 public static float BeamIntervalScale(BossPattern pattern) {
  switch(pattern){
   case BossPattern.Fan:return 1f;
   case BossPattern.Spiral:return .85f;
   case BossPattern.TwinSweep:return 1.1f;
   case BossPattern.Ring:return 1.15f;
   case BossPattern.Crossfire:return .9f;
   case BossPattern.Alternating:return 1f;
   case BossPattern.Rain:return 1.1f;
   case BossPattern.Vortex:return .85f;
   default:return 1f;
  }
 }

 /// <summary>HUD label for the phase-two signature burst.</summary>
 public static string SpecialName(BossPattern pattern) {
  switch(pattern){
   case BossPattern.Fan:return "BROADSIDE";
   case BossPattern.Spiral:return "HELIX STORM";
   case BossPattern.TwinSweep:return "TWIN LANCE";
   case BossPattern.Ring:return "ORBITAL BURST";
   case BossPattern.Crossfire:return "CROSS GRID";
   case BossPattern.Alternating:return "PENDULUM";
   case BossPattern.Rain:return "METEOR RAIN";
   case BossPattern.Vortex:return "SINGULARITY";
   default:return "SPECIAL";
  }
 }

 public static float FightLength(float health,float dps) => health/Mathf.Max(1f,dps);
}
}
