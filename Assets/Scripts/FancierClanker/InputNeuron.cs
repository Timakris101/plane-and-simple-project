using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class InputNeuron : Neuron {
    public InputNeuron() : base(1) {
        weights = new float[] {1f};
        THRESHOLD = 0f;
    }

    public float getOutput(float[] input) {
        return input[0];
    }
}