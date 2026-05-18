using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using Unity.Netcode;

public class CamScript : MonoBehaviour {

    private Vector3 offset;
    [SerializeField] private Bounds bounds;

    [Header("Zoom")]
    [SerializeField] private bool zoomed;
    [SerializeField] private float zoomInFoV;
    [SerializeField] private float zoomOutFoV;
    private PIDController zoomPID = new PIDController(4f, .3f, 0f);
    private PIDController movePID = new PIDController(.1f, 0f, 0f);

    [Header("Mode")]
    [SerializeField] private bool missionEditor;
    [SerializeField] private bool mainMenu;

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
        if (mainMenu) return;

        offset = new Vector3(0, 0, transform.position.z);
        
        GameObject vehicleToControlAtStart = null;
        if (vehicleToControl == null) {
            vehicleToControlAtStart = findNewVehicle(startingAlliance);
        } else {
            vehicleToControlAtStart = vehicleToControl;
        }
        takeControlOfVehicle(vehicleToControlAtStart);
        if (vehicleToControlAtStart != null) {
            transform.position = vehicleToControlAtStart.transform.position;
        }
        matchParentToPlane();

        initCrosshairPos = GameObject.Find("Canvas").transform.Find("CrosshairHolder").GetChild(0).localPosition;

        dialHandler = GameObject.Find("Canvas").transform.Find("SafeArea").transform.Find("DialHandler").gameObject;
    }

    void Update() {
        if (mainMenu) return;
        
        if (NetworkManager.Singleton != null) {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null) {
                takeControlOfVehicle(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject);
                matchParentToPlane();
            } else {
                handleVehicleSwitching();
            }
        } else {
            handleVehicleSwitching();
        }
        handleCam();
        handleGForceDisp();
        if (getControlledOrSpectatedVehicle() != null) dialHandler.GetComponent<BaseControl>().query();

        handleCrosshair();
        handleArrow();

        if (isScreenShaky && getControlledOrSpectatedVehicle() != null) {
            shakeTimer -= Time.deltaTime;
            transform.position += new Vector3(Random.Range(-shakeMag, shakeMag), Random.Range(-shakeMag, shakeMag), 0);
        }
        if (shakeTimer <= 0 || getControlledOrSpectatedVehicle() == null) {
            shakeTimer = 0;
            shakeMag = 0;
            isScreenShaky = false;
        }
    }

    private bool isScreenShaky;
    private float shakeTimer;
    public float shakeMag;
    public void shakeScreen(float time, float magnitude) {
        isScreenShaky = true;
        if (magnitude >= shakeMag) shakeTimer = time;
        shakeMag += Mathf.Abs(magnitude);
    }

    void LateUpdate() {
        transform.eulerAngles = new Vector3(0, 0, 0);
        transform.localScale = Vector3.one;
    }

    private void handleArrow() {
        Transform arrowHolder = GameObject.Find("Canvas").transform.Find("ArrowHolder");
        if (GameObject.Find("Canvas") != null) {
            if (nearestEnemy() != null && !vehicleToControl.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                Vector3 screenPos = GetComponent<Camera>().WorldToScreenPoint(nearestEnemy().transform.position);
                screenPos = (new Vector3(screenPos.x / Screen.width, screenPos.y / Screen.height, 0));
                if (screenPos.x > 0 && screenPos.x < 1 && screenPos.y > 0 && screenPos.y < 1) {
                    arrowHolder.GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = false;
                } else {
                    arrowHolder.GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = true;
                }
                
                arrowHolder.right = (nearestEnemy().transform.position - vehicleToControl.transform.position).normalized;

                arrowHolder.transform.position = GetComponent<Camera>().WorldToScreenPoint(vehicleToControl.transform.position);
            } else {
                arrowHolder.GetChild(0).GetComponent<UnityEngine.UI.Image>().enabled = false;
            }
        }
    }

    private void handleCrosshair() {
        Transform crosshairHolder = GameObject.Find("Canvas").transform.Find("CrosshairHolder");
        if (GameObject.Find("Canvas") != null && vehicleToControl != null) {
            if (vehicleToControl.GetComponent<PlaneController>() != null) {
                crosshairHolder.transform.position = GetComponent<Camera>().WorldToScreenPoint(vehicleToControl.transform.position);
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
    }

    private void handleGForceDisp() {
        GameObject.Find("Canvas").transform.Find("GForceDisp").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, Mathf.Max(0f, GameObject.Find("Canvas").transform.Find("GForceDisp").GetComponent<UnityEngine.UI.Image>().color.a - Time.deltaTime));
        GameObject.Find("Canvas").transform.Find("GForceDisp").GetComponent<RectTransform>().sizeDelta = GameObject.Find("Canvas").transform.GetComponent<RectTransform>().sizeDelta;
        if (vehicleToControl == null) return;
        if (vehicleToControl.GetComponent<GForcesScript>() == null) return;
        GameObject.Find("Canvas").transform.Find("GForceDisp").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, vehicleToControl.GetComponent<GForcesScript>().howSleepyIsPerson() * Constants.GForceEffectConstants.GlocDarkness);
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
            if (!GameObject.Find("Canvas").GetComponent<SquadronSpawner>().isInEditor()) {
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
        transform.localScale = Vector3.one;
        Camera camera = gameObject.GetComponent<Camera>();
        if (getControlledOrSpectatedVehicle() != null) {
            if (GetComponent<CustomInputs>().zoomCamInput()) zoomed = !zoomed;
            camera.fieldOfView += zoomPID.calculate(camera.fieldOfView, (zoomed ? zoomInFoV : zoomOutFoV), Time.deltaTime) * Time.deltaTime;
        }
        if (Input.touchCount == 0 || Input.touchCount == 2) {
            Vector3 prevPos = GetComponent<CustomInputs>().pointerPositionInput();
            
            if (getControlledOrSpectatedVehicle() == null) {
                if (Input.touchCount == 0) camera.fieldOfView -= Input.mouseScrollDelta.y;
                if (Input.touchCount == 2) camera.fieldOfView -= Vector2.Distance(Input.GetTouch(0).position + Input.GetTouch(0).deltaPosition, Input.GetTouch(1).position + Input.GetTouch(1).deltaPosition) - Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
            }

            if (camera.fieldOfView > maxP) { //makes cam size unable to go above max
                camera.fieldOfView = maxP;
            }
            if (camera.fieldOfView < minP) { //makes cam size unable to go below min
                camera.fieldOfView = minP;
            }

            Vector3 newPos = GetComponent<CustomInputs>().pointerPositionInput();

            if (getControlledOrSpectatedVehicle() == null) transform.position += prevPos - newPos;
        }

        if (transform.parent != null) {
            transform.localPosition = new Vector3(transform.localPosition.x + movePID.calculate(transform.localPosition.x, 0f, Time.deltaTime), transform.localPosition.y + movePID.calculate(transform.localPosition.y, 0f, Time.deltaTime), 0f) + offset;
        } else {
            if (spectatedVehicle != null) {
                transform.position = new Vector3(transform.position.x + movePID.calculate(transform.position.x, spectatedVehicle.transform.position.x, Time.deltaTime), transform.position.y + movePID.calculate(transform.position.y, spectatedVehicle.transform.position.y, Time.deltaTime), 0f);
                transform.position = Vector3.Lerp(transform.position, spectatedVehicle.transform.position, Mathf.Exp(-Mathf.Pow(Vector3.Distance(transform.position, spectatedVehicle.transform.position) / spectatedVehicle.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 2f)));
                transform.position = new Vector3(transform.position.x, transform.position.y, 0f) + offset;
            } else {
                Vector3 movementVec = new Vector3(0, 0, 0);
                if (Input.GetKey("w")) movementVec += new Vector3(0, 1, 0);
                if (Input.GetKey("a")) movementVec += new Vector3(-1, 0, 0);
                if (Input.GetKey("s")) movementVec += new Vector3(0, -1, 0);
                if (Input.GetKey("d")) movementVec += new Vector3(1, 0, 0);
                transform.position += movementVec.normalized * freeCamSpeedScaler * Mathf.Tan(camera.fieldOfView / 2f / 180f * 3.14f) * Time.deltaTime;
                float curCamScaling = (GetComponent<Camera>().WorldToScreenPoint(new Vector3(0,0,0)) - GetComponent<Camera>().WorldToScreenPoint(new Vector3(1,0,0))).magnitude;
                if (Input.touchCount == 1 && freeCamSpeedScaler != 0) {
                    movementVec = -(Vector3) Input.GetTouch(0).deltaPosition / curCamScaling;
                    transform.position += movementVec;
                }
            }
        }
        transform.eulerAngles = new Vector3(0, 0, 0);

        if (camera.WorldToScreenPoint(new Vector3(bounds.min.x, 0, 0)).x > 0) {
            transform.position = new Vector3(bounds.min.x - (camera.ScreenToWorldPoint(new Vector3(0, 0, -transform.position.z)) - transform.position).x, transform.position.y, transform.position.z);
        } 
        if (camera.WorldToScreenPoint(new Vector3(bounds.max.x, 0, 0)).x < camera.pixelWidth) {
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
