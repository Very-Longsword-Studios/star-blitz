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
 public string PowerStatus => weaponTimer > 0 ? "OVERDRIVE " + weaponTier + "  /  " + Mathf.CeilToInt(weaponTimer) + "s" : bubbleTimer > 0 ? "SHIELD ACTIVE" : "";
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
 public int BossPhase => PhaseFor(BossFraction);
 public bool BossTransition {get;private set;}
 public int NukesUsed {get;private set;}
 public string WaveLabel {get;private set;}
 public bool RewardClaimed {get;private set;}
 EntityPool enemies,bullets,pickups,particles,stars;
 Entity player,ring;Camera cam;
 StageData stage;BalanceData balance;PresentationData art;
 float fireTimer,hitTimer,regenTimer,weaponTimer,bubbleTimer,magnetTimer,multiplierTimer,spawnTimer,shakeTimer,flashTimer;
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
 void ClearRun(){enemies.Clear();bullets.Clear();pickups.Clear();particles.Clear();ring.gameObject.SetActive(false);shakeTimer=0;flashTimer=0;cam.transform.position=cameraOrigin;}
 public void StartStage(int index){
  if(index<0||index>=catalog.stages.Length||index>=Save.Value.unlocked)return;
  ClearRun();InputReader.ResetDrag();Elapsed=0;supplyTimer=7;engineTimer=0;player.Visual(ActiveShip.sprite,ActiveShip.visualSize,Color.white,10);runSerial++;StageIndex=index;stage=catalog.stages[index];State=GameState.Playing;
  Score=RunCoins=RunGems=Stars=NukesUsed=0;Won=RewardClaimed=false;BossFraction=BossCharge=0;BossTransition=false;
  Hull=ActiveShip.hull;Shield=MaxShield;Bombs=Mathf.Min(balance.bombCap,ActiveShip.bombs+Save.Value.upgrades[4]);
  fireTimer=hitTimer=regenTimer=weaponTimer=bubbleTimer=magnetTimer=multiplierTimer=0;weaponTier=1;
  waveIndex=spawned=0;waitingForClear=bossSpawned=false;spawnTimer=stage.waves.Length>0?stage.waves[0].delay:stage.bossDelay;
  player.gameObject.SetActive(true);player.Position=new Vector2(0,balance.playerStartY);player.view.color=Color.white;
  cam.backgroundColor=stage.background;backdrop.SetWorld(index/36);Audio.Stage(index);View.Gameplay();WaveLabel="ENTERING " + stage.title;
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
  hitTimer-=dt;regenTimer-=dt;bubbleTimer-=dt;magnetTimer-=dt;multiplierTimer-=dt;weaponTimer-=dt;
  if(weaponTimer<=0)weaponTier=1;
  if(regenTimer<=0)Shield=Mathf.Min(MaxShield,Shield+ActiveShip.shieldRegen*dt);
  MovePlayer(dt);fireTimer-=dt;
  if(fireTimer<=0)FirePlayer();
  Elapsed+=dt;supplyTimer-=dt;if(supplyTimer<=0){Drop(new Vector2(Random.Range(-2.7f,2.7f),balance.halfHeight-.4f));supplyTimer=12;}
  engineTimer-=dt;if(engineTimer<=0){Exhaust();engineTimer=.035f;}
  if(InputReader.BombPressed)Bomb();
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
  p.y=Mathf.Clamp(p.y,balance.playerBottom,balance.playerTop);player.Position=p;
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
 void SpawnBullet(Vector2 position,Vector2 direction,WeaponData w,bool friendly,float damage){
  var e=bullets.Take();if(!e)return;
  e.kind=friendly?EntityKind.PlayerBullet:EntityKind.EnemyBullet;e.Position=position;e.velocity=direction*w.speed;e.damage=damage;e.radius=w.radius;
  var sprite=friendly?art.pulse:art.hostileOrb;
  e.Visual(sprite?sprite:w.projectile,friendly?.52f:Mathf.Max(.27f,w.visualSize),friendly?ActiveShip.engineColor:new Color(1,.4f,.19f),6);e.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
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
  e.kind=EntityKind.Enemy;e.enemy=d;e.Position=new Vector2(x,balance.spawnY);e.homeX=x;e.hp=e.maxHp=d.health*stage.healthScale;e.radius=d.radius;
  e.shotTimer=d.fireInterval/stage.fireRateScale;e.phase=Random.Range(0,Mathf.PI*2);e.Visual(d.sprite,d.visualSize,d.tint,4);if(d.flight!=Flight.Boss)e.transform.rotation=Quaternion.Euler(0,0,180);return true;
 }
 void TickEnemies(float dt){
  foreach(var e in enemies.Items){
   if(!e.gameObject.activeSelf)continue;var d=e.enemy;e.age+=dt;e.view.color=Color.Lerp(e.view.color,d.tint,dt*14);Vector2 p=e.Position;
   if(d.flight==Flight.Boss){p.y=Mathf.MoveTowards(p.y,d.holdY,d.speed*dt);p.x=Mathf.Sin(e.age*d.sineFrequency)*d.sineAmplitude;BossFraction=Mathf.Max(0,e.hp/e.maxHp);}
   else if(d.flight==Flight.Turret){p.y=Mathf.MoveTowards(p.y,d.holdY,d.speed*stage.speedScale*dt);}
   else if(d.flight==Flight.Kamikaze){p=Vector2.MoveTowards(p,player.Position,d.speed*stage.speedScale*dt);}
   else{p.y-=d.speed*stage.speedScale*dt;if(d.flight==Flight.Weaver)p.x=e.homeX+Mathf.Sin(e.age*d.sineFrequency+e.phase)*d.sineAmplitude;}
   p.x=Mathf.Clamp(p.x,-balance.halfWidth+e.radius,balance.halfWidth-e.radius);e.Position=p;
   if(p.y<balance.despawnY){EntityPool.Return(e);continue;}
   if(d.flight==Flight.Boss){
    int phase=PhaseFor(e.hp/e.maxHp);
    if(e.bossStage!=phase){e.bossStage=phase;e.phaseTell=1.25f;e.shotTimer=1.65f;e.volley=0;
     foreach(var bullet in bullets.Items)if(bullet.gameObject.activeSelf&&bullet.kind==EntityKind.EnemyBullet)EntityPool.Return(bullet);
     Audio.Play(art.enemyFire);
    }
    e.phaseTell=Mathf.Max(0,e.phaseTell-dt);BossTransition=e.phaseTell>0;
    // Entry and phase tells pause attacks; damage still counts throughout.
    if(p.y>d.holdY+.1f||e.phaseTell>0){BossCharge=0;continue;}
   }
   e.shotTimer-=dt;
   if(d.flight!=Flight.Boss&&e.shotTimer<.25f)e.view.color=Color.Lerp(d.tint,new Color(1,.65f,.25f),.45f);
   if(d.flight==Flight.Boss){BossCharge=Mathf.InverseLerp(.4f,0,e.shotTimer);e.view.color=Color.Lerp(e.view.color,new Color(1,.65f,.42f),BossCharge*.45f);}
   if(d.flight==Flight.Turret && e.age>12){p.y-=d.speed*dt*3;e.Position=p;if(e.age>17){EntityPool.Return(e);continue;}}
   if(e.shotTimer<=0&&p.y<balance.halfHeight-.5f&&d.weapon){
    bool phase2=d.flight==Flight.Boss&&e.bossStage>=2;
    e.shotTimer=d.fireInterval/stage.fireRateScale*(phase2?d.bossPhaseFireMultiplier:1);
    int shots=d.shotCount+(phase2?d.bossPhaseExtraShots:0);
    Vector2 aim=(player.Position-p).normalized;
    if(d.flight==Flight.Boss){FireBoss(e,shots,phase2);e.volley++;if(e.volley%3==0)e.shotTimer+=1.1f;}
    else for(int i=0;i<shots;i++){float angle=(i-(shots-1)*.5f)*d.spreadAngle;SpawnBullet(p+Vector2.down*e.radius,Quaternion.Euler(0,0,angle)*aim,d.weapon,false,d.weapon.damage);}
   }
   if((p-player.Position).sqrMagnitude<Mathf.Pow(e.radius+ActiveShip.radius,2)){
    DamagePlayer(d.contactDamage);if(State!=GameState.Playing)return;if(d.flight!=Flight.Boss)KillEnemy(e);
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
 void FireBoss(Entity e,int shots,bool phase2){
  var d=e.enemy;Vector2 origin=e.Position+Vector2.down*e.radius;
  float sweep=Mathf.Sin(e.age*.9f)*50;
  for(int i=0;i<shots;i++){
   float angle=(i-(shots-1)*.5f)*d.spreadAngle;
   Vector2 direction=(player.Position-origin).normalized;Vector2 p=origin;
   var pattern=e.bossStage==2?BossPattern.TwinSweep:e.bossStage==3?(e.volley%2==0?BossPattern.Ring:BossPattern.Fan):d.pattern;
   switch(pattern){
    case BossPattern.Spiral:angle=i*360f/shots+e.age*38;direction=Vector2.down;break;
    case BossPattern.TwinSweep:p.x+=(i%2==0?-1:1)*.65f;angle=sweep+(i-(shots-1)*.5f)*12;direction=Vector2.down;break;
    case BossPattern.Ring:angle=i*360f/shots+(phase2?e.age*13:0);direction=Vector2.down;break;
    case BossPattern.Crossfire:p.x+=(i%2==0?-1:1)*1.1f;angle=(i%2==0?-1:1)*25+(i-shots/2)*6;direction=Vector2.down;break;
    case BossPattern.Alternating:angle+=(Mathf.FloorToInt(e.age)%2==0?22:-22);direction=Vector2.down;break;
    case BossPattern.Rain:p.x=Mathf.Lerp(-3.2f,3.2f,(float)i/Mathf.Max(1,shots-1));angle=Mathf.Sin(e.age)*12;direction=Vector2.down;break;
    case BossPattern.Vortex:angle=i*360f/shots-e.age*25;direction=Vector2.down;break;
   }
   SpawnBullet(p,Quaternion.Euler(0,0,angle)*direction,d.weapon,false,d.weapon.damage);
  }
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
  hitTimer=balance.hitInvulnerability;regenTimer=ActiveShip.shieldRegenDelay;shakeTimer=balance.shakeDuration;
  float beforeHull=Hull,absorbed=Mathf.Min(Shield,damage);Shield-=absorbed;Hull=Mathf.Max(0,Hull-(damage-absorbed));
  View.DamageFeedback(player.Position,Mathf.CeilToInt(absorbed),Mathf.CeilToInt(beforeHull-Hull));
  Burst(player.Position,art.danger,balance.hitParticles);Audio.Play(art.playerHit);
  if(Hull<=0){Burst(player.Position,art.accent,balance.explosionParticles);player.gameObject.SetActive(false);Finish(false);}
 }
 void KillEnemy(Entity e){
  if(!e.gameObject.activeSelf)return;
  var d=e.enemy;Vector2 pos=e.Position;EntityPool.Return(e);
  Score+=d.score*(multiplierTimer>0?balance.scoreMultiplier:1);RunCoins++;
  View.CoinFeedback(pos);Explosion(pos,d.flight==Flight.Boss?1.7f:.65f);
  Burst(pos,new Color(1,.62f,.2f),balance.explosionParticles);Shockwave(pos,d.flight==Flight.Boss?2:1);Audio.Play(art.explosion);
  if(d.flight==Flight.Boss){Burst(pos,Color.white,balance.explosionParticles*2);BossFraction=0;Finish(true);return;}
  if(Random.value<d.dropChance)Drop(pos);
 }
 void Drop(Vector2 pos){
  int total=0;foreach(int w in balance.dropWeights)total+=Mathf.Max(0,w);if(total<=0)return;
  int roll=Random.Range(0,total),type=0;
  for(int i=0;i<balance.dropWeights.Length;i++){roll-=Mathf.Max(0,balance.dropWeights[i]);if(roll<0){type=i;break;}}
  var e=pickups.Take();if(!e)return;e.kind=EntityKind.Pickup;e.Position=pos;e.pickup=type;e.radius=balance.pickupRadius;
  e.Visual(balance.pickupSprites[type],balance.pickupSize,Color.white,8);
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
    View.PickupFeedback(e.Position,e.pickup);Audio.Play(art.pickup);Burst(e.Position,art.accent,balance.hitParticles);EntityPool.Return(e);
   }else if(e.Position.y<balance.despawnY)EntityPool.Return(e);
  }
 }
 public void Bomb(){
  if(State!=GameState.Playing||Bombs<=0)return;Bombs--;NukesUsed++;Audio.Play(art.bomb);shakeTimer=balance.shakeDuration;flashTimer=balance.bombFlashDuration;Shockwave(player.Position,3);Explosion(player.Position,1.2f);
  foreach(var b in bullets.Items)if(b.gameObject.activeSelf&&b.kind==EntityKind.EnemyBullet)EntityPool.Return(b);
  foreach(var e in enemies.Items){if(!e.gameObject.activeSelf)continue;e.hp-=balance.bombDamage;Burst(e.Position,art.accent,balance.hitParticles);if(e.hp<=0)KillEnemy(e);if(State!=GameState.Playing)break;}
 }
 void Burst(Vector2 p,Color color,int count){for(int i=0;i<count;i++){var e=particles.Take();if(!e)break;e.kind=EntityKind.Particle;e.Position=p;e.velocity=Random.insideUnitCircle*balance.particleSpeed;e.life=balance.particleLife;e.Visual(art.particle,balance.particleSize,color,12);}}
 void Shockwave(Vector2 position,float size){var e=particles.Take();if(!e)return;e.kind=EntityKind.Particle;e.Position=position;e.life=.38f;e.pickup=1;e.Visual(art.shieldRing,size,new Color(1,.75f,.3f),11);}
 void Explosion(Vector2 p,float size){if(!art.blast)return;var e=particles.Take();if(!e)return;e.kind=EntityKind.Particle;e.Position=p;e.life=.24f;e.pickup=1;e.Visual(art.blast,size,new Color(1,.65f,.22f),12);}
 void TickParticles(float dt){foreach(var e in particles.Items){if(!e.gameObject.activeSelf)continue;e.age+=dt;e.Position+=e.velocity*dt;if(e.pickup==1)e.transform.localScale*=1+dt*5;Color c=e.view.color;c.a=Mathf.Clamp01(1-e.age/e.life);e.view.color=c;if(e.age>=e.life)EntityPool.Return(e);}}
 void TickStars(float dt){foreach(var e in stars.Items){e.view.enabled=backdrop.ShowStars;e.Position+=Vector2.down*balance.starSpeed*e.phase*dt;if(e.Position.y<-balance.halfHeight)e.Position=new Vector2(Random.Range(-balance.halfWidth,balance.halfWidth),balance.halfHeight);}}
 public void TogglePause(){InputReader.ResetDrag();if(State==GameState.Playing){State=GameState.Paused;View.Pause();}else if(State==GameState.Paused){State=GameState.Playing;View.Gameplay();}}
 void OnApplicationPause(bool paused){if(paused&&State==GameState.Playing)TogglePause();if(paused&&Save!=null)Save.Write();}
 void OnApplicationFocus(bool focus){if(!focus&&State==GameState.Playing)TogglePause();}
 void Finish(bool won){
  if(State!=GameState.Playing)return;Won=won;State=GameState.Results;
  Stars=won?(Hull/ActiveShip.hull>=balance.threeStarHull?3:Hull/ActiveShip.hull>=balance.twoStarHull?2:1):0;
  if(won){Save.Value.unlocked=Mathf.Min(catalog.stages.Length,Mathf.Max(Save.Value.unlocked,StageIndex+2));Save.Value.stars[StageIndex]=Mathf.Max(Save.Value.stars[StageIndex],Stars);Audio.Play(art.victory);}

  Save.Value.coins+=RunCoins;Save.Value.gems+=RunGems;Save.Value.bestScore=Mathf.Max(Save.Value.bestScore,Score);Save.Write();
  bullets.Clear();pickups.Clear();enemies.Clear();player.gameObject.SetActive(false);ring.gameObject.SetActive(false);cam.transform.position=cameraOrigin;View.Results();Ads.RunFinished();
 }
 public bool Upgrade(int index){
  if(index<0||index>=5)return false;int level=Save.Value.upgrades[index];int cost=balance.UpgradeCost(level);
  if(level>=balance.upgradeMaxLevel||Save.Value.coins<cost)return false;
  Save.Value.coins-=cost;Save.Value.upgrades[index]++;Save.Write();Audio.Play(art.pickup);return true;
 }
 public void DoubleReward(){
  if(State!=GameState.Results||RewardClaimed||!Ads.RewardReady)return;
  int serial=runSerial,amount=RunCoins;
  Ads.ShowRewarded(ok=>{if(!ok)return;Save.Value.coins+=amount;Save.Write();if(serial==runSerial){RewardClaimed=true;if(State==GameState.Results)View.Results();}});
 }
}
}

