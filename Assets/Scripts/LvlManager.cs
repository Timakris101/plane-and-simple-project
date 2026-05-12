using UnityEngine;
using static Utils;

public class LvlManager : MonoBehaviour {
    [SerializeField] private bool gameStarted;
    [SerializeField] private GameObject lossScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private int levelNum;
    private PIDController screenPID = new PIDController(.1f, 0f, 0f);

    void Start() {
        winScreen.SetActive(false);
        winScreen.transform.localPosition += 1000f * Vector3.up;
        lossScreen.SetActive(false);
        lossScreen.transform.localPosition += 1000f * Vector3.up;
    }
    
    void Update() {
        if (GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle() == null && gameStarted) {
            Invoke(nameof(bringUpLossScreen), 2f);
            return;
        } else {
            CancelInvoke(nameof(bringUpLossScreen));
        }
        if (allEnemiesGone() && gameStarted) {
            Invoke(nameof(bringUpWinScreen), 2f);
        } else {
            CancelInvoke(nameof(bringUpWinScreen));
        }
    }

    private void bringUpWinScreen() {
        PlayerPrefs.SetInt("MaxLevelUnlocked", levelNum + 1);
        GameObject.Find("Camera").GetComponent<CamScript>().uncoupleCam();
        winScreen.SetActive(true);
        winScreen.transform.localPosition += new Vector3(0f, screenPID.calculate(winScreen.transform.localPosition.y, 0f, Time.deltaTime), 0f);
        this.enabled = false;
    }

    private void bringUpLossScreen() {
        lossScreen.SetActive(true);
        lossScreen.transform.localPosition += new Vector3(0f, screenPID.calculate(lossScreen.transform.localPosition.y, 0f, Time.deltaTime), 0f);
        this.enabled = false;
    }

    private bool allEnemiesGone() {
        if (GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle() == null) return false;
        string friendlyAlliance = GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle().GetComponent<AllianceHolder>().getAlliance();
        foreach (GameObject vehicle in allVehiclesOfTags("Plane", "GroundVehicle")) {
            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == friendlyAlliance) continue;
            if (!vehicle.GetComponent<VehicleController>().vehicleDead()) {
                return false;
            }
        }
        return true;
    }

    public void startGame() {
        gameStarted = true;
    }
}
