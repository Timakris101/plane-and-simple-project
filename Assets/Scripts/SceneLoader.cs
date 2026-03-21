using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public void load(string name) {
        SceneManager.LoadSceneAsync(name);
    }
}
