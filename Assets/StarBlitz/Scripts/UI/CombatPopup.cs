using UnityEngine;
using UnityEngine.UI;
namespace StarBlitz.UI {
public sealed class CombatPopup : MonoBehaviour {
 public Text text;public bool reduced;float age;Vector2 start;
 void Start(){start=((RectTransform)transform).anchoredPosition;}
 void Update(){age+=Time.deltaTime;var c=text.color;c.a=1-Mathf.Clamp01((age-.3f)/.4f);text.color=c;if(!reduced)((RectTransform)transform).anchoredPosition=start+Vector2.up*(age*34);if(age>.7f)Destroy(gameObject);}
}
}
