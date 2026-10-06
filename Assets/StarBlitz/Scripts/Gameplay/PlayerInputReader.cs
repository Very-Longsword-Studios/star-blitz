using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
namespace StarBlitz.Gameplay {
/// <summary>Relative drag preserves position when a finger first touches down.</summary>
public sealed class PlayerInputReader : MonoBehaviour {
 public bool BombPressed {get;private set;}
 public bool PausePressed {get;private set;}
 public Vector2 Axis {get;private set;}
 public Vector2 PointerDelta {get;private set;}
 public Rect BombRect, PauseRect;
 int movementFinger=-1; Vector2 previous; bool mouseDragging;
 void OnEnable(){EnhancedTouchSupport.Enable();}
 void OnDisable(){EnhancedTouchSupport.Disable();ResetDrag();}
 public void ResetDrag(){movementFinger=-1;mouseDragging=false;PointerDelta=Vector2.zero;}
 public void Poll(){
  BombPressed=PausePressed=false;Axis=PointerDelta=Vector2.zero;
  var k=Keyboard.current;
  if(k!=null){Axis=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0)).normalized;BombPressed=k.bKey.wasPressedThisFrame;PausePressed=k.escapeKey.wasPressedThisFrame;}
  bool found=false;
  foreach(var t in Touch.activeTouches){
   bool began=t.phase==UnityEngine.InputSystem.TouchPhase.Began;
   if(t.phase==UnityEngine.InputSystem.TouchPhase.Ended||t.phase==UnityEngine.InputSystem.TouchPhase.Canceled)continue;
   Vector2 p=t.screenPosition;
   if(t.finger.index==movementFinger){PointerDelta=p-previous;previous=p;found=true;continue;}
   if(!began)continue;
   if(BombRect.Contains(p)){BombPressed=true;continue;}
   if(PauseRect.Contains(p)){PausePressed=true;continue;}
   if(movementFinger<0){movementFinger=t.finger.index;previous=p;found=true;}
  }
  if(!found)movementFinger=-1;
  var m=Mouse.current;
  if(Touch.activeTouches.Count==0&&m!=null){
   Vector2 p=m.position.ReadValue();
   if(m.leftButton.wasPressedThisFrame){
    if(BombRect.Contains(p))BombPressed=true;
    else if(PauseRect.Contains(p))PausePressed=true;
    else{mouseDragging=true;previous=p;}
   }
   if(mouseDragging&&m.leftButton.isPressed){PointerDelta=p-previous;previous=p;}
   if(!m.leftButton.isPressed)mouseDragging=false;
  }
 }
}
}
