using UnityEngine;

public class PIDController {
    private float kP;
    private float kI;
    private float kD;

    private float totalError;
    private float prevError;

    private bool isContinuous;
    private float min;
    private float max; 

    public PIDController(float p, float i, float d) {
        kP = p;
        kI = i;
        kD = d;
    }

    public PIDController withContinuity(float min, float max) {
        isContinuous = true;
        this.min = min;
        this.max = max;
        return this;
    }

    public float calculate(float current, float desired, float timeDelta) {
        float currentError = 0f;
        if (isContinuous) {
            double errorBound = (max - min) / 2.0;
            currentError = (float) inputModulus((double) (desired - current), -errorBound, errorBound);
        } else {
            currentError = desired - current;
        }

        totalError += currentError * timeDelta;
        float errorDerivative = (currentError - prevError) / timeDelta;
        prevError = currentError;
        return currentError * kP + totalError * kI + errorDerivative * kD;
    }

    public static double inputModulus(double input, double minimumInput, double maximumInput) {
        double modulus = maximumInput - minimumInput;

        // Wrap input if it's above the maximum input
        int numMax = (int) ((input - minimumInput) / modulus);
        input -= numMax * modulus;

        // Wrap input if it's below the minimum input
        int numMin = (int) ((input - maximumInput) / modulus);
        input -= numMin * modulus;

        return input;
    }
}
