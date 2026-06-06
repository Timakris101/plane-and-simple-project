using UnityEngine;
using TMPro;

public class EloDisp : MonoBehaviour {
    void Update() {GetComponent<TMP_Text>().text = "Elo: " + PlayerPrefs.GetInt("Elo").ToString();}
}
