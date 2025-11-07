using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class CamScript : MonoBehaviour {

    private Vector3 offset;
    [Header("Mode")]
    [SerializeField] private bool missionEditor;

    [Header("CamStats")]
    [SerializeField] private int minP; //min for perspective
    [SerializeField] private int maxP; //max for perspective
    [SerializeField] private float freeCamSpeedScaler;

    [Header("Vehicle")]
    [SerializeField] private GameObject vehicleToControl;
    [SerializeField] private GameObject spectatedVehicle;
    [SerializeField] private string startingAlliance;

    void Start() {
        offset = new Vector3(0, 0, transform.position.z);

        if (vehicleToControl == null) {
            takeControlOfVehicle(findNewVehicle(startingAlliance));
        } else {
            takeControlOfVehicle(vehicleToControl);
        }
        matchParentToPlane();
    }

    void Update() {
        handleArrow();
        handleVehicleSwitching();
        handleCam();
        handleCrosshair();
        handleGForceDisp();
    }

    void FixedUpdate() {
        transform.eulerAngles = new Vector3(0, 0, 0);
    }

    private void handleArrow() {
        if (transform.Find("Canvas") != null) {
            if (nearestEnemy() != null && !vehicleToControl.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                Vector3 screenPos = GetComponent<Camera>().WorldToScreenPoint(nearestEnemy().transform.position);
                screenPos = (new Vector3(screenPos.x / Screen.width, screenPos.y / Screen.height, 0));
                if (screenPos.x > 0 && screenPos.x < 1 && screenPos.y > 0 && screenPos.y < 1) {
                    transform.Find("Canvas").Find("ArrowHolder").GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = false;
                } else {
                    transform.Find("Canvas").Find("ArrowHolder").GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = true;
                }
                
                transform.Find("Canvas").Find("ArrowHolder").right = (nearestEnemy().transform.position - vehicleToControl.transform.position).normalized;
            } else {
                transform.Find("Canvas").Find("ArrowHolder").GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = false;
            }
        }
    }

    private void handleCrosshair() {
        if (transform.Find("Canvas") != null && vehicleToControl != null) {
            if (vehicleToControl.GetComponent<PlaneController>() != null) {
                if (!vehicleToControl.GetComponent<PlaneController>().gunnersAreManual()) {
                    transform.Find("Canvas").Find("CrosshairHolder").right = vehicleToControl.transform.right;
                    for (int i = 0; i < transform.Find("Canvas").Find("CrosshairHolder").childCount; i++) {
                        transform.Find("Canvas").Find("CrosshairHolder").GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = true;
                        transform.Find("Canvas").Find("CrosshairHolder").GetChild(i).eulerAngles = new Vector3(0, 0, 0);
                    }
                } else {
                    for (int i = 0; i < transform.Find("Canvas").Find("CrosshairHolder").childCount; i++) {
                        transform.Find("Canvas").Find("CrosshairHolder").GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = false;
                    }
                }
            } else {
                for (int i = 0; i < transform.Find("Canvas").Find("CrosshairHolder").childCount; i++) {
                    transform.Find("Canvas").Find("CrosshairHolder").GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = false;
                }
            }
        } else {
            for (int i = 0; i < transform.Find("Canvas").Find("CrosshairHolder").childCount; i++) {
                transform.Find("Canvas").Find("CrosshairHolder").GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = false;
            }
        }
    }

    private void handleGForceDisp() {
        transform.Find("Canvas").Find("GForceDisp").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, Mathf.Max(0f, transform.Find("Canvas").Find("GForceDisp").GetComponent<UnityEngine.UI.Image>().color.a - Time.deltaTime));
        transform.Find("Canvas").Find("GForceDisp").GetComponent<RectTransform>().sizeDelta = transform.Find("Canvas").GetComponent<RectTransform>().sizeDelta;
        if (vehicleToControl == null) return;
        if (vehicleToControl.GetComponent<GForcesScript>() == null) return;
        transform.Find("Canvas").Find("GForceDisp").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, vehicleToControl.GetComponent<GForcesScript>().howSleepyIsPerson() * Constants.GForceEffectConstants.GlocDarkness);
    }

    private void matchParentToPlane() {
        if (vehicleToControl != null) {
            transform.parent = vehicleToControl.transform;
        } else {
            transform.parent = null;
        }
    }

    private void handleVehicleSwitching() {
        if (missionEditor) {
            if (Input.GetKeyDown("n")) {
                if (Input.GetKey(KeyCode.LeftShift)) {
                    if (vehicleToControl == null) {
                        scrollSpectatableVehicles();
                    } else {
                        spectatedVehicle = vehicleToControl;
                        vehicleToControl = null;
                    }
                } else {
                    if (spectatedVehicle == null || spectatedVehicle.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                        scrollCrewedVehicles();
                    } else {
                        takeControlOfVehicle(spectatedVehicle);
                        spectatedVehicle = null;
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) {
                uncoupleCam();
            }
        }
        if (vehicleToControl != null) {
            if (vehicleToControl.GetComponent<VehicleController>().whenToRemoveCamera()) {
                GameObject newPlane = findNewVehicle(vehicleToControl.GetComponent<AllianceHolder>().getAlliance());
                if (newPlane != null) {
                    takeControlOfVehicle(newPlane);
                } else {
                    vehicleToControl = null;
                }
            }
        }
        matchParentToPlane();
    }

    public void uncoupleCam() {
        vehicleToControl = null;
        spectatedVehicle = null;
    }

    private void handleCam() {
        Camera camera = gameObject.GetComponent<Camera>();

        Vector3 prevMousePos = gameObject.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -transform.position.z));
        
        camera.fieldOfView -= Input.mouseScrollDelta.y;

        if (camera.fieldOfView > maxP) { //makes cam size unable to go above max
            camera.fieldOfView = maxP;
        }
        if (camera.fieldOfView < minP) { //makes cam size unable to go below min
            camera.fieldOfView = minP;
        }

        Vector3 newMousePos = gameObject.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -transform.position.z));

        transform.position += prevMousePos - newMousePos;

        if (transform.parent != null) {
            transform.position = transform.parent.position + offset;
        } else {
            if (spectatedVehicle != null) {
                transform.position = spectatedVehicle.transform.position + offset;
            } else {
                Vector3 movementVec = new Vector3(0, 0, 0);
                if (Input.GetKey("w")) movementVec += new Vector3(0, 1, 0);
                if (Input.GetKey("a")) movementVec += new Vector3(-1, 0, 0);
                if (Input.GetKey("s")) movementVec += new Vector3(0, -1, 0);
                if (Input.GetKey("d")) movementVec += new Vector3(1, 0, 0);

                transform.position += movementVec.normalized * freeCamSpeedScaler * Mathf.Tan(camera.fieldOfView / 2f / 180f * 3.14f) * Time.deltaTime;
            }
        }
        transform.eulerAngles = new Vector3(0, 0, 0);
    }

    private void scrollSpectatableVehicles() {
        vehicleToControl = null;

        GameObject[] vehicles = allVehiclesOfTags("Plane", "GroundVehicle");
        if (spectatedVehicle != null) {
            for (int i = 0; i < vehicles.Length; i++) {
                if (vehicles[i] == spectatedVehicle) {
                    spectatedVehicle = vehicles[(i + 1) % vehicles.Length];
                    break;
                }
            }
        } else {
            spectatedVehicle = vehicles[0];
        }
    }

    private void scrollCrewedVehicles() {
        spectatedVehicle = null;

        GameObject[] vehicles = allVehiclesOfTags("Plane", "GroundVehicle");
        List<GameObject> crewedVehicles = new List<GameObject>(); //Note: does not include vehicles with dead crew
        for (int i = 0; i < vehicles.Length; i++) {
            if (!vehicles[i].GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                crewedVehicles.Add(vehicles[i]);
            }
        }
        if (crewedVehicles.Count == 0) {
            vehicleToControl = null;
            return;
        }
        if (vehicleToControl != null) {
            if (!vehicleToControl.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                for (int i = 0; i < crewedVehicles.Count; i++) {
                    if (vehicleToControl == crewedVehicles[i]) {
                        takeControlOfVehicle(crewedVehicles[(i + 1) % crewedVehicles.Count]);
                        break;
                    }
                }
            } else {
                takeControlOfVehicle(crewedVehicles[0]);
            }
        } else {  
            takeControlOfVehicle(crewedVehicles[0]);
        }
    }

    public void takeControlOfVehicle(GameObject vehicle) {
        if (vehicle != null) {
            vehicleToControl = vehicle;
            foreach (VehicleController controller in vehicle.GetComponents<VehicleController>()) {
                controller.enabled = controller.GetType() != aiControllerOfVehicle(vehicle).GetType();
            }
        }
    }

    private GameObject findNewVehicle(string alliance) {
        foreach (GameObject vehicle in allVehiclesOfTags("Plane", "GroundVehicle")) {
            if (vehicle == vehicleToControl) continue;

            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == vehicleToControl.GetComponent<AllianceHolder>().getAlliance()) {
                if (!vehicle.GetComponent<VehicleController>().vehicleDead() && aiControllerOfVehicle(vehicle).enabled) {
                    return vehicle;
                }
            }
        }

        return null;
    }

    private GameObject nearestEnemy() {
        if (vehicleToControl == null) return null;
        GameObject nearestEnemy = null;
        foreach (GameObject vehicle in allVehiclesOfTags("Plane", "GroundVehicle")) {
            if (vehicle == vehicleToControl) continue;

            if (vehicle.GetComponent<AllianceHolder>().getAlliance() != vehicleToControl.GetComponent<AllianceHolder>().getAlliance()) {
                if (!vehicle.GetComponent<VehicleController>().vehicleDead()) {
                    if (nearestEnemy == null) {
                        nearestEnemy = vehicle;
                        continue;
                    }
                    if ((vehicle.transform.position - vehicleToControl.transform.position).magnitude < (nearestEnemy.transform.position - vehicleToControl.transform.position).magnitude) {
                        nearestEnemy = vehicle;
                    }
                }
            }
        }
        return nearestEnemy;
    }

    public GameObject getControlledVehicle() {
        return vehicleToControl;
    }

    public GameObject getControlledOrSpectatedVehicle() {
        if (vehicleToControl != null && spectatedVehicle == null) {
            return vehicleToControl;
        }
        if (vehicleToControl == null && spectatedVehicle != null) {
            return spectatedVehicle;
        } 
        return null;
    }
}
