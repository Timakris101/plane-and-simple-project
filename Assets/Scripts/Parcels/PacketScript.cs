using UnityEngine;
using static Utils;

public class PacketScript : MonoBehaviour {
    private float fallSpeed = .1f;
    private float lifetime = 60f;
    private float timer = 0f;

    void OnTriggerEnter2D(Collider2D col) {
        if (allObjectsInTreeWith<PacketReciever>(col.gameObject).Count == 0) return;
        if (maxAncestor(col.gameObject).GetComponent<VehicleController>().vehicleDead()) return;
        effect(maxAncestor(col.gameObject));
        Destroy(gameObject);
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer > lifetime) Destroy(gameObject);
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }

    public virtual void effect(GameObject vehicle) {}
}
