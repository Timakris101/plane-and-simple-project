using UnityEngine;

public class WorkWithLobby : MonoBehaviour {

    Lobby lobbyManager;

    void Start() {
        lobbyManager = GameObject.Find("Lobby").GetComponent<Lobby>();
    }

    public async void startMatchPublic(bool isRanked) {
        lobbyManager.startMatchPublic(isRanked);
    }

    public async void startMatchPublic() {
        lobbyManager.startMatchPublic(LobbyInfo.Parse(lobbyManager.currentLobby.Data["Info"].Value).isRanked);
    }

    public async void startMatchPrivate(bool creatingLobby) {
        lobbyManager.startMatchPrivate(creatingLobby);
    }

    public void forceLobbyUpdate() {
        lobbyManager.forceLobbyUpdate();
    }

    public async void cancel() {
        lobbyManager.cancel();
    }

    public async void leaveLobby(bool resigned) {
        lobbyManager.leaveLobby(resigned);
    }

    public async void endGame(bool resigned) {
        lobbyManager.endGame(resigned);
    }

    public void selectRematch() {
        lobbyManager.selectRematch();
    }

    public void setReadiness() {
        lobbyManager.setReadiness();
    }

    public void makeSelection(string str) {
        lobbyManager.makeSelection(str);
    }
}
