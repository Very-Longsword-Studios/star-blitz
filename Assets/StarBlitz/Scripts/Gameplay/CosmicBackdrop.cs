using UnityEngine;
using StarBlitz.Data;
namespace StarBlitz.Gameplay {
public sealed class CosmicBackdrop : MonoBehaviour {
 SpriteRenderer nebula;Camera cameraView;PresentationData presentation;
 GameObject props;SpriteRenderer[] clouds,meteorHeads,meteorTails;Vector2[] cloudVelocity,meteorVelocity;float[] meteorLength;System.Random propRng;
 public bool ShowStars {get;private set;} = true;
 public void Initialize(PresentationData art,Camera camera){
  cameraView=camera;presentation=art;
  var go=new GameObject("World backdrop");go.transform.SetParent(transform,false);nebula=go.AddComponent<SpriteRenderer>();nebula.sortingOrder=-100;nebula.color=new Color(.5f,.55f,.65f,1);SetWorld(0);
 }
 public void SetWorld(int index){
  if(!nebula)return;var worlds=presentation.worldBackgrounds;
  nebula.sprite=worlds!=null&&index>=0&&index<worlds.Length&&worlds[index]?worlds[index]:presentation.nebula;
  ShowStars=index<4;BuildProps(index);
 }
 void LateUpdate(){
  if(!nebula||!nebula.sprite)return;
  float scale=Mathf.Max(cameraView.orthographicSize*2/nebula.sprite.bounds.size.y,cameraView.orthographicSize*2*cameraView.aspect/nebula.sprite.bounds.size.x)*1.14f;
  nebula.transform.localScale=Vector3.one*scale;
  // Slow parallax drift. The 14% overscan leaves room to sway without ever exposing the art's edge,
  // and a sine path means it never snaps back into view.
  float t=Time.time,driftX=Mathf.Sin(t*.47f)*.60f,driftY=Mathf.Sin(t*.35f+1.7f)*.90f;
  nebula.transform.localPosition=new Vector3(driftX,driftY,4);
  if(props)props.transform.localPosition=new Vector3(driftX*.45f,driftY*.45f,0);
  float hw=cameraView.orthographicSize*cameraView.aspect,hh=cameraView.orthographicSize;
  TickClouds(Time.deltaTime,hw,hh);
  TickMeteors(Time.deltaTime,hw,hh);
 }
 // Far-background dressing: drifting gas, two soft planets and a few slow meteors.
 // Everything is dim, slow and behind the action so it reads as depth, never as a threat.
 void BuildProps(int index){
  if(props){props.SetActive(false);Destroy(props);}
  props=new GameObject("Backdrop props");props.transform.SetParent(transform,false);
  clouds=null;cloudVelocity=null;meteorHeads=meteorTails=null;meteorVelocity=null;meteorLength=null;
  if(!cameraView)return;
  propRng=new System.Random(index*7919+101);
  float hh=cameraView.orthographicSize,hw=hh*cameraView.aspect;
  // Gas clouds use a soft-edged sprite, so they can wrap forever without a visible seam.
  Color[] haze={new Color(.30f,.48f,.85f,.07f),new Color(.55f,.38f,.85f,.06f),new Color(.25f,.62f,.70f,.06f),new Color(.75f,.45f,.55f,.05f)};
  clouds=new SpriteRenderer[4];cloudVelocity=new Vector2[4];
  for(int i=0;i<clouds.Length;i++){
   var pos=new Vector2(Mathf.Lerp(-hw,hw,(float)propRng.NextDouble()),Mathf.Lerp(-hh,hh,(float)propRng.NextDouble()));
   clouds[i]=AddProp(presentation.particle,pos,1f,haze[(index+i)%haze.Length],-98);
   if(clouds[i]){
    float natural=Natural(clouds[i].sprite),w=Mathf.Lerp(2.8f,5.6f,(float)propRng.NextDouble());
    clouds[i].transform.localScale=new Vector3(w/natural,w*Mathf.Lerp(.5f,.8f,(float)propRng.NextDouble())/natural,1f);
   }
   cloudVelocity[i]=new Vector2(Mathf.Lerp(-.12f,.12f,(float)propRng.NextDouble()),-Mathf.Lerp(.34f,.70f,(float)propRng.NextDouble()));
  }
  Color[] tints={new Color(.30f,.46f,.70f,.13f),new Color(.52f,.34f,.60f,.11f),new Color(.25f,.55f,.52f,.12f),new Color(.62f,.40f,.30f,.10f)};
  for(int i=0;i<2;i++){
   float side=i==0?-1f:1f,px=side*Mathf.Lerp(hw*.60f,hw*.98f,(float)propRng.NextDouble()),py=Mathf.Lerp(-hh*.85f,hh*.60f,(float)propRng.NextDouble());
   float size=Mathf.Lerp(1.6f,2.6f,(float)propRng.NextDouble());Color tint=tints[(index+i)%tints.Length];
   // Halo + body + a dark offset terminator so the disc reads as a lit sphere, not a bullet.
   AddProp(presentation.hostileOrb,new Vector2(px,py),size*1.05f,new Color(tint.r*1.3f,tint.g*1.3f,tint.b*1.3f,.07f),-97);
   AddProp(presentation.hostileOrb,new Vector2(px,py),size,tint,-96);
   AddProp(presentation.hostileOrb,new Vector2(px+size*.13f,py+size*.10f),size*.92f,new Color(tint.r*.30f,tint.g*.30f,tint.b*.30f,.62f),-95);
  }
  meteorHeads=new SpriteRenderer[3];meteorTails=new SpriteRenderer[3];meteorVelocity=new Vector2[3];meteorLength=new float[3];
  for(int i=0;i<3;i++){
   Vector2 pos=new Vector2(Mathf.Lerp(-hw*.9f,hw*.9f,(float)propRng.NextDouble()),Mathf.Lerp(-hh*.9f,hh*.9f,(float)propRng.NextDouble()));
   Vector2 dir=new Vector2(Mathf.Lerp(-.35f,.35f,(float)propRng.NextDouble()),-1f).normalized;
   meteorVelocity[i]=dir*Mathf.Lerp(.55f,1.1f,(float)propRng.NextDouble());meteorLength[i]=Mathf.Lerp(.35f,.7f,(float)propRng.NextDouble());
   meteorHeads[i]=AddProp(presentation.particle,pos,.075f,new Color(.6f,.78f,1f,.4f),-92);
   meteorTails[i]=AddProp(presentation.particle,pos,1f,new Color(.5f,.7f,1f,.16f),-93);
   if(meteorTails[i]){float natural=Natural(meteorTails[i].sprite);meteorTails[i].transform.localScale=new Vector3(.07f/natural,meteorLength[i]/natural,1f);meteorTails[i].transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg-90f);}
  }
  TickClouds(0f,hw,hh);TickMeteors(0f,hw,hh);
 }
 void TickClouds(float dt,float hw,float hh){
  if(clouds==null)return;
  for(int i=0;i<clouds.Length;i++){
   if(!clouds[i])continue;
   Vector2 p=(Vector2)clouds[i].transform.localPosition+cloudVelocity[i]*dt;
   if(p.y<-hh-3.5f){p.y=hh+3.5f;p.x=Mathf.Lerp(-hw,hw,(float)propRng.NextDouble());}
   clouds[i].transform.localPosition=new Vector3(p.x,p.y,1f);
  }
 }
 void TickMeteors(float dt,float hw,float hh){
  if(meteorHeads==null)return;
  for(int i=0;i<meteorHeads.Length;i++){
   if(!meteorHeads[i])continue;
   Vector2 p=meteorHeads[i].transform.localPosition;p+=meteorVelocity[i]*dt;
   if(p.y<-hh-1.5f){p.y=hh+1.5f;p.x=Mathf.Lerp(-hw*.95f,hw*.95f,(float)propRng.NextDouble());}
   if(p.x<-hw-1.5f)p.x=hw+1.5f;else if(p.x>hw+1.5f)p.x=-hw-1.5f;
   meteorHeads[i].transform.localPosition=new Vector3(p.x,p.y,1f);
   if(meteorTails[i]){Vector2 dir=meteorVelocity[i].normalized;meteorTails[i].transform.localPosition=new Vector3(p.x-dir.x*meteorLength[i]*.5f,p.y-dir.y*meteorLength[i]*.5f,1f);}
  }
 }
 SpriteRenderer AddProp(Sprite sprite,Vector2 position,float size,Color tint,int order){
  if(!sprite||!props)return null;
  var go=new GameObject("Backdrop prop");go.transform.SetParent(props.transform,false);
  var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=tint;sr.sortingOrder=order;
  go.transform.localScale=Vector3.one*(size/Natural(sprite));go.transform.localPosition=new Vector3(position.x,position.y,1f);
  return sr;
 }
 float Natural(Sprite sprite){return Mathf.Max(.01f,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y));}
}
}
