using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;
using Unity.Netcode;

public class BulletMessageReader : NetworkBehaviour {

    [SerializeField] private GameObject basicTextObj;

    public void receivePacket(BulletMessagePacket packet) {
        if (!IsOwner && IsClient && GameObject.Find("NetworkManager") != null) {
            sendPacketRpc(packet.ToString());
        } else {
            foreach (BulletMessage message in packet.getMessages()) {
                GameObject newText = Instantiate(basicTextObj);
                newText.transform.SetParent(GameObject.Find("Canvas").transform, false);
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

    public void receivePacket(string packetStringified) {
        foreach (string message in packetStringified.Split("\n")) {
            if (message.Length == 0) continue;

            GameObject newText = Instantiate(basicTextObj);
            newText.transform.SetParent(GameObject.Find("Canvas").transform, false);
            newText.transform.position = GameObject.Find(message.Substring(0, message.IndexOf(" "))).transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
            progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().text = Mathf.Round(float.Parse(message.Substring(message.IndexOf(":") + 1, (message.IndexOf(",") == -1 ? message.Length - 1 : message.IndexOf(",")) - (message.IndexOf(":") + 1)).Trim())).ToString();
            if (message.Contains("critical")) {
                progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().color = Color.orange;
            }
            if (message.Contains("module down")) {
                progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().color = Color.red;
            }
            if (message.Contains("target down")) {
                progenyWithScript<TMP_Text>(newText)[0].GetComponent<TMP_Text>().color = Color.red;
            }
        }
    }

    [Rpc(SendTo.Owner)]
    void sendPacketRpc(string packetStringified) {
        receivePacket(packetStringified);
    }
}
