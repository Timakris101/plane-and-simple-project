using UnityEngine;

public class WorkWithLobby : MonoBehaviour {

    GameObject lobbyManager;

    void Start() {
        lobbyManager = GameObject.Find("Lobby");
    }

    public async void startMatchPublic(bool isRanked) {
        lobbyManager.GetComponent<Lobby>().startMatchPublic(isRanked);
    }

    public async void startMatchPrivate(bool creatingLobby) {
        lobbyManager.GetComponent<Lobby>().startMatchPrivate(creatingLobby);
    }

    public void forceLobbyUpdate() {
        lobbyManager.GetComponent<Lobby>().forceLobbyUpdate();
    }

    public async void cancel() {
        lobbyManager.GetComponent<Lobby>().cancel();
    }

    public async void leaveLobby(bool resigned) {
        lobbyManager.GetComponent<Lobby>().leaveLobby(resigned);
    }
}
