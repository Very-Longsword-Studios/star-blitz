using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using StarBlitz.Data;
using StarBlitz.Gameplay;
namespace StarBlitz.Editor {
public static class ProjectTools {
 const string Root="Assets/StarBlitz/";
 [MenuItem("Star Blitz/Open Game Scene")]
 public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Root+"Scenes/StarBlitz.unity");}
 [MenuItem("Star Blitz/Edit Game Catalog")]
 public static void Catalog(){Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameCatalog>(Root+"Data/GameCatalog.asset");}
 [MenuItem("Star Blitz/Validate Project")]
 public static void Validate(){
  var errors=ValidationErrors();if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
  Debug.Log("Star Blitz content validation passed: stages, waves, sprites, sound, prefabs, and balance references.");
 }
 public static List<string> ValidationErrors(){
  var result=new List<string>();var c=AssetDatabase.LoadAssetAtPath<GameCatalog>(Root+"Data/GameCatalog.asset");
  if(!c){result.Add("Missing GameCatalog");return result;}
  if(!c.ship||!c.ship.sprite||!c.ship.weapon||!c.ship.weapon.projectile)result.Add("Ship/weapon art reference missing");
  if(!c.entityPrefab||!c.entityPrefab.GetComponent<Entity>()||!c.entityPrefab.GetComponent<SpriteRenderer>())result.Add("Pooled entity prefab incomplete");
  if(!c.balance)result.Add("Balance missing");else{
   if(c.balance.dropWeights.Length!=7||c.balance.pickupSprites.Length!=7)result.Add("Expected 7 pickup weights/sprites");
   foreach(var sprite in c.balance.pickupSprites)if(!sprite)result.Add("Pickup sprite missing");
   if(c.balance.bulletPool<1||c.balance.enemyPool<1||c.balance.particlePool<1||c.balance.pickupPool<1)result.Add("Pool capacity must be positive");
   if(c.balance.scorePerCoin<1||c.balance.bombFlashDuration<=0)result.Add("Invalid balance divisor");
  }
  if(!c.presentation||!c.presentation.font||!c.presentation.star||!c.presentation.particle||!c.presentation.shieldRing)result.Add("Presentation reference missing");
  if(c.presentation){if(!c.presentation.fire||!c.presentation.pickup||!c.presentation.bomb||!c.presentation.explosion||!c.presentation.victory)result.Add("SFX reference missing");if(c.presentation.music.Length==0)result.Add("No music");foreach(var music in c.presentation.music)if(!music)result.Add("Missing music clip");}
  if(!c.ads)result.Add("Ads configuration missing");
  if(c.stages==null||c.stages.Length==0)result.Add("No stages");else foreach(var stage in c.stages){
   if(!stage){result.Add("Missing stage reference");continue;}
   if(!stage.boss||stage.boss.flight!=Flight.Boss)result.Add(stage.name+": missing boss");else CheckEnemy(stage.boss,result);
   if(stage.healthScale<=0||stage.fireRateScale<=0||stage.speedScale<=0)result.Add(stage.name+": invalid difficulty scaling");
   if(stage.waves==null||stage.waves.Length==0)result.Add(stage.name+": no waves");else foreach(var w in stage.waves){if(w.enemy)CheckEnemy(w.enemy,result);else result.Add(stage.name+": null wave enemy");if(w.count<1||w.spawnInterval<=0)result.Add(stage.name+": invalid wave");}
  }
  return result;
 }
 static void CheckEnemy(EnemyData e,List<string> errors){if(!e.sprite||!e.weapon||!e.weapon.projectile)errors.Add(e.name+": missing art/weapon");if(e.health<=0||e.fireInterval<=0||e.radius<=0)errors.Add(e.name+": invalid stats");}
 [MenuItem("Star Blitz/Repair Scene and Prefab")]
 public static void RepairScene(){
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  var c=AssetDatabase.LoadAssetAtPath<GameCatalog>(Root+"Data/GameCatalog.asset");if(!c)throw new InvalidOperationException("Restore Data/GameCatalog.asset from source control first.");
  if(!c.entityPrefab){var e=new GameObject("PooledEntity",typeof(SpriteRenderer),typeof(Entity));c.entityPrefab=PrefabUtility.SaveAsPrefabAsset(e,Root+"Prefabs/PooledEntity.prefab");UnityEngine.Object.DestroyImmediate(e);EditorUtility.SetDirty(c);}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);var cam=camera.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=7.6f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=c.stages[0].background;
  new GameObject("Star Blitz").AddComponent<GameController>().catalog=c;
  EditorSceneManager.SaveScene(scene,Root+"Scenes/StarBlitz.unity");AssetDatabase.SaveAssets();
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/StarBlitz.unity",true)};
 }
 [MenuItem("Star Blitz/Build Android Development APK")]
 public static void BuildAndroid(){
  PolishContent.ApplyAtlases();Validate();PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.starblitz.playtest");PlayerSettings.bundleVersion="0.5.0";PlayerSettings.Android.bundleVersionCode=6;PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
  PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
  PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
  Directory.CreateDirectory("Builds");EditorUserBuildSettings.buildAppBundle=false;
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"Scenes/StarBlitz.unity"},locationPathName="Builds/StarBlitz-development.apk",target=BuildTarget.Android,options=BuildOptions.Development});
  if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Android build failed; inspect Console.");
 }
}
}



