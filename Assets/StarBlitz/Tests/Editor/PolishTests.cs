using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using StarBlitz.Data;
using StarBlitz.Services;
namespace StarBlitz.Tests {
public class PolishTests {
 [Test] public void EffectsStayMutedWhenMusicIsEnabled(){
  var data=UnityEngine.ScriptableObject.CreateInstance<PresentationData>();var host=new UnityEngine.GameObject("Audio test");
  try{Assert.IsFalse(data.enableSfx);var director=host.AddComponent<StarBlitz.Audio.AudioDirector>();director.Initialize(data,true);director.SetEnabled(false);director.SetEnabled(true);var sources=host.GetComponents<UnityEngine.AudioSource>();Assert.IsFalse(sources[0].mute);Assert.IsTrue(sources[1].mute);}
  finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(data);}
 }
 [TestCase(1f,1)] [TestCase(.7f,2)] [TestCase(.4f,3)]
 public void BossThresholdsArePredictable(float hp,int expected){Assert.AreEqual(expected,StarBlitz.Gameplay.GameController.PhaseFor(hp));}
 [Test] public void SoundControlsAreIndependent(){
  var data=UnityEngine.ScriptableObject.CreateInstance<PresentationData>();data.enableSfx=true;var host=new UnityEngine.GameObject("Audio controls");
  try{var d=host.AddComponent<StarBlitz.Audio.AudioDirector>();d.Initialize(data,true);d.SetEnabled(false);var sources=host.GetComponents<UnityEngine.AudioSource>();Assert.IsTrue(sources[0].mute);Assert.IsFalse(sources[1].mute);d.SetEffectsEnabled(false);d.SetEnabled(true);Assert.IsFalse(sources[0].mute);Assert.IsTrue(sources[1].mute);}
  finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(data);}
 }
 [Test] public void BossDurabilitySurvivesSustainedFiveLaneFire(){
  var c=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/StarBlitz/Data/GameCatalog.asset");
  float dps=c.ships[0].weapon.damage*5/c.ships[0].weapon.interval;
  Assert.Greater(c.stages[0].boss.health*c.stages[0].healthScale/dps,25f);
 }
 [Test] public void MenuAndGameplayUseDistinctMusic(){
  var data=AssetDatabase.LoadAssetAtPath<PresentationData>("Assets/StarBlitz/Data/Presentation.asset");
  Assert.NotNull(data.menuMusic);Assert.Greater(data.menuMusic.length,30);Assert.NotNull(data.fire);Assert.Less(data.fire.length,.11f);
  Assert.AreNotEqual(data.menuMusic,data.music[0]);
  var host=new UnityEngine.GameObject("Music routing");
  try{var d=host.AddComponent<StarBlitz.Audio.AudioDirector>();d.Initialize(data,true);d.Menu();Assert.AreEqual(data.menuMusic,d.CurrentMusic);d.Stage(287);Assert.AreEqual(data.music[0],d.CurrentMusic);d.SetPaused(true);Assert.AreEqual(data.music[0],d.CurrentMusic);d.Menu();Assert.AreEqual(data.menuMusic,d.CurrentMusic);d.SetEnabled(false);foreach(var source in host.GetComponents<UnityEngine.AudioSource>())if(source.loop)Assert.IsTrue(source.mute);}
  finally{UnityEngine.Object.DestroyImmediate(host);}
 }
 string path;
 [SetUp] public void Setup(){path=Path.Combine(Path.GetTempPath(),"StarBlitzTest-"+Guid.NewGuid()+".json");}
 [TearDown] public void Cleanup(){foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
 [Test] public void ShipPurchaseChargesOnceAndPersistsSelection(){
  var save=new SaveService(288,100,path);save.ConfigureShips(6);
  Assert.IsFalse(save.SelectOrBuyShip(2,220));Assert.AreEqual(100,save.Value.coins);Assert.IsFalse(save.Value.ownedShips[2]);
  Assert.IsTrue(save.SelectOrBuyShip(1,90));Assert.AreEqual(10,save.Value.coins);
  Assert.IsTrue(save.SelectOrBuyShip(1,90));Assert.AreEqual(10,save.Value.coins);
  var restored=new SaveService(288,0,path);restored.ConfigureShips(6);Assert.AreEqual(1,restored.Value.selectedShip);Assert.IsTrue(restored.Value.ownedShips[1]);Assert.AreEqual(10,restored.Value.coins);
 }
 [Test] public void LegacySaveMigratesWithoutLosingCoinsOrStars(){
  File.WriteAllText(path,"{\"coins\":37,\"unlocked\":4,\"stars\":[3,2,1,0,0,0,0,0],\"upgrades\":[1,2,0,0,0]}");
  var save=new SaveService(288,0,path);save.ConfigureShips(6);Assert.AreEqual(288,save.Value.stars.Length);Assert.AreEqual(3,save.Value.stars[0]);Assert.AreEqual(37,save.Value.coins);Assert.IsTrue(save.Value.ownedShips[0]);Assert.AreEqual(4,save.Value.unlocked);
 }
 [Test] public void CampaignHasEveryBossAndNoExtraCombatCoinSources(){
  var c=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/StarBlitz/Data/GameCatalog.asset");Assert.AreEqual(288,c.stages.Length);Assert.AreEqual(6,c.ships.Length);Assert.AreEqual(0,c.ships[0].unlockCost);Assert.AreEqual(0,c.balance.dropWeights[3]);
  foreach(var s in c.stages){Assert.NotNull(s.boss);Assert.AreEqual(Flight.Boss,s.boss.flight);Assert.AreEqual(1,s.boss.coins);Assert.AreEqual(0,s.clearCoins);Assert.GreaterOrEqual(s.waves.Length,7);Assert.LessOrEqual(s.healthScale,2);foreach(var w in s.waves){Assert.NotNull(w.enemy);Assert.AreEqual(1,w.enemy.coins);}}
 }
}
}
