using UnityEngine;
using TMPro;

public class ControlDisplay : MonoBehaviour {

    [SerializeField] private bool isThrottleKeyDisp;

    void Start() {
        if (!isThrottleKeyDisp) return;
        transform.GetChild(0).GetComponent<TMP_Text>().text = CustomInputs.throttleUpKey.ToString();
        transform.GetChild(1).GetComponent<TMP_Text>().text = CustomInputs.throttleDownKey.ToString();
    }
}
