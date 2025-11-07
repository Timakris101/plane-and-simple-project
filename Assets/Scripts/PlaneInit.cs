using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneInit : MonoBehaviour {

    [SerializeField] private float speed;
    [SerializeField] private bool enginesStartOn;

    void Start() {
        GetComponent<Rigidbody2D>().linearVelocity = transform.right * speed;
        for (int i = 0; i < transform.childCount; i++) {
            if (transform.GetChild(i).GetComponent<Collider2D>() != null && transform.GetChild(i).localPosition.magnitude == 0) {
                if (transform.GetChild(i).GetComponent<BoxCollider2D>() != null) {
                    handleBoxCol(transform.GetChild(i));
                }
                if (transform.GetChild(i).GetComponent<PolygonCollider2D>()) {
                    handlePolyCol(transform.GetChild(i));
                }
                handleKiddosOf(transform.GetChild(i));
            }
        }
        
        foreach (PlaneController controller in GetComponents<PlaneController>()) {
            controller.setThrottle(enginesStartOn ? 1 : 0);
            controller.setEngines(enginesStartOn);
        }
    }

    void handleBoxCol(Transform child) {
        child.localPosition += (Vector3) child.GetComponent<Collider2D>().offset;
        child.GetComponent<Collider2D>().offset = new Vector2(0f, 0f);
    }

    void handlePolyCol(Transform child) {
        Vector2[] points = child.GetComponent<PolygonCollider2D>().points;
        Vector2 sum = new Vector2(0, 0);
        foreach (Vector2 point in points) {
            sum += point;
        }
        Vector2 avg = sum / points.Length;

        Vector2[] newPoints = new Vector2[points.Length];
        for (int i = 0; i < newPoints.Length; i++) {
            newPoints[i] = points[i] - avg;
        }
        child.GetComponent<PolygonCollider2D>().SetPath(0, newPoints);

        child.localPosition += (Vector3) avg;

        child.localPosition += (Vector3) child.GetComponent<Collider2D>().offset;
        child.GetComponent<Collider2D>().offset = new Vector2(0f, 0f);
    }

    void handleKiddosOf(Transform parent) {
        for (int j = 0; j < parent.childCount; j++) {
            parent.GetChild(j).localPosition -= parent.localPosition;
        }
    }

    public bool getEnginesStartOn() {
        return enginesStartOn;
    }
}
