using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class GunnerScript : MonoBehaviour {

    [SerializeField] protected bool manualControl;
    [SerializeField] protected GameObject targetedObj;
    [SerializeField] protected float angularThreshForGuns;
    [SerializeField] protected float maxRange;
    [SerializeField] private bool isInTail;

    private static bool shootThruSelf = false;
    private static bool rotatingGunners = true;

    private Sprite origSpriteOfPlane;

    private Vector3 positionToCheckShotFrom => (transform.GetChild(0).Find("BulletSpawnArea") == null ? transform.GetChild(0).position : transform.GetChild(0).Find("BulletSpawnArea").position);

    protected List<GameObject> guns;

    protected CustomInputs INPUTS;

    float[] origGunPosY;
    Vector2[] origMinMaxOfGuns;
    void Awake() {
        INPUTS = GameObject.Find("Camera").GetComponent<CustomInputs>();
        guns = progenyWithScript<GunScript>(gameObject);
        origGunPosY = new float[guns.Count];
        origMinMaxOfGuns = new Vector2[guns.Count];
    }

    protected virtual void Start() {
        origSpriteOfPlane = transform.parent.GetComponent<SpriteRenderer>().sprite;
    }

    protected virtual void Update() {
        for (int i = 0; i < guns.Count; i++) {
            if (guns[i] == null) continue;
            if (origGunPosY[i] == 0) origGunPosY[i] = guns[i].transform.localPosition.y + transform.localPosition.y;
            if (origMinMaxOfGuns[i].magnitude == 0) origMinMaxOfGuns[i] = new Vector2(guns[i].GetComponent<GunScript>().minDeflection, guns[i].GetComponent<GunScript>().maxDeflection);
        }
        if (transform.parent.gameObject.layer == LayerMask.NameToLayer("Vehicle")) { //if in plane
            if (isInTail && transform.parent.GetComponent<Animator>().GetBool("Tailless") && GetComponent<DamageModel>().isAlive()) transform.parent.GetComponent<BailoutHandler>().bailCrewMember(gameObject);
            if (transform.parent.gameObject.layer != LayerMask.NameToLayer("Vehicle")) return;
            int animIndex = int.Parse(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Substring(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Length - 1));
            for (int i = 0; i < guns.Count; i++) {
                if (guns[i] == null) continue;
                if (!rotatingGunners) continue;
                guns[i].transform.localPosition = new Vector3(guns[i].transform.localPosition.x,
                                                              origGunPosY[i] * (Mathf.Abs((4 - animIndex) / 2) - 1) - transform.localPosition.y,
                                                              guns[i].transform.localPosition.z);
                if ((Mathf.Abs((4 - animIndex) / 2) - 1) == -1) {
                    guns[i].GetComponent<GunScript>().minDeflection = origMinMaxOfGuns[i].y * (Mathf.Abs((4 - animIndex) / 2) - 1);
                    guns[i].GetComponent<GunScript>().maxDeflection = origMinMaxOfGuns[i].x * (Mathf.Abs((4 - animIndex) / 2) - 1);
                } else {
                    guns[i].GetComponent<GunScript>().minDeflection = origMinMaxOfGuns[i].x;
                    guns[i].GetComponent<GunScript>().maxDeflection = origMinMaxOfGuns[i].y;
                }
            }
            if (GetComponent<DamageModel>().isAlive() && !transform.parent.GetComponent<GForcesScript>().isPersonSleepy()) { //if concious and alive and plane is not spinning out
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
                        Vector3 screenToWorld = INPUTS.pointerPositionInput();
                        pointGunAt(new Vector3(screenToWorld.x, screenToWorld.y, 0));
                        if (hasAmmo()) attemptToShoot(new Vector3(screenToWorld.x, screenToWorld.y, 0), Input.GetMouseButton(0) || Input.touchCount == 1);
                    }
                }
            } else {
                attemptToShoot(false);
            }
        } else {
            if (transform.childCount != 0) {
                foreach (GameObject gun in guns) {
                    Destroy(gun);
                }
            }
        }
    }

    protected bool hasAmmo() {
        foreach (GameObject gun in guns) {
            if (gun.GetComponent<GunScript>().getAmmo() != 0) return true;
        }
        return false;
    } 

    protected void attemptToShoot(Vector3 posToShoot, bool b) {
        foreach (GameObject gun in guns) {
            GameObject gunToLookAt = gun.GetComponent<GunScript>().fixedToOtherGun ? transform.GetChild(0).gameObject : gun;
            bool tooFarFromSight = Mathf.Abs(Vector2.SignedAngle(posToShoot - gunToLookAt.transform.position, gunToLookAt.transform.right)) > angularThreshForGuns;
            bool hitsOwnPlane = false;
            if (!shootThruSelf) {
                RaycastHit2D[] hits = Physics2D.RaycastAll(positionToCheckShotFrom, gunToLookAt.transform.right);
                foreach (RaycastHit2D hit in hits) {
                    if (hit.collider.transform == transform.parent) { //checks if the specific collider is of the parent and not of the children. Hit.transform would return parent transform
                        hitsOwnPlane = true;
                        break;
                    }
                }
            }
            gun.GetComponent<GunScript>().setShooting(b && !hitsOwnPlane && !tooFarFromSight);
        }
    }

    protected void attemptToShoot(bool b) {
        foreach (GameObject gun in guns) {
            gun.GetComponent<GunScript>().setShooting(b);
        }
    }

    protected virtual void pointGunAt(Vector3 pos) {
        foreach (GameObject gun in guns) {
            GameObject gunToLookAt = gun.GetComponent<GunScript>().fixedToOtherGun ? transform.GetChild(0).gameObject : gun;
            gun.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2((pos - gunToLookAt.transform.position).y, (pos - gunToLookAt.transform.position).x) * Mathf.Rad2Deg);
            gun.transform.localEulerAngles = new Vector3(0, 0, boundedGunAngle(gunToLookAt.transform.localEulerAngles.z, gunToLookAt.GetComponent<GunScript>().minDeflection, gunToLookAt.GetComponent<GunScript>().maxDeflection));
        }
    }

    protected virtual void pointGunAt(Vector3 pos, Vector3 pointFromPos) {
        foreach (GameObject gun in guns) {
            GameObject gunToLookAt = gun.GetComponent<GunScript>().fixedToOtherGun ? transform.GetChild(0).gameObject : gun;
            gun.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2((pos - pointFromPos).y, (pos - pointFromPos).x) * Mathf.Rad2Deg);
            gun.transform.localEulerAngles = new Vector3(0, 0, boundedGunAngle(gunToLookAt.transform.transform.localEulerAngles.z, gunToLookAt.GetComponent<GunScript>().minDeflection, gunToLookAt.GetComponent<GunScript>().maxDeflection));
        }
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
        bool inSights = false;
        foreach (GameObject gun in guns) {
            GameObject bullet = gun.transform.GetComponent<GunScript>().getBullet();
            if (!inSights) inSights = Mathf.Abs(Vector3.SignedAngle((positionToTarget() - gun.transform.position).normalized, gun.transform.right, gun.transform.forward)) < angularThreshForGuns;
        }
        return inSights;
    }

    protected virtual Vector3 positionToTarget() {
        GameObject bullet = transform.GetChild(0).GetComponent<GunScript>().getBullet();
        
        Vector3 a = targetedObj.GetComponent<AccelerationHolder>().getAccel() - parentWithScript<AccelerationHolder>(gameObject).GetComponent<AccelerationHolder>().getAccel();
        Vector3 v = targetedObj.GetComponent<Rigidbody2D>().linearVelocity - parentWithScript<Rigidbody2D>(gameObject).GetComponent<Rigidbody2D>().linearVelocity;
        Vector3 p = targetedObj.transform.position - transform.GetChild(0).position;
        float s = bullet.GetComponent<BulletScript>().getInitSpeed();

        float[] coefficients = new float[5];
        coefficients[0] = (Mathf.Pow(a.x, 2f) + Mathf.Pow(a.y, 2f) + Mathf.Pow(a.z, 2f)) / 4f;
        coefficients[1] = (a.x * v.x + a.y * v.y + a.z * v.z);
        coefficients[2] = (Mathf.Pow(v.x, 2f) + p.x * a.x + Mathf.Pow(v.y, 2f) + p.y * a.y + Mathf.Pow(v.z, 2f) + p.z * a.z - Mathf.Pow(s, 2f));
        coefficients[3] = 2f * (p.x * v.x + p.y * v.y + p.z * v.z);
        coefficients[4] = (Mathf.Pow(p.x, 2f) + Mathf.Pow(p.y, 2f) + Mathf.Pow(p.z, 2f));

        float timeOfFlight = newtonRaphson(s == 0 ? 0f : p.magnitude / s, 5, 5f, coefficients);

        if (timeOfFlight == -Mathf.Infinity) return targetedObj.transform.position;
        return targetedObj.transform.position + (Vector3) v * timeOfFlight + (Vector3) (a - (Vector3) Physics2D.gravity) * Mathf.Pow(timeOfFlight, 2) / 2f;
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
