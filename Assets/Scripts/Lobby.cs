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

public class Lobby : NetworkBehaviour {
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

    private GameObject selectionScreen;

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
                PlayerPrefs.SetInt("Elo", PlayerPrefs.GetInt("Elo", 100));
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
        // NetworkManager.Singleton.LocalClient.PlayerObject.transform.position += new Vector3(500f, 0f, 0f);
        // NetworkManager.Singleton.LocalClient.PlayerObject.transform.localEulerAngles += new Vector3(0f, 0f, 180f);
        goToSelectionScreen();
    }

    public async void StartClientWithRelay() {
        Debug.Log(currentLobby.Data["RelayJoinCode"].Value);
        string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: relayJoinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "udp"));
        NetworkManager.Singleton.StartClient();
        goToSelectionScreen();
    }


    private static bool gameStarted;
    private static float timer;
    private static bool querying;
    private static int roundNum;
    private static int roundAmt = 3;
    private static int tickets;
    private static int startTickets = 10;
    private static bool inSelectionScreen = false;
    private bool isEnemyReady;
    private bool isSelfReady;
    private string mySelected;
    private string enemySelection;
    private static float selectionTimer;
    private bool deathSent;
    private float altPerTicket = 500f;
    async void Update() {
        if (PlayerPrefs.GetInt("Elo", 100) < 100) PlayerPrefs.SetInt("Elo", 100);

        if (GameObject.Find("SelectionScreen") != null) selectionScreen = GameObject.Find("SelectionScreen");

        if (SceneManager.GetActiveScene().name == "MultiplayerMainMenu") {
            if (createInputField == null) createInputField = GameObject.Find("CreateCustomField");
            if (searchInputField == null) searchInputField = GameObject.Find("SearchCustomField");
            if (tierSlider == null) tierSlider = GameObject.Find("TierSlider");
        }
        if (NetworkManager.Singleton != null) {
            if (NetworkManager.Singleton.IsConnectedClient) sendEloToEnemyRpc(PlayerPrefs.GetInt("Elo"));
        }

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
                    roundNum = 0;
                    tickets = startTickets;
                    scoreOfMatch = 0f;
                }
                return;
            }
            checkUpdateCurLobbyWithNewInfo();
        }

        if (inSelectionScreen) {
            if (GameObject.Find("SelectionScreen") != null) selectionScreen = GameObject.Find("SelectionScreen");
            GameObject.Find("Camera").GetComponent<CamScript>().uncoupleCam();
            GameObject.Find("Camera").transform.parent = null;
            //hide(selectionScreen, false);
            Debug.Log("Sel: " + (mySelected == null ? "bf110" : mySelected));
            makeSelection((mySelected == null ? "bf110" : mySelected));
            makeEnemySelection((enemySelection == null ? "bf110" : enemySelection));
            Debug.Log("areeee you ready kids: " + isSelfReady);
            selectionTimer -= Time.deltaTime;
            selectionScreen.transform.Find("Timer").GetComponent<TMP_Text>().text = selectionTimer.ToString();
            if (selectionTimer < 0) {
                isSelfReady = true;
                sendReadinessToEnemyRpc();
                Debug.Log("readiness forced");
            }

            foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("YourOptions").gameObject)) {
                if (b.transform.GetChild(1) == null) continue;
                int cost = int.Parse(b.transform.GetChild(1).GetComponent<TMP_Text>().text);
                if (cost > tickets) {
                    b.GetComponent<Button>().interactable = false;
                }
            }

            GameObject altitudeSlider = progenyWithScript<Slider>(selectionScreen.transform.Find("YourOptions").gameObject)[0];
            int costOfCur = tickets;
            foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("YourOptions").gameObject)) {
                if (b.transform.GetChild(1) == null) continue;
                int cost = int.Parse(b.transform.GetChild(1).GetComponent<TMP_Text>().text);
                if (b.transform.GetChild(0).GetComponent<TMP_Text>().text == mySelected && mySelected != null) costOfCur = cost;
            }
            float maxAltAvailable = (float) Mathf.Min(4, tickets - costOfCur);

            altitudeSlider.GetComponent<RectTransform>().offsetMax = new Vector2(altitudeSlider.GetComponent<RectTransform>().offsetMax.x, -(4f - maxAltAvailable) / 4f * altitudeSlider.transform.parent.GetComponent<RectTransform>().rect.height);
            altitudeSlider.GetComponent<Slider>().maxValue = maxAltAvailable;
            altitudeSlider.transform.Find("MaxAlt").GetComponent<TMP_Text>().text = maxAltAvailable.ToString();

            sendAltToEnemyRpc((int) altitudeSlider.GetComponent<Slider>().value);

            selectionScreen.transform.Find("YourOptions").Find("Tickets").GetComponent<TMP_Text>().text = (tickets - costOfCur - altitudeSlider.GetComponent<Slider>().value).ToString();

            sendTicketCountToEnemyRpc((int) (tickets - costOfCur - altitudeSlider.GetComponent<Slider>().value));
            
            if (isEnemyReady && isSelfReady) {
                startRound();
                spawnPlayer(mySelected, costOfCur + (int) altitudeSlider.GetComponent<Slider>().value, altitudeSlider.GetComponent<Slider>().value * altPerTicket);
            }
        }

        if (gameStarted && !inSelectionScreen && NetworkManager.Singleton.LocalClient.PlayerObject != null) {
            GameObject.Find("Camera").GetComponent<CamScript>().takeControlOfVehicle(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject);

            GameObject enemyPlayer = null;
            foreach (GameObject g in allVehiclesOfTags("Plane")) {
                Debug.Log("checking for enemy");
                if (g != NetworkManager.Singleton.LocalClient.PlayerObject.gameObject) enemyPlayer = g;
            }

            enemyPlayer.GetComponent<AllianceHolder>().setAlliance("enemy");

            if (enemyPlayer != null) {
                Debug.Log("EP: " + enemyPlayer);

                isSelfReady = false;
                isEnemyReady = false;
                Debug.Log(enemyPlayer.GetComponent<VehicleController>().vehicleDead());
                if (enemyPlayer.GetComponent<VehicleController>().vehicleDead() || deathSent) {
                    scoreOfMatch += 1f / roundAmt;
                    roundNum++;
                    goToSelectionScreen();
                }
                if (NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<VehicleController>().vehicleDead()) {
                    scoreOfMatch += 0f / roundAmt;
                    roundNum++;
                    sendDeathMessageRpc();
                    goToSelectionScreen();
                }
                if (enemyPlayer.GetComponent<VehicleController>().vehicleDead() && NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<VehicleController>().vehicleDead()) {
                    scoreOfMatch += 0.5f / roundAmt;
                    roundNum++;
                    goToSelectionScreen();
                }

                Debug.Log("score: " + scoreOfMatch);

                if (roundNum == roundAmt) leaveLobby(false);
            }
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

    [Rpc(SendTo.NotMe)]
    private void sendTicketCountToEnemyRpc(int amt) {
        selectionScreen.transform.Find("EnemyOptions").Find("Tickets").GetComponent<TMP_Text>().text = amt.ToString();
    }

    [Rpc(SendTo.NotMe)]
    private void sendDeathMessageRpc() {
        deathSent = true;
    }

    [Rpc(SendTo.NotMe)]
    private void sendAltToEnemyRpc(int val) {
        GameObject altitudeSlider = progenyWithScript<Slider>(selectionScreen.transform.Find("EnemyOptions").gameObject)[0];
        altitudeSlider.GetComponent<Slider>().value = val;
    }

    private void spawnPlayer(string selection, int cost, float additionalAlt) {
        tickets -= cost;
        Debug.Log("host: " + isTheOneWhoKnocks);
        GameObject.Find("MultiplayerCreateAndDestroy").GetComponent<MultiplayerCreateAndDestroy>().createServerRpc(selection + "Multiplayer", new Vector3(0f, 200f + additionalAlt, 0f), Quaternion.identity, NetworkManager.Singleton.LocalClientId);
        if (isTheOneWhoKnocks) {
            NetworkManager.Singleton.LocalClient.PlayerObject.transform.position += new Vector3(500f, 0f, 0f);
            NetworkManager.Singleton.LocalClient.PlayerObject.transform.localEulerAngles += new Vector3(0f, 0f, 180f);
        }
    }

    public void setReadiness() {
        isSelfReady = true;
        sendReadinessToEnemyRpc();
    }

    public void makeSelection(string selection) {
        if (GameObject.Find("SelectionScreen") != null) selectionScreen = GameObject.Find("SelectionScreen");
        mySelected = selection;
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("YourOptions").gameObject)) {
            Debug.Log("selecting: " + selection + "; " + "comparing: " + b.transform.GetChild(0).GetComponent<TMP_Text>().text);
            if (b.transform.GetChild(0).GetComponent<TMP_Text>().text == selection) {
                int cost = int.Parse(b.transform.GetChild(1).GetComponent<TMP_Text>().text);
            }
            progenyWithScript<Image>(b)[0].GetComponent<Image>().enabled = b.transform.GetChild(0).GetComponent<TMP_Text>().text == selection;
        }
        sendSelectionToEnemyRpc(selection);
    }

    public void makeEnemySelection(string es) {
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("EnemyOptions").gameObject)) {
            progenyWithScript<Image>(b)[0].GetComponent<Image>().enabled = b.transform.GetChild(0).GetComponent<TMP_Text>().text == es;
        }
    }

    [Rpc(SendTo.NotMe)]
    public void sendSelectionToEnemyRpc(string selection) {
        enemySelection = selection;
        if (!selectionScreen.GetComponent<Image>().enabled) return;
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("EnemyOptions").gameObject)) {
            progenyWithScript<Image>(b)[0].GetComponent<Image>().enabled = b.transform.GetChild(0).GetComponent<TMP_Text>().text == selection;
        }
    }

    [Rpc(SendTo.NotMe)]
    public void sendReadinessToEnemyRpc() {
        GameObject.Find("Lobby").GetComponent<Lobby>().isEnemyReady = true;
        Debug.Log("EnemyReadiness: " + isEnemyReady);
    }

    public void goToSelectionScreen() {
        GameObject.Find("Camera").GetComponent<CamScript>().uncoupleCam();
        GameObject.Find("Camera").transform.parent = null;
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null) {
            Debug.Log("por favor destruirlo" + NetworkManager.Singleton.LocalClient.PlayerObject);
            GameObject.Find("MultiplayerCreateAndDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject, .1f);
        }
        // if (GameObject.Find("SelectionScreen") != null) selectionScreen = GameObject.Find("SelectionScreen");
        selectionTimer = 100f;
        inSelectionScreen = true;

        if (mySelected == null) {
            makeSelection("bf110");
        }

        int costOfCur = startTickets + 1;
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("YourOptions").gameObject)) {
            if (b.transform.GetChild(1) == null) continue;
            int cost = int.Parse(b.transform.GetChild(1).GetComponent<TMP_Text>().text);
            if (b.transform.GetChild(0).GetComponent<TMP_Text>().text == mySelected && mySelected != "") costOfCur = cost;
        }

        Debug.Log("costofcur: " + costOfCur);

        if (costOfCur <= tickets) {
            makeSelection(mySelected);
        }

        hide(selectionScreen, false);
    }

    public void hideScreen() {
        inSelectionScreen = false;
        hide(selectionScreen, true);
    }

    public void startRound() {
        deathSent = false;
        hideScreen();
        Debug.Log("Startrnd");
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
            inSelectionScreen = false;
            leftLobby = true;
            if (resigned && NetworkManager.Singleton != null) sendResignationToOthersRpc();

            if (enemyResigned) scoreOfMatch = 1;
            if (resigned) scoreOfMatch = 0;
            if (enemyResigned && resigned) scoreOfMatch = .5f;

            enemyResigned = false;
            resigned = false;

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
