using UnityEngine;
using Unity.Services.Core;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
//using ParrelSync;
using UnityEngine.Networking;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using static Utils;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;

public class Lobby : MonoBehaviour {
    private static int maxEloDifference = 100000;
    private static Unity.Services.Lobbies.Models.Lobby currentLobby = null;
    private static bool isTheOneWhoKnocks = false;
    private static bool signedIn = false;
    private int enemyElo;

    private float scoreOfMatch;
    private bool enemyResigned;

    private Allocation hostAllocation;

    [SerializeField] private GameObject createInputField;
    [SerializeField] private GameObject searchInputField;

    void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    private async void Start() {
        if (SceneManager.GetActiveScene().name != "MultiplayerTest") {
            InitializationOptions options = new InitializationOptions();
            // if (ClonesManager.IsClone()) {
            //     options.SetProfile("clone");
            // }

            if (!signedIn) {
                await UnityServices.InitializeAsync(options);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("SIGNED IN: " + AuthenticationService.Instance.PlayerId);
                PlayerPrefs.SetInt("Elo", Random.Range(0, 100));
                Debug.Log("Bestowed Elo: " + PlayerPrefs.GetInt("Elo"));
            }

            signedIn = true;

            leftLobby = false;
        } else {
            
        }
    }

    public async void StartHostWithRelay() {
        Debug.Log(currentLobby.Data["RelayJoinCode"].Value);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(hostAllocation, "udp"));
        NetworkManager.Singleton.StartHost();
    }

    public async void StartClientWithRelay() {
        Debug.Log(currentLobby.Data["RelayJoinCode"].Value);
        string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: relayJoinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "udp"));
        NetworkManager.Singleton.StartClient();
    }


    private static bool gameStarted;
    private static float timer;
    private static bool querying;
    async void Update() {
        // if (NetworkManager.Singleton != null) {
        //     if (NetworkManager.Singleton.IsConnectedClient) sendEloToEnemyRpc(PlayerPrefs.GetInt("Elo"));
        // }

        timer += Time.deltaTime;
        if (currentLobby != null && !gameStarted) {
            if (currentLobby.Players.Count == 2) {
                if (isTheOneWhoKnocks && !querying) {
                    querying = true;
                    hostAllocation = await RelayService.Instance.CreateAllocationAsync(currentLobby.Players.Count - 1);
                    string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);
                    UpdateLobbyOptions options = new UpdateLobbyOptions();
                    options.IsPrivate = true;
                    options.Data = new Dictionary<string, DataObject>()
                    {
                        { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                    };
                    currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
                }
                if (!isTheOneWhoKnocks && currentLobby.Data["RelayJoinCode"].Value == "") {//nullref
                    if (currentLobby.Id != null && LobbyService.Instance != null) currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                }
                if (currentLobby.Data["RelayJoinCode"].Value != "") {
                    if (isTheOneWhoKnocks) {
                        StartHostWithRelay();
                        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerTest", LoadSceneMode.Single);
                    } else {
                        StartClientWithRelay();
                    }
                    gameStarted = true;
                    querying = false;
                }
                return;
            }
        }

        if (gameStarted) {
            GameObject enemyPlayer = null;
            foreach (GameObject g in allVehiclesOfTags("Plane")) {
                if (g != NetworkManager.Singleton.LocalClient.PlayerObject.gameObject) enemyPlayer = g;
            }

            if (enemyPlayer != null) {
                bool gameEnd = false;
                if (enemyPlayer.GetComponent<VehicleController>().vehicleDead()) {
                    scoreOfMatch = 1f;
                    gameEnd = true;
                }
                if (NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<VehicleController>().vehicleDead()) {
                    scoreOfMatch = 0f;
                    gameEnd = true;
                }
                if (enemyPlayer.GetComponent<VehicleController>().vehicleDead() && NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<VehicleController>().vehicleDead()) {
                    scoreOfMatch = 0.5f;
                    gameEnd = true;
                }
                if (gameEnd) leaveLobby(false);
            }

            Debug.Log("score: " + scoreOfMatch);
        }

        if (timer > 2f && currentLobby != null) {//nullref
            timer = 0f;
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        }

        if (currentLobby == null) return;
        if (currentLobby.Players.Count == 1 && !leftLobby && SceneManager.GetActiveScene().name == "MultiplayerTest") {
            leftLobby = true;
            Debug.Log("leaving bc no players");
            leaveLobby(false);
        }
    }

    public async void startMatch() {
        bool canJoin = await tryJoinLobby();
        if (!canJoin) createLobby();
    }

    public async void createLobbyWithInputtedName() {
        createLobby(createInputField.GetComponent<TMP_InputField>().text);
    }

    private async void createLobby() {
        createLobby(false, "");
    }

    private async void createLobby(string name) {
        createLobby(true, name);
    }

    private async void createLobby(bool customName, string nameOtherThanElo) {
        string name = (!customName ? PlayerPrefs.GetInt("Elo").ToString() : nameOtherThanElo);
        int maxPlayers = 2;

        CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>()
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "") }
            }
        };
        Unity.Services.Lobbies.Models.Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, createLobbyOptions);
        currentLobby = lobby;
        isTheOneWhoKnocks = true;
        Debug.Log("created lobby");
    }

    public async void tryJoinLobbyWithInputtedName() {
        tryJoinLobby(searchInputField.GetComponent<TMP_InputField>().text);
    }

    private async Task<bool> tryJoinLobby() {
        return await tryJoinLobby(false, "");
    }

    public async Task<bool> tryJoinLobby(string name) {
        return await tryJoinLobby(true, name);
    }

    private async Task<bool> tryJoinLobby(bool findSpecificGame, string findGameString) {
        int elo = PlayerPrefs.GetInt("Elo");
        QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
        foreach (Unity.Services.Lobbies.Models.Lobby l in queryResponse.Results) {
            if (findSpecificGame) {
                if (l.Name == findGameString) {
                    await LobbyService.Instance.JoinLobbyByIdAsync(l.Id);
                    currentLobby = l;
                    isTheOneWhoKnocks = false;
                    Debug.Log("joined lobby");
                    return true;
                }
            } else {
                if (elo < int.Parse(l.Name) + maxEloDifference && elo > int.Parse(l.Name) - maxEloDifference && l.Players.Count == 1) {
                    await LobbyService.Instance.JoinLobbyByIdAsync(l.Id);
                    currentLobby = l;
                    isTheOneWhoKnocks = false;
                    Debug.Log("joined lobby");
                    return true;
                }
            }
        }
        return false;
    }

    bool leftLobby;
    public async void leaveLobby(bool resigned) {
        if (currentLobby != null) {
            leftLobby = true;
            // if (resigned && NetworkManager.Singleton != null) sendResignationToOthersRpc();

            if (enemyResigned) scoreOfMatch = 1;
            if (resigned) scoreOfMatch = 0;
            if (enemyResigned && resigned) scoreOfMatch = .5f;

            MultiplayerDuelScoring.applyScoringToPlayer(enemyElo, scoreOfMatch);

            string playerId = AuthenticationService.Instance.PlayerId;
            if (isTheOneWhoKnocks) {
                NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerMainMenu", LoadSceneMode.Single);
            } else {
                SceneManager.LoadScene("MultiplayerMainMenu", LoadSceneMode.Single);
            }
            if (NetworkManager.Singleton != null) Unity.Netcode.NetworkManager.Singleton.Shutdown();
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);

            currentLobby = null;
            isTheOneWhoKnocks = false;
            gameStarted = false;

            if (currentLobby != null) await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
        }
    }

    public async void cancel() {
        if (currentLobby == null) return;
        if (isTheOneWhoKnocks) {
            if (NetworkManager.Singleton != null) Unity.Netcode.NetworkManager.Singleton.Shutdown();
            LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
        } else {
            leaveLobby(false);
        }
        Debug.Log("cancelled");
        currentLobby = null;
        isTheOneWhoKnocks = false;
        gameStarted = false;
    }

    void OnApplicationQuit() {
        if (currentLobby != null) leaveLobby(true);
    }

    [Rpc(SendTo.NotMe)]
    public void sendEloToEnemyRpc(int elo) {
        enemyElo = elo;
    }

    [Rpc(SendTo.NotMe)]
    public void sendResignationToOthersRpc() {
        enemyResigned = true;
    }

    void OnDisable() {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
    }
}
