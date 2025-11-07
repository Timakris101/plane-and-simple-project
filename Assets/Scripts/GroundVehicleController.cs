using UnityEngine;
using static Utils;

public class GroundVehicleController : VehicleController {

    new void Update() {
        setGunnersToManual(true);
        base.Update();
    }

    public override void handleFeasibleControls() {
        if (progenyWithScript<TrackScript>(gameObject).Count == 0) return;
        if (progenyWithScript<TrackScript>(gameObject)[0].GetComponent<TrackScript>().usable() && !allCrewGoneFromVehicle()) {
            handleFacing();
            applyForces(moveDir());
        } else {
            progenyWithScript<TrackScript>(gameObject)[0].GetComponent<TrackScript>().braking(true);
        }
    }

    protected virtual Vector3 moveDir() {
        Vector3 movementDir = new Vector3(0,0,0);
        if (Input.GetKey("a")) {
            movementDir += -transform.right * transform.localScale.y;
        }
        if (Input.GetKey("d")) {
            movementDir += transform.right * transform.localScale.y;
        }
        return movementDir;
    }

    protected virtual void handleFacing() {
        if (Input.GetKeyDown("q")) {
            if (transform.localScale.y == 1f) {
                transform.localScale = new Vector3(1f, -1f, 1f);
                transform.localEulerAngles += new Vector3(0f, 0f, 180f);
            }
        }
        if (Input.GetKeyDown("e")) {
            if (transform.localScale.y == -1f) {
                transform.localScale = new Vector3(1f, 1f, 1f);
                transform.localEulerAngles += new Vector3(0f, 0f, 180f);
            }
        }
    }

    private void applyForces(Vector3 movementDir) {
        bool goingReverse = movementDir.x / transform.right.x < 0f;
        progenyWithScript<TrackScript>(gameObject)[0].GetComponent<TrackScript>().braking(movementDir.magnitude == 0f);
        GetComponent<Rigidbody2D>().AddForce(movementDir * transform.Find("EngineHitbox").GetComponent<EngineScript>().getThrustNewtons(GetComponent<Rigidbody2D>().linearVelocity.magnitude, goingReverse));
    }
        
    public override bool whenToRemoveCamera() {return allCrewGoneFromVehicle();}
}
