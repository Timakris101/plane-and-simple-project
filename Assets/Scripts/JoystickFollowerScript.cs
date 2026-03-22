using UnityEngine;

public class JoystickFollowerScript : MonoBehaviour {
    [SerializeField] GameObject handle;
    private PIDController followPID = new PIDController(10f, 1f, 0f).withContinuity(0f, 360f);
    void Update() {
        Vector3 dir = handle.transform.position - transform.position;
        transform.localEulerAngles += new Vector3(0, 0, followPID.calculate(transform.localEulerAngles.z + 90f, Mathf.Atan2(dir.normalized.y, dir.normalized.x) * 180f / 3.14f, Time.deltaTime) * Time.deltaTime);
    }
}
