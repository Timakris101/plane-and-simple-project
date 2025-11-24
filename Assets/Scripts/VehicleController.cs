using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Utils;

public class VehicleController : MonoBehaviour {
    
    private Sprite origSprite;

    protected GameObject[] allVehicles;
    [SerializeField] protected GameObject targetedObj;
    [Tooltip("Put into [nonAi]Controller only")]
    [SerializeField] protected string[] tagsToTarget;
    protected int framesBeforeTargetUpdate => allVehicles.Length;
    protected int frameCounter;
    protected int index;

    VehicleCacheScript vcs;
    
    public void updateLocalVehicleChache() {
        allVehicles = vcs.vehicles();
    }

    public void findTarget() {
        targetedObj = null;
        foreach (GameObject vehicle in allVehicles) {
            if (vehicle.GetComponent<VehicleController>().vehicleDead()) continue;
            if (!tagsToTarget.Contains(vehicle.tag)) continue;
            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == GetComponent<AllianceHolder>().getAlliance()) continue;

            if (targetedObj == null) targetedObj = vehicle;

            if ((vehicle.transform.position - transform.position).magnitude < (targetedObj.transform.position - transform.position).magnitude) {
                targetedObj = vehicle;
            }
        }
    }

    public GameObject getTargetedObj() {
        if (!this.enabled && this == aiControllerOfVehicle(gameObject)) return nonAiControllerOfVehicle(gameObject).targetedObj;
        if (!this.enabled && this == nonAiControllerOfVehicle(gameObject)) return aiControllerOfVehicle(gameObject).targetedObj;
        return targetedObj;
    }

    protected void Awake() {
        origSprite = GetComponent<SpriteRenderer>().sprite;
    }

    public Sprite getOrigSprite() {
        return origSprite;
    }

    protected virtual void Start() {
        vcs = GameObject.Find("VehicleCache").GetComponent<VehicleCacheScript>();
        if (this == aiControllerOfVehicle(gameObject)) tagsToTarget = nonAiControllerOfVehicle(gameObject).tagsToTarget;
        vcs.forceUpdate();
        updateLocalVehicleChache();
        findTarget();
        for (int i = 0; i < allVehicles.Length; i++) {
            if (allVehicles[i] == gameObject) {
                index = i;
                break;
            }
        }
    }

    protected virtual void Update() {
        handleFeasibleControls();
        if (transform.Find("Camera") == null) {
            foreach (VehicleController controller in GetComponents<VehicleController>()) {
                controller.enabled = controller == aiControllerOfVehicle(gameObject);
            }
        }

        frameCounter++;
        
        bool targetDead = false;
        if (targetedObj != null) {
            targetDead = targetedObj.GetComponent<VehicleController>().vehicleDead();
        }
        if ((frameCounter + (int) ((float) (index) * ((float) framesBeforeTargetUpdate / (float) allVehicles.Length))) % (framesBeforeTargetUpdate) == 0 || targetDead) {
            updateLocalVehicleChache();
            findTarget();
        }
    }

    public virtual void handleFeasibleControls() {}

    public virtual bool vehicleDead() {
        bool criticalSystemDamage = false;
        foreach (GameObject damageModel in progenyWithScript<DamageModel>(gameObject)) {
            if (!damageModel.GetComponent<DamageModel>().isCrewRole() && damageModel.GetComponent<DamageModel>().isCritical()) {
                if (!damageModel.GetComponent<DamageModel>().isAlive()) {
                    criticalSystemDamage = true;
                    break;
                }
            }
        }
        if (allCrewGoneFromVehicle()) return true;
        return criticalSystemDamage;
    }

    public bool allCrewGoneFromVehicle() {
        foreach (GameObject damageModel in progenyWithScript<DamageModel>(gameObject)) {
            if (damageModel.GetComponent<DamageModel>().isCrewRole()) {
                if (damageModel.GetComponent<DamageModel>().isAlive()) {
                    return false;
                }
            }
        }
        return true;
    }

    public virtual bool whenToRemoveCamera() {return true;}
    
    protected void setGunnersToManual(bool manual) {
        foreach(GameObject gunner in progenyWithScript<GunnerScript>(gameObject)) {
            gunner.GetComponent<GunnerScript>().setManualControl(manual);
        }
    }

    protected void toggleGunners() {
        foreach(GameObject gunner in progenyWithScript<GunnerScript>(gameObject)) {
            gunner.GetComponent<GunnerScript>().setManualControl(!gunner.GetComponent<GunnerScript>().getManualControl());
        }
    }

    public bool gunnersAreManual() {
        foreach(GameObject gunner in progenyWithScript<GunnerScript>(gameObject)) {
            return gunner.GetComponent<GunnerScript>().getManualControl();
        }
        return false;
    }
}
