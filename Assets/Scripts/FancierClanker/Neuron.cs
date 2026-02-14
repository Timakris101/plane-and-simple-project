using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Neuron {
    protected int numInputs;
    protected float[] weights;
    protected float THRESHOLD = 0;

    public Neuron(int numInputs) {
        this.numInputs = numInputs;
        weights = initWeights(numInputs);
        THRESHOLD = Random.Range(-EvolutionHelper.variationMag, EvolutionHelper.variationMag);
    }

    public Neuron(float[] weights, float thresh) {
        this.numInputs = weights.Length;
        this.weights = weights;
        THRESHOLD = thresh;
    }

    /***
     * Create and return a float array with randomly initialized weights from [-1 to 1]
     * @param numInputs the number of weights needed (Length of the array to create)
     * @return the initialized weights array
     */
    public float[] initWeights(int numInputs) {
        // TODO:  initialize the weights
        float[] weights = new float[numInputs];
        for (int i = 0; i < numInputs; i++) {
            weights[i] = Random.Range(-EvolutionHelper.variationMag, EvolutionHelper.variationMag);
        }
        return weights;
    }

    /***
     * Run the perceptron on the input and return 0 or 1 for the output category
     * @param input input vector
     * @return 0 or 1 representing the possible output categories or -1 if there's an error
     */
    public float getOutput(float[] input) {
        // TODO:  Implement this.
        float sum = 0;
        for (int i = 0; i < weights.Length; i++) {
            sum += input[i] * weights[i];
        }

        sum -= THRESHOLD;
        // Do a linear combination of the inputs multiplied by the weights.
        // Run the sum through the activiationFunction and return the result
        return activationFunction(sum);
    }

    private float activationFunction(float sum) {
        return (float) (1.0 / (1 + Mathf.Exp(-sum)));
    }

    /***
     * Train the perceptron using the input feature vector and its correct label.
     * Return true if there was a non-zero error and training occured (weights got adjusted)
     *
     * @param input
     * @param correctLabel
     * @return
     */
    public bool train(float[] input, string correctLabel) {
        // TODO:  Implement this.
        return false;
    }

    public float[] getWeights() {
        return weights;
    }

    public float getThreshold() {
        return THRESHOLD;
    }
}