using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudManagerScript : MonoBehaviour {
    [SerializeField] private GameObject cloud;
    [SerializeField] private float cloudAmt;
    [SerializeField] private float minAltitude;
    [SerializeField] private float maxAltitude;
    [SerializeField] private float minX;
    [SerializeField] private float maxX;

    void Update() {
        if (GameObject.FindGameObjectsWithTag("Cloud").Length < cloudAmt) {
            Instantiate(cloud, new Vector3(Random.Range(minX, maxX), Random.Range(minAltitude, maxAltitude), 0f), Quaternion.identity);
        }
    }
}
