using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using ParrelSync;
using UnityEngine.Networking;
using Unity.Netcode;

public class Lobby : MonoBehaviour {
    private int maxEloDifference = 100;
    private Unity.Services.Lobbies.Models.Lobby currentLobby = null;
    private bool isTheOneWhoKnocks = false;

    private async void Start() {
        InitializationOptions options = new InitializationOptions();
        if (ClonesManager.IsClone()) {
            options.SetProfile("clone");
            Debug.Log("Beep boop clone detected *lazer sound*");
        }
        await UnityServices.InitializeAsync(options);

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log("SIGNED IN: " + AuthenticationService.Instance.PlayerId);
    }

    async void Update() {
        Debug.Log(currentLobby.Name + ", Player Count: " + currentLobby.Players.Count);
        if (currentLobby != null) {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            if (currentLobby.Players.Count == 2) {
                if (isTheOneWhoKnocks) {
                    NetworkManager.Singleton.StartHost();
                } else {
                    NetworkManager.Singleton.StartClient();
                }
            }
        }
    }

    public async void startMatch() {
        bool canJoin = await tryJoinLobby();
        if (!canJoin) createLobby();
    }

    private async void createLobby() {
        try {
            string name = PlayerPrefs.GetInt("Elo").ToString();
            int maxPlayers = 2;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
                IsPrivate = false
            };
            Unity.Services.Lobbies.Models.Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, createLobbyOptions);
            currentLobby = lobby;
            isTheOneWhoKnocks = true;
            Debug.Log("created lobby");
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async Task<bool> tryJoinLobby() {
        int elo = PlayerPrefs.GetInt("Elo");
        QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
        foreach (Unity.Services.Lobbies.Models.Lobby l in queryResponse.Results) {
            if (elo < int.Parse(l.Name) + maxEloDifference && elo > int.Parse(l.Name) - maxEloDifference) {
                await LobbyService.Instance.JoinLobbyByIdAsync(l.Id);
                currentLobby = l;
                isTheOneWhoKnocks = false;
                Debug.Log("joined lobby");
                return true;
            }
        }
        return false;
    }
}
