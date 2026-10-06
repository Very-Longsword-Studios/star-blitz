using UnityEngine;
using StarBlitz.Data;
namespace StarBlitz.Gameplay {
public enum EntityKind { Enemy, PlayerBullet, EnemyBullet, Pickup, Particle, Star }
/// <summary>Reusable state reset by Pool.Take; no per-bullet MonoBehaviour.Update.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class Entity : MonoBehaviour {
 [HideInInspector] public EntityKind kind;
 [HideInInspector] public EnemyData enemy;
 [HideInInspector] public Vector2 velocity;
 [HideInInspector] public float hp, maxHp, radius, damage, age, shotTimer, phase, homeX, life;
 [HideInInspector] public int pickup, bossStage, volley;
 [HideInInspector] public float phaseTell;
 [HideInInspector] public SpriteRenderer view;
 public Vector2 Position { get => transform.position; set => transform.position = new Vector3(value.x,value.y,0); }
 public void ResetState() {
  if(!view) view=GetComponent<SpriteRenderer>();
  enemy=null;velocity=Vector2.zero;hp=maxHp=radius=damage=age=shotTimer=phase=homeX=0;life=1;pickup=bossStage=volley=0;phaseTell=0;
  transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;view.color=Color.white;
 }
 public void Visual(Sprite sprite,float size,Color tint,int order) {
  view.sprite=sprite;view.color=tint;view.sortingOrder=order;
  float natural = sprite ? Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y) : 1;
  transform.localScale=Vector3.one*(size/Mathf.Max(.01f,natural));
 }
}
}
