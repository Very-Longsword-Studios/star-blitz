using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using StarBlitz.Data;
namespace StarBlitz.Editor {
/// <summary>Reproducible original assets and editable campaign. Explicit menu command only.</summary>
public static class PolishContent {
 const string Root="Assets/StarBlitz/";
 static T Load<T>(string name) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(Root+"Data/"+name+".asset");
 static T Asset<T>(string path) where T:ScriptableObject {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(!a){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
 [MenuItem("Star Blitz/Generate Polished Campaign")]
 public static void Generate(){
  Directory.CreateDirectory(Root+"Art/Fleet");Directory.CreateDirectory(Root+"Data/Campaign");Directory.CreateDirectory(Root+"Data/Fleet");Directory.CreateDirectory(Root+"Data/Bosses");
  var catalog=Load<GameCatalog>("GameCatalog");var presentation=catalog.presentation;var balance=catalog.balance;
  presentation.nebula=ImportSprite(Root+"Art/Nebula.png");presentation.subtitle="THE FRONTIER IS CALLING.";presentation.muted=new Color(.58f,.7f,.8f);presentation.accent=new Color(.25f,.91f,1);presentation.musicVolume=.19f;presentation.sfxVolume=.5f;
  string[] names={"VANGUARD","KESTREL","PHANTOM","BULWARK","SOLARIS","TEMPEST"};
  string[] roles={"Balanced interceptor","Rapid assault","Precision striker","Armored guardian","Heavy bombardier","Elite multirole"};
  string[] desc={"Your first fighter. Balanced shields, handling and firepower.","A lightweight frame with a fast pulse cannon.","High-impact rounds in a sleek, agile strike frame.","Reinforced armor and deep shields for dangerous crossings.","Heavy plasma fire. Slower handling, exceptional damage.","Advanced systems combine speed, shielding and rapid fire."};
  int[] costs={0,90,220,420,700,1100};Color[] colors={new Color(.2f,.85f,1),new Color(.35f,1,.65f),new Color(.7f,.4f,1),new Color(1,.55f,.2f),new Color(1,.8f,.3f),new Color(1,.35f,.55f)};
  float[] hulls={100,85,90,165,125,140},shields={60,50,65,110,80,100},speeds={9,12,11,7,8,11},damage={12,10,20,15,26,18},intervals={.18f,.12f,.22f,.19f,.25f,.14f};
  catalog.ships=new ShipData[6];
  for(int i=0;i<6;i++){
   var s=Asset<ShipData>(Root+"Data/Fleet/"+names[i]+".asset");s.displayName=names[i];s.role=roles[i];s.description=desc[i];s.unlockCost=costs[i];s.hull=hulls[i];s.shield=shields[i];s.moveSpeed=speeds[i];s.engineColor=colors[i];s.bombs=i==4?3:2;s.sprite=DrawShip(names[i],i,colors[i],false);s.visualSize=1.15f;s.radius=.21f;
   var w=Asset<WeaponData>(Root+"Data/Fleet/"+names[i]+"Cannon.asset");EditorUtility.CopySerialized(Load<WeaponData>("PlayerCannon"),w);w.damage=damage[i];w.interval=intervals[i];w.maximumTier=5;w.spreadAngle=7;w.visualSize=.4f;w.tint=colors[i];s.weapon=w;EditorUtility.SetDirty(w);EditorUtility.SetDirty(s);catalog.ships[i]=s;
  }
  catalog.ship=catalog.ships[0];
  string[] worlds={"CYGNUS REACH","JADE EXPANSE","VIOLET RIFT","EMBER BELT","SOLAR FRONT","CRIMSON VEIL","FROZEN HALO","THE DARK CROWN"};
  string[] bosses={"IRON WARDEN","HELIX REAPER","TWIN SERAPH","ORBITAL MAW","CROSSBONE","ECLIPSE ENGINE","STORM CITADEL","VOID SOVEREIGN"};
  Color[] bossColors={new Color(1,.4f,.25f),new Color(.3f,1,.5f),new Color(.8f,.4f,1),new Color(1,.6f,.15f),new Color(1,.8f,.4f),new Color(1,.2f,.5f),new Color(.4f,.8f,1),new Color(.8f,.5f,1)};
  var archetypes=new EnemyData[8];
  for(int i=0;i<8;i++){var b=Asset<EnemyData>(Root+"Data/Bosses/"+bosses[i]+".asset");EditorUtility.CopySerialized(Load<EnemyData>("Dreadnought"),b);b.displayName=bosses[i];b.pattern=(BossPattern)i;b.sprite=DrawShip(bosses[i],i,bossColors[i],true);b.tint=Color.white;b.health=1100+i*110;b.shotCount=(i==1||i==3||i==7)?12:5;b.spreadAngle=15;b.fireInterval=(i==1||i==3||i==7)?1.55f:1.3f;b.bossPhaseExtraShots=2;b.bossPhaseFireMultiplier=.78f;b.sineFrequency=.55f+i*.04f;b.sineAmplitude=1.5f;b.radius=.9f;b.visualSize=2.8f;b.coins=1;b.holdY=3.2f;EditorUtility.SetDirty(b);archetypes[i]=b;}
  EnemyData[] enemies={Load<EnemyData>("Grunt"),Load<EnemyData>("Weaver"),Load<EnemyData>("Turret")};
  for(int i=0;i<3;i++){enemies[i].coins=1;enemies[i].health=26+i*14;enemies[i].speed=1.4f+i*.18f;enemies[i].dropChance=.075f;enemies[i].sprite=DrawShip("RAIDER_"+i,i+2,bossColors[i],false);enemies[i].visualSize=.8f;EditorUtility.SetDirty(enemies[i]);}
  catalog.stages=new StageData[288];
  for(int i=0;i<288;i++){
   int world=i/36,local=i%36;var stage=Asset<StageData>(Root+"Data/Campaign/Mission"+(i+1).ToString("000")+".asset");stage.title=worlds[world]+" / "+(local+1).ToString("00");stage.sector=worlds[world];stage.accent=bossColors[world];stage.background=new Color(.008f,.014f,.033f);stage.healthScale=1+world*.1f+local*.008f;stage.fireRateScale=1+world*.035f+local*.003f;stage.speedScale=1+world*.02f;stage.clearCoins=0;stage.bossDelay=3;stage.targetSeconds=120+world*8+local%4*8;
   // Each mission owns an editable boss configuration; eight silhouette / pattern families.
   var boss=Asset<EnemyData>(Root+"Data/Bosses/Mission"+(i+1).ToString("000")+"Boss.asset");EditorUtility.CopySerialized(archetypes[(world+local)%8],boss);boss.displayName=archetypes[(world+local)%8].displayName+" "+(local+1).ToString("00");boss.sineFrequency+=local*.005f;boss.health*=1+local*.007f;stage.boss=boss;EditorUtility.SetDirty(boss);
   int waveCount=7+local%3;stage.waves=new Wave[waveCount];for(int w=0;w<waveCount;w++){int type=w<2&&i==0?0:(w+local+world)%3;stage.waves[w]=new Wave{enemy=enemies[type],count=type==2?3:7+local%4,spawnInterval=type==2?1.1f:.48f,delay=1.7f,width=2.5f,alternateSides=(w+local)%2==0,formation=(w+local)%3};}
   EditorUtility.SetDirty(stage);catalog.stages[i]=stage;
  }
  balance.dropWeights=new[]{50,25,15,0,0,5,5};balance.pickupFallSpeed=1.5f;balance.pickupSize=.63f;balance.weaponDuration=18;balance.playerTop=5.8f;balance.playerBottom=-6.1f;balance.bulletPool=420;balance.particlePool=480;balance.explosionParticles=26;balance.hitParticles=4;balance.upgradeBaseCost=45;balance.upgradeCostGrowth=1.42f;balance.startingCoins=0;balance.shakeStrength=.08f;balance.particleSize=.16f;
  MakeAudio(presentation);EditorUtility.SetDirty(balance);EditorUtility.SetDirty(presentation);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();AssetDatabase.Refresh();ProjectTools.RepairScene();Debug.Log("POLISH_CONTENT_COMPLETE: 288 stages, 288 boss configs, 6 ships.");
 }
 static Sprite ImportSprite(string path){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
 static Sprite DrawShip(string name,int variant,Color accent,bool boss){
  int size=256;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);var pix=new Color[size*size];var shapes=new List<Vector2[]>();
  // Symmetric, beveled metal panels. Different silhouettes are authored as polygon geometry.
  float span=boss?.91f:.64f+(variant%3)*.09f, nose=boss?.63f:.93f;
  shapes.Add(Poly(0,nose,.18f,.42f,span,-.2f,span*.94f,-.57f,.28f,-.4f,.16f,-.75f,-.16f,-.75f,-.28f,-.4f,-span*.94f,-.57f,-span,-.2f,-.18f,.42f));
  if(variant%3==1)shapes.Add(Poly(-span,.38f,-span*.65f,.17f,-.25f,-.48f,-span*.9f,-.65f));
  if(variant%3==1)shapes.Add(Poly(span,.38f,span*.65f,.17f,.25f,-.48f,span*.9f,-.65f));
  if(variant%3==2){shapes.Add(Poly(-.75f,.58f,-.48f,.38f,-.4f,-.72f,-.76f,-.44f));shapes.Add(Poly(.75f,.58f,.48f,.38f,.4f,-.72f,.76f,-.44f));}
  if(boss){shapes.Add(Poly(-.9f,.55f,-.54f,.7f,-.32f,.15f,-.45f,-.75f,-.85f,-.42f));shapes.Add(Poly(.9f,.55f,.54f,.7f,.32f,.15f,.45f,-.75f,.85f,-.42f));}
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   var p=new Vector2((x-127.5f)/128,(y-127.5f)/128);Color c=Color.clear;
   foreach(var poly in shapes)if(Inside(p,poly)){float edge=Edge(p,poly);float light=.18f+.22f*(p.y+1)/2+.13f*(1-Mathf.Abs(p.x));c=Color.Lerp(new Color(.13f,.17f,.24f),new Color(.68f,.76f,.83f),light);if(edge<.028f)c=Color.Lerp(accent,Color.white,.35f);else if(edge<.055f)c=Color.Lerp(c,Color.black,.55f);else if(Mathf.Abs(p.x)>.27f&&Mathf.Abs(p.x)<.36f)c=Color.Lerp(accent,c,.25f);}
   if(c.a>0){if(Mathf.Abs(p.x)<.105f&&p.y>.06f&&p.y<.55f-Mathf.Abs(p.x)*2)c=Color.Lerp(accent,Color.white,.4f+(.55f-p.y)*.5f);if(Mathf.Abs(p.x)<.018f&&p.y<.05f&&p.y>-.57f)c=accent;if(Mathf.Abs(p.x)>.47f&&Mathf.Abs(p.y+.27f)<.014f)c=accent;}
   for(int engine=-1;engine<=1;engine+=2){float ex=p.x-engine*.2f;float glow=Mathf.Exp(-(ex*ex*700+Mathf.Pow((p.y+.77f)*8,2)));if(glow>.03f&&p.y<-.52f)c=Color.Lerp(c,new Color(accent.r,accent.g,accent.b,1),glow);}
   pix[y*size+x]=c;
  }
  t.SetPixels(pix);t.Apply();string path=Root+"Art/Fleet/"+name.Replace(' ','_')+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);return ImportSprite(path);
 }
 static Vector2[] Poly(params float[] xy){var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
 static bool Inside(Vector2 p,Vector2[] poly){bool inside=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){var a=poly[i];var b=poly[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}return inside;}
 static float Edge(Vector2 p,Vector2[] poly){float min=10;for(int i=0;i<poly.Length;i++){var a=poly[i];var d=poly[(i+1)%poly.Length]-a;min=Mathf.Min(min,Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)));}return min;}
 static void MakeAudio(PresentationData p){
  p.fire=Tone("Pulse",.16f,0);p.explosion=Tone("Impact",.55f,1);p.pickup=Tone("Supply",.38f,2);p.bomb=Tone("Nova",1.0f,3);p.click=Tone("Interface",.08f,4);p.victory=Tone("Clear",1.2f,5);
 }
 static AudioClip Tone(string name,float duration,int type){
  const int rate=44100;int count=(int)(rate*duration);var random=new System.Random(41+type);string path=Root+"Audio/"+name+"V2.wav";
  using(var writer=new BinaryWriter(File.Create(path))){writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
   float phase=0;double smooth=0;for(int i=0;i<count;i++){float t=(float)i/rate,u=t/duration;float noise=(float)(random.NextDouble()*2-1);smooth=smooth*.83+noise*.17;float freq=type==0?Mathf.Lerp(1600,180,u):type==1?Mathf.Lerp(130,35,u):type==2?440*Mathf.Pow(2,Mathf.Floor(u*4)/4):type==3?Mathf.Lerp(95,25,u):type==4?1100:440*Mathf.Pow(2,new[]{0,4,7,12}[Mathf.Min(3,(int)(u*4))]/12f);phase+=Mathf.PI*2*freq/rate;float sample=Mathf.Sin(phase);if(type==1||type==3)sample=sample*.35f+(float)smooth*1.4f+noise*.12f;else sample=sample*.65f+Mathf.Sin(phase*2)*.12f;float envelope=Mathf.Min(1,t/.004f)*Mathf.Pow(1-u,type==3?2:3);writer.Write((short)(Mathf.Clamp(sample*envelope*.65f,-1,1)*32767));}
  }
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
 }
 [MenuItem("Star Blitz/Apply Painted Atlases")]
 public static void ApplyAtlases(){
  var catalog=Load<GameCatalog>("GameCatalog");
  var fighters=Slice(File.Exists(Root+"Art/FleetAtlasV3.png")?"FleetAtlasV3":"FleetAtlas",3,2);if(fighters.Length!=6)throw new Exception("Fleet atlas must contain six valid sprites");for(int i=0;i<6;i++){catalog.ships[i].sprite=fighters[i];EditorUtility.SetDirty(catalog.ships[i]);}
  var bosses=Slice("BossAtlas",4,2);if(bosses.Length!=8)throw new Exception("Boss atlas must contain eight valid sprites");foreach(var stage in catalog.stages){stage.boss.sprite=bosses[(int)stage.boss.pattern];stage.boss.holdY=3.2f;EditorUtility.SetDirty(stage.boss);}
  AssetDatabase.SaveAssets();Debug.Log("PAINTED_ATLASES_APPLIED");
 }
 static Sprite[] Slice(string name,int columns,int rows){
  string path=Root+"Art/"+name+".png";if(!File.Exists(path))return new Sprite[0];AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
  importer.npotScale=TextureImporterNPOTScale.None;int sourceWidth,sourceHeight;importer.GetSourceTextureWidthAndHeight(out sourceWidth,out sourceHeight);float w=sourceWidth/(float)columns,h=sourceHeight/(float)rows;var metadata=new SpriteMetaData[columns*rows];
  for(int i=0;i<metadata.Length;i++)metadata[i]=new SpriteMetaData{name=name+"_"+i,rect=new Rect(i%columns*w,(rows-1-i/columns)*h,w,h),alignment=0,pivot=new Vector2(.5f,.5f)};
  importer.spritesheet=metadata;importer.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
 }
 public static void BuildWindows(){
  ApplyAtlases();
  var errors=ProjectTools.ValidationErrors();if(errors.Count>0)throw new Exception(string.Join("\n",errors));
  Directory.CreateDirectory("Builds/Windows");PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"Scenes/StarBlitz.unity"},locationPathName="Builds/Windows/StarBlitz.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");Debug.Log("POLISH_BUILD_COMPLETE");
 }
}
}

