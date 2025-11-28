using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class BulletMessageReader : MonoBehaviour {

    [SerializeField] private GameObject basicTextObj;

    public void receivePacket(BulletMessagePacket packet) {
        foreach (BulletMessage message in packet.getMessages()) {
            GameObject newText = Instantiate(basicTextObj);
            newText.transform.SetParent(progenyWithScript<Canvas>(gameObject)[0].transform, false);
            newText.transform.position = message.getDamageModel().transform.position;
            progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().text = Mathf.Round(message.getDamage()).ToString();
            if (message.modelCrit()) {
                progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().color = Color.orange;
            }
            if (message.modelDown()) {
                progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().color = Color.red;
            }
            if (message.targetDown()) {
                progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().color = Color.red;
            }
        }
    }
}
