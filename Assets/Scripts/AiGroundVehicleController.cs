using UnityEngine;
using static Utils;

public class AiGroundVehicleController : GroundVehicleController {
    new void Update() { 
        setGunnersToManual(false);
        base.Update();
    }

    protected override void handleFacing() {
        bool goingReverse = moveDir().x / transform.right.x < 0f;
        if (goingReverse) {
            transform.localScale = new Vector3(1f, transform.localScale.y * -1f, 1f);
            transform.localEulerAngles += new Vector3(0f, 0f, 180f);
        }
    }

    protected override Vector3 moveDir() {
        if (targetedObj == null) return new Vector3(0,0,0);
        return Vector3.Project(targetedObj.transform.position - transform.position, transform.right).normalized;
    }
}
