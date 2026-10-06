using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace StarBlitz.UI {
// Small vector UI primitives: no textures, materials or package dependencies.
public sealed class DeckGraphic : MaskableGraphic {
 public enum Shape { Panel, Hex, Disc, Diamond, Lock, Ship, Upgrade, Settings, Nuke, Rounded }
 public Shape shape;public Color edge=new Color(.22f,.65f,.8f,1);
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();var r=rectTransform.rect;
  if(shape==Shape.Rounded){
   // Pixel-width rim and a shaded face keep wide and small buttons consistent.
   Rounded(vh,r,new Color(color.r*.4f,color.g*.4f,color.b*.4f,color.a),edge,0);
   var face=new Rect(r.xMin+3,r.yMin+7,r.width-6,r.height-10);
   Rounded(vh,face,color,Color.Lerp(color,Color.white,.22f),1);return;
  }
  if(shape<=Shape.Diamond){
   int count=shape==Shape.Disc?48:shape==Shape.Hex?6:shape==Shape.Diamond?4:8;
   Vector2[] points=new Vector2[count];
   if(shape==Shape.Panel){float c=Mathf.Min(12,r.height*.2f);points=new[]{new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin,r.yMax-c),new Vector2(r.xMin,r.yMin+c)};}
   else for(int i=0;i<count;i++){float a=(i*360f/count+(shape==Shape.Hex?30:0))*Mathf.Deg2Rad;points[i]=r.center+new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f);}
   for(int i=0;i<count;i++){Vector2 a=points[i],b=points[(i+1)%count],ai=Vector2.Lerp(a,r.center,.045f),bi=Vector2.Lerp(b,r.center,.045f);Tri(vh,r.center,ai,bi,color);Tri(vh,a,b,ai,edge);Tri(vh,b,bi,ai,edge);}
   return;
  }
  float s=Mathf.Min(r.width,r.height);Vector2 o=r.center;
  if(shape==Shape.Lock){RectFill(vh,o+new Vector2(-.24f,-.3f)*s,new Vector2(.48f,.4f)*s,color);for(int i=0;i<16;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Stroke(vh,o+new Vector2(Mathf.Cos(a)*.19f,.09f+Mathf.Sin(a)*.24f)*s,o+new Vector2(Mathf.Cos(b)*.19f,.09f+Mathf.Sin(b)*.24f)*s,.06f*s,color);}}
  else if(shape==Shape.Ship||shape==Shape.Nuke){Tri(vh,o+new Vector2(0,.43f)*s,o+new Vector2(-.32f,-.3f)*s,o+new Vector2(0,-.12f)*s,color);Tri(vh,o+new Vector2(0,.43f)*s,o+new Vector2(0,-.12f)*s,o+new Vector2(.32f,-.3f)*s,color);if(shape==Shape.Nuke){Stroke(vh,o+new Vector2(-.09f,-.22f)*s,o+new Vector2(-.09f,-.46f)*s,.045f*s,color);Stroke(vh,o+new Vector2(.09f,-.22f)*s,o+new Vector2(.09f,-.46f)*s,.045f*s,color);}}
  else if(shape==Shape.Upgrade){for(int i=0;i<3;i++){float y=(i-1)*.25f;Stroke(vh,o+new Vector2(-.3f,y-.09f)*s,o+new Vector2(0,y+.1f)*s,.055f*s,color);Stroke(vh,o+new Vector2(0,y+.1f)*s,o+new Vector2(.3f,y-.09f)*s,.055f*s,color);}}
  else {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Stroke(vh,o+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.25f*s,o+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.25f*s,.065f*s,color);}for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Stroke(vh,o+d*.27f*s,o+d*.4f*s,.11f*s,color);}}
 }
 static void Rounded(VertexHelper v,Rect r,Color bottom,Color top,int unused){
  float radius=Mathf.Min(14,r.height*.24f);Vector2 previous=Vector2.zero,first=Vector2.zero;
  for(int corner=0;corner<4;corner++)for(int j=0;j<=6;j++){
   float a=(corner*90+j*15)*Mathf.Deg2Rad;
   Vector2 center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
   Vector2 point=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
   if(corner==0&&j==0)first=point;else GradientTri(v,r.center,previous,point,r,bottom,top);previous=point;
  }
  GradientTri(v,r.center,previous,first,r,bottom,top);
 }
 static void GradientTri(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Rect r,Color bottom,Color top){int n=v.currentVertCount;foreach(var p in new[]{a,b,c})v.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);v.AddTriangle(n,n+1,n+2);}
 static void RectFill(VertexHelper v,Vector2 p,Vector2 s,Color c){Tri(v,p,p+Vector2.right*s.x,p+s,c);Tri(v,p,p+s,p+Vector2.up*s.y,c);}
 static void Stroke(VertexHelper v,Vector2 a,Vector2 b,float width,Color c){Vector2 n=new Vector2(a.y-b.y,b.x-a.x).normalized*width*.5f;Tri(v,a+n,b+n,b-n,c);Tri(v,a+n,b-n,a-n,c);}
 static void Tri(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color col){int n=v.currentVertCount;v.AddVert(a,col,Vector2.zero);v.AddVert(b,col,Vector2.zero);v.AddVert(c,col,Vector2.zero);v.AddTriangle(n,n+1,n+2);}
}
public sealed class ButtonMotion : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler {
 float target=1;
 void Update(){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*target,1-Mathf.Exp(-25*Time.unscaledDeltaTime));}
 void OnEnable(){target=1;transform.localScale=Vector3.one;}
 public void OnPointerDown(PointerEventData e){var b=GetComponent<Button>();if(b&&b.IsInteractable())target=.95f;}
 public void OnPointerUp(PointerEventData e){target=1;}
 public void OnPointerExit(PointerEventData e){target=1;}
}
}
