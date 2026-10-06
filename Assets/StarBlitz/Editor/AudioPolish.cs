using UnityEngine;
using UnityEditor;
using StarBlitz.Data;
namespace StarBlitz.Editor {
public static class AudioPolish {
 public static void Apply(){
  const string root="Assets/StarBlitz/";
  foreach(string name in new[]{"CommandDeckV5","FlightDriveV5","WeaponV5"}){
   string path=root+"Audio/"+name+".wav";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
   settings.loadType=name=="WeaponV5"?AudioClipLoadType.DecompressOnLoad:AudioClipLoadType.Streaming;
   settings.compressionFormat=name=="WeaponV5"?AudioCompressionFormat.PCM:AudioCompressionFormat.Vorbis;settings.quality=.8f;
   importer.defaultSampleSettings=settings;importer.SaveAndReimport();
  }
  var p=AssetDatabase.LoadAssetAtPath<PresentationData>(root+"Data/Presentation.asset");
  p.menuMusic=AssetDatabase.LoadAssetAtPath<AudioClip>(root+"Audio/CommandDeckV5.wav");
  p.music=new[]{AssetDatabase.LoadAssetAtPath<AudioClip>(root+"Audio/FlightDriveV5.wav")};
  p.fire=AssetDatabase.LoadAssetAtPath<AudioClip>(root+"Audio/WeaponV5.wav");p.musicVolume=.38f;p.sfxVolume=.32f;p.minimumShotAudioInterval=.10f;p.enableSfx=true;
  EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();Debug.Log("AUDIO_V5_APPLIED");
 }
 public static void BuildPreview(){Apply();PolishContent.BuildWindows();}
}
}
