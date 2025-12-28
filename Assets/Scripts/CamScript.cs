using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using Unity.Netcode;

public class CamScript : MonoBehaviour {

    private Vector3 offset;
    [SerializeField] private Bounds bounds;
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

    Vector3 initCrosshairPos;

    GameObject dialHandler;
    
    void Start() {
        offset = new Vector3(0, 0, transform.position.z);

        if (vehicleToControl == null) {
            takeControlOfVehicle(findNewVehicle(startingAlliance));
        } else {
            takeControlOfVehicle(vehicleToControl);
        }
        matchParentToPlane();

        initCrosshairPos = transform.Find("Canvas").Find("CrosshairHolder").GetChild(0).localPosition;

        dialHandler = transform.Find("Canvas").Find("DialHandler").gameObject;
    }

    void Update() {
        if (GameObject.Find("NetworkManager") != null) {
            takeControlOfVehicle(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject);
            matchParentToPlane();
        } else {
            handleVehicleSwitching();
        }
        handleCam();
        handleGForceDisp();

        handleCrosshair();
        handleArrow();
        if (getControlledOrSpectatedVehicle() != null) dialHandler.GetComponent<BaseControl>().query();
    }

    void FixedUpdate() {
        transform.eulerAngles = new Vector3(0, 0, 0);
    }

    private void handleArrow() {
        Transform arrowHolder = transform.Find("Canvas").Find("ArrowHolder");
        if (transform.Find("Canvas") != null) {
            if (nearestEnemy() != null && !vehicleToControl.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                Vector3 screenPos = GetComponent<Camera>().WorldToScreenPoint(nearestEnemy().transform.position);
                screenPos = (new Vector3(screenPos.x / Screen.width, screenPos.y / Screen.height, 0));
                if (screenPos.x > 0 && screenPos.x < 1 && screenPos.y > 0 && screenPos.y < 1) {
                    arrowHolder.GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = false;
                } else {
                    arrowHolder.GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = true;
                }
                
                arrowHolder.right = (nearestEnemy().transform.position - vehicleToControl.transform.position).normalized;

                arrowHolder.GetComponent<RectTransform>().position = GetComponent<Camera>().WorldToScreenPoint(vehicleToControl.transform.position);
            } else {
                arrowHolder.GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = false;
            }
        }
    }

    private void handleCrosshair() {
        Transform crosshairHolder = transform.Find("Canvas").Find("CrosshairHolder");
        if (transform.Find("Canvas") != null && vehicleToControl != null) {
            if (vehicleToControl.GetComponent<PlaneController>() != null) {
                crosshairHolder.GetComponent<RectTransform>().position = GetComponent<Camera>().WorldToScreenPoint(vehicleToControl.transform.position);
                if (!vehicleToControl.GetComponent<PlaneController>().gunnersAreManual()) {
                    crosshairHolder.right = vehicleToControl.transform.right;
                    for (int i = 0; i < crosshairHolder.childCount; i++) {
                        crosshairHolder.GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = true;
                        crosshairHolder.GetChild(i).eulerAngles = new Vector3(0, 0, 0);
                        crosshairHolder.GetChild(i).localPosition = initCrosshairPos;
                    }
                } else {
                    float curCamScaling = (GetComponent<Camera>().WorldToScreenPoint(new Vector3(0,0,0)) - GetComponent<Camera>().WorldToScreenPoint(new Vector3(1,0,0))).magnitude;
                    crosshairHolder.transform.position = GetComponent<Camera>().WorldToScreenPoint(GetComponent<CustomInputs>().pointerPositionInput());
                    for (int i = 0; i < crosshairHolder.childCount; i++) {
                        crosshairHolder.GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = true;
                        crosshairHolder.GetChild(i).localPosition = new Vector3(0,0,0);
                    }
                }
            } else {
                for (int i = 0; i < crosshairHolder.childCount; i++) {
                    crosshairHolder.GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = false;
                }
            }
        } else {
            for (int i = 0; i < crosshairHolder.childCount; i++) {
                crosshairHolder.GetChild(i).GetComponent<UnityEngine.UI.Image>().enabled = false;
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
            if (GetComponent<CustomInputs>().spectatePlaneInput()) {
                if (vehicleToControl == null) {
                    scrollSpectatableVehicles();
                } else {
                    spectatedVehicle = vehicleToControl;
                    vehicleToControl = null;
                }
            }
            if (GetComponent<CustomInputs>().swapPlaneInput()) {
                if (spectatedVehicle == null || spectatedVehicle.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                    scrollCrewedVehicles();
                } else {
                    takeControlOfVehicle(spectatedVehicle);
                    spectatedVehicle = null;
                }
            }
            if (GetComponent<CustomInputs>().escapeInput()) {
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

        if (Input.touchCount == 0) {
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
        } else if (Input.touchCount == 2) {
            Vector3 prevTouchPos = gameObject.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, -transform.position.z));
            
            camera.fieldOfView -= Input.GetTouch(0).deltaPosition.y;

            if (camera.fieldOfView > maxP) { //makes cam size unable to go above max
                camera.fieldOfView = maxP;
            }
            if (camera.fieldOfView < minP) { //makes cam size unable to go below min
                camera.fieldOfView = minP;
            }

            Vector3 newTouchPos = gameObject.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, -transform.position.z));

            transform.position += prevTouchPos - newTouchPos;
        }

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
                if (Input.touchCount == 1) movementVec += (Vector3) Input.GetTouch(0).deltaPosition.normalized;

                transform.position += movementVec.normalized * freeCamSpeedScaler * Mathf.Tan(camera.fieldOfView / 2f / 180f * 3.14f) * Time.deltaTime;
            }
        }
        transform.eulerAngles = new Vector3(0, 0, 0);

        if (camera.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.min.y, 0)).x > 0) {
            transform.position = new Vector3(bounds.min.x - (camera.ScreenToWorldPoint(new Vector3(0, 0, -transform.position.z)) - transform.position).x, transform.position.y, transform.position.z);
        } 
        if (camera.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.min.y, 0)).x < camera.pixelWidth) {
            transform.position = new Vector3(bounds.max.x - (camera.ScreenToWorldPoint(new Vector3(camera.pixelWidth, 0, -transform.position.z)) - transform.position).x, transform.position.y, transform.position.z);
        }
        if (camera.WorldToScreenPoint(new Vector3(0, bounds.min.y, 0)).y > 0) {
            transform.position = new Vector3(transform.position.x, bounds.min.y - (camera.ScreenToWorldPoint(new Vector3(0, 0, -transform.position.z)) - transform.position).y, transform.position.z);
        }
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
            if (vehicles.Length > 0) spectatedVehicle = vehicles[0];
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

            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == alliance) {
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
