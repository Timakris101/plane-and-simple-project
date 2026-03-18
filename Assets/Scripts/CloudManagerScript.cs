using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudManagerScript : MonoBehaviour {
    [SerializeField] private GameObject[] clouds;
    [SerializeField] private float cloudAmt;
    [SerializeField] private float minAltitude;
    [SerializeField] private float maxAltitude;
    [SerializeField] private float minX;
    [SerializeField] private float maxX;
    private float maxSize = 80f;

    void Update() {
        if (GameObject.FindGameObjectsWithTag("Cloud").Length < cloudAmt) {
            GameObject newCloud = Instantiate(clouds[Random.Range(0, clouds.Length)], new Vector3(Random.Range(minX, maxX), Random.Range(minAltitude, maxAltitude), 0f), Quaternion.identity);
            float bigness = Random.Range(maxSize / 2f, maxSize);
            newCloud.transform.localScale = new Vector3(bigness * (Random.Range(0, 2) * 2 - 1), bigness, 1f);
        }
    }
}
