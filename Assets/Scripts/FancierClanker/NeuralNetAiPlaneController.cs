using UnityEngine;

public class NeuralNetAiPlaneController : AiPlaneController {

    private Layer[] brain;
    private int[] neuronAmounts = {0, 12, 5, 8, 2};
    [SerializeField] private float timer = 0;
    public string brainAsString;
    public bool algorithmic;

    void Start() {
        base.Start();
        algorithmic = !targetedObj.GetComponent<NeuralNetAiPlaneController>().algorithmic;
        if (algorithmic) return;

        brain = new Layer[neuronAmounts.Length];
        if (EvolutionHelper.mainBrain.Length == 0) {
            for (int i = 0; i < neuronAmounts.Length; i++) {
                if (i == 0){
                    brain[i] = new Layer(i.ToString(), getInputs().Length, null);
                } else {
                    brain[i] = new Layer(i.ToString(), neuronAmounts[i], brain[i - 1]);
                }
            }
        } else {
            brain[0] = new Layer(0.ToString(), getInputs().Length, null);
            for (int i = 1; i < brain.Length; i++) {
                Neuron[] newNeuronsOfThisLayer = new Neuron[neuronAmounts[i]];
                for (int j = 0; j < newNeuronsOfThisLayer.Length; j++) {
                    float[] newWeightsForThisNeuron = new float[EvolutionHelper.mainBrain[i].getNeurons()[j].getWeights().Length];
                    for (int k = 0; k < newWeightsForThisNeuron.Length; k++) {
                        newWeightsForThisNeuron[k] = EvolutionHelper.mainBrain[i].getNeurons()[j].getWeights()[k] + Random.Range(-EvolutionHelper.variationMag, EvolutionHelper.variationMag);
                    }
                    float newThresh = EvolutionHelper.mainBrain[i].getNeurons()[j].getThreshold() + Random.Range(-EvolutionHelper.variationMag, EvolutionHelper.variationMag);
                    newNeuronsOfThisLayer[j] = new Neuron(newWeightsForThisNeuron, newThresh);
                }
                brain[i] = new Layer(i.ToString(), newNeuronsOfThisLayer);
            }
        }
    }

    void Update() {
        base.Update();
        timer += Time.deltaTime;
        if (timer > EvolutionHelper.generationLength) {
            timer = 0f;
            Destroy(gameObject, 1f);
            if (!EvolutionHelper.called) EvolutionHelper.setMainBrain();
        }

        brainAsString = "";
        if (algorithmic) return;
        foreach (Layer l in brain) {
            brainAsString += l.ToString() + "\n";
        }
    }

    protected override void handleControls() {
        base.handleControls();
        if (!algorithmic) {
            setThrottle(calculateOutputs(getInputs())[1]);
            inWEP = false;
        }
    }

    public float[] getInputs() {
        if (targetedObj == null) {
            Destroy(gameObject);
            return new float[6];
        }
        return new float[] {
            transform.position.x - targetedObj.transform.position.x,
            transform.position.y - targetedObj.transform.position.y,
            GetComponent<Rigidbody2D>().linearVelocity.x - targetedObj.GetComponent<Rigidbody2D>().linearVelocity.x,
            GetComponent<Rigidbody2D>().linearVelocity.y - targetedObj.GetComponent<Rigidbody2D>().linearVelocity.y,
            transform.localEulerAngles.z - targetedObj.transform.localEulerAngles.z,
            GetComponent<Rigidbody2D>().angularVelocity - targetedObj.GetComponent<Rigidbody2D>().angularVelocity
        };
    }

    protected override float wantedDir() {
        if (algorithmic) return base.wantedDir();
        primaryBullet = null;
        foreach (GameObject gunOrBh in guns) {
            if (gunOrBh.transform.parent == transform) {
                isBomber = gunOrBh.TryGetComponent<BombHolderScript>(out BombHolderScript component);
                primaryBullet = gunOrBh.GetComponent<GunScript>().getBullet();
                break;
            }
        }
        if (altitudeFromTerrain() < minAltitude) return pointTowards(transform.position + Vector3.up);
        return calculateOutputs(getInputs())[0] * 2f - 1f;
    }

    public float[] calculateOutputs(float[] inputs) {
        float[] curOutput = inputs;
        for (int i = 0; i < brain.Length; i++) {
            curOutput = brain[i].getOutput(curOutput);
        }
        return curOutput;
    }

    public Layer[] getBrain() {
        return brain;
    }
}
