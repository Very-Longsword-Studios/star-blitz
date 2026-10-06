using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StarBlitz.Gameplay;
namespace StarBlitz.Tests {
public class InputTests : InputTestFixture {
 [Test] public void TouchDragAndSecondFingerBombRemainIndependent(){
  var screen=InputSystem.AddDevice<Touchscreen>();var host=new GameObject("Touch test");var reader=host.AddComponent<PlayerInputReader>();reader.BombRect=new Rect(0,0,100,100);
  try{BeginTouch(1,new Vector2(200,200),screen:screen);reader.Poll();Assert.AreEqual(Vector2.zero,reader.PointerDelta);MoveTouch(1,new Vector2(240,250),screen:screen);reader.Poll();Assert.AreEqual(new Vector2(40,50),reader.PointerDelta);BeginTouch(2,new Vector2(50,50),screen:screen);reader.Poll();Assert.IsTrue(reader.BombPressed);MoveTouch(1,new Vector2(260,280),screen:screen);reader.Poll();Assert.AreEqual(new Vector2(20,30),reader.PointerDelta);EndTouch(1,new Vector2(260,280),screen:screen);reader.Poll();EndTouch(2,new Vector2(50,50),screen:screen);reader.Poll();BeginTouch(3,new Vector2(500,600),screen:screen);reader.Poll();Assert.AreEqual(Vector2.zero,reader.PointerDelta);}
  finally{Object.DestroyImmediate(host);InputSystem.RemoveDevice(screen);}
 }
 public override void Setup(){base.Setup();UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();}
 public override void TearDown(){UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();base.TearDown();}
 [Test] public void RelativeDragDoesNotJumpOnInitialTouchOrReposition(){
  var mouse=InputSystem.AddDevice<Mouse>();var host=new GameObject("Input test");var reader=host.AddComponent<PlayerInputReader>();
  try{
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(100,200)}.WithButton(MouseButton.Left));InputSystem.Update();reader.Poll();Assert.AreEqual(Vector2.zero,reader.PointerDelta);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(140,250)}.WithButton(MouseButton.Left));InputSystem.Update();reader.Poll();Assert.AreEqual(new Vector2(40,50),reader.PointerDelta);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(140,250)});InputSystem.Update();reader.Poll();
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(500,600)}.WithButton(MouseButton.Left));InputSystem.Update();reader.Poll();Assert.AreEqual(Vector2.zero,reader.PointerDelta);
  }finally{Object.DestroyImmediate(host);InputSystem.RemoveDevice(mouse);}
 }
 [Test] public void BombPressDoesNotAlsoStartMovement(){
  var mouse=InputSystem.AddDevice<Mouse>();var host=new GameObject("Input test");var reader=host.AddComponent<PlayerInputReader>();reader.BombRect=new Rect(0,0,100,100);
  try{InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(50,50)}.WithButton(MouseButton.Left));InputSystem.Update();reader.Poll();Assert.IsTrue(reader.BombPressed);Assert.AreEqual(Vector2.zero,reader.PointerDelta);InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(150,150)}.WithButton(MouseButton.Left));InputSystem.Update();reader.Poll();Assert.AreEqual(Vector2.zero,reader.PointerDelta);}
  finally{Object.DestroyImmediate(host);InputSystem.RemoveDevice(mouse);}
 }
}
}



