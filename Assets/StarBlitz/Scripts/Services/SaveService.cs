using System;
using System.IO;
using UnityEngine;
namespace StarBlitz.Services {
[Serializable] public sealed class SaveData {
 public int version = 1, coins, gems, unlocked = 1;
 public int[] upgrades = new int[5];
 public int[] stars = new int[8];
 public bool sound = true, dragToFire;
 public bool sfxMuted;
 public string dailyClaim = "";
 public int bestScore;
 public int selectedShip;
 public bool[] ownedShips = new bool[] {true};
 public bool reducedEffects;
}
/// <summary>Single boundary for local persistence; replace with cloud storage later.</summary>
public sealed class SaveService {
 public SaveData Value { get; private set; }
 readonly string path;
 public SaveService(int stageCount, int startingCoins, string overridePath = null) {
  path = overridePath ?? Path.Combine(Application.persistentDataPath, "star-blitz-v1.json");
  Value = Read(path) ?? Read(path + ".bak") ?? new SaveData {coins = startingCoins};
  if(Value.upgrades == null) Value.upgrades = new int[5];
  if(Value.upgrades.Length != 5) Array.Resize(ref Value.upgrades,5);
  if(Value.stars == null) Value.stars = new int[stageCount];
  if(Value.stars.Length != stageCount) Array.Resize(ref Value.stars,stageCount);
  Value.unlocked = Mathf.Clamp(Value.unlocked,1,stageCount);
  Value.coins = Mathf.Max(0,Value.coins); Value.gems = Mathf.Max(0,Value.gems);
 }
 SaveData Read(string p) {
  try { return File.Exists(p) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(p)) : null; }
  catch(Exception e) {Debug.LogWarning("Save read failed: " + e.Message);return null;}
 }
 public void Write() {
  try {
   File.WriteAllText(path + ".tmp",JsonUtility.ToJson(Value,true));
   if(File.Exists(path)) File.Copy(path,path+".bak",true);
   File.Copy(path+".tmp",path,true); File.Delete(path+".tmp");
  } catch(Exception e) {Debug.LogError("Save write failed: " + e.Message);}
 }
 public bool CanClaimDaily => Value.dailyClaim != DateTime.UtcNow.ToString("yyyy-MM-dd");
 public void ConfigureShips(int count) {
  if(Value.ownedShips==null)Value.ownedShips=new bool[count];
  if(Value.ownedShips.Length!=count)Array.Resize(ref Value.ownedShips,count);
  Value.ownedShips[0]=true;
  Value.selectedShip=Mathf.Clamp(Value.selectedShip,0,count-1);
  if(!Value.ownedShips[Value.selectedShip])Value.selectedShip=0;
 }
 public bool SelectOrBuyShip(int index,int cost) {
  if(index<0||index>=Value.ownedShips.Length||cost<0)return false;
  if(!Value.ownedShips[index]){if(Value.coins<cost)return false;Value.coins-=cost;Value.ownedShips[index]=true;}
  Value.selectedShip=index;Write();return true;
 }
 public bool ClaimDaily(int coins,int gems) {
  if(!CanClaimDaily) return false;
  Value.dailyClaim = DateTime.UtcNow.ToString("yyyy-MM-dd");
  Value.coins += coins; Value.gems += gems; Write(); return true;
 }
}
}
