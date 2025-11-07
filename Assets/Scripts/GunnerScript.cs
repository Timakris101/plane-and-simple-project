using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class GunnerScript : MonoBehaviour {

    [SerializeField] protected bool manualControl;
    [SerializeField] protected GameObject targetedObj;
    [SerializeField] protected float angularThreshForGuns;
    [SerializeField] protected float minDeflection;
    [SerializeField] protected float maxDeflection;
    [SerializeField] protected float maxRange;

    private Sprite origSpriteOfPlane;

    private Vector3 positionToCheckShotFrom => (transform.GetChild(0).Find("BulletSpawnArea") == null ? transform.GetChild(0).position : transform.GetChild(0).Find("BulletSpawnArea").position);

    protected virtual void Start() {
        origSpriteOfPlane = transform.parent.GetComponent<SpriteRenderer>().sprite;
    }

    protected virtual void Update() {
        if (transform.parent.gameObject.layer == LayerMask.NameToLayer("Vehicle")) { //if in plane
            int animIndex = int.Parse(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Substring(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Length - 1));
            if (GetComponent<DamageModel>().isAlive() && !transform.parent.GetComponent<GForcesScript>().isPersonSleepy() && animIndex <= 1) { //if concious and alive and plane is not spinning out
                setTargetedObj(transform.parent.GetComponent<AiPlaneController>().getTargetedObj());

                if (!manualControl) {
                    bool inRange = false;
                    if (targetedObj != null) {
                        inRange = (transform.position - targetedObj.transform.position).magnitude < maxRange;
                    }
                    if (targetedObj != null && inRange) {
                        pointGunAt(positionToTarget());
                        attemptToShoot(positionToTarget(), targetInSights());
                    } else {
                        attemptToShoot(false);
                    }
                } else {
                    if (transform.parent.Find("Camera") != null) {
                        Vector3 screenToWorld = transform.parent.Find("Camera").GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -transform.parent.Find("Camera").position.z));
                        pointGunAt(new Vector3(screenToWorld.x, screenToWorld.y, 0));
                        attemptToShoot(new Vector3(screenToWorld.x, screenToWorld.y, 0), Input.GetMouseButton(0));
                    }
                }
            } else {
                attemptToShoot(false);
            }
        } else {
            if (transform.childCount != 0) {
                Destroy(transform.GetChild(0).gameObject);
            }
        }
    }

    protected void attemptToShoot(Vector3 posToShoot, bool b) {
        bool tooFarFromSight = Mathf.Abs(Vector2.SignedAngle(posToShoot - transform.GetChild(0).position, transform.GetChild(0).right)) > angularThreshForGuns;
        bool hitsOwnPlane = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(positionToCheckShotFrom, transform.GetChild(0).right);
        foreach (RaycastHit2D hit in hits) {
            if (hit.collider.transform == transform.parent) { //checks if the specific collider is of the parent and not of the children. Hit.transform would return parent transform
                hitsOwnPlane = true;
                break;
            }
        }
        transform.GetChild(0).GetComponent<GunScript>().setShooting(b && !hitsOwnPlane && !tooFarFromSight);
    }

    protected void attemptToShoot(bool b) {
        transform.GetChild(0).GetComponent<GunScript>().setShooting(b);
    }

    protected virtual void pointGunAt(Vector3 pos) {
        transform.GetChild(0).eulerAngles = new Vector3(0, 0, Mathf.Atan2((pos - transform.GetChild(0).position).y, (pos - transform.GetChild(0).position).x) * Mathf.Rad2Deg);
        transform.GetChild(0).localEulerAngles = new Vector3(0, 0, boundedGunAngle(transform.GetChild(0).localEulerAngles.z, minDeflection, maxDeflection));
    }

    protected virtual void pointGunAt(Vector3 pos, Vector3 pointFromPos) {
        transform.GetChild(0).eulerAngles = new Vector3(0, 0, Mathf.Atan2((pos - pointFromPos).y, (pos - pointFromPos).x) * Mathf.Rad2Deg);
        transform.GetChild(0).localEulerAngles = new Vector3(0, 0, boundedGunAngle(transform.GetChild(0).localEulerAngles.z, minDeflection, maxDeflection));
    }

    protected float boundedGunAngle(float unboundedAngle, float minDeflection, float maxDeflection) {
        if (minDeflection < maxDeflection) {
            return Mathf.Clamp(unboundedAngle + (unboundedAngle > 180f + (minDeflection + maxDeflection) / 2f ? -360f : 0f), minDeflection, maxDeflection);
        } else {
            return (Mathf.Clamp(unboundedAngle + (unboundedAngle < (minDeflection + maxDeflection) / 2f ? 360f : 0f), minDeflection, maxDeflection + 360f)) - 360f;
        }
    }

    protected float rotOfBase() {
        return transform.parent.eulerAngles.z + (transform.parent.eulerAngles.z < 0f ? 360f : 0f);
    }

    protected bool targetInSights() {
        GameObject bullet = transform.GetChild(0).GetComponent<GunScript>().getBullet();
        return Mathf.Abs(Vector3.SignedAngle((positionToTarget() - transform.GetChild(0).position).normalized, transform.GetChild(0).right, transform.GetChild(0).forward)) < angularThreshForGuns;
    }

    protected virtual Vector3 positionToTarget() {
        GameObject bullet = transform.GetChild(0).GetComponent<GunScript>().getBullet();
        float timeOfFlight = (targetedObj.transform.position - transform.GetChild(0).position).magnitude / (bullet.GetComponent<BulletScript>().getInitSpeed());
        return targetedObj.transform.position + (Vector3) (targetedObj.GetComponent<Rigidbody2D>().linearVelocity - parentWithScript<Rigidbody2D>(gameObject).GetComponent<Rigidbody2D>().linearVelocity) * timeOfFlight - Physics.gravity * Mathf.Pow(timeOfFlight, 2) / 2f;
    }

    public void setManualControl(bool b) {
        manualControl = b;
    }

    public bool getManualControl() {
        return manualControl;
    }

    public void setTargetedObj(GameObject obj) {
        targetedObj = obj;
    }
}
