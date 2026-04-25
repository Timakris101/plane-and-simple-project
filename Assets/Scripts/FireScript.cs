using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireScript : MonoBehaviour {

    private GameObject terrain;
    [SerializeField] private bool extinguished = false;

    void Start() {
        terrain = GameObject.Find("Terrain");
    }

    void Update() {
        if (waterLogged() || extinguished) {
            if (GetComponent<ParticleSystem>() == null) {
                Destroy(gameObject);
            } else {
                var emissionModule = GetComponent<ParticleSystem>().emission;
                emissionModule.rateOverTime = 0f;
                emissionModule.rateOverDistance = 0f;
                if (transform.childCount == 1) Destroy(transform.GetChild(0).gameObject);
                if (GetComponent<ParticleSystem>().particleCount == 0 && emissionModule.rateOverTime.constant == 0f) {
                    Destroy(gameObject);
                }
            }
        }
    }

    public void extinguish() {
        extinguished = true;
    }

    private bool waterLogged() {
        return Constants.Water.seaLevel > transform.position.y - 1f;
    }
}
