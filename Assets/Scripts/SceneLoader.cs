using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public void load(string name) {
        string cur =  SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(name);
        SceneManager.UnloadScene(cur);
    }
}
