using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Layer {
    private Neuron[] neurons;
    private string name;

    public Layer(string name, int amountNeurons, Layer prevLayer) {
        this.name = name;
        neurons = new Neuron[amountNeurons];
        for (int i = 0; i < neurons.Length; i++) {
            if (prevLayer != null) {
                neurons[i] = new Neuron(prevLayer.neurons.Length);
            } else {
                neurons[i] = new InputNeuron();
            }
        }
    }

    public Layer(string name, Neuron[] neurons) {
        this.name = name;
        this.neurons = neurons;
    }

    public float[] getOutput(float[] inputs) {
        float[] outputs = new float[neurons.Length];
        for (int i = 0; i < outputs.Length; i++) {
            outputs[i] = neurons[i].getOutput(inputs);
        }
        return outputs;
    }

    public override string ToString() {
        string str = "\n" + name + "\n";
        for (int i = 0; i < neurons.Length; i++) {
            str += "\n" + i + ": \n";
            for (int j = 0; j < neurons[i].getWeights().Length; j++) {
                str += neurons[i].getWeights()[j] + "\n";
            }
            str += "Thresh: " + neurons[i].getThreshold();
        } 
        return str;
    }

    public Neuron[] getNeurons() {
        return neurons;
    }
}