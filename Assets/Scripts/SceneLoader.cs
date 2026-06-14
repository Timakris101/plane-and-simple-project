using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public string sceneToLoadAutomtically;
    public GameObject lvlManager;

    void Start() {
        if (SceneUtility.GetBuildIndexByScenePath(sceneToLoadAutomtically) != -1) load(sceneToLoadAutomtically);
    }

    public void load(string name) {
        string cur =  SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(name);
        SceneManager.UnloadScene(cur);
    }

    public void reload() {
        string cur =  SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(cur);
    }

    public void loadMM() {
        SceneManager.LoadScene("MainMenu");
    }

    public void loadLS() {
        SceneManager.LoadScene("LevelSelector");
    }

    public void loadNextLevel() {
        if (lvlManager != null) {
            SceneManager.LoadScene("Level" + (lvlManager.GetComponent<LvlManager>().getLevelNum() + 1).ToString());
        }
    }
}
