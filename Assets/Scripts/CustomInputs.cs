using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class InteractableModePair {
    public string mode;
    public GameObject obj;

    public InteractableModePair(GameObject obj, string mode) {
        this.obj = obj;
        this.mode = mode;
    }
}

public class CustomInputs : MonoBehaviour {

    List<InteractableModePair> controlList = new List<InteractableModePair>();
    string baseControlFind = "Canvas/Controls/";

    public delegate float SliderInputDelegate(out bool change);

    public float basicSliderInput(SliderControl slider, SliderInputDelegate controlOfComp, InitializeFloatType init) {
        bool changeToComp;
        controlOfComp(out changeToComp);
        if (changeToComp) setModeOf(slider.gameObject, "computer");

        float val = init();
        switch (modeOfObj(slider.gameObject)) {
            case "computer": 
            val = controlOfComp(out bool b);
            slider.setVal(val);
            break;

            case "mobile": 
            val = slider.getVal();
            break;

            default: 
            slider.setVal(val);
            return val;
        }

        return val;
    }

    public delegate float InitializeFloatType();

//-------------------------------------------------------------------------------------------------
    public float directionInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "ControlSlider");
        if (uiInput == null) return computerControlBasedDirectionInput(out bool b);

        SliderControl control = uiInput.GetComponent<SliderControl>();
        return basicSliderInput(control, computerControlBasedDirectionInput, readDirection);
    } 

    public float computerControlBasedDirectionInput(out bool buttonsTouched) {
        buttonsTouched = false;
        int val = 0;
        if (Input.GetKey("a")) {
            val = -1;
            buttonsTouched = true;
        }
        if (Input.GetKey("d")) {
            val = 1;
            buttonsTouched = true;
        }
        if (Input.GetKey("d") && Input.GetKey("a")) {
            val = 0;
            buttonsTouched = true;
        }
        return val;
    }

    public float readDirection() {
        return 0f;
    }

//-------------------------------------------------------------------------------------------------
    public float throttleInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "ThrottleSlider");
        if (uiInput == null) return computerControlBasedThrottleInput(out bool b);

        SliderControl control = uiInput.GetComponent<SliderControl>();
        GameObject linkedWEP = GameObject.Find(baseControlFind + "WEPButton");
        if (control.getVal() < 1f && linkedWEP != null) linkedWEP.GetComponent<ButtonControl>().setVal(false);

        return basicSliderInput(control, computerControlBasedThrottleInput, readThrottle);
    }

    public float computerControlBasedThrottleInput(out bool buttonsTouched) {
        buttonsTouched = false;
        GameObject vehicle = parentWithScript<VehicleController>(gameObject);
        PlaneController pc = (PlaneController) nonAiControllerOfVehicle(vehicle);
        if (Input.GetKey("w") && pc.getThrottle() < 1) {
            buttonsTouched = true;
            return pc.getThrottle() + pc.throttleChangeSpeed * Time.deltaTime;
        }
        if (Input.GetKey("s") && pc.getThrottle() > 0) {
            buttonsTouched = true;
            return pc.getThrottle() - pc.throttleChangeSpeed * Time.deltaTime;
        }
        return pc.getThrottle();
    }

    public float readThrottle() {
        GameObject vehicle = parentWithScript<VehicleController>(gameObject);
        PlaneController pc = (PlaneController) nonAiControllerOfVehicle(vehicle);
        return pc.getThrottle();
    }
//-------------------------------------------------------------------------------------------------

    public delegate bool ButtonInputDelegate(ButtonControl mobileButton, out bool change);

    public bool basicButtonInput(ButtonControl button, ButtonInputDelegate controlOfComp, InitializeBoolType init) {
        bool changeToComp;
        controlOfComp(button, out changeToComp);
        if (changeToComp) setModeOf(button.gameObject, "computer");

        bool val = init();
        switch (modeOfObj(button.gameObject)) {
            case "computer": 
            val = controlOfComp(button, out bool b);
            button.setVal(val);
            break;

            case "mobile": 
            val = button.getVal();
            break;

            default: 
            button.setVal(val);
            return val;
        }

        return val;
    }

    public delegate bool InitializeBoolType();

//-------------------------------------------------------------------------------------------------
    public bool gunInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "GunButton");
        if (uiInput == null) return computerControlBasedGunInput();

        ButtonControl control = GameObject.Find(baseControlFind + "GunButton").GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedGunInput, readGuns);
    }

    public bool computerControlBasedGunInput(ButtonControl mobileButton, out bool buttonsTouched) {
        buttonsTouched = false;
        if (Input.GetMouseButton(0) && !mobileButton.buttonPressed) buttonsTouched = true;
        return Input.GetMouseButton(0);
    }

    public bool computerControlBasedGunInput() {
        return Input.GetMouseButton(0);
    }

    public bool readGuns() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool wepInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "WEPButton");
        if (uiInput == null) return computerControlBasedWepInput(null, out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedWepInput, readWEP);
    }

    public bool computerControlBasedWepInput(ButtonControl mobileButton, out bool buttonsTouched) {
        GameObject vehicle = parentWithScript<VehicleController>(gameObject);
        PlaneController pc = (PlaneController) nonAiControllerOfVehicle(vehicle);
        bool inWEP = false;
        if (Input.GetKey("w") && pc.getThrottle() + pc.throttleChangeSpeed * Time.deltaTime > 1) {
            inWEP = true;
        }
        buttonsTouched = false;
        if (Input.GetKey("s") || Input.GetKey("w")) buttonsTouched = true;

        return inWEP;
    }

    public bool readWEP() {
        return false;
    }

//-------------------------------------------------------------------------------------------------

    public delegate Vector3 VectorInputDelegate(out bool change);

    public Vector3 basicVectorInput(VectorInputDelegate controlOfComp, VectorInputDelegate controlOfMobile) {
        string localMode = "";

        bool changeToComp;
        controlOfComp(out changeToComp);
        if (changeToComp) localMode = "computer";

        bool changeToMobile;
        controlOfMobile(out changeToMobile);
        if (changeToMobile) localMode = "mobile";

        Vector3 val = new Vector3(0,0,0);
        switch (localMode) {
            case "computer": 
            val = controlOfComp(out bool b);
            break;

            case "mobile": 
            val = controlOfMobile(out bool b1);
            break;

            default: 
            return val;
        }

        return val;
    }

//-------------------------------------------------------------------------------------------------

    public Vector3 pointerPositionInput() {
        return basicVectorInput(computerControlBasedMouseInput, mobileControlBasedTouchInput);
    }
    
    public Vector3 computerControlBasedMouseInput(out bool buttonsTouched) {
        buttonsTouched = Input.touchCount == 0;
        return GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -transform.position.z));
    }

    public Vector3 mobileControlBasedTouchInput(out bool buttonsTouched) {
        buttonsTouched = Input.touchCount > 0;
        if (!buttonsTouched) return new Vector3(0,0,0);
        return GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, -transform.position.z));
    }

//-------------------------------------------------------------------------------------------------

    public void setModeOf(GameObject objInPair, string mode) {
        foreach (InteractableModePair pair in controlList) {
            if (pair.obj == objInPair) {
                pair.mode = mode;
                return;
            }
        }
        controlList.Add(new InteractableModePair(objInPair, mode));
    }

    public InteractableModePair returnPairOfObj(GameObject obj) {
        foreach (InteractableModePair pair in controlList) {
            if (pair.obj == obj) {
                return pair;
            }
        }
        return null;
    }

    public string modeOfObj(GameObject obj) {
        if (returnPairOfObj(obj) != null) return returnPairOfObj(obj).mode;
        return "";
    }
}

