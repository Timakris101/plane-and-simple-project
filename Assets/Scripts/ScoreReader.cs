using UnityEngine;
using TMPro;

public class ScoreReader : MonoBehaviour {
    [SerializeField] private GameObject arcadeManager;
    void Update() {
        GetComponent<TMP_Text>().text = arcadeManager.GetComponent<ArcadeManager>().getScore().ToString();
    }
}
