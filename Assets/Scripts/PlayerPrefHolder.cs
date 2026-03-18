using UnityEngine;

public class PlayerPrefHolder : MonoBehaviour {

    [SerializeField] private string key;
    public void setKey(string key) {
        this.key = key;
    }

    public string getKey() {
        return key;
    }

    public void setPlayerPref(int newVal) {
        PlayerPrefs.SetInt(key, newVal);
    }

    public void setPlayerPref(float newVal) {
        PlayerPrefs.SetFloat(key, newVal);
    }

    public void setPlayerPref(string newVal) {
        PlayerPrefs.SetString(key, newVal);
    }
}
