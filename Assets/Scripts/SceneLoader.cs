using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public string sceneToLoadAutomtically;

    void Start() {
        if (SceneUtility.GetBuildIndexByScenePath(sceneToLoadAutomtically) != -1) load(sceneToLoadAutomtically);
    }

    public void load(string name) {
        string cur =  SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(name);
        SceneManager.UnloadScene(cur);
    }
}
