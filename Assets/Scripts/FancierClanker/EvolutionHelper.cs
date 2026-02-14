using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class EvolutionHelper : MonoBehaviour {
    public static Layer[] mainBrain = new Layer[0];
    private static float bestVal = -Mathf.Infinity;
    private static GameObject[] currentGeneration = new GameObject[0];
    public static bool called;
    public static float variationMag;
    public static float generationLength;
    public static int generationCounter;

    public string brainVisible;
    public float varmagVisible;
    public int generationCounterVisible;
    public float visibleGenerationLength;

    float timer = 0f;

    void Awake() {
        generationLength = visibleGenerationLength;
        variationMag = varmagVisible;
    }

    void Update() {
        GameObject[] allPlanes = allVehiclesOfTags("Plane", "GroundVehicle");
        List<GameObject> listNetPlanes = new List<GameObject>();
        int index = 0;
        for (int i = 0; i < allPlanes.Length; i++) {
            if (!allPlanes[i].GetComponent<NeuralNetAiPlaneController>().algorithmic) {
                listNetPlanes.Add(allPlanes[i]);
            }
        }
        currentGeneration = listNetPlanes.ToArray();
        if (called) {
            timer += Time.deltaTime;
            if (timer > 5f) {
                timer = 0f;
                called = false;
            }
        }

        //setterType
        variationMag = varmagVisible;
        generationLength = visibleGenerationLength;

        //getterType
        generationCounterVisible = generationCounter;
        brainVisible = "";
        foreach (Layer l in mainBrain) {
            brainVisible += l.ToString() + "\n";
        }
    }

    public static void setMainBrain() {
        generationCounter++;
        called = true;
        foreach (GameObject g in currentGeneration) {
            float totalDamageTaken = 0;
            foreach (GameObject d in g.GetComponent<VehicleController>().getDamageModels()) {
                if (d == null) continue;
                totalDamageTaken += d.GetComponent<DamageModel>().getMaxHealth() - d.GetComponent<DamageModel>().getHealth();
            }
            float totalDamageDealt = 0;
            foreach (GameObject d in g.GetComponent<NeuralNetAiPlaneController>().getTargetedObj().GetComponent<VehicleController>().getDamageModels()) {
                if (d == null) continue;
                totalDamageDealt += d.GetComponent<DamageModel>().getMaxHealth() - d.GetComponent<DamageModel>().getHealth();
            }
            float val = totalDamageDealt - totalDamageTaken - Mathf.Abs(g.GetComponent<Rigidbody2D>().rotation) - (g.GetComponent<NeuralNetAiPlaneController>().getTargetedObj().GetComponent<VehicleController>().vehicleDead() ? 0f : Vector3.Distance(g.transform.position, g.GetComponent<NeuralNetAiPlaneController>().getTargetedObj().transform.position));
            if (g.GetComponent<VehicleController>().vehicleDead()) val -= 1000f;
            if (val > bestVal) {
                bestVal = val;
                mainBrain = g.GetComponent<NeuralNetAiPlaneController>().getBrain();
            }
        }

        Debug.Log("VAL: " + bestVal);
    }
}
