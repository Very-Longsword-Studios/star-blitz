#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace StarBlitz.Gameplay {
/// <summary>Opt-in development smoke test. Uses an isolated save; never runs in normal play.</summary>
public sealed class DevelopmentSmoke : MonoBehaviour {
 GameController game;string output;int checks;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-starblitz-qa")>=0)new GameObject("Development smoke runner").AddComponent<DevelopmentSmoke>();}
 IEnumerator Start(){Application.runInBackground=true;output=Path.Combine(Application.dataPath,"../QA");Directory.CreateDirectory(output);var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"smoke-failed.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);yield break;}if(!more)break;yield return run.Current;}File.WriteAllText(Path.Combine(output,"smoke-passed.txt"),checks+" runtime checks passed");Application.Quit(0);}
 void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Debug.Log("QA PASS: "+label);}
 object Call(string name,params object[] args){return typeof(GameController).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);}
 T Field<T>(string name){return (T)typeof(GameController).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);}
 IEnumerator Shot(string name,float delay=.4f){
  yield return new WaitForSecondsRealtime(delay);yield return new WaitForEndOfFrame();
  // Render explicitly: Windows suppresses the backbuffer of hidden test windows.
  var camera=Camera.main;var canvas=FindObjectOfType<Canvas>();var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;int previousOrder=canvas.sortingOrder;
  var target=RenderTexture.GetTemporary(540,960,24,RenderTextureFormat.ARGB32);var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
  canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.sortingOrder=100;canvas.planeDistance=1;camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
  var image=new Texture2D(540,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);
  camera.targetTexture=previousTarget;canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;canvas.sortingOrder=previousOrder;RenderTexture.active=previousActive;RenderTexture.ReleaseTemporary(target);yield return null;
 }
 IEnumerator Run(){
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-starblitz-backgrounds")>=0){yield return Backgrounds();yield break;}
  yield return new WaitForSecondsRealtime(1);game=FindObjectOfType<GameController>();Check(game!=null&&game.View!=null,"game initializes");Check(game.catalog.stages.Length==288,"288 missions loaded");yield return Shot("01-menu");
  Check(game.Audio.CurrentMusic==game.catalog.presentation.menuMusic,"command deck uses menu soundtrack");
  game.Audio.Stage(0);yield return new WaitForSecondsRealtime(1);
  var audio=game.Audio.GetComponents<AudioSource>();Check(game.Audio.CurrentMusic==game.catalog.presentation.music[0],"gameplay uses separate soundtrack");
  Check(audio.Count(a=>a.loop&&a.isPlaying)==1,"crossfade ends with one music voice");
  game.Audio.SetPaused(true);yield return new WaitForSecondsRealtime(1);Check(audio.Where(a=>a.loop).Max(a=>a.volume)<game.catalog.presentation.musicVolume*.5f,"pause ducks soundtrack");
  game.Audio.SetEffectsEnabled(false);game.Audio.SetEnabled(true);Check(audio.Where(a=>!a.loop).All(a=>a.mute),"music toggle preserves SFX mute on every voice");
  game.Audio.SetEffectsEnabled(true);game.Audio.Menu();yield return new WaitForSecondsRealtime(1);

  typeof(UI.GameView).GetField("shipIndex",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.View,1);game.View.Fleet();yield return Shot("02-fleet-locked");Check(!game.SelectShip(1),"unaffordable fighter stays locked");game.Save.Value.coins=100;Check(game.SelectShip(1),"fighter unlock succeeds");Check(game.Save.Value.coins==10,"fighter costs exactly 90 coins");Check(game.SelectShip(1)&&game.Save.Value.coins==10,"equipping owned fighter never charges again");
  game.View.StageSelect();yield return Shot("03-campaign");game.StartStage(0);yield return new WaitForSecondsRealtime(1);Check(Field<EntityPool>("bullets").Items.Any(e=>e.gameObject.activeSelf&&e.kind==EntityKind.PlayerBullet),"automatic fire without held input");
  game.TogglePause();float elapsed=game.Elapsed;yield return Shot("04-pause");Check(game.State==GameState.Paused&&game.Elapsed==elapsed,"pause freezes simulation");game.TogglePause();
  yield return new WaitForSecondsRealtime(9);yield return Shot("05-combat");Check(Field<EntityPool>("pickups").Items.Any(e=>e.gameObject.activeSelf),"timed supplies descend from top");
  game.StartStage(0);var pool=Field<EntityPool>("enemies");Call("SpawnEnemy",game.catalog.stages[0].waves[0].enemy,0f);var enemy=pool.Items.First(e=>e.gameObject.activeSelf);Call("KillEnemy",enemy);Call("KillEnemy",enemy);Check(game.RunCoins==1,"destroyed enemy awards exactly one coin once");int wallet=game.Save.Value.coins;Call("Finish",false);Check(game.Save.Value.coins==wallet+1,"defeat banks only destroyed-enemy coins");Call("Finish",false);Check(game.Save.Value.coins==wallet+1,"settlement cannot duplicate coins");yield return Shot("06-results");
  game.StartStage(0);Call("ClearRun");typeof(GameController).GetField("waveIndex",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,game.catalog.stages[0].waves.Length);typeof(GameController).GetField("spawnTimer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,0f);yield return new WaitForSecondsRealtime(4);yield return Shot("07-boss");Check(game.BossFraction>0,"end boss spawns and exposes health");
  foreach(var boss in pool.Items.Where(e=>e.gameObject.activeSelf).ToArray())Call("KillEnemy",boss);Check(game.Won&&game.Save.Value.unlocked==2,"boss victory unlocks next mission");Check(game.RunCoins==1,"boss also awards one coin without clear bonus");yield return Shot("08-victory");
  game.Save.Value.unlocked=288;game.StartStage(287);Check(game.StageIndex==287,"final mission starts");game.TogglePause();yield return Shot("09-final-mission");
  for(int i=0;i<8;i++){
   game.StartStage(i);Call("ClearRun");var data=game.catalog.stages[i].boss;Call("SpawnEnemy",data,0f);var actor=pool.Items.First(e=>e.gameObject.activeSelf);Call("FireBoss",actor,data.shotCount,false);
   Check(Field<EntityPool>("bullets").Items.Any(e=>e.gameObject.activeSelf&&e.kind==EntityKind.EnemyBullet),"boss pattern "+data.pattern+" fires valid projectiles");
  }
  game.StartStage(0);Call("ClearRun");Call("SpawnEnemy",game.catalog.stages[0].boss,0f);var livingBoss=pool.Items.First(e=>e.gameObject.activeSelf);float health=livingBoss.hp;int charges=game.Bombs;game.Bomb();Check(game.Bombs==charges-1&&game.NukesUsed==1,"Nuke consumes one existing charge");Check(Mathf.Approximately(livingBoss.hp,health-game.catalog.balance.bombDamage),"Nuke damage unchanged");
  livingBoss.Position=new Vector2(0,livingBoss.enemy.holdY);livingBoss.hp=livingBoss.maxHp*.65f;Call("TickEnemies",.01f);
  Check(livingBoss.bossStage==2&&game.BossTransition,"70 percent triggers phase tell");
  Check(!Field<EntityPool>("bullets").Items.Any(e=>e.gameObject.activeSelf&&e.kind==EntityKind.EnemyBullet),"phase transition clears hostile bullets");
  livingBoss.hp=livingBoss.maxHp*.35f;Call("TickEnemies",.01f);Check(livingBoss.bossStage==3&&game.BossTransition,"40 percent triggers final phase tell");
  livingBoss.phaseTell=0;livingBoss.shotTimer=0;Call("TickEnemies",.01f);Check(Field<EntityPool>("bullets").Items.Any(e=>e.gameObject.activeSelf&&e.kind==EntityKind.EnemyBullet),"final phase resumes fire after warning");
  game.ShowMenu();game.View.Worlds();yield return Shot("10-worlds");game.View.Fleet();yield return Shot("11-fleet");game.View.Settings();yield return Shot("12-settings");
  game.StartStage(0);Call("ClearRun");Call("SpawnEnemy",game.catalog.stages[0].waves[0].enemy,0f);var victim=pool.Items.First(e=>e.gameObject.activeSelf);victim.Position=new Vector2(0,1);Call("KillEnemy",victim);yield return Shot("13-explosion",.065f);game.Bomb();yield return Shot("14-nuke",.065f);
  game.StartStage(4);Call("ClearRun");typeof(GameController).GetField("weaponTier",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,5);typeof(GameController).GetField("weaponTimer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,10f);
  var roster=game.catalog.stages.SelectMany(s=>s.waves).Select(w=>w.enemy).Distinct().Take(3).ToArray();for(int i=0;i<roster.Length;i++){Call("SpawnEnemy",roster[i],i*2-2f);pool.Items.Last(e=>e.gameObject.activeSelf).Position=new Vector2(i*2-2,3);}
  yield return Shot("15-roster",.5f);
 }
 IEnumerator Backgrounds(){
  yield return new WaitForSecondsRealtime(1);game=FindObjectOfType<GameController>();
  Check(game.catalog.presentation.worldBackgrounds.Length==8,"eight existing worlds have backgrounds");
  Check(game.catalog.stages.Length==288,"mission count preserved");
  yield return Shot("background-menu");game.Save.Value.unlocked=288;
  for(int i=0;i<8;i++){
   game.StartStage(i*36);yield return new WaitForSecondsRealtime(3);
   var backdrop=game.GetComponentsInChildren<SpriteRenderer>().First(s=>s.gameObject.name=="World backdrop");
   Check(backdrop.sprite==game.catalog.presentation.worldBackgrounds[i],"world "+(i+1)+" art selected");
   yield return Shot("world-"+(i+1).ToString("00"));
  }
 }
}
}
#endif
