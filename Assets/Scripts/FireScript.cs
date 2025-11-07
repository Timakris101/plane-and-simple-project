using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireScript : MonoBehaviour {

    private GameObject terrain;

    void Start() {
        terrain = GameObject.Find("Terrain");
    }

    void Update() {
        if (waterLogged()) {
            if (GetComponent<ParticleSystem>() == null) {
                Destroy(gameObject);
            } else {
                var emissionModule = GetComponent<ParticleSystem>().emission;
                emissionModule.rateOverTime = 0f;
                if (GetComponent<ParticleSystem>().particleCount == 0 && emissionModule.rateOverTime.constant == 0f) {
                    Destroy(gameObject);
                }
            }
        }
    }

    private bool waterLogged() {
        return Constants.Water.seaLevel > transform.position.y - 1f;
    }
}
