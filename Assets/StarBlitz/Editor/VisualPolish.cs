using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using StarBlitz.Data;
namespace StarBlitz.Editor {
public static class VisualPolish {
 const string Root="Assets/StarBlitz/";
 [MenuItem("Star Blitz/Apply Visual Polish")]
 public static void Apply(){
  var p=AssetDatabase.LoadAssetAtPath<PresentationData>(Root+"Data/Presentation.asset");
  p.pulse=Make("PulseV3",0);p.hostileOrb=Make("HostileOrbV3",1);p.flame=Make("ThrusterV3",2);p.blast=Make("BlastV3",3);EditorUtility.SetDirty(p);
  var sprites=Slice("EnemyAtlasV3",2,2);
  foreach(var id in AssetDatabase.FindAssets("t:EnemyData",new[]{Root+"Data"})){
   var e=AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(id));if(e.flight==Flight.Boss)continue;
   e.sprite=sprites[e.flight==Flight.Weaver?1:e.flight==Flight.Turret?2:e.flight==Flight.Kamikaze?3:0];e.tint=Color.white;EditorUtility.SetDirty(e);
  }
  AssetDatabase.SaveAssets();Debug.Log("VISUAL_POLISH_APPLIED: art only, combat values preserved.");
 }
 static Sprite[] Slice(string name,int columns,int rows){
  string path=Root+"Art/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.maxTextureSize=2048;
  int width,height;imp.GetSourceTextureWidthAndHeight(out width,out height);float w=width/(float)columns,h=height/(float)rows;
  imp.spritesheet=Enumerable.Range(0,columns*rows).Select(i=>new SpriteMetaData{name=name+"_"+i,rect=new Rect(i%columns*w,(rows-1-i/columns)*h,w,h),alignment=0,pivot=new Vector2(.5f,.5f)}).ToArray();imp.SaveAndReimport();
  return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
 }
 // Small procedural energy shapes extend the original code-authored effects library.
 static Sprite Make(string name,int kind){
  const int n=128;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color[n*n];
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   float px=(x+.5f)/n*2-1,py=(y+.5f)/n*2-1,a=0,v=1;
   if(kind==0){float r=Mathf.Sqrt(px*px*15+py*py*1.5f);a=Mathf.Clamp01((1-r)*5);v=Mathf.Lerp(.65f,1,Mathf.Clamp01((.5f-r)*4));}
   if(kind==1){float r=Mathf.Sqrt(px*px+py*py);a=Mathf.Clamp01((.83f-r)*8);v=.6f+.4f*Mathf.Clamp01((.58f-r)*5);}
   if(kind==2){float width=.04f+(py+1)*.2f;float r=Mathf.Abs(px)/width;a=Mathf.Clamp01((1-r)*3)*Mathf.Clamp01((py+1)*1.4f)*Mathf.Clamp01((.8f-py)*5);}
   if(kind==3){float angle=Mathf.Atan2(py,px),r=Mathf.Sqrt(px*px+py*py);float edge=.55f+.075f*Mathf.Sin(angle*7)+.045f*Mathf.Cos(angle*11);a=Mathf.Clamp01((edge-r)*10);v=.55f+.45f*Mathf.Clamp01((.38f-r)*6);}
   pixels[y*n+x]=new Color(v,v,v,a);
  }
  t.SetPixels(pixels);t.Apply();string path=Root+"Art/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 public static void BuildPreview(){Apply();PolishContent.BuildWindows();}
}
}
