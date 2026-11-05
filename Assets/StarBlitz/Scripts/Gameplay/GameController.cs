using UnityEngine;
using StarBlitz.Data;
using StarBlitz.Audio;
using StarBlitz.Services;
using StarBlitz.UI;
namespace StarBlitz.Gameplay {
public enum GameState { Menu, Playing, Paused, Results }
public sealed class GameController : MonoBehaviour {
 public GameCatalog catalog;
 public ShipData ActiveShip => catalog.ships != null && catalog.ships.Length > 0 ? catalog.ships[Save == null ? 0 : Save.Value.selectedShip] : catalog.ship;
 public float Elapsed {get;private set;}
 public string BossName => stage ? stage.boss.displayName : "";
 public string PowerStatus => rayTimer > 0 ? "RAY GUN  /  " + Mathf.CeilToInt(rayTimer) + "s" : weaponTimer > 0 ? "OVERDRIVE " + weaponTier + "  /  " + Mathf.CeilToInt(weaponTimer) + "s" : bubbleTimer > 0 ? "SHIELD ACTIVE" : "";
 float supplyTimer, engineTimer; Vector2 moveDelta; CosmicBackdrop backdrop;
 public GameState State {get;private set;}
 public SaveService Save {get;private set;}
 public AudioDirector Audio {get;private set;}
 public AdsService Ads {get;private set;}
 public PlayerInputReader InputReader {get;private set;}
 public GameView View {get;private set;}
 public int StageIndex {get;private set;}
 public int Score {get;private set;}
 public int RunCoins {get;private set;}
 public int RunGems {get;private set;}
 public int Bombs {get;private set;}
 public int Stars {get;private set;}
 public bool Won {get;private set;}
 public float Hull {get;private set;}
 public float Shield {get;private set;}
 public float MaxShield => ActiveShip.shield*(1+Save.Value.upgrades[3]*ActiveShip.shieldPerLevel);
 public float BossFraction {get;private set;}
 public float BossCharge {get;private set;}
 public static int PhaseFor(float fraction) => fraction > .7f ? 1 : fraction > .4f ? 2 : 3;
 public int BossPhase => bossEntity&&bossEntity.gameObject.activeSelf?Mathf.Clamp(bossEntity.bossStage,1,3):PhaseFor(BossFraction);
 public bool BossTransition {get;private set;}
 public bool BossEnraged => bossEntity&&bossEntity.gameObject.activeSelf&&bossEntity.fightTime>=balance.bossEnrageSeconds;
 public bool BossBeamTelegraph => bossEntity&&bossEntity.gameObject.activeSelf&&bossEntity.beamTell>0;
 public bool BossBeamActive => bossEntity&&bossEntity.gameObject.activeSelf&&bossEntity.beamActive>0;
 public float BossFightTime => bossEntity&&bossEntity.gameObject.activeSelf?bossEntity.fightTime:0;
 public bool BossSpecialTelegraph => bossEntity&&bossEntity.gameObject.activeSelf&&bossEntity.specialTell>0;
 public string BossSpecialName => bossEntity&&bossEntity.gameObject.activeSelf&&bossEntity.enemy?BossTuning.SpecialName(bossEntity.enemy.pattern):"";
 public float BossHazardScale => (1f+(StageIndex/36)*.05f)*(1f+(ngScale-1f)*.5f);
 public int RayGunsUsed {get;private set;}
 public bool RayActive => rayTimer>0f;
 public float RayTimeLeft => Mathf.Max(0,rayTimer);
 public float OverdriveTimeLeft => Mathf.Max(0,weaponTimer);
 public float MagnetTimeLeft => Mathf.Max(0,magnetTimer);
 public float MultiplierTimeLeft => Mathf.Max(0,multiplierTimer);
 public float ShieldTimeLeft => Mathf.Max(0,bubbleTimer);
 public string WaveLabel {get;private set;}
 public bool RewardClaimed {get;private set;}
 // Combo multiplies score only; the one-coin-per-kill economy is never affected.
 public int Combo {get;private set;}
 public float ComboBonus => 1f+Mathf.Min(Combo,balance.comboMax)*balance.comboScoreBonus;
 public string ComboStatus => Combo>1?"COMBO "+Combo+"  (+"+Mathf.RoundToInt((ComboBonus-1)*100)+"%)":"";
 EntityPool enemies,bullets,pickups,particles,stars;
 Entity player,ring;Camera cam;
 StageData stage;BalanceData balance;PresentationData art;
 float fireTimer,hitTimer,regenTimer,weaponTimer,bubbleTimer,magnetTimer,multiplierTimer,spawnTimer,shakeTimer,flashTimer,comboTimer,rayTimer,rayParticleAccumulator,rayCrackleTimer;
 float ngScale = 1f; // New Game+ difficulty multiplier (1 + 25% per cleared cycle).
 GameObject rayBeam; SpriteRenderer rayCoreView, rayCapTopView, rayCapBottomView; Color rayNeon;
 Entity bossEntity; GameObject bossBeamRoot; SpriteRenderer bossBeamCore,bossBeamCap;
 int weaponTier,waveIndex,spawned,runSerial;bool waitingForClear,bossSpawned;
 Vector3 cameraOrigin;
 void Awake(){
  if(!catalog||!ActiveShip||!catalog.entityPrefab){Debug.LogError("Star Blitz: missing catalog. Run Star Blitz > Validate Project, then Repair Scene and Prefab.");enabled=false;return;}
  balance=catalog.balance;art=catalog.presentation;
  Application.targetFrameRate=60;Screen.sleepTimeout=SleepTimeout.NeverSleep;
  string qaSave=null;
  #if DEVELOPMENT_BUILD || UNITY_EDITOR
  if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-starblitz-qa")>=0)qaSave=System.IO.Path.Combine(Application.temporaryCachePath,"starblitz-qa-"+System.Guid.NewGuid()+".json");
  #endif
  Save=new SaveService(catalog.stages.Length,balance.startingCoins,qaSave); Save.ConfigureShips(catalog.ships != null && catalog.ships.Length > 0 ? catalog.ships.Length : 1);
  for(int i=0;i<Save.Value.upgrades.Length;i++) Save.Value.upgrades[i]=Mathf.Clamp(Save.Value.upgrades[i],0,balance.upgradeMaxLevel);
  cam=Camera.main;cam.orthographic=true;cameraOrigin=cam.transform.position;FitCamera();
  Audio=gameObject.AddComponent<AudioDirector>();Audio.Initialize(art,Save.Value.sound);Audio.SetEffectsEnabled(!Save.Value.sfxMuted);Audio.Menu();
  Ads=gameObject.AddComponent<AdsService>();Ads.Initialize(catalog.ads);
  InputReader=gameObject.AddComponent<PlayerInputReader>(); backdrop=gameObject.AddComponent<CosmicBackdrop>();backdrop.Initialize(art,cam);
  enemies=new EntityPool("Enemies",balance.enemyPool,catalog.entityPrefab,transform);
  bullets=new EntityPool("Bullets",balance.bulletPool,catalog.entityPrefab,transform);
  pickups=new EntityPool("Pickups",balance.pickupPool,catalog.entityPrefab,transform);
  particles=new EntityPool("Particles",balance.particlePool,catalog.entityPrefab,transform);
  stars=new EntityPool("Starfield",balance.starCount,catalog.entityPrefab,transform);
  player=Instantiate(catalog.entityPrefab,transform).GetComponent<Entity>();player.ResetState();player.Visual(ActiveShip.sprite,ActiveShip.visualSize,Color.white,10);
  ring=Instantiate(catalog.entityPrefab,transform).GetComponent<Entity>();ring.ResetState();ring.Visual(art.shieldRing,ActiveShip.visualSize*1.65f,art.accent,9);ring.gameObject.SetActive(false);
  foreach(var e in stars.Items){e.ResetState();e.gameObject.SetActive(true);e.kind=EntityKind.Star;e.Position=new Vector2(Random.Range(-balance.halfWidth,balance.halfWidth),Random.Range(-balance.halfHeight,balance.halfHeight));e.phase=Random.Range(.3f,1.5f);e.Visual(art.star,.02f+e.phase*.035f,new Color(.45f,.66f,.85f,e.phase*.45f),-20);}
  View=gameObject.AddComponent<GameView>();View.Initialize(this);ShowMenu();
 }
 void FitCamera(){cam.orthographicSize=Mathf.Max(balance.halfHeight,balance.halfWidth/cam.aspect);}
 public void ShowMenu(){
  State=GameState.Menu;ClearRun();backdrop.SetWorld((Save.Value.unlocked-1)/36);cam.backgroundColor=catalog.stages[0].background;player.gameObject.SetActive(false);View.MainMenu();
 }
 void ClearRun(){enemies.Clear();bullets.Clear();pickups.Clear();particles.Clear();ring.gameObject.SetActive(false);shakeTimer=0;flashTimer=0;comboTimer=0;Combo=0;EndRay();DestroyBossBeam();bossEntity=null;cam.transform.position=cameraOrigin;}
 public void StartStage(int index){
  if(index<0||index>=catalog.stages.Length||index>=Save.Value.unlocked)return;
  ngScale=1f+Save.Value.ngPlus*.25f;
  ClearRun();InputReader.ResetDrag();Elapsed=0;supplyTimer=7;engineTimer=0;player.Visual(ActiveShip.sprite,ActiveShip.visualSize,Color.white,10);runSerial++;StageIndex=index;stage=catalog.stages[index];State=GameState.Playing;
  Score=RunCoins=RunGems=Stars=RayGunsUsed=0;Won=RewardClaimed=false;BossFraction=BossCharge=0;BossTransition=false;
  Hull=ActiveShip.hull;Shield=MaxShield;Bombs=Mathf.Min(balance.bombCap,ActiveShip.bombs+Save.Value.upgrades[4]);
  fireTimer=hitTimer=regenTimer=weaponTimer=bubbleTimer=magnetTimer=multiplierTimer=0;weaponTier=1;
  waveIndex=spawned=0;waitingForClear=bossSpawned=false;spawnTimer=stage.waves.Length>0?stage.waves[0].delay:stage.bossDelay;
  player.gameObject.SetActive(true);player.Position=new Vector2(0,balance.playerStartY);player.view.color=Color.white;
  cam.backgroundColor=stage.background;backdrop.SetWorld(index/36);Audio.Stage(index);View.Gameplay();WaveLabel=(Save.Value.ngPlus>0?"NG+"+Save.Value.ngPlus+"  /  ":"")+"ENTERING " + stage.title;
 }
 void Update(){
  if(!catalog||Save==null)return;
  FitCamera();float dt=Mathf.Min(Time.deltaTime,.05f);
  if(State!=GameState.Paused)TickStars(dt);
  if(State==GameState.Menu){player.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time)*4);return;}
  InputReader.Poll();
  if(InputReader.PausePressed&&(State==GameState.Playing||State==GameState.Paused)){TogglePause();return;}
  if(State==GameState.Results){TickParticles(dt);return;}
  if(State!=GameState.Playing)return;
  hitTimer-=dt;regenTimer-=dt;bubbleTimer-=dt;magnetTimer-=dt;multiplierTimer-=dt;weaponTimer-=dt;comboTimer-=dt;if(comboTimer<=0&&Combo>0)Combo=0;
  if(weaponTimer<=0)weaponTier=bossSpawned?Mathf.Max(1,balance.bossWeaponTier):1;
  if(regenTimer<=0)Shield=Mathf.Min(MaxShield,Shield+ActiveShip.shieldRegen*dt);
  MovePlayer(dt);fireTimer-=dt;
  if(fireTimer<=0)FirePlayer();
  Elapsed+=dt;supplyTimer-=dt;if(supplyTimer<=0){Drop(new Vector2(Random.Range(-2.7f,2.7f),balance.halfHeight-.4f));supplyTimer=12;}
  engineTimer-=dt;if(engineTimer<=0){Exhaust();engineTimer=.035f;}
  if(InputReader.BombPressed)ActivateRayGun();
  if(rayTimer>0)TickRay(dt);
  if(State!=GameState.Playing)return;
  TickWaves(dt);TickEnemies(dt);if(State!=GameState.Playing)return;
  TickBullets(dt);if(State!=GameState.Playing)return;
  TickPickups(dt);TickParticles(dt);
  ring.gameObject.SetActive(bubbleTimer>0);ring.Position=player.Position;
  player.view.color=hitTimer>0?new Color(1,1,1,.4f+Mathf.PingPong(Time.time*12,.6f)):Color.white;
  shakeTimer-=dt;flashTimer-=dt;
  cam.transform.position=cameraOrigin+(shakeTimer>0&&!Save.Value.reducedEffects?(Vector3)(Random.insideUnitCircle*balance.shakeStrength):Vector3.zero);
  View.TickHud(Mathf.Clamp01(flashTimer/balance.bombFlashDuration));
 }
 void MovePlayer(float dt){
  float speed=ActiveShip.moveSpeed*(1+Save.Value.upgrades[2]*ActiveShip.speedPerLevel);
  Vector2 p=player.Position;
  moveDelta=InputReader.PointerDelta*(2*cam.orthographicSize/Screen.height);
  p+=moveDelta+InputReader.Axis*speed*dt;
  p.x=Mathf.Clamp(p.x,-balance.halfWidth+ActiveShip.radius,balance.halfWidth-ActiveShip.radius);
  float top=balance.playerTop;
  // The boss owns the airspace above its hull: no hiding over its head while it fires.
  if(bossEntity&&bossEntity.gameObject.activeSelf&&bossEntity.enemy&&bossEntity.enemy.flight==Flight.Boss)
   top=Mathf.Min(top,bossEntity.Position.y-balance.bossPlayerClearance);
  p.y=Mathf.Clamp(p.y,balance.playerBottom,Mathf.Max(balance.playerBottom,top));player.Position=p;
  player.transform.rotation=Quaternion.Lerp(player.transform.rotation,Quaternion.Euler(0,0,-Mathf.Clamp(moveDelta.x*25+InputReader.Axis.x*8,-16,16)),dt*12);
 }
 void FirePlayer(){
  var w=ActiveShip.weapon;fireTimer=w.interval/(1+Save.Value.upgrades[1]*ActiveShip.fireRatePerLevel);
  int count=Mathf.Clamp(weaponTier,1,w.maximumTier);
  for(int i=0;i<count;i++){
   float angle=(i-(count-1)*.5f)*w.spreadAngle;
   Vector2 direction=Quaternion.Euler(0,0,angle)*Vector2.up;
   SpawnBullet(player.Position+new Vector2((i-(count-1)*.5f)*.18f,.5f),direction,w,true,w.damage*(1+Save.Value.upgrades[0]*ActiveShip.damagePerLevel));
  }
  Audio.Shot();
 }
 void SpawnBullet(Vector2 position,Vector2 direction,WeaponData w,bool friendly,float damage,Color? tint=null,float? size=null){
  var e=bullets.Take();if(!e)return;
  e.kind=friendly?EntityKind.PlayerBullet:EntityKind.EnemyBullet;e.Position=position;e.velocity=direction*w.speed;e.damage=damage;e.radius=w.radius;
  var sprite=friendly?art.pulse:art.hostileOrb;
  float visual=size??Mathf.Max(.27f,w.visualSize);
  Color color=tint??(friendly?ActiveShip.engineColor:new Color(1,.4f,.19f));
  e.Visual(sprite?sprite:w.projectile,visual,color,6);e.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
 }
 void TickWaves(float dt){
  if(bossSpawned)return;
  spawnTimer-=dt;if(spawnTimer>0)return;
  if(waitingForClear){
   foreach(var e in enemies.Items)if(e.gameObject.activeSelf)return;
   waitingForClear=false;waveIndex++;spawned=0;
   spawnTimer=waveIndex<stage.waves.Length?stage.waves[waveIndex].delay:stage.bossDelay;return;
  }
  if(waveIndex>=stage.waves.Length){if(SpawnEnemy(stage.boss,0)){bossSpawned=true;WaveLabel="WARNING / " + stage.boss.displayName;Audio.Play(art.bomb);}return;}
  Wave w=stage.waves[waveIndex];WaveLabel="WAVE "+(waveIndex+1).ToString("00")+" / "+stage.waves.Length.ToString("00");
  float fraction=w.count<=1?.5f:(float)spawned/(w.count-1);
  float x=w.formation==1?Mathf.Sin(fraction*Mathf.PI*2)*w.width:w.formation==2?(spawned%2==0?-w.width:w.width):Mathf.Lerp(-w.width,w.width,fraction);if(w.alternateSides&&spawned%2==1)x=-x;
  if(!SpawnEnemy(w.enemy,x))return;
  spawned++;spawnTimer=w.spawnInterval;if(spawned>=w.count)waitingForClear=true;
 }
 bool SpawnEnemy(EnemyData d,float x){
  if(!d)return false;var e=enemies.Take();if(!e)return false;
  if(d.flight==Flight.Boss&&weaponTier<balance.bossWeaponTier){weaponTier=balance.bossWeaponTier;weaponTimer=0;}
  e.kind=EntityKind.Enemy;e.enemy=d;e.Position=new Vector2(x,balance.spawnY);e.homeX=x;
  e.hp=e.maxHp=d.flight==Flight.Boss?BossHealth(d):d.health*stage.healthScale*ngScale;e.radius=d.radius;
  e.shotTimer=d.flight==Flight.Boss?d.fireInterval:d.fireInterval/(stage.fireRateScale*ngScale);e.phase=Random.Range(0,Mathf.PI*2);e.Visual(d.sprite,d.visualSize,d.tint,4);if(d.flight!=Flight.Boss)e.transform.rotation=Quaternion.Euler(0,0,180);
  if(d.flight==Flight.Boss){bossEntity=e;e.bossStage=1;e.fightTime=0;e.beamTell=e.beamTellMax=e.beamActive=0;e.beamTimer=balance.bossBeamDelay;e.specialTimer=e.specialTell=0;e.bossEntered=false;CreateBossBeam();}
  return true;
 }
 float DpsOf(int tier){
  var w=ActiveShip.weapon;int count=Mathf.Clamp(tier,1,w.maximumTier);
  float damage=w.damage*(1+Save.Value.upgrades[0]*ActiveShip.damagePerLevel);
  float interval=w.interval/(1+Save.Value.upgrades[1]*ActiveShip.fireRatePerLevel);
  return count*damage/Mathf.Max(.01f,interval);
 }
 float BossHealth(EnemyData d){
  // Authored HP targets 38 seconds for the ship the content was built around. Scale it to the
  // active ship so every hull gets the same reference fight length, then cap under-geared runs.
  int world=BossTuning.WorldOf(StageIndex);
  float authored=d.health*ngScale;
  float authoredDps=d.referenceDps>0f?d.referenceDps:BossTuning.ReferenceDps(catalog.ships!=null&&catalog.ships.Length>0?catalog.ships[0]:null,world);
  authored=BossTuning.HealthForShip(authored,authoredDps,ActiveShip,world);
  float cap=DpsOf(weaponTier)*balance.bossMaxFightSeconds;
  return Mathf.Max(1f,Mathf.Min(authored,cap));
 }
 void TickEnemies(float dt){
  foreach(var e in enemies.Items){
   if(!e.gameObject.activeSelf)continue;var d=e.enemy;e.age+=dt;e.view.color=Color.Lerp(e.view.color,d.tint,dt*14);Vector2 p=e.Position;
   if(d.flight==Flight.Boss){
    int phase=Mathf.Clamp(e.bossStage<=0?1:e.bossStage,1,3);
    bool enraged=e.fightTime>=balance.bossEnrageSeconds;
    float moveScale=(phase==1?1f:phase==2?1.15f:1.35f)*(enraged?balance.bossEnrageMoveScale:1f);
    float holdY=d.holdY-(phase-1)*balance.bossPhaseDescent;
    Vector2 target=BossMoveTarget(d,phase,e.age,holdY);
    // The boss commits to its own lane for the beam; it never chases the player.
    if(e.beamTell>0||e.beamActive>0)target=p;
    float moveSpeed=balance.bossMoveSpeed*BossTuning.MotionSpeed(d.pattern)*moveScale;
    // Drop in quickly, then hover with only subtle vertical bob.
    float verticalSpeed=e.bossEntered?moveSpeed*.6f:balance.bossMoveSpeed*1.5f;
    p.x=Mathf.MoveTowards(p.x,target.x,moveSpeed*dt);
    p.y=Mathf.MoveTowards(p.y,target.y,verticalSpeed*dt);
    BossFraction=Mathf.Max(0,e.hp/e.maxHp);
   }
   else if(d.flight==Flight.Turret){p.y=Mathf.MoveTowards(p.y,d.holdY,d.speed*stage.speedScale*ngScale*dt);}
   else if(d.flight==Flight.Kamikaze){p=Vector2.MoveTowards(p,player.Position,d.speed*stage.speedScale*ngScale*dt);}
   else{p.y-=d.speed*stage.speedScale*ngScale*dt;if(d.flight==Flight.Weaver)p.x=e.homeX+Mathf.Sin(e.age*d.sineFrequency+e.phase)*d.sineAmplitude;}
   p.x=Mathf.Clamp(p.x,-balance.halfWidth+e.radius,balance.halfWidth-e.radius);e.Position=p;
   if(p.y<balance.despawnY){EntityPool.Return(e);continue;}
   if(d.flight==Flight.Boss){
    int phase=PhaseFor(e.hp/e.maxHp);
    if(e.bossStage!=phase&&e.beamTell<=0&&e.beamActive<=0){e.bossStage=phase;e.phaseTell=1.25f;e.shotTimer=1.65f;e.volley=0;e.beamTimer=Mathf.Max(e.beamTimer,balance.bossBeamDelay*.5f);e.specialTimer=phase==2?4f:3f;e.specialTell=0;
     foreach(var bullet in bullets.Items)if(bullet.gameObject.activeSelf&&bullet.kind==EntityKind.EnemyBullet)EntityPool.Return(bullet);
     Audio.Play(art.enemyFire);
    }
    e.phaseTell=Mathf.Max(0,e.phaseTell-dt);BossTransition=e.phaseTell>0;
    if(!e.bossEntered&&p.y<=d.holdY+.4f)e.bossEntered=true;
    // Entry and phase tells pause attacks; damage still counts throughout.
    if(!e.bossEntered||e.phaseTell>0){BossCharge=0;UpdateBossBeam(e);continue;}
    e.fightTime+=dt;
   }
   e.shotTimer-=dt;
   if(d.flight!=Flight.Boss&&e.shotTimer<.25f)e.view.color=Color.Lerp(d.tint,new Color(1,.65f,.25f),.45f);
   if(d.flight==Flight.Boss){
    bool enraged=e.fightTime>=balance.bossEnrageSeconds;
    BossCharge=Mathf.InverseLerp(.4f,0,e.shotTimer);
    e.view.color=Color.Lerp(e.view.color,new Color(1,.65f,.42f),BossCharge*.45f);
    if(enraged)e.view.color=Color.Lerp(e.view.color,art.danger,.2f+.12f*Mathf.Sin(Time.time*10f));
    TickBossBeam(e,dt);
    TickBossSpecial(e,Mathf.Clamp(e.bossStage<=0?1:e.bossStage,1,3),dt);
    if(e.specialTell>0){BossCharge=Mathf.Max(BossCharge,1f-e.specialTell/.7f);e.view.color=Color.Lerp(e.view.color,art.danger,.55f);}
    if(State!=GameState.Playing||!e.gameObject.activeSelf)continue;
    p=e.Position;
   }
   if(d.flight==Flight.Turret && e.age>12){p.y-=d.speed*dt*3;e.Position=p;if(e.age>17){EntityPool.Return(e);continue;}}
   // The beam is the boss's whole focus: hold bullet fire while it charges and fires,
   // then give the player a short beat before the next volley.
   bool beamBusy=d.flight==Flight.Boss&&(e.beamTell>0||e.beamActive>0);
   bool specialBusy=d.flight==Flight.Boss&&e.specialTell>0;
   if(beamBusy||specialBusy)e.shotTimer=Mathf.Max(e.shotTimer,.5f);
   if(e.shotTimer<=0&&!beamBusy&&!specialBusy&&p.y<balance.halfHeight-.5f&&d.weapon){
    bool boss=d.flight==Flight.Boss;
    int phase=boss?Mathf.Clamp(e.bossStage,1,3):1;
    bool enraged=boss&&e.fightTime>=balance.bossEnrageSeconds;
    float interval=boss?d.fireInterval*(phase>=2?d.bossPhaseFireMultiplier:1f)*(phase>=3?.9f:1f)/Mathf.Max(1f,ngScale):d.fireInterval/(stage.fireRateScale*ngScale);
    if(enraged)interval*=balance.bossEnrageFireScale;
    e.shotTimer=interval;
    int shots=d.shotCount+(phase>=2?d.bossPhaseExtraShots:0);
    Vector2 aim=(player.Position-p).normalized;
    if(boss){FireBoss(e,shots);e.volley++;if(e.volley%3==0)e.shotTimer+=(phase>=3?.8f:1.1f);}
    else for(int i=0;i<shots;i++){float angle=(i-(shots-1)*.5f)*d.spreadAngle;SpawnBullet(p+Vector2.down*e.radius,Quaternion.Euler(0,0,angle)*aim,d.weapon,false,d.weapon.damage);}
   }
   if((p-player.Position).sqrMagnitude<Mathf.Pow(e.radius+ActiveShip.radius,2)){
    DamagePlayer(d.flight==Flight.Boss?balance.bossContactDamage*BossHazardScale:d.contactDamage);if(State!=GameState.Playing)return;if(d.flight!=Flight.Boss)KillEnemy(e);
   }
  }
 }
 void TickBullets(float dt){
  foreach(var b in bullets.Items){
   if(!b.gameObject.activeSelf)continue;b.Position+=b.velocity*dt;
   if(Mathf.Abs(b.Position.x)>balance.halfWidth+1||Mathf.Abs(b.Position.y)>balance.halfHeight+2){EntityPool.Return(b);continue;}
   if(b.kind==EntityKind.PlayerBullet){
    foreach(var e in enemies.Items){if(!e.gameObject.activeSelf)continue;
     if((e.Position-b.Position).sqrMagnitude>Mathf.Pow(e.radius+b.radius,2))continue;
     e.hp-=b.damage;e.view.color=new Color(2,2,2,1);Burst(b.Position,art.accent,balance.hitParticles);EntityPool.Return(b);
     if(e.hp<=0)KillEnemy(e);break;
    }
   }else if((b.Position-player.Position).sqrMagnitude<Mathf.Pow(b.radius+ActiveShip.radius,2)){EntityPool.Return(b);DamagePlayer(b.damage);}
   if(State!=GameState.Playing)return;
  }
 }
 void FireBoss(Entity e,int shots){
  var d=e.enemy;Vector2 origin=e.Position+Vector2.down*e.radius;
  int phase=Mathf.Clamp(e.bossStage<=0?1:e.bossStage,1,3);
  float sweep=Mathf.Sin(e.age*(.9f+(phase-1)*.25f))*50;
  float damage=balance.bossBulletDamage*BossHazardScale;
  float speedScale=1f+(phase-1)*.08f+(e.fightTime>=balance.bossEnrageSeconds?.05f:0f);
  for(int i=0;i<shots;i++){
   float angle=(i-(shots-1)*.5f)*d.spreadAngle;
   Vector2 direction=Vector2.down;Vector2 p=origin;
   // Each boss keeps its signature pattern in every phase; phases add speed and density.
   switch(d.pattern){
    case BossPattern.Spiral:angle=i*360f/shots+e.age*(38f+(phase-1)*14f);direction=Vector2.down;break;
    case BossPattern.TwinSweep:p.x+=(i%2==0?-1:1)*.65f;angle=sweep+(i-(shots-1)*.5f)*(12f+(phase-1)*3f);direction=Vector2.down;break;
    case BossPattern.Ring:angle=i*360f/shots+(phase>=2?e.age*(13f+(phase-2)*8f):0);direction=Vector2.down;break;
    case BossPattern.Crossfire:p.x+=(i%2==0?-1:1)*1.1f;angle=(i%2==0?-1:1)*(25f+(phase-1)*8f)+(i-shots/2)*6;direction=Vector2.down;break;
    case BossPattern.Alternating:angle+=(Mathf.FloorToInt(e.age*(1f+(phase-1)*.3f))%2==0?22:-22);direction=Vector2.down;break;
    case BossPattern.Rain:p.x=Mathf.Lerp(-3.2f,3.2f,(float)i/Mathf.Max(1,shots-1));angle=Mathf.Sin(e.age*(1f+(phase-1)*.5f))*12;direction=Vector2.down;break;
    case BossPattern.Vortex:angle=i*360f/shots-e.age*(25f+(phase-1)*10f);direction=Vector2.down;break;
   }
   SpawnBullet(p,Quaternion.Euler(0,0,angle)*direction*speedScale,d.weapon,false,damage,art.danger,.34f);
  }
 }
  // Each boss family owns a patrol shape. None of them read the player's position:
  // the player reads the boss instead of the boss mirroring the player.
  Vector2 BossMoveTarget(EnemyData d,int phase,float age,float holdY){
   float scale=phase==1?1f:phase==2?1.08f:1.16f;
   float x=0,y=holdY;
   switch(d.pattern){
    case BossPattern.Fan:x=Mathf.Sin(age*.55f)*2.2f;break;
    case BossPattern.Spiral:x=Mathf.Sin(age*.75f+1.1f)*2.6f;y=holdY+Mathf.Abs(Mathf.Cos(age*.75f+1.1f))*.22f;break;
    case BossPattern.TwinSweep:x=Mathf.Sin(age*.6f+2.2f)*2.9f;break;
    case BossPattern.Ring:x=Mathf.Sin(age*.35f+3.3f)*1.2f;y=holdY+Mathf.Abs(Mathf.Sin(age*.7f+3.3f))*.16f;break;
    case BossPattern.Crossfire:x=Mathf.Sin(age*1.15f+4.4f)*2.7f;break;
    case BossPattern.Alternating:x=Mathf.Sin(age*.7f+5.5f)*2.4f;y=holdY+Mathf.Abs(Mathf.Sin(age*1.4f+5.5f))*.2f;break;
    case BossPattern.Rain:x=Mathf.Sin(age*.4f+6.6f)*1.6f;break;
    case BossPattern.Vortex:x=Mathf.Cos(age*.8f+7.7f)*2.3f;y=holdY+Mathf.Abs(Mathf.Sin(age*.8f+7.7f))*.28f;break;
   }
   return new Vector2(Mathf.Clamp(x*scale,-balance.halfWidth+d.radius,balance.halfWidth-d.radius),y);
  }
  // Phase two adds one authored signature burst per boss family. It is telegraphed by
  // the boss turning danger-red, then the pattern fires once and goes on cooldown.
  void TickBossSpecial(Entity e,int phase,float dt){
   if(phase<2)return;
   if(e.specialTell>0){
    e.specialTell-=dt;
    if(e.specialTell<=0){
     FireBossSpecial(e,phase);
     bool enraged=e.fightTime>=balance.bossEnrageSeconds;
     e.specialTimer=(phase>=3?7.5f:10.5f)*(enraged?.75f:1f);
     e.shotTimer=Mathf.Max(e.shotTimer,.9f);
    }
    return;
   }
   if(e.beamTell>0||e.beamActive>0)return;
   e.specialTimer-=dt;
   if(e.specialTimer<=0)e.specialTell=.7f;
  }
  void FireBossSpecial(Entity e,int phase){
   var d=e.enemy;if(!d||!d.weapon)return;
   float damage=balance.bossBulletDamage*BossHazardScale;
   float speed=1.1f+(phase-2)*.06f;
   Vector2 origin=e.Position+Vector2.down*e.radius;
   switch(d.pattern){
    case BossPattern.Fan:
     for(int i=0;i<11;i++){float a=(i-5)*8f;SpawnBullet(origin,Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
    case BossPattern.Spiral:
     for(int ring=0;ring<2;ring++)for(int i=0;i<10;i++){float a=i*36f+(ring==0?1f:-1f)*e.age*45f;SpawnBullet(origin,Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
    case BossPattern.TwinSweep:
     for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++){float a=(i-2)*7f;Vector2 p=origin+new Vector2(side*.8f,0);SpawnBullet(p,Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
    case BossPattern.Ring:
     for(int i=0;i<18;i++){float a=i*20f+(phase>=3?e.age*18f:0f);SpawnBullet(origin,Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
    case BossPattern.Crossfire:
     for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++){float a=side*(18f+i*8f);float x=Mathf.Clamp(e.Position.x+side*(1.1f+i*.1f),-balance.halfWidth+.3f,balance.halfWidth-.3f);SpawnBullet(new Vector2(x,origin.y),Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
    case BossPattern.Alternating:
     for(int i=0;i<15;i++){float a=((i%3)-1)*30f+(i/3-2)*4f;SpawnBullet(origin,Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
    case BossPattern.Rain:
     for(int i=0;i<12;i++){float x=Mathf.Lerp(-balance.halfWidth+1f,balance.halfWidth-1f,(i+.5f)/12f);float a=Mathf.Sin(e.age+i)*6f;SpawnBullet(new Vector2(x,balance.halfHeight-.4f),Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
    case BossPattern.Vortex:
     for(int i=0;i<20;i++){float a=i*18f-e.age*40f;SpawnBullet(origin,Quaternion.Euler(0,0,a)*Vector2.down*speed,d.weapon,false,damage,art.danger,.38f);}
     break;
   }
   Audio.Play(art.enemyFire);
  }
  float BeamWidthOf(EnemyData d){return balance.bossBeamWidth*(d?BossTuning.BeamWidthScale(d.pattern):1f);}
  float BeamTelegraphOf(EnemyData d){return balance.bossBeamTelegraph*(d?BossTuning.BeamTelegraphScale(d.pattern):1f);}
  float BeamIntervalOf(EnemyData d){return balance.bossBeamInterval*(d?BossTuning.BeamIntervalScale(d.pattern):1f);}
 // Boss beam: a telegraphed vertical lance. The boss commits to the lane while charging,
 // so the player can read the warning and step out before it fires.
 void TickBossBeam(Entity e,float dt){
  if(!bossBeamRoot)return;
  bool enraged=e.fightTime>=balance.bossEnrageSeconds;
  if(e.beamTell>0){
   e.beamTell-=dt;
   if(e.beamTell<=0){e.beamActive=balance.bossBeamDuration;e.beamHit=false;Audio.Play(art.bomb);}
  }else if(e.beamActive>0){
   e.beamActive-=dt;
   float half=BeamWidthOf(e.enemy)*.5f+ActiveShip.radius;
   if(!e.beamHit&&player.Position.y<e.Position.y&&Mathf.Abs(player.Position.x-e.Position.x)<=half){
    e.beamHit=true;DamagePlayer(balance.bossBeamDamage*BossHazardScale);
   }
   if(e.beamActive<=0){e.beamActive=0;e.beamTellMax=0;e.beamTimer=BeamIntervalOf(e.enemy)*(enraged?.75f:1f);}
  }else{
   e.beamTimer-=dt;
   if(e.beamTimer<=0){
    e.beamTellMax=BeamTelegraphOf(e.enemy)*(enraged?.8f:1f);e.beamTell=e.beamTellMax;
    Audio.Play(art.enemyFire);
   }
  }
  UpdateBossBeam(e);
 }
 void CreateBossBeam(){
  if(bossBeamRoot||!art.rayCore||!art.rayCap)return;
  var go=new GameObject("Boss Beam");go.transform.SetParent(transform,false);bossBeamRoot=go;
  bossBeamCore=MakeSpritePart(go,"Core",art.rayCore,art.danger);
  bossBeamCap=MakeSpritePart(go,"Cap",art.rayCap,art.danger);
  bossBeamCore.sortingOrder=bossBeamCap.sortingOrder=5;
  go.SetActive(false);
 }
 void DestroyBossBeam(){if(bossBeamRoot){Destroy(bossBeamRoot);bossBeamRoot=null;bossBeamCore=bossBeamCap=null;}}
 void UpdateBossBeam(Entity e){
  if(!bossBeamRoot)return;
  bool show=e.beamTell>0||e.beamActive>0;
  if(bossBeamRoot.activeSelf!=show)bossBeamRoot.SetActive(show);
  if(!show||!bossBeamCore||!bossBeamCore.sprite||!bossBeamCap||!bossBeamCap.sprite)return;
  float top=e.Position.y-e.radius*.55f,bottom=-balance.halfHeight-.2f;
  float beamWidth=BeamWidthOf(e.enemy);
  float width=beamWidth;
  if(e.beamTell>0){
   float charge=1f-Mathf.Clamp01(e.beamTell/Mathf.Max(.01f,e.beamTellMax));
   width=Mathf.Lerp(.16f,beamWidth,charge);
  }
  float height=Mathf.Max(.1f,top-bottom);
  bossBeamCore.transform.position=new Vector3(e.Position.x,(bottom+top)*.5f,0);
  bossBeamCore.transform.localScale=new Vector3(width/bossBeamCore.sprite.bounds.size.x,height/bossBeamCore.sprite.bounds.size.y,1);
  float capScale=width/bossBeamCap.sprite.bounds.size.x;
  bossBeamCap.transform.position=new Vector3(e.Position.x,top,0);
  bossBeamCap.transform.localScale=new Vector3(capScale,capScale,1);
  Color color=e.beamTell>0?art.danger:new Color(1,.95f,.85f,.95f);
  if(e.beamTell>0)color.a=.35f+.35f*Mathf.Abs(Mathf.Sin(Time.time*24f));
  bossBeamCore.color=color;bossBeamCap.color=color;
 }
 void Exhaust(){
  if(Save.Value.reducedEffects)return;
  for(int i=0;i<2;i++){var e=particles.Take();if(!e)return;e.kind=EntityKind.Particle;e.Position=player.Position+new Vector2(i==0?-.19f:.19f,-.48f);e.velocity=new Vector2(Random.Range(-.08f,.08f),-2.5f);e.life=.18f;e.Visual(art.flame?art.flame:art.particle,.32f,ActiveShip.engineColor,9);}
 }
 public bool SelectShip(int index){
  if(State==GameState.Playing||State==GameState.Paused||catalog.ships==null||index<0||index>=catalog.ships.Length)return false;
  bool ok=Save.SelectOrBuyShip(index,catalog.ships[index].unlockCost);if(ok)Audio.Play(art.pickup);return ok;
 }
 void DamagePlayer(float damage){
  if(hitTimer>0||bubbleTimer>0||State!=GameState.Playing)return;
  Combo=0;comboTimer=0;
  hitTimer=balance.hitInvulnerability;regenTimer=ActiveShip.shieldRegenDelay;shakeTimer=balance.shakeDuration;
  float absorbed=Mathf.Min(Shield,damage);Shield-=absorbed;Hull=Mathf.Max(0,Hull-(damage-absorbed));
  View.HitFlash();
  Burst(player.Position,art.danger,balance.hitParticles);Audio.Play(art.playerHit);
  if(Hull<=0){Burst(player.Position,art.accent,balance.explosionParticles);player.gameObject.SetActive(false);Finish(false);}
 }
 void KillEnemy(Entity e){
  if(!e.gameObject.activeSelf)return;
  var d=e.enemy;Vector2 pos=e.Position;EntityPool.Return(e);
  Combo=Mathf.Min(Combo+1,balance.comboMax);comboTimer=balance.comboWindow;
  Score+=Mathf.RoundToInt(d.score*(multiplierTimer>0?balance.scoreMultiplier:1)*ComboBonus);RunCoins++;
  Explosion(pos,d.flight==Flight.Boss?1.7f:.65f);
  Burst(pos,new Color(1,.62f,.2f),balance.explosionParticles);Shockwave(pos,d.flight==Flight.Boss?2:1);Audio.Play(art.explosion);
  if(d.flight==Flight.Boss){Burst(pos,Color.white,balance.explosionParticles*2);BossFraction=0;DestroyBossBeam();bossEntity=null;Finish(true);return;}
  if(Random.value<d.dropChance)Drop(pos);
 }
 void Drop(Vector2 pos){
  int total=0;foreach(int w in balance.dropWeights)total+=Mathf.Max(0,w);if(total<=0)return;
  int roll=Random.Range(0,total),type=0;
  for(int i=0;i<balance.dropWeights.Length;i++){roll-=Mathf.Max(0,balance.dropWeights[i]);if(roll<0){type=i;break;}}
  var e=pickups.Take();if(!e)return;e.kind=EntityKind.Pickup;e.Position=pos;e.pickup=type;e.radius=balance.pickupRadius;
  e.Visual(balance.pickupSprites[type],balance.pickupSize,type==2?NeonOf(ActiveShip.engineColor):Color.white,8);
 }
 void TickPickups(float dt){
  foreach(var e in pickups.Items){if(!e.gameObject.activeSelf)continue;
   float distance=Vector2.Distance(e.Position,player.Position);
   if(magnetTimer>0&&distance<balance.magnetRadius)e.Position=Vector2.MoveTowards(e.Position,player.Position,balance.magnetSpeed*dt);
   else e.Position+=Vector2.down*balance.pickupFallSpeed*dt;
   if(Vector2.Distance(e.Position,player.Position)<balance.collectRadius+e.radius){
    switch(e.pickup){
     case 0:weaponTier=Mathf.Min(ActiveShip.weapon.maximumTier,weaponTier+1);weaponTimer=balance.weaponDuration;break;
     case 1:bubbleTimer=balance.shieldDuration;Shield=MaxShield;break;
     case 2:Bombs=Mathf.Min(balance.bombCap,Bombs+1);break;
     case 3:weaponTimer=balance.weaponDuration;weaponTier=Mathf.Min(ActiveShip.weapon.maximumTier,weaponTier+1);break;
     case 4:RunGems+=balance.gemPickup;break;
     case 5:magnetTimer=balance.magnetDuration;break;
     case 6:multiplierTimer=balance.multiplierDuration;break;
    }
    Audio.Play(art.pickup);Burst(e.Position,art.accent,balance.hitParticles);EntityPool.Return(e);
   }else if(e.Position.y<balance.despawnY)EntityPool.Return(e);
  }
 }
 // Particle Ray Gun: a channeled 3-4 second beam straight ahead of the ship. Consumes one charge.
 public void ActivateRayGun(){
  if(State!=GameState.Playing||Bombs<=0||rayTimer>0)return;
  Bombs--;RayGunsUsed++;Audio.Play(art.bomb);flashTimer=balance.bombFlashDuration*.6f;shakeTimer=Mathf.Max(shakeTimer,balance.shakeDuration*.5f);
  rayTimer=balance.rayDuration;rayParticleAccumulator=0;rayCrackleTimer=0;
  rayNeon=NeonOf(ActiveShip.engineColor);
  if(art.rayCore&&art.rayCap){
   var go=new GameObject("Particle Ray Gun");go.transform.SetParent(transform,false);rayBeam=go;
   rayCoreView=MakeSpritePart(go,"Core",art.rayCore,rayNeon);
   rayCapTopView=MakeSpritePart(go,"Cap top",art.rayCap,rayNeon);
   rayCapBottomView=MakeSpritePart(go,"Cap bottom",art.rayCap,rayNeon);
   rayCapBottomView.transform.localRotation=Quaternion.Euler(0,0,180f);
   PositionRayBeam();
  }
 }
 SpriteRenderer MakeSpritePart(GameObject parent,string name,Sprite sprite,Color tint){
  var part=new GameObject(name);part.transform.SetParent(parent.transform,false);
  var sr=part.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=7;sr.color=tint;return sr;
 }
 void PositionRayBeam(){
  if(!rayBeam)return;
  Vector2 p=player.Position;float bottom=p.y+.5f,top=balance.halfHeight+1.2f,h=Mathf.Max(.1f,top-bottom);
  float w=balance.rayWidth,stretch=1f+Mathf.Sin(Time.time*22f)*.12f;
  rayCoreView.transform.position=new Vector3(p.x,(bottom+top)*.5f,0);
  rayCoreView.transform.localScale=new Vector3(w*stretch/rayCoreView.sprite.bounds.size.x,h/rayCoreView.sprite.bounds.size.y,1);
  float capScale=w*stretch/rayCapTopView.sprite.bounds.size.x;
  rayCapTopView.transform.position=new Vector3(p.x,top,0);
  rayCapTopView.transform.localScale=new Vector3(capScale,capScale,1);
  rayCapBottomView.transform.position=new Vector3(p.x,bottom,0);
  rayCapBottomView.transform.localScale=new Vector3(capScale,capScale,1);
 }
 // Bright neon hue of anything the player owns, e.g. the ray gun matching the fighter.
 public static Color NeonOf(Color c){Color.RGBToHSV(c,out float h,out float s,out _);return Color.HSVToRGB(h,Mathf.Min(1f,s*1.15f+.12f),1f);}
 void TickRay(float dt){
  rayTimer-=dt;
  Vector2 p=player.Position;
  PositionRayBeam();
  if(rayBeam){float a=.7f+.25f*Mathf.Abs(Mathf.Sin(Time.time*18f));var c=rayNeon;c.a=a;rayCoreView.color=c;rayCapTopView.color=c;rayCapBottomView.color=c;}
  // The beam melts everything in its lane: enemies take continuous damage, hostile fire is burned away.
  foreach(var e in enemies.Items){
   if(!e.gameObject.activeSelf)continue;
   if(e.Position.y<=p.y-.2f)continue;
   if(Mathf.Abs(e.Position.x-p.x)>balance.rayWidth*.5f+e.radius)continue;
   e.hp-=balance.rayDamagePerSecond*dt;
   if(Random.value<dt*22f)Burst(e.Position,art.accent,2);
   if(e.hp<=0)KillEnemy(e);
   if(State!=GameState.Playing)return;
  }
  foreach(var b in bullets.Items){
   if(!b.gameObject.activeSelf||b.kind!=EntityKind.EnemyBullet)continue;
   if(b.Position.y<p.y)continue;
   if(Mathf.Abs(b.Position.x-p.x)>balance.rayWidth*.6f)continue;
   EntityPool.Return(b);
   if(Random.value<.4f)Burst(b.Position,art.accent,2);
  }
  rayParticleAccumulator+=dt*balance.rayParticleRate;
  int n=Mathf.FloorToInt(rayParticleAccumulator);rayParticleAccumulator-=n;
  for(int i=0;i<n;i++){var e=particles.Take();if(!e)break;e.kind=EntityKind.Particle;e.Position=p+new Vector2(Random.Range(-.18f,.18f),.5f);e.velocity=new Vector2(Random.Range(-1.4f,1.4f),Random.Range(24f,34f));e.life=.32f;e.Visual(art.pulse?art.pulse:art.particle,Random.Range(.16f,.3f),i%3==0?Color.white:(i%3==1?rayNeon:Color.Lerp(rayNeon,Color.white,.45f)),12);}
  rayCrackleTimer-=dt;if(rayCrackleTimer<=0){Audio.Play(art.enemyFire);rayCrackleTimer=.7f;}
  if(rayTimer<=0)EndRay();
 }
 void EndRay(){rayTimer=0;if(rayBeam){Destroy(rayBeam);rayBeam=null;rayCoreView=rayCapTopView=rayCapBottomView=null;}}
 void Burst(Vector2 p,Color color,int count){for(int i=0;i<count;i++){var e=particles.Take();if(!e)break;e.kind=EntityKind.Particle;e.Position=p;e.velocity=Random.insideUnitCircle*balance.particleSpeed;e.life=balance.particleLife;e.Visual(art.particle,balance.particleSize,color,12);}}
 void Shockwave(Vector2 position,float size){var e=particles.Take();if(!e)return;e.kind=EntityKind.Particle;e.Position=position;e.life=.38f;e.pickup=1;e.Visual(art.shieldRing,size,new Color(1,.75f,.3f),11);}
 void Explosion(Vector2 p,float size){if(!art.blast)return;var e=particles.Take();if(!e)return;e.kind=EntityKind.Particle;e.Position=p;e.life=.24f;e.pickup=1;e.Visual(art.blast,size,new Color(1,.65f,.22f),12);}
 void TickParticles(float dt){foreach(var e in particles.Items){if(!e.gameObject.activeSelf)continue;e.age+=dt;e.Position+=e.velocity*dt;if(e.pickup==1)e.transform.localScale*=1+dt*5;Color c=e.view.color;c.a=Mathf.Clamp01(1-e.age/e.life);e.view.color=c;if(e.age>=e.life)EntityPool.Return(e);}}
 void TickStars(float dt){foreach(var e in stars.Items){e.view.enabled=backdrop.ShowStars;e.Position+=Vector2.down*balance.starSpeed*e.phase*dt;if(e.Position.y<-balance.halfHeight)e.Position=new Vector2(Random.Range(-balance.halfWidth,balance.halfWidth),balance.halfHeight);}}
 public void TogglePause(){InputReader.ResetDrag();if(State==GameState.Playing){State=GameState.Paused;View.Pause();}else if(State==GameState.Paused){State=GameState.Playing;View.Gameplay();}}
 void OnApplicationPause(bool paused){if(paused&&State==GameState.Playing)TogglePause();if(paused&&Save!=null)Save.Write();}
 void OnApplicationFocus(bool focus){if(!focus&&State==GameState.Playing)TogglePause();}
 void Finish(bool won){
  if(State!=GameState.Playing)return;Won=won;State=GameState.Results;Combo=0;comboTimer=0;EndRay();DestroyBossBeam();bossEntity=null;
  Stars=won?(Hull/ActiveShip.hull>=balance.threeStarHull?3:Hull/ActiveShip.hull>=balance.twoStarHull?2:1):0;
  if(won){Save.Value.unlocked=Mathf.Min(catalog.stages.Length,Mathf.Max(Save.Value.unlocked,StageIndex+2));Save.Value.stars[StageIndex]=Mathf.Max(Save.Value.stars[StageIndex],Stars);Audio.Play(art.victory);}

  if(won&&Save.Value.ngPlus>0)Save.Value.gems+=balance.ngPlusRewardGems;Save.Value.coins+=RunCoins;Save.Value.gems+=RunGems;Save.Value.bestScore=Mathf.Max(Save.Value.bestScore,Score);Save.Write();
  bullets.Clear();pickups.Clear();enemies.Clear();player.gameObject.SetActive(false);ring.gameObject.SetActive(false);cam.transform.position=cameraOrigin;View.Results();Ads.RunFinished();
 }
 public bool Upgrade(int index){
  if(index<0||index>=5)return false;int level=Save.Value.upgrades[index];int cost=balance.UpgradeCost(level);
  if(level>=balance.upgradeMaxLevel||Save.Value.coins<cost)return false;
  Save.Value.coins-=cost;Save.Value.upgrades[index]++;Save.Write();Audio.Play(art.pickup);return true;
 }
 public bool FirstRewardedAdToday => Save==null || Save.Value.rewardedBonusDate != System.DateTime.UtcNow.ToString("yyyy-MM-dd");
 // First rewarded ad of the day pays double; later ones pay +50%. Prevents ad-gated progression.
 public void DoubleReward(){
  if(State!=GameState.Results||RewardClaimed||!Ads.RewardReady)return;
  int serial=runSerial;bool first=FirstRewardedAdToday;int amount=first?RunCoins:Mathf.CeilToInt(RunCoins*.5f);
  Ads.ShowRewarded(ok=>{if(!ok)return;Save.Value.coins+=amount;if(first)Save.Value.rewardedBonusDate=System.DateTime.UtcNow.ToString("yyyy-MM-dd");Save.Write();if(serial==runSerial){RewardClaimed=true;if(State==GameState.Results)View.Results();}});
 }
 public void EnterNgPlus(){
  if(State!=GameState.Results)return;
  Save.Value.ngPlus++;Save.Write();ShowMenu();
 }
}
}

