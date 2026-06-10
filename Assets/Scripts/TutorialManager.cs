using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class TutorialManager : MonoBehaviour {
    [SerializeField] GameObject camera;
    [SerializeField] private GameObject textObj;
    private Instruction[] instructions = new Instruction[] {
        new Instruction("Kindly perform a counter-clockwise turn, soldier", () => TutorialManager.plen.GetComponent<Rigidbody2D>().angularVelocity > 0f, 1f)
    };
    [SerializeField] private int instructionIndex;
    [SerializeField] GameObject lvlManager;

    public static GameObject plen;

    float timeUnderCondition = 0f;
    void Update() {
        plen = camera.GetComponent<CamScript>().getControlledOrSpectatedVehicle();
        if (plen == null) return;

        bool danger = plen.GetComponent<PlaneController>().altitudeFromTerrain() < 100f;

        if (danger) {
            camera.GetComponent<CamScript>().spectateVehicle(plen);
            textObj.GetComponent<TMP_Text>().text = "ARE YOU TRYING TO KILL US AIRMAN??";
        } else {
            camera.GetComponent<CamScript>().takeControlOfVehicle(plen);
            textObj.GetComponent<TMP_Text>().text = instructions[instructionIndex].getText();
        }
        if (instructions[instructionIndex].conditionMet()) {
            timeUnderCondition += Time.deltaTime; 
        }
        if (timeUnderCondition > instructions[instructionIndex].getTime()) {
            instructionIndex++;
            timeUnderCondition = 0;
        }
        if (instructionIndex == instructions.Length) {
            lvlManager.GetComponent<LvlManager>().bringUpWinScreen();
            textObj.GetComponent<TMP_Text>().text = "";
        }
    }
}

public class Instruction {
    private string text;
    private Func<bool> condition;
    private float time;

    public Instruction(string text, Func<bool> condition, float time) {
        this.text = text;
        this.condition = condition;
        this.time = time;
    }

    public bool conditionMet() {
        return condition();
    }

    public float getTime() {
        return time;
    }

    public string getText() {
        return text;
    }
}
