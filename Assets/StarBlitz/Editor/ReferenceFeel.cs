using System.IO;
using UnityEngine;
using UnityEditor;
using StarBlitz.Data;
namespace StarBlitz.Editor {
public static class ReferenceFeel {
 const string Root="Assets/StarBlitz/";
 [MenuItem("Star Blitz/Apply Reference Combat Feel")]
 public static void Apply(){
  var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>(Root+"Data/GameCatalog.asset");
  for(int i=0;i<catalog.stages.Length;i++){
   var boss=catalog.stages[i].boss;
   // Authored durability, independent of player upgrades. Existing world scaling still applies.
   boss.health=(8500+(i/36)*650)*(1+(i%36)*.007f);
   boss.bossPhaseThreshold=.7f;boss.bossPhaseFireMultiplier=.85f;boss.bossPhaseExtraShots=2;
   EditorUtility.SetDirty(boss);
  }
  var p=catalog.presentation;p.enableSfx=true;p.sfxVolume=.32f;p.musicVolume=.13f;p.minimumShotAudioInterval=.12f;
  p.fire=Sound("Weapon",.105f,0);p.explosion=Sound("Impact",.3f,1);p.bomb=Sound("Nuke",.8f,2);p.click=Sound("Button",.065f,3);p.enemyFire=Sound("Warning",.45f,4);p.playerHit=Sound("ShieldHit",.18f,5);
  EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();Debug.Log("REFERENCE_FEEL_APPLIED");
 }
 static AudioClip Sound(string name,float duration,int type){
  const int rate=44100;int count=(int)(rate*duration);string path=Root+"Audio/"+name+"V4.wav";var random=new System.Random(903+type);
  using(var w=new BinaryWriter(File.Create(path))){
   w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
   float phase=0,low=0;
   for(int i=0;i<count;i++){
    float t=(float)i/rate,u=t/duration,n=(float)random.NextDouble()*2-1;low=low*.86f+n*.14f;
    float frequency=type==0?Mathf.Lerp(240,95,u):type==1?Mathf.Lerp(110,42,u):type==2?Mathf.Lerp(75,28,u):type==3?680:type==4?340+60*Mathf.Sin(t*24):190;
    phase+=2*Mathf.PI*frequency/rate;
    float body=Mathf.Sin(phase),sample=type==0?body*.45f+low*.9f+n*.16f*Mathf.Exp(-t*90):type==3?body*.45f:type==4?body*.4f*(.6f+.4f*Mathf.Sin(t*32)):body*.35f+low*1.5f+n*.12f;
    float env=Mathf.Min(1,t/.003f)*Mathf.Pow(1-u,2.5f);w.Write((short)(Mathf.Clamp(sample*env*.7f,-.95f,.95f)*32767));
   }
  }
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
 }
 public static void BuildPreview(){Apply();PolishContent.BuildWindows();}
}
}
