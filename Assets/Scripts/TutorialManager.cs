using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class TutorialManager : MonoBehaviour {
    [SerializeField] GameObject camera;
    [SerializeField] GameObject glider;
    [SerializeField] private GameObject textObj;
    private Instruction[] instructions = new Instruction[] {
        new Instruction("Kindly perform a counter-clockwise turn, soldier", () => TutorialManager.plen.GetComponent<Rigidbody2D>().angularVelocity > 1f, .5f),
        new Instruction("Fire your weapons\n(left mouse button or shoot button to shoot)", () => TutorialManager.inputs.GetComponent<CustomInputs>().gunInput(), .1f),
        new Instruction("Remember to always try to get behind or above your enemy, your attacks will be made more effective", 3f),
        new Instruction("Find the 'glider' bf109 and bring it down!\n(follow the red arrow)", () => TutorialManager.gliderAccessible.GetComponent<VehicleController>().vehicleDead(), 2f),
        new Instruction("Brilliant job pilot", 3f),
    };
    [SerializeField] private int instructionIndex;
    [SerializeField] GameObject lvlManager;

    public static GameObject plen;
    public static CustomInputs inputs;
    public static GameObject gliderAccessible;

    float timeUnderCondition = 0f;
    void Update() {
        plen = camera.GetComponent<CamScript>().getControlledOrSpectatedVehicle();
        inputs = camera.GetComponent<CustomInputs>();
        gliderAccessible = glider;

        if (plen == null) return;

        bool danger = plen.GetComponent<PlaneController>().altitudeFromTerrain() < 100f;

        if (danger) {
            camera.GetComponent<CamScript>().spectateVehicle(plen);
            textObj.GetComponent<TMP_Text>().text = "ARE YOU TRYING TO KILL US AIRMAN??";
            timeUnderCondition = 0f;
        } else {
            camera.GetComponent<CamScript>().takeControlOfVehicle(plen);
            textObj.GetComponent<TMP_Text>().text = instructions[instructionIndex].getText();
        }
        if (instructions[instructionIndex].conditionMet()) {
            timeUnderCondition += Time.deltaTime; 
        } else {
            timeUnderCondition = 0;
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

    public Instruction(string text, float time) {
        this.text = text;
        this.condition = () => true;
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
