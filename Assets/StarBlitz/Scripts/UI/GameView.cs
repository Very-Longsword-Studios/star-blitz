using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using StarBlitz.Data;
using StarBlitz.Gameplay;
namespace StarBlitz.UI {
/// <summary>Portrait, safe-area aware flight deck. All controls are standard editable uGUI.</summary>
public sealed class GameView : MonoBehaviour {
 GameController game;PresentationData theme;RectTransform safe,screen;Canvas canvas;
 Text score,wave,coins,bombCount,status,timer,bossState;Image hull,shield,boss,flash,damageEdge;RectTransform bombControl,pauseControl,hero;
 float hurtGlow,displayScore;float heroY=390;
 readonly Vector3[] corners=new Vector3[4]; bool hud;string page;Vector2 lastScreen;Rect lastSafe;int stagePage,shipIndex;float opened;
 Color Gold=new Color(1,.77f,.32f), Navy=new Color(.025f,.045f,.09f,.94f);
 public void Initialize(GameController controller){
  game=controller;theme=game.catalog.presentation;
  var root=new GameObject("Star Blitz UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(540,960);scaler.matchWidthOrHeight=.5f;
  safe=NewRect("Safe Area",root.transform);ApplySafeArea();Canvas.ForceUpdateCanvases();
  if(!FindObjectOfType<EventSystem>()){var es=new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
 }
 void ApplySafeArea(){Rect r=Screen.safeArea;safe.anchorMin=new Vector2(r.xMin/Screen.width,r.yMin/Screen.height);safe.anchorMax=new Vector2(r.xMax/Screen.width,r.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;lastScreen=new Vector2(Screen.width,Screen.height);lastSafe=r;Canvas.ForceUpdateCanvases();Layout();}
 void Layout(){if(screen)screen.localScale=Vector3.one*Mathf.Min(1,safe.rect.height/960f,safe.rect.width/540f);}
 void Update(){
  if(lastScreen.x!=Screen.width||lastScreen.y!=Screen.height||lastSafe!=Screen.safeArea)ApplySafeArea();
  if(hud){game.InputReader.BombRect=ScreenRect(bombControl);game.InputReader.PauseRect=ScreenRect(pauseControl);if(bombControl)bombControl.GetComponent<Button>().interactable=game.Bombs>0;}
  if(hero){var p=hero.anchoredPosition;p.y=-heroY+(game.Save.Value.reducedEffects?0:Mathf.Sin(Time.unscaledTime*1.6f)*5);hero.anchoredPosition=p;hero.localRotation=Quaternion.Euler(0,0,game.Save.Value.reducedEffects?0:Mathf.Sin(Time.unscaledTime*.8f)*2);}
  hurtGlow=Mathf.Max(0,hurtGlow-Time.unscaledDeltaTime*2);if(damageEdge)damageEdge.color=new Color(1,.2f,.16f,hurtGlow*.6f);
  if(screen){var group=screen.GetComponent<CanvasGroup>();if(group)group.alpha=Mathf.Clamp01((Time.unscaledTime-opened)*6);}
 }
 Rect ScreenRect(RectTransform r){if(!r)return new Rect();r.GetWorldCorners(corners);return new Rect(corners[0].x,corners[0].y,corners[2].x-corners[0].x,corners[2].y-corners[0].y);}
 RectTransform NewRect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
 void Position(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
 Image Box(string name,float x,float y,float w,float h,Color color){var r=NewRect(name,screen);Position(r,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
 void Panel(float x,float y,float w,float h,Color color){Box("Panel",x,y,w,h,color);Box("Edge",x-w/2+1,y,2,h-14,new Color(.2f,.85f,1,.45f));Box("Corner",x+w/2-13,y-h/2+1,26,2,theme.accent);Box("Corner",x-w/2+13,y+h/2-1,26,2,theme.accent);}
 Text Label(string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter){var r=NewRect(value,screen);Position(r,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=theme.font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
 Image Sprite(Sprite sprite,float x,float y,float w,float h,Color color){var i=Box("Artwork",x,y,w,h,color);i.sprite=sprite;i.preserveAspect=true;return i;}
 DeckGraphic Decor(DeckGraphic.Shape shape,float x,float y,float w,float h,Color color,Color edge){var r=NewRect(shape.ToString(),screen);Position(r,x,y,w,h);var g=r.gameObject.AddComponent<DeckGraphic>();g.shape=shape;g.color=color;g.edge=edge;g.raycastTarget=false;return g;}
 void Line(Vector2 a,Vector2 b,Color color,float width=2){var image=Box("Route",(a.x+b.x)/2,(a.y+b.y)/2,Vector2.Distance(a,b),width,color);image.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);}
 void IconButton(DeckGraphic.Shape icon,string caption,float x,float y,Action action){var b=Button("",x,y,82,action,false,76);var g=b.GetComponent<DeckGraphic>();g.shape=DeckGraphic.Shape.Rounded;Decor(icon,x,y,34,34,theme.accent,theme.accent);Label(caption,x,y+59,145,25,12,theme.muted);}
 Button Button(string title,float x,float y,float w,Action action,bool primary=false,float height=58){
  var i=Decor(DeckGraphic.Shape.Rounded,x,y,w,height,primary?new Color(.96f,.64f,.12f):new Color(.065f,.13f,.22f),primary?new Color(1,.87f,.43f):new Color(.28f,.49f,.66f));i.name=title;i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;i.gameObject.AddComponent<ButtonMotion>();
  var colors=b.colors;colors.highlightedColor=new Color(.85f,1,1);colors.pressedColor=new Color(.5f,.8f,.9f);colors.disabledColor=new Color(.28f,.35f,.4f,.7f);b.colors=colors;
  var text=Label(title,x,y,w-18,height,16,primary?new Color(.12f,.065f,.015f):theme.ink);text.transform.SetParent(i.transform,true);text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
  if(action!=null)b.onClick.AddListener(()=>{game.Audio.Play(theme.click);action();});return b;
 }
 void Reset(string name){
  if(name=="game")game.Audio.SetPaused(false);else if(name=="pause"||name=="confirm")game.Audio.SetPaused(true);else game.Audio.Menu();
  hud=false;page=name;hero=null;heroY=390;damageEdge=null;bombControl=pauseControl=null;game.InputReader.BombRect=game.InputReader.PauseRect=new Rect();game.InputReader.ResetDrag();
  if(screen){screen.gameObject.SetActive(false);Destroy(screen.gameObject);}screen=NewRect(name,safe);screen.anchorMin=screen.anchorMax=new Vector2(.5f,.5f);screen.sizeDelta=new Vector2(540,960);screen.anchoredPosition=Vector2.zero;Layout();opened=Time.unscaledTime;screen.gameObject.AddComponent<CanvasGroup>();
  if(name!="game")Box("Atmosphere",0,480,540,960,new Color(.005f,.014f,.035f,.35f));
 }
 void Top(string title,string subtitle){Label("S T A R  B L I T Z   /   FLIGHT COMMAND",0,35,480,24,11,theme.accent);Box("Rule",0,61,468,1,new Color(.2f,.8f,1,.32f));Label(title,0,110,478,60,32,theme.ink);Label(subtitle,0,155,470,34,12,theme.muted);}
 void Wallet(float y=201){Panel(0,y,468,43,Navy);Label(game.Save.Value.coins.ToString("N0")+"  COINS",-105,y,225,33,16,Gold);Decor(DeckGraphic.Shape.Diamond,94,y,13,18,new Color(.68f,.42f,1),new Color(.8f,.65f,1));Label(game.Save.Value.gems+"  GEMS",151,y,125,33,14,new Color(.8f,.65f,1));}
 void Back(){Button("<  COMMAND DECK",0,899,468,game.ShowMenu);}
 public void MainMenu(){
  Reset("menu");Wallet(45);Decor(DeckGraphic.Shape.Diamond,0,211,305,242,new Color(.035f,.05f,.11f,.6f),new Color(.25f,.6f,.75f,.6f));
  Label("STAR",0,171,470,97,66,theme.ink);Label("B L I T Z",0,247,470,75,46,theme.accent);Label("FRONTIER DEFENSE",0,305,430,26,11,theme.muted);
  heroY=457;Sprite(theme.shieldRing,0,465,248,248,new Color(.2f,.6f,.8f,.2f));
  if(theme.flame)Sprite(theme.flame,0,536,65,113,game.ActiveShip.engineColor);
  hero=Sprite(game.ActiveShip.sprite,0,457,230,230,Color.white).rectTransform;
  Label(game.ActiveShip.displayName,0,600,450,32,20,game.ActiveShip.engineColor);
  Button("TAP TO START   >",0,671,428,()=>{stagePage=(game.Save.Value.unlocked-1)/6;StageSelect();},true,66);
  IconButton(DeckGraphic.Shape.Ship,"FLEET",-154,795,()=>{shipIndex=game.Save.Value.selectedShip;Fleet();});IconButton(DeckGraphic.Shape.Upgrade,"UPGRADES",0,795,Hangar);IconButton(DeckGraphic.Shape.Settings,"SETTINGS",154,795,Settings);
  Label("DRAG TO FLY  /  AUTO FIRE",0,920,460,24,11,theme.muted);
 }
 public void StageSelect(){
  Reset("stages");int pages=Mathf.CeilToInt(game.catalog.stages.Length/6f);stagePage=Mathf.Clamp(stagePage,0,pages-1);int first=stagePage*6;var current=game.catalog.stages[first];Top(current.sector,"WORLD "+(first/36+1)+" / 8   Ãƒâ€šÃ‚Â·   SELECT A MISSION");Wallet();
  var positions=new Vector2[6];for(int j=0;j<6;j++)positions[j]=new Vector2(Mathf.Sin(j*1.5f)*146,730-j*84);
  for(int j=0;j<5;j++)Line(positions[j],positions[j+1],first+j+1<game.Save.Value.unlocked?new Color(.2f,.7f,.8f,.7f):new Color(.2f,.3f,.4f,.45f),3);
  for(int j=0;j<6&&first+j<game.catalog.stages.Length;j++){
   int n=first+j;bool unlocked=n<game.Save.Value.unlocked,active=n==game.Save.Value.unlocked-1;float x=positions[j].x,y=positions[j].y;
   if(active)Decor(DeckGraphic.Shape.Disc,x,y,84,84,new Color(.16f,.12f,.035f,.8f),Gold);
   var b=Button(unlocked?(n+1).ToString("000"):"",x,y,65,()=>game.StartStage(n),active,65);b.GetComponent<DeckGraphic>().shape=DeckGraphic.Shape.Disc;b.interactable=unlocked;
   if(!unlocked)Decor(DeckGraphic.Shape.Lock,x,y,25,28,theme.muted,theme.muted);
   int stars=game.Save.Value.stars[n];Label(unlocked?new string('\u2605',stars)+new string('\u2606',3-stars):(n+1).ToString("000")+" LOCKED",x+(x>20?-93:93),y,110,24,12,unlocked?Gold:theme.muted);
  }
  Button("<",-195,810,65,()=>{stagePage--;StageSelect();}).interactable=stagePage>0;Button("WORLDS",0,810,212,Worlds);Button(">",195,810,65,()=>{stagePage++;StageSelect();}).interactable=stagePage<pages-1;Back();
 }
 public void Worlds(){
  Reset("worlds");Top("THE FRONTIER","8 WORLDS  /  288 MISSIONS");Wallet();
  var positions=new Vector2[8];for(int i=0;i<8;i++)positions[i]=new Vector2(Mathf.Sin(i*1.2f)*145,758-i*67);
  for(int i=0;i<7;i++)Line(positions[i],positions[i+1],new Color(.2f,.4f,.5f,.5f),2);
  for(int i=0;i<8;i++){int w=i,first=w*36;float x=positions[i].x,y=positions[i].y;bool unlocked=first<game.Save.Value.unlocked,active=w==(game.Save.Value.unlocked-1)/36;
   if(active)Decor(DeckGraphic.Shape.Disc,x,y,63,63,new Color(.14f,.11f,.04f),Gold);
   var b=Button(unlocked?(w+1).ToString("00"):"",x,y,49,()=>{stagePage=w*6;StageSelect();},active,49);b.GetComponent<DeckGraphic>().shape=DeckGraphic.Shape.Disc;b.interactable=unlocked;
   if(!unlocked)Decor(DeckGraphic.Shape.Lock,x,y,20,23,theme.muted,theme.muted);
   float labelX=x+(x>0?-134:134);Label(game.catalog.stages[first].sector,labelX,y-8,183,24,11,theme.ink);int earned=0;for(int n=first;n<first+36;n++)earned+=game.Save.Value.stars[n];Label(unlocked?earned+" / 108 STARS":"WORLD "+(w+1)+" LOCKED",labelX,y+14,183,20,10,unlocked?Gold:theme.muted);
  }Back();
 }
 void Popup(string value,Vector2 position,Color color){
  if(!hud||!screen||screen.GetComponentsInChildren<CombatPopup>().Length>=10)return;
  Vector2 p;RectTransformUtility.ScreenPointToLocalPointInRectangle(screen,Camera.main.WorldToScreenPoint(position),null,out p);p.x=Mathf.Clamp(p.x,-190,190);p.y=Mathf.Clamp(p.y,-330,300);
  var t=Label(value,0,0,180,28,13,color);t.rectTransform.anchoredPosition=new Vector2(p.x,p.y-screen.rect.height*.5f);var fx=t.gameObject.AddComponent<CombatPopup>();fx.text=t;fx.reduced=game.Save.Value.reducedEffects;
 }
 public void CoinFeedback(Vector2 position){Popup("+1 COIN",position,Gold);}
 public void DamageFeedback(Vector2 position,int shieldDamage,int hullDamage){hurtGlow=1;if(shieldDamage>0)Popup("SHIELD -"+shieldDamage,position+Vector2.up*.6f,new Color(.7f,.65f,1));if(hullDamage>0)Popup("HULL -"+hullDamage,position+Vector2.up*1.1f,theme.danger);}
 public void PickupFeedback(Vector2 position,int type){string[] names={"WEAPON UP","SHIELD","NUKE PICKUP","WEAPON UP","+GEMS","MAGNET","SCORE BOOST"};Popup(names[Mathf.Clamp(type,0,names.Length-1)],position,theme.accent);}
 public void Fleet(){
  Reset("fleet");var ships=game.catalog.ships;shipIndex=Mathf.Clamp(shipIndex,0,ships.Length-1);var ship=ships[shipIndex];bool owned=game.Save.Value.ownedShips[shipIndex];bool selected=game.Save.Value.selectedShip==shipIndex;
  Top("YOUR FLEET","CHOOSE YOUR FIGHTER  /  "+(shipIndex+1)+" OF "+ships.Length);Wallet();
  Sprite(theme.shieldRing,0,390,280,280,new Color(ship.engineColor.r,ship.engineColor.g,ship.engineColor.b,.3f));if(theme.flame)Sprite(theme.flame,0,469,58,100,ship.engineColor);hero=Sprite(ship.sprite,0,390,225,225,owned?Color.white:new Color(.65f,.72f,.8f)).rectTransform;
  Button("<",-211,390,48,()=>{shipIndex=(shipIndex+ships.Length-1)%ships.Length;Fleet();});Button(">",211,390,48,()=>{shipIndex=(shipIndex+1)%ships.Length;Fleet();});
  Label(ship.displayName,0,537,460,42,30,theme.ink);Label(ship.role.ToUpperInvariant(),0,578,460,29,12,theme.accent);Label(ship.description,0,623,448,48,14,theme.muted);
  Panel(0,701,468,84,Navy);Label("HULL\n"+ship.hull,-155,701,140,60,17,theme.ink);Label("DAMAGE\n"+ship.weapon.damage,0,701,140,60,17,theme.ink);Label("SPEED\n"+ship.moveSpeed,155,701,140,60,17,theme.ink);
  var b=Button(selected?"EQUIPPED":owned?"EQUIP FIGHTER":"UNLOCK  /  "+ship.unlockCost+" COINS",0,798,468,()=>{game.SelectShip(shipIndex);Fleet();},true,65);b.interactable=!selected&&(owned||game.Save.Value.coins>=ship.unlockCost);
  if(!owned)Label(Mathf.Max(0,ship.unlockCost-game.Save.Value.coins)+" MORE COINS NEEDED  /  1 COIN PER ENEMY",0,848,468,26,11,Gold);Back();
 }
 public void Gameplay(){
  Reset("game");hud=true;displayScore=game.Score;Panel(0,61,510,107,new Color(.012f,.025f,.06f,.94f));
  Label("MISSION "+(game.StageIndex+1).ToString("000"),-166,28,170,25,11,theme.accent);score=Label("000000",-165,60,170,38,26,theme.ink);coins=Label("0 C",115,60,133,28,15,Gold);timer=Label("0:00",115,28,110,23,11,theme.muted);
  pauseControl=Button("II",220,45,43,null,false,48).GetComponent<RectTransform>();
  Box("Hull track",-42,92,378,5,new Color(.15f,.2f,.3f));hull=Box("Hull",-42,92,378,5,theme.accent);Box("Shield track",-42,104,378,3,new Color(.15f,.2f,.3f));shield=Box("Shield",-42,104,378,3,new Color(.55f,.55f,1));
  wave=Label("",0,143,480,30,13,theme.ink);boss=Box("Boss",0,175,410,8,theme.danger);boss.gameObject.SetActive(false);bossState=Label("",0,199,465,23,11,theme.danger);
  foreach(var bar in new[]{hull,shield,boss}){float left=bar.rectTransform.anchoredPosition.x-bar.rectTransform.sizeDelta.x*.5f;bar.rectTransform.pivot=new Vector2(0,.5f);bar.rectTransform.anchoredPosition=new Vector2(left,bar.rectTransform.anchoredPosition.y);}
  status=Label("",62,849,305,26,12,theme.accent);var nuke=Button("",-190,875,82,null,false,88);nuke.GetComponent<DeckGraphic>().shape=DeckGraphic.Shape.Disc;nuke.GetComponent<DeckGraphic>().edge=Gold;bombControl=nuke.GetComponent<RectTransform>();Decor(DeckGraphic.Shape.Nuke,-190,862,31,35,Gold,Gold);Label("NUKE",-190,899,80,24,11,Gold);bombCount=Label("",-190,938,150,24,11,theme.muted);
  Label("DRAG TO MOVE\nAUTO FIRE ONLINE",62,895,305,52,11,theme.muted);flash=Box("Flash",0,480,540,960,new Color(.5f,.9f,1,0));damageEdge=Box("Damage warning",0,122,510,3,Color.clear);TickHud(0);Canvas.ForceUpdateCanvases();game.InputReader.BombRect=ScreenRect(bombControl);game.InputReader.PauseRect=ScreenRect(pauseControl);
 }
 void SetBar(Image bar,float fraction,float width){bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width*Mathf.Clamp01(fraction));}
 public void TickHud(float amount){if(!hud)return;displayScore=Mathf.MoveTowards(displayScore,game.Score,Mathf.Max(40,(game.Score-displayScore)*14)*Time.deltaTime);score.text=Mathf.RoundToInt(displayScore).ToString("000000");coins.text=game.RunCoins+" COINS";wave.text=game.BossFraction>0?game.BossName:game.WaveLabel;bombCount.text=game.Bombs+" CHARGES";timer.text=((int)game.Elapsed/60)+":"+((int)game.Elapsed%60).ToString("00");SetBar(hull,game.Hull/game.ActiveShip.hull,378);SetBar(shield,game.Shield/game.MaxShield,378);boss.gameObject.SetActive(game.BossFraction>0);SetBar(boss,game.BossFraction,410);bossState.text=game.BossFraction>0?"PHASE "+game.BossPhase+"  /  "+Mathf.CeilToInt(game.BossFraction*100)+"%"+(game.BossTransition?"  /  GET READY":game.BossCharge>.25f?"  Ãƒâ€šÃ‚Â·  WEAPONS CHARGING":""):"";status.text=game.PowerStatus;flash.color=new Color(.5f,.9f,1,game.Save.Value.reducedEffects?0:amount*.25f);}
 public void Pause(){Reset("pause");Box("Dim",0,480,540,960,new Color(.005f,.012f,.035f,.88f));Top("FLIGHT PAUSED","TAKE A BREATH, PILOT.");Panel(0,335,468,142,Navy);Label("MISSION "+(game.StageIndex+1).ToString("000"),0,305,430,30,16,theme.accent);Label(game.RunCoins+" COINS  /  "+game.Score+" POINTS",0,357,430,34,21,theme.ink);Button("RESUME FLIGHT",0,491,468,game.TogglePause,true,68);Button("RESTART MISSION",0,579,468,()=>ConfirmLeave(true));Button("COMMAND DECK",0,657,468,()=>ConfirmLeave(false));Label("Auto-fire resumes when you return.",0,736,460,30,13,theme.muted);}
 void ConfirmLeave(bool restart){Reset("confirm");Top(restart?"RESTART MISSION?":"LEAVE THIS RUN?","UNBANKED RUN COINS WILL BE LOST.");Button(restart?"RESTART":"LEAVE",0,435,460,()=>{if(restart)game.StartStage(game.StageIndex);else game.ShowMenu();},true);Button("KEEP PLAYING",0,519,460,game.TogglePause);}
 public void Results(){
  Reset("results");Box("Dim",0,480,540,960,new Color(.005f,.01f,.03f,.65f));Top(game.Won?"MISSION COMPLETE":"SIGNAL LOST",game.catalog.stages[game.StageIndex].title);
  Label(game.Won?new string('\u2605',game.Stars)+new string('\u2606',3-game.Stars):"REARM. RETURN.",0,282,460,90,game.Won?54:28,Gold);Label(game.Score.ToString("N0"),0,392,460,80,58,theme.ink);Label("TOTAL SCORE",0,449,460,25,12,theme.muted);
  Panel(0,547,468,146,Navy);Label("+"+game.RunCoins+" COINS BANKED",0,508,440,37,25,Gold);Label("GEMS EARNED   +"+game.RunGems,0,547,430,25,13,new Color(.8f,.65f,1));Label("NUKES USED   "+game.NukesUsed+"     /     COINS SPENT   0",0,580,440,25,12,theme.muted);Label("ONE DESTROYED ENEMY = ONE COIN",0,607,440,20,10,theme.muted);
  int next=game.Won?game.StageIndex+1:game.StageIndex;Button(next>=game.catalog.stages.Length?"CAMPAIGN COMPLETE":game.Won?"NEXT MISSION   >":"RETRY MISSION",0,673,468,()=>{if(next>=game.catalog.stages.Length)StageSelect();else game.StartStage(next);},true,68);Button("FLEET",-121,756,226,()=>{shipIndex=game.Save.Value.selectedShip;Fleet();});Button("UPGRADES",121,756,226,Hangar);Back();
 }
 public void Hangar(){
  Reset("hangar");Top("ENGINEERING",game.ActiveShip.displayName+"  /  FLEET-WIDE UPGRADES");Wallet();string[] names={"WEAPON DAMAGE","FIRE RATE","THRUST","SHIELD CAPACITY","NUKE PAYLOAD"};string[] units={"Harder hits","Faster automatic fire","Keyboard flight speed","More shield protection","Extra starting charges"};
  for(int i=0;i<5;i++){int n=i,level=game.Save.Value.upgrades[i],cost=game.catalog.balance.UpgradeCost(level);bool max=level>=game.catalog.balance.upgradeMaxLevel;float y=291+i*104;Panel(0,y,468,90,Navy);Label(names[i]+"  /  "+level,-75,y-17,288,29,15,theme.ink,TextAnchor.MiddleLeft);Label(units[i],-75,y+17,288,25,12,theme.muted,TextAnchor.MiddleLeft);var b=Button(max?"MAX":cost+" C",161,y,124,()=>{game.Upgrade(n);Hangar();},true,53);b.interactable=!max&&game.Save.Value.coins>=cost;}
  Label("Applies to all owned fighters on the next mission.",0,811,468,35,12,theme.muted);Back();
 }
 public void Settings(){Reset("settings");Top("FLIGHT SETTINGS","BUILT FOR ONE-FINGER FLIGHT.");Button("MUSIC  /  "+(game.Save.Value.sound?"ON":"OFF"),0,295,468,()=>{game.Save.Value.sound=!game.Save.Value.sound;game.Save.Write();game.Audio.SetEnabled(game.Save.Value.sound);Settings();});Button("SFX  /  "+(!game.Save.Value.sfxMuted?"ON":"OFF"),0,373,468,()=>{game.Save.Value.sfxMuted=!game.Save.Value.sfxMuted;game.Save.Write();game.Audio.SetEffectsEnabled(!game.Save.Value.sfxMuted);Settings();});Button("SCREEN EFFECTS  /  "+(game.Save.Value.reducedEffects?"REDUCED":"FULL"),0,451,468,()=>{game.Save.Value.reducedEffects=!game.Save.Value.reducedEffects;game.Save.Write();Settings();});Panel(0,675,468,280,Navy);Label("AUTO FIRE IS ALWAYS ON",0,563,440,34,18,theme.accent);Label("Drag anywhere on the playfield to fly.\nLift and reposition your finger without moving the ship.\n\nCollect falling power-ups to boost your weapons.\nTap NUKE to clear enemy bullets.\n\nDesktop: drag or WASD / arrows.\nB = Nuke     Esc = pause",0,691,431,227,15,theme.ink);if(game.Ads.PrivacyOptionsRequired)Button("PRIVACY OPTIONS",0,840,468,()=>game.Ads.ShowPrivacyOptions(),false,46);Back();}
 }
}


