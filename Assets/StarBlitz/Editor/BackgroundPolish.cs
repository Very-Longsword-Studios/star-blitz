using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StarBlitz.Data;
namespace StarBlitz.Editor {
/// <summary>Imports only visual assets. Never regenerates campaign or changes balance.</summary>
public static class BackgroundPolish {
 [MenuItem("Star Blitz/Apply World Backgrounds")]
 public static void Apply(){
  var presentation=AssetDatabase.LoadAssetAtPath<PresentationData>("Assets/StarBlitz/Data/Presentation.asset");
  presentation.worldBackgrounds=Import("DistantSpaceV3",0).Concat(Import("AlienHullV3",4)).ToArray();
  if(presentation.worldBackgrounds.Length!=8)throw new InvalidOperationException("Expected eight world backgrounds.");
  EditorUtility.SetDirty(presentation);AssetDatabase.SaveAssets();Debug.Log("WORLD_BACKGROUNDS_APPLIED: minimal space and alien hulls, existing 8 worlds / 288 missions preserved.");
 }
 static Sprite[] Import(string name,int firstWorld){
  string Atlas="Assets/StarBlitz/Art/"+name+".png";
  AssetDatabase.ImportAsset(Atlas,ImportAssetOptions.ForceSynchronousImport);
  var importer=AssetImporter.GetAtPath(Atlas) as TextureImporter;if(!importer)throw new InvalidOperationException("World background atlas is missing.");
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.alphaIsTransparency=false;
  int width,height;importer.GetSourceTextureWidthAndHeight(out width,out height);
  float w=width/2f,h=height/2f;
  importer.spritesheet=Enumerable.Range(0,4).Select(i=>new SpriteMetaData{name="World"+(firstWorld+i+1).ToString("00"),rect=new Rect((i%2)*w+3,(1-i/2)*h+3,w-6,h-6),alignment=0,pivot=new Vector2(.5f,.5f)}).ToArray();
  importer.SaveAndReimport();
  return AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
 }
 public static void BuildPreview(){Apply();PolishContent.BuildWindows();}
}
}
