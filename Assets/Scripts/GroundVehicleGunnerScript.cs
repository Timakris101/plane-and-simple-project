using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class GroundVehicleGunnerScript : GunnerScript {

    [SerializeField] private bool rotatableTurret;
    [SerializeField] private bool ammoHasFuse;
    private GameObject maxAncestor => maxAncestor(gameObject);

    protected override void Start() {}

    protected override void Update() {
        if (GetComponent<DamageModel>().isAlive()) {
            setTargetedObj(maxAncestor.GetComponent<AiGroundVehicleController>().getTargetedObj());
            if (!manualControl) {
                bool inRange = false;
                if (targetedObj != null) {
                    inRange = (transform.position - targetedObj.transform.position).magnitude < maxRange;
                }
                if (targetedObj != null && inRange) {
                    pointGunAt(positionToTarget());
                    attemptToShoot(positionToTarget(), targetInSights());
                    if (ammoHasFuse) transform.GetChild(0).GetComponent<GunScript>().setFuseOfBullets(positionToTarget());
                } else {
                    attemptToShoot(false);
                }
            } else {
                if (maxAncestor.transform.Find("Camera") != null) {
                    Vector3 screenToWorld = maxAncestor.transform.Find("Camera").GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -maxAncestor.transform.Find("Camera").position.z));
                    pointGunAt(new Vector3(screenToWorld.x, screenToWorld.y, 0));
                    attemptToShoot(new Vector3(screenToWorld.x, screenToWorld.y, 0), Input.GetMouseButton(0));
                    if (ammoHasFuse) transform.GetChild(0).GetComponent<GunScript>().setFuseOfBullets(new Vector3(screenToWorld.x, screenToWorld.y, 0));
                }
            }
        } else {
            attemptToShoot(false);
        }
    }

    protected override void pointGunAt(Vector3 pos) {
        bool posToTheFrontSide = Vector3.Project(pos - maxAncestor.transform.position, maxAncestor.transform.right).x / maxAncestor.transform.right.x > 0f;
        
        Vector3 reflectedPos = new Vector3(2 * transform.position.x - pos.x, pos.y, 0);

        Transform gunPivot = transform.GetChild(0).Find("GunPivotPoint");
        Vector3 gunPivotPrevPos = gunPivot.position;

        if (rotatableTurret) {
            float yAnglePrev = transform.parent.localEulerAngles.y;
            transform.parent.localEulerAngles = new Vector3(0, (posToTheFrontSide ? 0 : 180f), 0f);
            float newYAngle = transform.parent.localEulerAngles.y;
            if (yAnglePrev != newYAngle) {
                return;
            }

            float eulersZ = 0f;
            if (posToTheFrontSide) {
                eulersZ = Mathf.Atan2((pos - gunPivotPrevPos).y, (pos - gunPivotPrevPos).x) * Mathf.Rad2Deg - rotOfBase();
                if (eulersZ < 0) eulersZ += 360f;
            }
            if (!posToTheFrontSide) {
                eulersZ = Mathf.Atan2((reflectedPos - gunPivotPrevPos).y, (reflectedPos - gunPivotPrevPos).x) * Mathf.Rad2Deg - rotOfBase();
                if (eulersZ < 0) eulersZ += 360f;
            }
            transform.GetChild(0).eulerAngles = new Vector3(0, transform.parent.eulerAngles.y, boundedGunAngle(eulersZ, minDeflection, maxDeflection) + rotOfBase());
        } else {
            base.pointGunAt(pos, gunPivotPrevPos);
        }

        Vector3 newGunPivotPos = gunPivot.position;

        transform.GetChild(0).position += gunPivotPrevPos - newGunPivotPos;
    }
}
