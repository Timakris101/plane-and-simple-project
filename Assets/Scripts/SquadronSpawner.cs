using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class SquadronSpawner : MonoBehaviour {
    [SerializeField] private bool activateOnAwake;

    [Header("Mode")]
    [SerializeField] private bool keepUp;
    [SerializeField] private bool arcade;
    private bool arcadeOn;
    [SerializeField] private bool clankerTraining;
    [SerializeField] private bool menu;

    [Header("SelectionSpawner")]
    [SerializeField] private bool selectionSpawner;
    [SerializeField] private GameObject curSelected;
    [SerializeField] private GameObject[] vehicles;
    [SerializeField] private GameObject baseSpawner;
    [SerializeField] private Sprite unselected;
    [SerializeField] private Sprite selected;

    [Header("Stats")]
    [SerializeField] private GameObject vehicle;
    [SerializeField] private bool containsPlayer;
    [SerializeField] private int amt;
    [SerializeField] private string alliance;
    [SerializeField] private Vector2 offset;
    [SerializeField] new private GameObject camera;
    private Vector3 origCamPos;
    private float origCamSize;
    private HashSet<GameObject> objectsWhichExistedInEditor;

    [Header("InputAreas")]
    [SerializeField] private GameObject amountTextField;
    [SerializeField] private GameObject selectorDropdown;
    [SerializeField] private GameObject containsPlayerToggle;
    [SerializeField] private GameObject allianceDropdown;

    private bool inEditor;

    void Awake() {
        if (activateOnAwake) {
            spawnVehicles();
            GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    void Start() {
        inEditor = true;
        if (selectionSpawner) {
            for (int i = 0; i < vehicles.Length; i++) {
                selectorDropdown.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData(vehicles[i].name));
            }
            selectorDropdown.transform.Find("Label").GetComponent<TMP_Text>().text = baseSpawner.GetComponent<SquadronSpawner>().vehicle.name;
            for (int i = 0; i < selectorDropdown.GetComponent<TMP_Dropdown>().options.Count; i++) {
                if (selectorDropdown.GetComponent<TMP_Dropdown>().options[i].text == selectorDropdown.transform.Find("Label").GetComponent<TMP_Text>().text) {
                    selectorDropdown.GetComponent<TMP_Dropdown>().value = i;
                }
            }
            allianceDropdown.transform.Find("Label").GetComponent<TMP_Text>().text = baseSpawner.GetComponent<SquadronSpawner>().vehicle.GetComponent<AllianceHolder>().getAlliance();
            for (int i = 0; i < allianceDropdown.GetComponent<TMP_Dropdown>().options.Count; i++) {
                if (allianceDropdown.GetComponent<TMP_Dropdown>().options[i].text == allianceDropdown.transform.Find("Label").GetComponent<TMP_Text>().text) {
                    allianceDropdown.GetComponent<TMP_Dropdown>().value = i;
                }
            }
            amountTextField.GetComponent<TMP_InputField>().text = baseSpawner.GetComponent<SquadronSpawner>().amt.ToString();
            containsPlayerToggle.GetComponent<Toggle>().isOn = baseSpawner.GetComponent<SquadronSpawner>().containsPlayer;
        }
        camera = GameObject.Find("Camera");
    }

    void Update() {
        if (camera != null && selectionSpawner && inEditor) {
            if (Input.GetMouseButtonDown(0) || Input.touchCount == 1) {
                foreach (Collider2D col in Physics2D.OverlapCircleAll(camera.GetComponent<CustomInputs>().pointerPositionInput(), .1f)) {
                    if (col.transform.GetComponent<SquadronSpawner>() != null) setCurrentSelectedObj(col.gameObject != curSelected ? col.gameObject : curSelected);
                }
            }
            if (Input.GetKey(KeyCode.Escape)) {
                confirmSqsp();
            }
            if (Input.GetKey(KeyCode.Backspace)) destroySpawner();
            if ((Input.GetMouseButtonDown(1) || Input.touchCount == 2) && curSelected == null) {
                makeNewSpawnerAt(camera.GetComponent<CustomInputs>().pointerPositionInput());
            }
            editSpawner(curSelected);
        }
        if (arcade && arcadeOn) {
            if (keepUp) {
                spawnVehicles(amt - vehicleCount(vehicle.GetComponent<AllianceHolder>().getAlliance()));
            } else {
                if (!anyVehiclesLeft(vehicle.GetComponent<AllianceHolder>().getAlliance())) {
                    spawnVehicles();
                    GameObject.Find("Score").GetComponent<TMP_Text>().text = (int.Parse(GameObject.Find("Score").GetComponent<TMP_Text>().text) + (containsPlayer ? -1 : 1)).ToString();
                }
            }
        }
        if (clankerTraining || menu) {
            //Debug.Log(!anyVehiclesLeft(vehicle.GetComponent<AllianceHolder>().getAlliance()) + alliance);
            if (!anyVehiclesLeft(alliance)) {
                spawnVehicles();
            }
        }
        if (GetComponent<SpriteRenderer>() != null) {
            handleLr();
        }
    }

    private void handleLr() {
        GetComponent<LineRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        if (GetComponent<SpriteRenderer>().enabled) {
            GetComponent<LineRenderer>().SetPosition(0, transform.position);
            GetComponent<LineRenderer>().SetPosition(1, new Vector3(camera.transform.position.x, camera.transform.position.y, 0));
        } else {
            GetComponent<LineRenderer>().SetPosition(0, transform.position);
            GetComponent<LineRenderer>().SetPosition(1, transform.position);
        }
    }

    public void destroySpawner() {
        if (!amountTextField.GetComponent<TMP_InputField>().isFocused) Destroy(curSelected);
    }

    public void makeNewSpawnerAt(Vector3 position) {
        GameObject newSpawner = Instantiate(baseSpawner, position, Quaternion.identity);
        setSpawnerToPanelStats(newSpawner);
        setCurrentSelectedObj(newSpawner);
    }

    public void makeNewSpawnerAt() {
        GameObject newSpawner = Instantiate(baseSpawner, new Vector3(camera.transform.position.x, camera.transform.position.y, 0f), Quaternion.identity);
        setSpawnerToPanelStats(newSpawner);
        setCurrentSelectedObj(newSpawner);
    }

    public void confirmSqsp() {
        setCurrentSelectedObj(curSelected);
        if (curSelected != null) containsPlayerToggle.GetComponent<Toggle>().isOn = false;
        setCurrentSelectedObj(null);
    }

    public bool anyVehiclesLeft(string alliance) {
        foreach (GameObject vehicle in allVehiclesOfTags("Plane", "GroundVehicle")) {
            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == alliance) {
                if (clankerTraining) {
                    return true;
                } else {
                    if (!vehicle.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public int vehicleCount(string alliance) {
        int counter = 0;
        foreach (GameObject vehicle in allVehiclesOfTags("Plane", "GroundVehicle")) {
            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == alliance) {
                if (!vehicle.GetComponent<VehicleController>().allCrewGoneFromVehicle()) {
                    counter++;
                }
            }
        }
        return counter;
    }

    public void editSpawner(GameObject spawnerToEdit) {
        if (spawnerToEdit != null && inEditor) {
            setSpawnerToPanelStats(spawnerToEdit);
            setContainsPlayer(spawnerToEdit);
            if (Input.GetMouseButton(0) || Input.touchCount == 1) {
                Vector3 dir = camera.GetComponent<CustomInputs>().pointerPositionInput() - curSelected.transform.position;
                if (dir.magnitude < curSelected.GetComponent<CircleCollider2D>().radius * 2f) {
                    spawnerToEdit.transform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.normalized.y, dir.normalized.x) * 180f / 3.14f);
                }
            }
            if (Input.GetMouseButton(1) || Input.touchCount == 2) {
                spawnerToEdit.transform.position = camera.GetComponent<CustomInputs>().pointerPositionInput();
            }
        }
    }

    public void setContainsPlayer(GameObject spawnerToEdit) {
        foreach (GameObject spawner in GameObject.FindGameObjectsWithTag("Spawner")) {
            if (spawner != spawnerToEdit && spawnerToEdit.GetComponent<SquadronSpawner>().containsPlayer) {
                spawner.GetComponent<SquadronSpawner>().setContainsPlayer(false);
            }
        }
    }

    private void setSpawnerToPanelStats(GameObject spawnerToEdit) {
        int ignore;
        if (int.TryParse(amountTextField.GetComponent<TMP_InputField>().text, out ignore)) spawnerToEdit.GetComponent<SquadronSpawner>().amt = int.Parse(amountTextField.GetComponent<TMP_InputField>().text);
        spawnerToEdit.GetComponent<SquadronSpawner>().containsPlayer = containsPlayerToggle.GetComponent<Toggle>().isOn;
        spawnerToEdit.GetComponent<SquadronSpawner>().vehicle = vehicles[selectorDropdown.GetComponent<TMP_Dropdown>().value];
        spawnerToEdit.GetComponent<SquadronSpawner>().alliance = allianceDropdown.GetComponent<TMP_Dropdown>().options[allianceDropdown.GetComponent<TMP_Dropdown>().value].text;
    }

    public void setContainsPlayer(bool b) {
        containsPlayer = b;
    }

    public void activateSquadronSpawners() {
        saveObjectsToNotClear();
        origCamPos = camera.transform.position;
        origCamSize = camera.GetComponent<Camera>().fieldOfView;
        foreach (GameObject spawner in GameObject.FindGameObjectsWithTag("Spawner")) {
            spawner.GetComponent<SquadronSpawner>().spawnVehicles();
            spawner.GetComponent<SpriteRenderer>().enabled = false;
            if (spawner.GetComponent<SquadronSpawner>().arcade) spawner.GetComponent<SquadronSpawner>().arcadeOn = true;
        }

        inEditor = false;
    }

    private void saveObjectsToNotClear() {
        objectsWhichExistedInEditor = new HashSet<GameObject>();
        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            objectsWhichExistedInEditor.Add(obj);
        }
    }

    public void goBackToEditor() {
        camera.GetComponent<CamScript>().uncoupleCam();
        camera.transform.parent = null;
        camera.transform.position = origCamPos;
        camera.GetComponent<Camera>().fieldOfView = origCamSize;

        foreach (GameObject spawner in GameObject.FindGameObjectsWithTag("Spawner")) {
            spawner.GetComponent<SpriteRenderer>().enabled = true;
        }

        clearUnsavedObjects();

        inEditor = true;
    }

    private void clearUnsavedObjects() {
        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (!objectsWhichExistedInEditor.Contains(obj)) Destroy(obj);
        }
        objectsWhichExistedInEditor.Clear();
    }

    public void spawnVehicles() {
        spawnVehicles(amt);
    }

    public void spawnVehicles(float amt) {
        List<GameObject> planes = new List<GameObject>();
        for (int i = 0; i < amt; i++) {
            GameObject newVehicle = Instantiate(vehicle, transform.position + (Vector3) offset * i, transform.rotation);
            planes.Add(newVehicle);
            newVehicle.GetComponent<AllianceHolder>().setAlliance(alliance);
            if (containsPlayer && i == 0) {
                camera.GetComponent<CamScript>().takeControlOfVehicle(newVehicle);
                continue;
            }
            foreach (VehicleController vc in newVehicle.GetComponents<VehicleController>()) {
                if (vc == aiControllerOfVehicle(newVehicle)) {
                    vc.enabled = true;
                } else {
                    vc.enabled = false;
                }
            }
        }
        if (amt == 0) return;
        if (aiControllerOfVehicle(planes[0]).GetType().ToString() == "AiPlaneController") {
            foreach (GameObject plane in planes) {
                ((AiPlaneController) aiControllerOfVehicle(plane)).setSquadronList(planes);
                ((AiPlaneController) aiControllerOfVehicle(plane)).setOffset(offset);
                if (menu) Destroy(plane, 120f);
            }
        }
    }

    public void setCurrentSelectedObj(GameObject obj) {
        foreach (GameObject spawner in GameObject.FindGameObjectsWithTag("Spawner")) {
            spawner.GetComponent<SpriteRenderer>().sprite = unselected;
        }
        if (obj != null) {
            curSelected = obj;
            amountTextField.GetComponent<TMP_InputField>().text = curSelected.GetComponent<SquadronSpawner>().amt.ToString();
            containsPlayerToggle.GetComponent<Toggle>().isOn = curSelected.GetComponent<SquadronSpawner>().containsPlayer;
            for (int i = 0; i < selectorDropdown.GetComponent<TMP_Dropdown>().options.Count; i++) {
                if (selectorDropdown.GetComponent<TMP_Dropdown>().options[i].text == curSelected.GetComponent<SquadronSpawner>().vehicle.name) {
                    selectorDropdown.GetComponent<TMP_Dropdown>().value = i;
                }
            }
            selectorDropdown.transform.Find("Label").GetComponent<TMP_Text>().text = selectorDropdown.GetComponent<TMP_Dropdown>().options[selectorDropdown.GetComponent<TMP_Dropdown>().value].text;

            for (int i = 0; i < allianceDropdown.GetComponent<TMP_Dropdown>().options.Count; i++) {
                if (allianceDropdown.GetComponent<TMP_Dropdown>().options[i].text == curSelected.GetComponent<SquadronSpawner>().alliance) {
                    allianceDropdown.GetComponent<TMP_Dropdown>().value = i;
                }
            }
            allianceDropdown.transform.Find("Label").GetComponent<TMP_Text>().text = allianceDropdown.GetComponent<TMP_Dropdown>().options[allianceDropdown.GetComponent<TMP_Dropdown>().value].text;

            curSelected.GetComponent<SpriteRenderer>().sprite = selected;
        } else {
            curSelected = null;
        }
    }

    public bool isInEditor() {return inEditor;}
}
