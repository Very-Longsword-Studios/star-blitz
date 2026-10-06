using UnityEngine;
namespace StarBlitz.Data {
[CreateAssetMenu(menuName="Star Blitz/Game Catalog")]
public sealed class GameCatalog : ScriptableObject {
 public ShipData ship;
 public ShipData[] ships;
 public StageData[] stages;
 public BalanceData balance;
 public PresentationData presentation;
 public AdsData ads;
 public GameObject entityPrefab;
}
}
