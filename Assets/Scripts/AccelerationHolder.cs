using UnityEngine;

public class AccelerationHolder : MonoBehaviour {
    [SerializeField] private Vector3 acceleration;
    private Vector3 prevVel;

    void FixedUpdate() {
        calculateAccel();
    }

    private void calculateAccel() {
        Vector3 curVel = GetComponent<Rigidbody2D>().linearVelocity;
        if (prevVel.magnitude != 0) acceleration = (curVel - prevVel) / Time.fixedDeltaTime;
        
        prevVel = GetComponent<Rigidbody2D>().linearVelocity;
    }

    public Vector3 getAccel() {
        return acceleration;
    }
}
