using UnityEngine;

public class PacketDropper : MonoBehaviour {

    [SerializeField] private GameObject[] packets;
    private bool dropped;

    void Update() {
        if (!dropped && GetComponent<VehicleController>().vehicleDead()) {
            dropped = true;
            if (packets.Length == 0) return;
            Instantiate(packets[Random.Range(0, packets.Length)], transform.position, Quaternion.identity);
        }
    }
}
