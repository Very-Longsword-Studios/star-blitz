using UnityEngine;
using StarBlitz.Data;
namespace StarBlitz.Gameplay {
public sealed class CosmicBackdrop : MonoBehaviour {
 SpriteRenderer nebula;Camera cameraView;PresentationData presentation;
 public bool ShowStars {get;private set;} = true;
 public void Initialize(PresentationData art,Camera camera){
  cameraView=camera;presentation=art;
  var go=new GameObject("World backdrop");go.transform.SetParent(transform,false);nebula=go.AddComponent<SpriteRenderer>();nebula.sortingOrder=-100;nebula.color=new Color(.5f,.55f,.65f,1);SetWorld(0);
 }
 public void SetWorld(int index){
  if(!nebula)return;var worlds=presentation.worldBackgrounds;
  nebula.sprite=worlds!=null&&index>=0&&index<worlds.Length&&worlds[index]?worlds[index]:presentation.nebula;
  ShowStars=index<4;
 }
 void LateUpdate(){
  if(!nebula||!nebula.sprite)return;
  float scale=Mathf.Max(cameraView.orthographicSize*2/nebula.sprite.bounds.size.y,cameraView.orthographicSize*2*cameraView.aspect/nebula.sprite.bounds.size.x)*1.08f;
  nebula.transform.localScale=Vector3.one*scale;
  // Static backdrop: streaks baked into the art must not slide around behind the action. Overscan prevents exposed edges.
  nebula.transform.localPosition=new Vector3(0,0,4);
 }
}
}
