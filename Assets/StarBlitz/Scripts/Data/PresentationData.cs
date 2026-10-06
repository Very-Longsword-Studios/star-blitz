using UnityEngine;
namespace StarBlitz.Data {
[CreateAssetMenu(menuName="Star Blitz/Presentation")]
public sealed class PresentationData : ScriptableObject {
 public Font font;
 public Sprite star, particle, shieldRing;
 public Sprite nebula;
 [Tooltip("One background per existing 36-mission world. The original nebula is the fallback.")]
 public Sprite[] worldBackgrounds;
 public Sprite pulse, hostileOrb, flame, blast;
 public Color ink = new Color(.85f,.94f,1), muted = new Color(.42f,.52f,.65f), accent = new Color(.32f,.95f,1), danger = new Color(1,.25f,.42f), panel = new Color(.035f,.055f,.11f,.96f);
 public bool enableSfx = false;
 public AudioClip playerHit;
 public AudioClip fire, enemyFire, explosion, pickup, bomb, click, victory;
 public AudioClip menuMusic;
 public AudioClip[] music;
 [Range(0,1)] public float musicVolume = .3f, sfxVolume = .5f;
 public float minimumShotAudioInterval = .08f;
 public string gameTitle = "STAR BLITZ", subtitle = "ONE SHIP. AN ENTIRE FRONTIER.";
}
}
