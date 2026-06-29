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

    public virtual Vector3 moveDir() {
        if (!Object.Equals(GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle(), gameObject)) return new Vector3(0,0,0);
        CustomInputs INPUTS = progenyWithScript<CamScript>(gameObject)[0].GetComponent<CustomInputs>();
        switch (PlayerPrefs.GetString("ControlMode", "Joystick")) {
            case "Joystick": 
                return INPUTS.directionInput() * transform.right * transform.localScale.y;
            case "Joystick1":
                float desiredTheta = INPUTS.directionInput1().y;
                return new Vector3(Mathf.Cos(desiredTheta), Mathf.Sin(desiredTheta), 0f) * INPUTS.directionInput1().x;
            default:
                return Vector3.zero;
        }
    }

    protected virtual void handleFacing() {
        if (!Object.Equals(GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle(), gameObject)) return;
        if (progenyWithScript<CamScript>(gameObject)[0].GetComponent<CustomInputs>().rotateVehicleInput()) {
            transform.localScale = new Vector3(1f, transform.localScale.y * -1f, 1f);
            transform.localEulerAngles += new Vector3(0f, 0f, 180f);
        }
    }

    private void applyForces(Vector3 movementDir) {
        bool goingReverse = movementDir.x / transform.right.x < 0f;
        progenyWithScript<TrackScript>(gameObject)[0].GetComponent<TrackScript>().braking(movementDir.magnitude == 0f || GetComponent<Rigidbody2D>().linearVelocity.x / movementDir.x < 0f);
        GetComponent<Rigidbody2D>().AddForce(Vector3.Project(movementDir, transform.right) * transform.Find("EngineHitbox").GetComponent<EngineScript>().getThrustNewtons(GetComponent<Rigidbody2D>().linearVelocity.magnitude, goingReverse));
    }
        
    public override bool whenToRemoveCamera() {return allCrewGoneFromVehicle();}
}
