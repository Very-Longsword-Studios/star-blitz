using UnityEngine;
using StarBlitz.Data;
namespace StarBlitz.Gameplay {
public sealed class CosmicBackdrop : MonoBehaviour {
 SpriteRenderer nebula;Camera cameraView;PresentationData presentation;GameController game;float drift;
 public bool ShowStars {get;private set;} = true;
 public void Initialize(PresentationData art,Camera camera){
  cameraView=camera;presentation=art;game=GetComponent<GameController>();
  var go=new GameObject("World backdrop");go.transform.SetParent(transform,false);nebula=go.AddComponent<SpriteRenderer>();nebula.sortingOrder=-100;nebula.color=new Color(.82f,.85f,.92f,1);SetWorld(0);
 }
 public void SetWorld(int index){
  if(!nebula)return;var worlds=presentation.worldBackgrounds;
  nebula.sprite=worlds!=null&&index>=0&&index<worlds.Length&&worlds[index]?worlds[index]:presentation.nebula;
  ShowStars=index<4;
 }
 void LateUpdate(){
  if(!nebula||!nebula.sprite)return;
  if(!game||game.State!=GameState.Paused)drift+=Time.deltaTime;
  float scale=Mathf.Max(cameraView.orthographicSize*2/nebula.sprite.bounds.size.y,cameraView.orthographicSize*2*cameraView.aspect/nebula.sprite.bounds.size.x)*1.08f;
  nebula.transform.localScale=Vector3.one*scale;
  // Slow distant motion beneath the existing faster starfield; overscan prevents exposed edges.
  nebula.transform.localPosition=new Vector3(Mathf.Sin(drift*.045f)*.12f,Mathf.Sin(drift*.028f)*.2f,4);
 }
}
}
