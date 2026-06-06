using UnityEngine;
using Unity.Services.Core;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using ParrelSync;
using UnityEngine.Networking;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using static Utils;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class Lobby : MonoBehaviour {
    public static int maxEloDifference = 100;
    private static Unity.Services.Lobbies.Models.Lobby currentLobby = null;
    private static bool isTheOneWhoKnocks = false;
    private static bool signedIn = false;
    private int enemyElo;

    private float scoreOfMatch;
    private bool enemyResigned;

    private Allocation hostAllocation;

    [SerializeField] private GameObject createInputField;
    [SerializeField] private GameObject searchInputField;
    [SerializeField] private GameObject tierSlider;

    void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    private async void Start() {
        if (!signedIn) {
            InitializationOptions options = new InitializationOptions();
            if (ClonesManager.IsClone()) {
                options.SetProfile("clone");
            }

            if (!signedIn) {
                await UnityServices.InitializeAsync(options);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("SIGNED IN: " + AuthenticationService.Instance.PlayerId);
                PlayerPrefs.SetInt("Elo", 100);
                Debug.Log("Bestowed Elo: " + PlayerPrefs.GetInt("Elo"));
            }

            signedIn = true;

            leftLobby = false;
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
        if (SceneManager.GetActiveScene().name == "MultiplayerMainMenu") {
            if (createInputField == null) createInputField = GameObject.Find("CreateCustomField");
            if (searchInputField == null) searchInputField = GameObject.Find("SearchCustomField");
            if (tierSlider == null) tierSlider = GameObject.Find("TierSlider");
        }
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
                        { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                        { "Info", new DataObject(DataObject.VisibilityOptions.Public, lobbyInfo.ToString()) }
                    };
                    currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
                }
                if (!isTheOneWhoKnocks && currentLobby.Data["RelayJoinCode"].Value == "rjc") {//nullref
                    Debug.Log("searching for cod");
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                }
                if (currentLobby.Data["RelayJoinCode"].Value != "rjc") {
                    Debug.Log("cod!");
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
            checkUpdateCurLobbyWithNewInfo();
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

        if (timer > 1f && currentLobby != null) {//nullref
            inMoodForUpdate = true;
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

    bool inMoodForUpdate;

    public void forceLobbyUpdate() {
        inMoodForUpdate = true;
    }

    private async void checkUpdateCurLobbyWithNewInfo() {
        if (!inMoodForUpdate || currentLobby == null || currentLobby.Players.Count == 2) return;
        inMoodForUpdate = false;
        lobbyInfo = new LobbyInfo((int) tierSlider.GetComponent<Slider>().value, PlayerPrefs.GetInt("Elo"), lobbyInfo.isPrivate, lobbyInfo.isRanked, lobbyInfo.accessCode);
        if (!isTheOneWhoKnocks || querying) return;
        UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>()
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "rjc") },
                { "Info", new DataObject(DataObject.VisibilityOptions.Public, lobbyInfo.ToString()) }
            }
        };
        currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, updateLobbyOptions);
        Debug.Log("tier: " + LobbyInfo.Parse(currentLobby.Data["Info"].Value).tier);
    }

    public async void startMatchPublic(bool isRanked) {
        startMatch(false, isRanked, true);
    }

    public async void startMatchPrivate(bool creatingLobby) {
        startMatch(true, false, creatingLobby);
    }

    private async void startMatch(bool isPrivate, bool isRanked, bool creatingLobby) {
        if (currentLobby != null) {
            Debug.Log("nuh uh buddy");
            return;
        }
        lobbyInfo = new LobbyInfo((int) tierSlider.GetComponent<Slider>().value, PlayerPrefs.GetInt("Elo"), isPrivate, isRanked, creatingLobby ? createInputField.GetComponent<TMP_InputField>().text : searchInputField.GetComponent<TMP_InputField>().text);
        bool canJoin = await tryJoinLobby();
        if (!canJoin) createLobby();
    }

    LobbyInfo lobbyInfo;

    private async void createLobby() {
        string name = "Come Up With Something Funny Later";
        int maxPlayers = 2;

        CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>()
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "rjc") },
                { "Info", new DataObject(DataObject.VisibilityOptions.Public, lobbyInfo.ToString()) }
            }
        };
        Unity.Services.Lobbies.Models.Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, createLobbyOptions);
        currentLobby = lobby;
        isTheOneWhoKnocks = true;
        Debug.Log("created lobby");
    }

    private async Task<bool> tryJoinLobby() {
        QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
        foreach (Unity.Services.Lobbies.Models.Lobby l in queryResponse.Results) {
            if (LobbyInfo.Parse(l.Data["Info"].Value).isSuitableMatch(lobbyInfo)) {
                await LobbyService.Instance.JoinLobbyByIdAsync(l.Id);
                currentLobby = l;
                isTheOneWhoKnocks = false;
                Debug.Log("joined lobby");
                return true;
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

            if (LobbyInfo.Parse(currentLobby.Data["Info"].Value).isRanked) MultiplayerDuelScoring.applyScoringToPlayer(enemyElo, scoreOfMatch);

            string playerId = AuthenticationService.Instance.PlayerId;
            if (NetworkManager.Singleton != null) {
                if (isTheOneWhoKnocks) {
                    NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerMainMenu", LoadSceneMode.Single);
                } else {
                    SceneManager.LoadScene("MultiplayerMainMenu", LoadSceneMode.Single);
                }
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

public class LobbyInfo {
    public int tier;
    public int elo;
    public bool isPrivate;
    public bool isRanked;
    public string accessCode;

    public LobbyInfo() {}

    public LobbyInfo(int tier, int elo, bool isPrivate, bool isRanked, string accessCode) {
        this.tier = tier;
        this.elo = elo;
        this.isPrivate = isPrivate;
        this.isRanked = isRanked;
        this.accessCode = accessCode;
    }

    public override string ToString() {
        return tier.ToString() + ", " +
               elo.ToString() + ", " +
               isPrivate.ToString() + ", " +
               isRanked.ToString() + ", " +
               accessCode.ToString();
    }

    public static LobbyInfo Parse(string infoAsString) {
        string[] split = Regex.Split(infoAsString, ", ");
        int fieldAmt = typeof(LobbyInfo).GetFields().Length;
        string accessCodePastedBackTogether = "";
        for (int i = fieldAmt - 1; i < split.Length; i++) {
            accessCodePastedBackTogether += split[i];
        }
        return new LobbyInfo(
            int.Parse(split[0]),
            int.Parse(split[1]),
            bool.Parse(split[2]),
            bool.Parse(split[3]),
            accessCodePastedBackTogether
        );
    }

    public bool isSuitableMatch(LobbyInfo myInfo) {
        if (!isPrivate) {
            if (tier != myInfo.tier) return false;
            if (Mathf.Abs(elo - myInfo.elo) > Lobby.maxEloDifference && isRanked) return false;
            if (isRanked != myInfo.isRanked) return false;
        } else if (accessCode != myInfo.accessCode) {
            return false;
        }
        return true;
    }
}
