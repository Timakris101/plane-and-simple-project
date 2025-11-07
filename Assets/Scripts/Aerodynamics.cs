using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Aerodynamics : MonoBehaviour {

    [Header("Thrust")]
    [SerializeField] private EngineScript es;

    [Header("Lift / Induced Drag")]
    [SerializeField] private AnimationCurve cL;
    [SerializeField] private float wingArea;
    private float startWingArea;
    [SerializeField] private float wingSpan;
    [SerializeField] private float wingEfficiency;

    [Header("Drag")]
    [SerializeField] private AnimationCurve cD;
    [SerializeField] private float baseFrontArea;
    [SerializeField] private float elevatorArea;
    [SerializeField] private float trueFrontArea;

    [Header("Torque")]
    [SerializeField] private AnimationCurve cT;
    [SerializeField] private AnimationCurve torqueStrength;
    [SerializeField] private float irlTurnTime;
    private float baseTorque;
    private float instantaneousTurnRateFactor = 1.5f;
    [SerializeField] private float speedOfControlEffectiveness;

    [Header("Atmosphere")]
    private static float seaLevelAirDensity = 9f;
    private static float normalSeaLevelAirDensity = 1.225f;
    private static float scaleHeight = 8500f;

    private PlaneController pc;

    private Rigidbody2D rb;
    private FlapScript fs;
    private GearScript gs;

    void Awake() {
        baseTorque = 360f / irlTurnTime * Mathf.Pow(seaLevelAirDensity / normalSeaLevelAirDensity, 1f/2f) * instantaneousTurnRateFactor;
        startWingArea = wingArea;

        rb = GetComponent<Rigidbody2D>();
    }

    void Start() {
        setPlaneController(); 
        es = progenyWithScript<EngineScript>(gameObject)[0].GetComponent<EngineScript>(); 
        fs = null;
        if (progenyWithScript<FlapScript>(gameObject).Count != 0) fs = progenyWithScript<FlapScript>(gameObject)[0].GetComponent<FlapScript>();
        gs = null;
        if (progenyWithScript<GearScript>(gameObject).Count != 0) gs = progenyWithScript<GearScript>(gameObject)[0].GetComponent<GearScript>();
        rb.centerOfMass = transform.Find("CoM").localPosition;       
    }

    void Update() {
        setPlaneController();
        updateTrueFrontArea();
        if (fs != null) {
            if (fs.transform.parent == null) fs = null;
        }
        if (gs != null) {
            if (gs.transform.parent == null) gs = null;
        }
        handleTorque();
    }

    void setPlaneController() {
        pc = null;
        foreach (PlaneController c in GetComponents<PlaneController>()) {
            if (c.enabled) {
                pc = c;
                break;
            }
        } 
    }

    void FixedUpdate() {
        handleThrust();
        handleDrag();
        handleLift();
    }

    private void updateTrueFrontArea() {
        trueFrontArea = baseFrontArea + 
                        elevatorArea * Mathf.Abs(pc.getDir()) * torqueStrength.Evaluate(rb.linearVelocity.magnitude) +
                        (fs == null ? 0 : (fs.getFlapDrag() * fs.deflection() / fs.getMaxDeflection())) + 
                        (gs == null ? 0 : gs.getGearDrag());
    }

    private float getAirDensity() {
        return seaLevelAirDensity / Mathf.Exp(transform.position.y / scaleHeight);
    }

    private void handleLift() {
        float liftForce = .5f * (cL.Evaluate(AoA()) + (fs == null ? 0 : (fs.getFlapEffectiveness() * fs.deflection() / fs.getMaxDeflection()))) * getAirDensity() * Mathf.Pow(rb.linearVelocity.magnitude, 2) * wingArea;
        Vector2 liftDir = transform.localScale.y * Vector3.Cross(rb.linearVelocity, -transform.forward).normalized;
        rb.AddForceAtPosition(liftDir * liftForce, transform.Find("CoL").position);
    }

    private void handleDrag() {
        float inducedDragCoef = Mathf.Pow(cL.Evaluate(AoA()), 2) / (Mathf.PI * wingAspectRatio() * wingEfficiency);
        float totalDragCoef = inducedDragCoef + cD.Evaluate(AoA());

        float dragForce = .5f * totalDragCoef * getAirDensity() * Mathf.Pow(rb.linearVelocity.magnitude, 2) * trueFrontArea;

        rb.AddForce(-rb.linearVelocity.normalized * dragForce);
    }

    private void handleTorque() {
        if (pc != null) {
            float dirToTurn = pc.getDir();

            if (rb.linearVelocity.magnitude < speedOfControlEffectiveness) return;
                
            rb.angularVelocity = dirToTurn * torqueStrength.Evaluate(rb.linearVelocity.magnitude) * baseTorque;

            float torque = .5f * cT.Evaluate(AoA()) * transform.localScale.y * Mathf.Pow(rb.linearVelocity.magnitude, 2) * Mathf.Max(wingArea, startWingArea / 2f) * wingSpan * getAirDensity();
            if (Mathf.Abs(AoA()) > 3f) {
                rb.angularVelocity += torque / rb.mass;
            }
        }
    }

    private void handleThrust() {
        rb.AddForce(transform.right * es.getThrustNewtons(rb.linearVelocity.magnitude));
    }

    private float AoA() {
        Vector2 velocity = rb.linearVelocity;
        if (velocity.magnitude < 1f) return 0;
        return Vector3.SignedAngle(velocity, transform.right, transform.forward) * transform.localScale.y;
    }

    private float wingAspectRatio() {
        return (Mathf.Pow(wingSpan, 2)) / wingArea;
    }

    public void setBaseTorque(float val) {
        baseTorque = val;
    }

    public float getBaseTorque() {
        return baseTorque;
    }

    public void setSpeedOfControlEff(float val) {
        speedOfControlEffectiveness = val;
    }

    public float getSpeedOfControlEff() {
        return speedOfControlEffectiveness;
    }

    public void setWingArea(float val) {
        wingArea = val;
    }

    public float getWingArea() {
        return wingArea;
    }

    public void setWingEfficiency(float val) {
        wingEfficiency = val;
    }

    public float getWingEfficiency() {
        return wingEfficiency;
    }

    public float getFrontArea() {
        return baseFrontArea;
    }

    public void setFrontArea(float val) {
        baseFrontArea = val;
    }
}
