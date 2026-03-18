using UnityEngine;

public class PIDController {
    private float kP;
    private float kI;
    private float kD;

    private float totalError;
    private float prevError;

    public PIDController(float p, float i, float d) {
        kP = p;
        kI = i;
        kD = d;
    }

    public float calculate(float current, float desired, float timeDelta) {
        float currentError = desired - current;
        totalError += currentError * timeDelta;
        float errorDerivative = (currentError - prevError) / timeDelta;
        prevError = currentError;
        return currentError * kP + totalError * kI + errorDerivative * kD;
    }
}
