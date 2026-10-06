using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using StarBlitz.Data;
using StarBlitz.Gameplay;
using StarBlitz.Editor;
namespace StarBlitz.Tests {
public class ContentTests {
 [Test] public void CampaignHasValidContent(){Assert.That(ProjectTools.ValidationErrors(),Is.Empty);}
 [Test] public void UpgradeCostsStayPositiveAndIncrease(){
  var b=AssetDatabase.LoadAssetAtPath<BalanceData>("Assets/StarBlitz/Data/Balance.asset");
  Assert.That(b.UpgradeCost(0),Is.GreaterThan(0));for(int i=1;i<b.upgradeMaxLevel;i++)Assert.That(b.UpgradeCost(i),Is.GreaterThan(b.UpgradeCost(i-1)));
 }
 [Test] public void FixedPoolDoesNotReuseActiveObjectsAndResetsReturnedObjects(){
  var parent=new GameObject("test");var prefab=new GameObject("entity",typeof(SpriteRenderer),typeof(Entity));
  try{
   var pool=new EntityPool("test pool",2,prefab,parent.transform);var first=pool.Take();var second=pool.Take();
   Assert.That(first,Is.Not.SameAs(second));Assert.That(pool.Take(),Is.Null);
   first.hp=999;first.velocity=Vector2.one;EntityPool.Return(first);var reused=pool.Take();
   Assert.That(reused,Is.SameAs(first));Assert.That(reused.hp,Is.Zero);Assert.That(reused.velocity,Is.EqualTo(Vector2.zero));
   pool.Clear();Assert.That(first.gameObject.activeSelf,Is.False);Assert.That(second.gameObject.activeSelf,Is.False);
  }finally{Object.DestroyImmediate(parent);Object.DestroyImmediate(prefab);}
 }
}
}
