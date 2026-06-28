using UnityEngine;
using Unity.Services.Core;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
// using ParrelSync;
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
    public Unity.Services.Lobbies.Models.Lobby currentLobby = null;
    [SerializeField] private bool isTheOneWhoKnocks = false;
    private static bool signedIn = false;
    [SerializeField] private int enemyElo;

    [SerializeField] private bool lobbyExists;
    [SerializeField] private float scoreOfMatch;
    [SerializeField] private bool enemyResigned;
    [SerializeField] private bool resigned;

    private Allocation hostAllocation;

    private GameObject createInputField;
    private GameObject searchInputField;
    private GameObject tierSlider;

    private GameObject selectionScreen;

    void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    private async void Start() {
        if (!signedIn) {
            InitializationOptions options = new InitializationOptions();
            // if (ClonesManager.IsClone()) {
            //     string cloneId = ClonesManager.GetArgument();

            //     switch (cloneId)
            //     {
            //         case "0":
            //             options.SetProfile("clone0");
            //             break;

            //         case "1":
            //             options.SetProfile("clone1");
            //             break;

            //         case "2":
            //             options.SetProfile("clone2");
            //             break;

            //         default:
            //             options.SetProfile("clone");
            //             break;
            //     }
            // }

            if (!signedIn) {
                await UnityServices.InitializeAsync(options);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                // Debug.Log("SIGNED IN: " + AuthenticationService.Instance.PlayerId);
                PlayerPrefs.SetInt("Elo", PlayerPrefs.GetInt("Elo", 100));
                // Debug.Log("Bestowed Elo: " + PlayerPrefs.GetInt("Elo"));
            }

            signedIn = true;
        }
    }

    public async void StartHostWithRelay() {
        // Debug.Log(currentLobby.Data["RelayJoinCode"].Value);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(hostAllocation, "udp"));
        NetworkManager.Singleton.StartHost();
        // NetworkManager.Singleton.LocalClient.PlayerObject.transform.position += new Vector3(500f, 0f, 0f);
        // NetworkManager.Singleton.LocalClient.PlayerObject.transform.localEulerAngles += new Vector3(0f, 0f, 180f);
        goToSelectionScreen();
    }

    public async void StartClientWithRelay() {
        // Debug.Log(currentLobby.Data["RelayJoinCode"].Value);
        string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: relayJoinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "udp"));
        NetworkManager.Singleton.StartClient();
        goToSelectionScreen();
    }


    [SerializeField] private bool gameStarted;
    private static float timer;
    [SerializeField] private bool querying;
    [SerializeField] private int roundNum;
    private static int roundAmt = 3;
    [SerializeField] private int tickets;
    private static int startTickets = 10;
    [SerializeField] private bool inSelectionScreen = false;
    private bool isEnemyReady;
    private bool isSelfReady;
    private string mySelected;
    private string enemySelection;
    private static float selectionTimer;
    private bool deathSent;
    private float altPerTicket = 250f;

    private float timeWOnePlayer;
    private float timeInLobbyJustChillin;

    async void Update() {
        lobbyExists = currentLobby != null;
        // PlayerPrefs.SetInt("Elo", 100);
        if (PlayerPrefs.GetInt("Elo", 100) < 100) PlayerPrefs.SetInt("Elo", 100);

        if (GameObject.Find("SelectionScreen") != null) selectionScreen = GameObject.Find("SelectionScreen");

        if (SceneManager.GetActiveScene().name == "MultiplayerMainMenu") {
            if (createInputField == null) createInputField = GameObject.Find("CreateCustomField");
            if (searchInputField == null) searchInputField = GameObject.Find("SearchCustomField");
            if (tierSlider == null) tierSlider = GameObject.Find("TierSlider");

            GameObject.Find("Create").GetComponent<Button>().interactable = createInputField.GetComponent<TMP_InputField>().text != "" && !lobbyExists;
            GameObject.Find("Search").GetComponent<Button>().interactable = searchInputField.GetComponent<TMP_InputField>().text != "" && !lobbyExists;
        }

        if (NetworkManager.Singleton != null && lobbyExists && !leftLobby && SceneManager.GetActiveScene().name == "MultiplayerTest") {
            if (!NetworkManager.Singleton.IsListening) {
                leftLobby = true;
                // Debug.Log("leaving bc no players");
                enemyResigned = roundNum != 0 || !inSelectionScreen;
                endGame(resigned);
            }

            if (NetworkManager.Singleton.ConnectedClientsList.Count == 1) {
                timeWOnePlayer += Time.deltaTime;
                if (timeWOnePlayer > ((roundNum != 0 || !inSelectionScreen) ? 2f : 10f)) {
                    timeWOnePlayer = 0f;
                    leftLobby = true;
                    // Debug.Log("leaving bc no players");
                    enemyResigned = roundNum != 0 || !inSelectionScreen;
                    endGame(resigned);
                }
            }
        }

        if (NetworkManager.Singleton != null) {
            if (roundNum < roundAmt && lobbyExists && NetworkManager.Singleton.IsListening) {
                if (currentLobby.Players.Count == 2 && IsSpawned) sendEloToEnemyRpc(PlayerPrefs.GetInt("Elo"));
            }
        }

        if (selfWantsRematch && enemyWantsRematch) {
            startGame();
        }

        if (roundNum >= roundAmt || enemyResigned) {
            endGame(resigned);
        }
        if (GameObject.Find("Exit") != null) hide(GameObject.Find("Exit"), roundNum >= roundAmt);

        if (GameObject.Find("Cancel") != null && currentLobby != null) GameObject.Find("Cancel").GetComponent<Button>().interactable = currentLobby.Players.Count != 2;

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
                    // Debug.Log("searching for cod");
                    try {
                        currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                    } 
                    catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyNotFound) {
                        if (!LobbyInfo.Parse(currentLobby.Data["Info"].Value).isPrivate) {
                            leaveLobby();
                            bool canJoin = await tryJoinLobby();
                            if (!canJoin) createLobby();
                            Debug.Log("retry");
                            return;
                        }
                    }
                    catch (System.NullReferenceException ex) {
                        // Debug.Log(":(");
                    }
                    // Debug.Log("curcod: " + currentLobby.Data["RelayJoinCode"].Value);
                }
                if (currentLobby.Data["RelayJoinCode"].Value != "rjc" && roundNum == 0) {
                    // Debug.Log("cod!");
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
            } else {
                timeInLobbyJustChillin += Time.deltaTime;
                if (timeInLobbyJustChillin > 10f) {
                    timeInLobbyJustChillin = 0f;
                    if (!LobbyInfo.Parse(currentLobby.Data["Info"].Value).isPrivate) {
                        bool canJoin = await tryJoinLobby();
                        Debug.Log("im so lonely mark, all the other viltrumites fear me");
                        if (canJoin) {
                            Debug.Log("its been a while");
                            leaveLobby();
                        }
                        return;
                    }
                }
            }
            checkUpdateCurLobbyWithNewInfo();
        }

        if (gameStarted && inSelectionScreen && NetworkManager.Singleton?.IsListening == true) {
            handleSelectionScreen();
            hideWinLossScreen();
        }

        if (gameStarted && !inSelectionScreen && !scoringAppliedThisGame && NetworkManager.Singleton?.LocalClient.PlayerObject != null) {
            handleGameplay();
        }

        if (timer > 1f && currentLobby != null) {//nullref
            try {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            } 
            catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyNotFound) {
                if (!LobbyInfo.Parse(currentLobby.Data["Info"].Value).isPrivate) {
                    leaveLobby();
                    bool canJoin = await tryJoinLobby();
                    if (!canJoin) createLobby();
                    Debug.Log("retry");
                    return;
                }
            }
            catch (System.NullReferenceException ex) {
                // Debug.Log(":(");
            }
        }
    }

    private void startGame() {
        resetGameVals();
        hideWinLossScreen();
        CancelInvoke("bringUpWinLossScreen");
        Invoke("goToSelectionScreen", 2f);
        gameStarted = true;
        querying = false;
        scoringAppliedThisGame = false;
    }

    private void handleSelectionScreen() {
        if (GameObject.Find("SelectionScreen") != null) selectionScreen = GameObject.Find("SelectionScreen");
        GameObject.Find("Camera").GetComponent<CamScript>().uncoupleCam();
        GameObject.Find("Camera").transform.parent = null;
        if (selectionScreen == null) return;
        hide(selectionScreen, false);
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("YourOptions").gameObject)) {
            if (b.transform.GetChild(1) == null) continue;
            int cost = int.Parse(b.transform.GetChild(1).GetComponent<TMP_Text>().text);
            if (b.transform.GetChild(0).GetComponent<TMP_Text>().text == mySelected && cost > tickets) {
                makeSelection("bf110");
            }
        }
        // Debug.Log("Sel: " + (mySelected == null ? "bf110" : mySelected));
        makeSelection((mySelected == null ? "bf110" : mySelected));
        makeEnemySelection((enemySelection == null ? "bf110" : enemySelection));
        // Debug.Log("areeee you ready kids: " + isSelfReady);
        selectionTimer -= Time.deltaTime;
        selectionScreen.transform.Find("Timer").GetComponent<TMP_Text>().text = selectionTimer.ToString("G2");
        if (selectionTimer < 0) {
            isSelfReady = true;
            sendReadinessToEnemyRpc();
            // Debug.Log("readiness forced");
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
//-
    [Rpc(SendTo.NotMe)]
    private void sendTicketCountToEnemyRpc(int amt) {
        if (selectionScreen == null) return;
        selectionScreen.transform.Find("EnemyOptions").Find("Tickets").GetComponent<TMP_Text>().text = amt.ToString();
    }

    [Rpc(SendTo.NotMe)]
    private void sendDeathMessageRpc() {
        deathSent = true;
    }

    [Rpc(SendTo.NotMe)]
    private void sendAltToEnemyRpc(int val) {
        if (selectionScreen == null) return;
        GameObject altitudeSlider = progenyWithScript<Slider>(selectionScreen.transform.Find("EnemyOptions").gameObject)[0];
        altitudeSlider.GetComponent<Slider>().value = val;
    }

    private void spawnPlayer(string selection, int cost, float additionalAlt) {
        tickets -= cost;
        // Debug.Log("host: " + isTheOneWhoKnocks);
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
            // Debug.Log("selecting: " + selection + "; " + "comparing: " + b.transform.GetChild(0).GetComponent<TMP_Text>().text);
            if (b.transform.GetChild(0).GetComponent<TMP_Text>().text == selection) {
                int cost = int.Parse(b.transform.GetChild(1).GetComponent<TMP_Text>().text);
            }
            progenyWithScript<Image>(b)[1].GetComponent<Image>().enabled = b.transform.GetChild(0).GetComponent<TMP_Text>().text == selection;
        }
        sendSelectionToEnemyRpc(selection);
    }

    public void makeEnemySelection(string es) {
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("EnemyOptions").gameObject)) {
            progenyWithScript<Image>(b)[1].GetComponent<Image>().enabled = b.transform.GetChild(0).GetComponent<TMP_Text>().text == es;
        }
    }

    [Rpc(SendTo.NotMe)]
    public void sendSelectionToEnemyRpc(string selection) {
        enemySelection = selection;
        if (selectionScreen == null) return;
        if (!selectionScreen.GetComponent<Image>().enabled) return;
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("EnemyOptions").gameObject)) {
            progenyWithScript<Image>(b)[1].GetComponent<Image>().enabled = b.transform.GetChild(0).GetComponent<TMP_Text>().text == selection;
        }
    }

    [Rpc(SendTo.NotMe)]
    public void sendReadinessToEnemyRpc() {
        GameObject.Find("Lobby").GetComponent<Lobby>().isEnemyReady = true;
        // Debug.Log("EnemyReadiness: " + isEnemyReady);
    }

    public void goToSelectionScreen() {
        GameObject.Find("Camera").GetComponent<CamScript>().uncoupleCam();
        GameObject.Find("Camera").transform.parent = null;
        selectionTimer = 99f;
        inSelectionScreen = true;
        transitionIntoSelection = false;
        if (GameObject.Find("SelectionScreen") != null) selectionScreen = GameObject.Find("SelectionScreen");
        if (selectionScreen == null) return;
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null) {
            // Debug.Log("por favor destruirlo" + NetworkManager.Singleton.LocalClient.PlayerObject);
            GameObject.Find("MultiplayerCreateAndDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject, .1f);
        }

        if (mySelected == null) {
            makeSelection("bf110");
        }

        int costOfCur = startTickets + 1;
        foreach (GameObject b in progenyWithScript<Button>(selectionScreen.transform.Find("YourOptions").gameObject)) {
            if (b.transform.GetChild(1) == null) continue;
            int cost = int.Parse(b.transform.GetChild(1).GetComponent<TMP_Text>().text);
            if (b.transform.GetChild(0).GetComponent<TMP_Text>().text == mySelected && mySelected != "") costOfCur = cost;
        }

        // Debug.Log("costofcur: " + costOfCur);

        if (costOfCur <= tickets) {
            makeSelection(mySelected);
        }

        hide(selectionScreen, false);
    }

    public void hideScreen() {
        inSelectionScreen = false;
        if (selectionScreen == null) return;
        hide(selectionScreen, true);
    }

    public void startRound() {
        deathSent = false;
        hideScreen();
        // Debug.Log("Startrnd");
    }
//-

    bool transitionIntoSelection;
    private void handleGameplay() {
        GameObject.Find("Camera").GetComponent<CamScript>().takeControlOfVehicle(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject);

        GameObject enemyPlayer = null;
        foreach (GameObject g in allVehiclesOfTags("Plane")) {
            // Debug.Log("checking for enemy");
            if (g != NetworkManager.Singleton.LocalClient.PlayerObject.gameObject) enemyPlayer = g;
        }

        if (enemyPlayer != null) {
            enemyPlayer.GetComponent<AllianceHolder>().setAlliance("enemy");
            // Debug.Log("EP: " + enemyPlayer);

            isSelfReady = false;
            isEnemyReady = false;
            // Debug.Log(enemyPlayer.GetComponent<VehicleController>().vehicleDead());

            if (transitionIntoSelection) return;

            if (enemyPlayer.GetComponent<VehicleController>().vehicleDead() || deathSent) {
                scoreOfMatch += 1f / roundAmt;
                roundNum++;
                if (roundNum == roundAmt) return;
                Invoke("goToSelectionScreen", 2f);
                transitionIntoSelection = true;
            }
            if (NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<VehicleController>().vehicleDead()) {
                scoreOfMatch += 0f / roundAmt;
                roundNum++;
                sendDeathMessageRpc();
                if (roundNum == roundAmt) return;
                Invoke("goToSelectionScreen", 2f);
                transitionIntoSelection = true;
            }
            if (enemyPlayer.GetComponent<VehicleController>().vehicleDead() && NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<VehicleController>().vehicleDead()) {
                scoreOfMatch += 0.5f / roundAmt;
                roundNum++;
                if (roundNum == roundAmt) return;
                Invoke("goToSelectionScreen", 2f);
                transitionIntoSelection = true;
            }
        }
    }

    private bool scoringAppliedThisGame;
    private bool enemyScoringAppliedThisGame;
    float prevElo;
    int roundGameEnded;
    public void endGame(bool resigned) {
        Invoke("bringUpWinLossScreen", ((resigned || enemyResigned) ? 0f : 2f));
        if (!scoringAppliedThisGame) prevElo = PlayerPrefs.GetInt("Elo");
        if (!scoringAppliedThisGame) roundGameEnded = roundNum - (inSelectionScreen ? 1 : 0);

        hideScreen();
        roundNum = roundAmt;

        this.resigned = resigned;

        gameStarted = false;

        if (!scoringAppliedThisGame) {
            scoreOfMatch += tickets * 0.01f;
            if (enemyResigned) scoreOfMatch = 1;
            if (resigned) scoreOfMatch = 0;
            if (enemyResigned && resigned) scoreOfMatch = .5f;
        }
        if (currentLobby != null) {
            if (LobbyInfo.Parse(currentLobby.Data["Info"].Value).isRanked && !scoringAppliedThisGame && roundGameEnded != -1) MultiplayerDuelScoring.applyScoringToPlayer(enemyElo, scoreOfMatch);
        }
        scoringAppliedThisGame = true;

        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsListening) return;

        // Debug.Log("im resigning it, im resigning it");
        if (resigned) {
            // Debug.Log("yooooo, send it!");
            sendResignationToOthersRpc();
        }

        sendDonenessToOthersRpc();

        // Debug.Log("er: " + enemyResigned + ", r: " + resigned + "; combined: " + (!enemyResigned && !resigned));
    }

    [Rpc(SendTo.NotMe)]
    public void sendDonenessToOthersRpc() {
        enemyScoringAppliedThisGame = true;
    }

    private void hideWinLossScreen() {
        GameObject wlScreen = GameObject.Find("WinLossScreen");
        if (wlScreen == null) return;
        hide(wlScreen, true);
    }

    PIDController eloPid = new PIDController(.1f, 0f, 0f);
    private void bringUpWinLossScreen() {
        GameObject.Find("Camera").GetComponent<CamScript>().uncoupleCam();
        GameObject.Find("Camera").transform.parent = null;
        if (NetworkManager.Singleton?.LocalClient.PlayerObject != null) {
            // Debug.Log("por favor destruirlo" + NetworkManager.Singleton.LocalClient.PlayerObject);
            GameObject.Find("MultiplayerCreateAndDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject, .1f);
        }
        GameObject wlScreen = GameObject.Find("WinLossScreen");
        if (wlScreen == null) return;
        hide(wlScreen, false);
        if (wlScreen.GetComponent<Image>().enabled) {
            bool showRematchButton = !enemyResigned && !resigned && roundGameEnded != -1;
            if (isTheOneWhoKnocks && NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList.Count != 2) showRematchButton = false;
            if (!isTheOneWhoKnocks && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening) showRematchButton = false;
            hide(wlScreen.transform.Find("RematchButton").gameObject, !showRematchButton);
            if (!showRematchButton && isTheOneWhoKnocks) {
                if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
            }
        }
        // Debug.Log("showing rmb: " + wlScreen.transform.Find("RematchButton").gameObject.GetComponent<Image>().enabled);
        if (currentLobby != null) {
            if (LobbyInfo.Parse(currentLobby.Data["Info"].Value).isRanked) {
                float elo = PlayerPrefs.GetInt("Elo");
                prevElo += eloPid.calculate(prevElo, elo, Time.deltaTime);
                string inBetweenString = "";
                if (elo > prevElo) {
                    inBetweenString = "+";
                } else {
                    inBetweenString = "-";
                }
                wlScreen.transform.Find("EloText").GetComponent<TMP_Text>().text = prevElo.ToString("F0") + (Mathf.Abs(elo - prevElo) >= .1f ? " " + inBetweenString + " " + (Mathf.Abs(elo - prevElo) + .9f).ToString("F0") : "");
            } else {
                wlScreen.transform.Find("EloText").GetComponent<TMP_Text>().text = "";
            }
        } else {
            wlScreen.transform.Find("EloText").GetComponent<TMP_Text>().text = "";
        }

        string winLossStr = "DRAW";
        if (scoreOfMatch > .5f) {
            winLossStr = "VICTORY";
        }
        if (scoreOfMatch < .5f) {
            winLossStr = "DEFEAT";
        }
        Debug.Log("rge: " + roundGameEnded);
        if (roundGameEnded == -1) winLossStr = "GAME CANCELLED";
        wlScreen.transform.Find("WinLossText").GetComponent<TMP_Text>().text = winLossStr;
    }

    private bool selfWantsRematch;
    private bool enemyWantsRematch;
    public void selectRematch() {
        // Debug.Log("im rematching it, im rematching it");
        selfWantsRematch = true;
        sendRematchReqToEnemyRpc(true);
    }

    public void unSelectRematch() {
        // Debug.Log("im rematching it, im rematching it");
        selfWantsRematch = false;
        sendRematchReqToEnemyRpc(false);
    }

    [Rpc(SendTo.NotMe)]
    public void sendRematchReqToEnemyRpc(bool wantsRematch) {
        enemyWantsRematch = wantsRematch;
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
        // Debug.Log("tier: " + LobbyInfo.Parse(currentLobby.Data["Info"].Value).tier);
    }

    public async void startMatchPublic(bool isRanked) {
        startMatch(false, isRanked, true);
    }

    public async void startMatchPrivate(bool creatingLobby) {
        startMatch(true, false, creatingLobby);
    }

    private async void startMatch(bool isPrivate, bool isRanked, bool creatingLobby) {
        if (currentLobby != null) {
            leaveLobby();
        }
        lobbyInfo = new LobbyInfo((int) tierSlider.GetComponent<Slider>().value, PlayerPrefs.GetInt("Elo"), isPrivate, isRanked, creatingLobby ? createInputField.GetComponent<TMP_InputField>().text : searchInputField.GetComponent<TMP_InputField>().text);
        if (!isPrivate) {
            bool canJoin = await tryJoinLobby();
            if (!canJoin) createLobby();
        } else {
            if (creatingLobby) {
                createLobby();
            } else {
                bool canJoin = await tryJoinLobby();
                if (!canJoin) GameObject.Find("Cancel").SetActive(false);
            }
        }
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
        leftLobby = false;
        Debug.Log("created lobby");
    }

    private async Task<bool> tryJoinLobby() {
        QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
        foreach (Unity.Services.Lobbies.Models.Lobby l in queryResponse.Results) {
            if (l.Players.Count == 2) continue;
            if (currentLobby != null) {
                if (l.Id == currentLobby.Id) continue;
            }
            if (LobbyInfo.Parse(l.Data["Info"].Value).isSuitableMatch(lobbyInfo)) {
                await LobbyService.Instance.JoinLobbyByIdAsync(l.Id);
                currentLobby = l;
                isTheOneWhoKnocks = false;
                Debug.Log("joined lobby");
                leftLobby = false;
                return true;
            }
        }
        return false;
    }

    bool leftLobby;
    public async void leaveLobby() {
        if (NetworkManager.Singleton.IsListening) unSelectRematch();
        isTheOneWhoKnocks = false;
        querying = false;
        currentLobby = null;
        gameStarted = false;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        if (NetworkManager.Singleton != null && SceneManager.GetActiveScene().name != "MultiplayerMainMenu") {
            if (isTheOneWhoKnocks) {
                NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerMainMenu", LoadSceneMode.Single);
            } else {
                SceneManager.LoadScene("MultiplayerMainMenu", LoadSceneMode.Single);
            }
        }
        resetGameVals();
        if (currentLobby != null) {
            string lobbyId = currentLobby.Id;
            bool wasTheOneWhoKnocks = isTheOneWhoKnocks;
            leftLobby = true;
            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);

            if (currentLobby != null && wasTheOneWhoKnocks) await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
    }

    private void resetGameVals() {
        roundNum = 0;
        inSelectionScreen = false;
        tickets = startTickets;
        scoreOfMatch = 0f;

        scoringAppliedThisGame = false;
        enemyScoringAppliedThisGame = false;

        selfWantsRematch = false;
        enemyWantsRematch = false;

        enemyResigned = false;
        resigned = false;

        querying = false;
        enemyElo = 0;
        timeWOnePlayer = 0f;
        timeInLobbyJustChillin = 0f;
    }

    public async void cancel() {
        if (currentLobby == null) return;
        if (isTheOneWhoKnocks) {
            if (NetworkManager.Singleton != null) Unity.Netcode.NetworkManager.Singleton.Shutdown();
            await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
        }
        leaveLobby();
        Debug.Log("cancelled");
        currentLobby = null;
        isTheOneWhoKnocks = false;
        gameStarted = false;
    }

    void OnApplicationQuit() {
        if (currentLobby != null) endGame(!scoringAppliedThisGame && !enemyScoringAppliedThisGame);
    }

    [Rpc(SendTo.NotMe)]
    public void sendEloToEnemyRpc(int elo) {
        enemyElo = elo;
        // Debug.Log("enemy elo sent: " + enemyElo);
    }

    [Rpc(SendTo.NotMe)]
    public void sendResignationToOthersRpc() {
        // Debug.Log("resignation recieved");
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
            accessCodePastedBackTogether += split[i] + (i != split.Length - 1 ? ", " : "");
        }
        if (infoAsString.Substring(infoAsString.Length - 1).Equals(", ")) accessCodePastedBackTogether += ", ";
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
