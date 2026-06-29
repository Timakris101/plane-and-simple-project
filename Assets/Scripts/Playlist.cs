using UnityEngine;
using UnityEngine.SceneManagement;

public class Playlist : MonoBehaviour {

    [SerializeField] private AudioClip[] music;
    private string[] acceptableScenesToPlay = {"MainMenu", "MultiplayerMainMenu", "Arcade", "LevelSelector"};

    void Start() {
        DontDestroyOnLoad(gameObject);
    }

    void Update() {
        AudioSource audioSource = GetComponent<AudioSource>();
        bool canPlay = false;
        foreach (string name in acceptableScenesToPlay) {
            if (name == SceneManager.GetActiveScene().name) canPlay = true;
        }
        if (canPlay) {
            if (!audioSource.isPlaying) {
                audioSource.clip = music[Random.Range(0, music.Length)];
                audioSource.Play();
            }
        } else {
            audioSource.Stop();
        }
    }
}
