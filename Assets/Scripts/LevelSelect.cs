using UnityEngine;

[ExecuteInEditMode]
public class LevelSelect : MonoBehaviour {
    [SerializeField] private GameObject[] lvls;
    private Vector2[] lvlPercentagePositions;
    [SerializeField] private bool locked;

    void Start() {
        updateLvlLocs();
    }

    void Update() {
        for (int i = 0; i < lvls.Length; i++) {
            lvls[i].SetActive(i <= (PlayerPrefs.GetInt("MaxLevelUnlocked", 1) - 1));
        }
        if (!locked) {
            updateLvlLocs();
        } else {
            setLvlLocs();
        }
    }

    private void updateLvlLocs() {
        lvlPercentagePositions = new Vector2[lvls.Length];
        for (int i = 0; i < lvls.Length; i++) {
            lvlPercentagePositions[i] = new Vector2(lvls[i].transform.position.x / Screen.width, lvls[i].transform.position.y / Screen.height);
        }
    }

    private void setLvlLocs() {
        for (int i = 0; i < lvls.Length; i++) {
            lvls[i].transform.position = new Vector3(lvlPercentagePositions[i].x * Screen.width, lvlPercentagePositions[i].y * Screen.height, 0f);
        }
    }
}
