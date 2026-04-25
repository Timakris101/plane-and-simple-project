using UnityEngine;
using static Utils;
using System.Collections;
using System.Collections.Generic;

public class FuelTankScript : MonoBehaviour {

    [SerializeField] private float fuelAmt;
    [SerializeField] private float maxFuel;
    [SerializeField] private float leekRate;
    [SerializeField] private float unleekRateRate;
    [SerializeField] private GameObject engine;
    [SerializeField] private GameObject leekEffect;
    private float leekRepresentationAmount = 10f;
    private int maxLeeks = 3;
    private List<GameObject> leeks = new List<GameObject>();

    void Start() {
        engine = allObjectsInTreeWith<EngineScript>(gameObject)[0];
        fuelAmt = maxFuel;
    }

    void Update() {
        if (leekRepresentationAmount * leeks.Count < leekRate + leekRepresentationAmount && leeks.Count < maxLeeks) {
            GameObject newLeek = Instantiate(leekEffect, transform.position, Quaternion.identity, transform);
            newLeek.transform.localPosition += new Vector3(Random.Range(GetComponent<BoxCollider2D>().size.x / 2f, GetComponent<BoxCollider2D>().size.x / 2f) + GetComponent<BoxCollider2D>().offset.x, 
                                                           Random.Range(GetComponent<BoxCollider2D>().size.y / 2f, GetComponent<BoxCollider2D>().size.y / 2f) + GetComponent<BoxCollider2D>().offset.y,
                                                           0f);
            leeks.Add(newLeek);
        }
        if (leekRepresentationAmount * leeks.Count > leekRate || leeks.Count > maxLeeks) {
            GameObject leek = leeks[0];
            leeks.Remove(leek);
            var emissionModule = leek.GetComponent<ParticleSystem>().emission;
            emissionModule.rateOverTime = 0f;
            emissionModule.rateOverDistance = 0f;
            var main = leek.GetComponent<ParticleSystem>().main;
            Destroy(leek, leek.GetComponent<ParticleSystem>().particleCount == 0 ? 0f : main.startLifetime.constantMax);
        }

        if (progenyWithScript<FireScript>(gameObject).Count != 0) {
            maxLeeks = 0;
            if (empty()) progenyWithScript<FireScript>(gameObject)[0].GetComponent<FireScript>().extinguish();
        }

        if (empty()) maxLeeks = 0;

        fuelAmt -= engine.GetComponent<EngineScript>().consumptionRateFuelPerSec() * Time.deltaTime;
        fuelAmt -= leekRate * Time.deltaTime;
        leekRate -= unleekRateRate * Time.deltaTime;
        if (leekRate < 0) leekRate = 0;
        if (fuelAmt < 0) fuelAmt = 0;
    }

    public void setLeekRate(float rate) {
        if (rate <= 0) return;
        leekRate = rate;
    }

    public void addLeekRate(float rate) {
        leekRate += rate;
    }

    public void forceStopLeek() {
        leekRate = 0;
    }

    public bool empty() {
        return fuelAmt <= 0;
    }

    public float fuelPercent() {
        return fuelAmt / maxFuel;
    }

    public void addFuelPercent(float percent) {
        fuelAmt += maxFuel * percent;
        if (fuelAmt > maxFuel) fuelAmt = maxFuel;
    }
}
