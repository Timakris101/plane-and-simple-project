using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Utils;

public class VehicleCacheScript : MonoBehaviour {
    protected GameObject[] allVehicles = new GameObject[0];
    protected string[] vehicleTags = {"GroundVehicle", "Plane"};
    [SerializeField] private int framesBeforeChacheUpdate;
    private int frameCounter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        updateVehicleChache();
    }

    // Update is called once per frame
    void Update() {
        frameCounter++;
        if (allVehicles.Length == 0) updateVehicleChache();
        if ((frameCounter + (int) ((float) (0f) * ((float) framesBeforeChacheUpdate / (float) allVehicles.Length))) % (framesBeforeChacheUpdate) == 0) {
            updateVehicleChache();
        }
    }

    public void forceUpdate() {
        updateVehicleChache();
    }

    public void updateVehicleChache() {
        allVehicles = allVehiclesOfTags(vehicleTags);

    }

    public GameObject[] vehicles() {
        return allVehicles;
    }

    public GameObject[] vehicles(string alliance) {
        List<GameObject> list = new List<GameObject>();
        foreach (GameObject v in allVehicles) {
            if (v.GetComponent<AllianceHolder>().getAlliance() != alliance) {
                list.Add(v);
            }
        }
        return list.ToArray();
    }
}
