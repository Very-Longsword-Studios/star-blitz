using UnityEngine;
namespace StarBlitz.Gameplay {
/// <summary>Fixed-capacity pools avoid Instantiate/Destroy and GC spikes during play.</summary>
public sealed class EntityPool {
 public readonly Entity[] Items;
 int cursor;
 public EntityPool(string name,int capacity,GameObject prefab,Transform parent) {
  var root=new GameObject(name);root.transform.SetParent(parent);
  Items=new Entity[Mathf.Max(1,capacity)];
  for(int i=0;i<Items.Length;i++) {
   var g=Object.Instantiate(prefab,root.transform);g.name=name+" "+i;
   Items[i]=g.GetComponent<Entity>();g.SetActive(false);
  }
 }
 public Entity Take() {
  for(int i=0;i<Items.Length;i++) {
   int n=(cursor+i)%Items.Length;var e=Items[n];
   if(e.gameObject.activeSelf) continue;
   cursor=(n+1)%Items.Length;e.ResetState();e.gameObject.SetActive(true);return e;
  }
  return null; // Saturation drops cosmetic/bullet spawn, never reallocates mid-frame.
 }
 public void Clear(){foreach(var e in Items)e.gameObject.SetActive(false);}
 public static void Return(Entity e){e.gameObject.SetActive(false);}
}
}
