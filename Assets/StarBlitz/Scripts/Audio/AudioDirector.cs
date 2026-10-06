using UnityEngine;
using StarBlitz.Data;
namespace StarBlitz.Audio {
public sealed class AudioDirector : MonoBehaviour {
 AudioSource music,sfx,otherMusic,weapon; PresentationData data;
 float nextShot,nextImpact,blend,fromVolume,toVolume;int shotIndex;
 AudioSource incoming,outgoing;bool paused;
 public AudioClip CurrentMusic {get;private set;}
 public void Initialize(PresentationData d,bool enabled){
  data=d;music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=0;
  sfx=gameObject.AddComponent<AudioSource>();sfx.volume=d.sfxVolume;
  otherMusic=gameObject.AddComponent<AudioSource>();otherMusic.loop=true;otherMusic.volume=0;
  weapon=gameObject.AddComponent<AudioSource>();weapon.volume=d.sfxVolume*.7f;weapon.priority=100;
  SetEnabled(enabled);SetEffectsEnabled(true);
 }
 public void SetEnabled(bool value){if(music)music.mute=!value;if(otherMusic)otherMusic.mute=!value;}
 public void SetEffectsEnabled(bool value){bool mute=!value||!data.enableSfx;if(sfx)sfx.mute=mute;if(weapon)weapon.mute=mute;}
 public void Menu(){paused=false;SwitchMusic(data.menuMusic);}
 public void Stage(int index){paused=false;if(data.music!=null&&data.music.Length>0)SwitchMusic(data.music[Mathf.Abs(index)%data.music.Length]);}
 public void SetPaused(bool value){paused=value;}
 void SwitchMusic(AudioClip clip){
  if(!clip||CurrentMusic==clip)return;
  CurrentMusic=clip;
  // Reuse the quieter source, including when a transition is interrupted.
  incoming=music.volume<=otherMusic.volume?music:otherMusic;outgoing=incoming==music?otherMusic:music;
  incoming.Stop();incoming.clip=clip;incoming.volume=0;incoming.Play();fromVolume=outgoing.volume;toVolume=data.musicVolume;blend=0;
 }
 void Update(){
  if(!incoming)return;blend=Mathf.Min(1,blend+Time.unscaledDeltaTime/.8f);
  float target=data.musicVolume*(paused?.4f:1);toVolume=Mathf.MoveTowards(toVolume,target,Time.unscaledDeltaTime*data.musicVolume*3);
  incoming.volume=Mathf.SmoothStep(0,toVolume,blend);outgoing.volume=Mathf.SmoothStep(fromVolume,0,blend);
  if(blend>=1&&outgoing.isPlaying)outgoing.Stop();
 }
 public void Play(AudioClip clip){if(!data.enableSfx||!clip||sfx.mute)return;if(clip==data.explosion){if(Time.unscaledTime<nextImpact)return;nextImpact=Time.unscaledTime+.065f;}sfx.PlayOneShot(clip,clip==data.explosion?.65f:1f);}
 public void Shot(){
  if(!data.enableSfx||!data.fire||weapon.mute||Time.unscaledTime<nextShot)return;
  nextShot=Time.unscaledTime+Mathf.Max(.10f,data.minimumShotAudioInterval);
  // A dedicated voice prevents rapid fire from accumulating overlapping sound.
  weapon.pitch=1+((shotIndex++%5)-2)*.018f;weapon.clip=data.fire;weapon.Play();
 }
}
}
