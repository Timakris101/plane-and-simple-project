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

    void Start() {
        Input.simulateMouseWithTouches = false;
    }

    List<InteractableModePair> controlList = new List<InteractableModePair>();
    string baseControlFind = "Canvas/SafeArea/Controls/";

//-------------------------------------------------------------------------------------------------

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

    public delegate bool ButtonInputDelegateUseButton(ButtonControl mobileButton, out bool change);

    public bool basicButtonInput(ButtonControl button, ButtonInputDelegateUseButton controlOfComp, InitializeBoolType init) {
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

    public delegate bool ButtonInputDelegate(out bool change);

    public bool basicButtonInput(ButtonControl button, ButtonInputDelegate controlOfComp, InitializeBoolType init) {
        bool changeToComp;
        controlOfComp(out changeToComp);
        if (changeToComp) setModeOf(button.gameObject, "computer");

        bool val = init();
        switch (modeOfObj(button.gameObject)) {
            case "computer": 
            val = controlOfComp(out bool b);
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
    public bool bombInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "BombButton");
        if (uiInput == null) return computerControlBasedBombInput();

        ButtonControl control = GameObject.Find(baseControlFind + "BombButton").GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedBombInput, readBombs);
    }

    public bool computerControlBasedBombInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown(KeyCode.Space);
        return Input.GetKeyDown(KeyCode.Space);
    }

    public bool computerControlBasedBombInput() {
        return Input.GetKeyDown(KeyCode.Space);
    }

    public bool readBombs() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool wepInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "WEPButton");
        if (uiInput == null) return computerControlBasedWepInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedWepInput, readWEP);
    }

    public bool computerControlBasedWepInput(out bool buttonsTouched) {
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
    public bool gearInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "GearButton");
        if (uiInput == null) return computerControlBasedGearInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedGearInput, readGear);
    }

    public bool computerControlBasedGearInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown("g");
        return Input.GetKeyDown("g");
    }

    public bool readGear() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool swapViewInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "SwapViewButton");
        if (uiInput == null) return computerControlBasedSwapInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedSwapInput, readSwap);
    }

    public bool computerControlBasedSwapInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown("v");
        return Input.GetKeyDown("v");
    }

    public bool readSwap() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool ejectInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "EjectButton");
        if (uiInput == null) return computerControlBasedEjectInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedEjectInput, readEject);
    }

    public bool computerControlBasedEjectInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKey("j");
        return Input.GetKey("j");
    }

    public bool readEject() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool brakeInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "BrakeButton");
        if (uiInput == null) return computerControlBasedBrakeInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedBrakeInput, readBrake);
    }

    public bool computerControlBasedBrakeInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKey("s");
        GameObject vehicle = parentWithScript<VehicleController>(gameObject);
        PlaneController pc = (PlaneController) nonAiControllerOfVehicle(vehicle);
        return Input.GetKey("s") && pc.getThrottle() - pc.throttleChangeSpeed * Time.deltaTime < 0;
    }

    public bool readBrake() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool flapInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "FlapButton");
        if (uiInput == null) return computerControlBasedFlapInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedFlapInput, readFlaps);
    }

    public bool computerControlBasedFlapInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown("f");
        return Input.GetKeyDown("f");
    }

    public bool readFlaps() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool engineInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "EngineButton");
        if (uiInput == null) return computerControlBasedEngineInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedEngineInput, readEngine);
    }

    public bool computerControlBasedEngineInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown("i");
        return Input.GetKeyDown("i");
    }

    public bool readEngine() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool swapPlaneInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "SwapPlaneButton");
        if (uiInput == null) return computerControlBasedSwapPlaneInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedSwapPlaneInput, readSwapPlane);
    }

    public bool computerControlBasedSwapPlaneInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown("n") && !Input.GetKey(KeyCode.LeftShift);
        return Input.GetKeyDown("n") && !Input.GetKey(KeyCode.LeftShift);
    }

    public bool readSwapPlane() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool spectatePlaneInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "SpectatePlaneButton");
        if (uiInput == null) return computerControlBasedSpectatePlaneInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedSpectatePlaneInput, readSpectatePlane);
    }

    public bool computerControlBasedSpectatePlaneInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown("n") && Input.GetKey(KeyCode.LeftShift);
        return Input.GetKeyDown("n") && Input.GetKey(KeyCode.LeftShift);
    }

    public bool readSpectatePlane() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool escapeInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "EscapeButton");
        if (uiInput == null) return computerControlBasedEscapeInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedEscapeInput, readEscape);
    }

    public bool computerControlBasedEscapeInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown(KeyCode.Escape);
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public bool readEscape() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool rotateVehicleInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "RotateButton");
        if (uiInput == null) return computerControlBasedRotateInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedRotateInput, readRotate);
    }

    public bool computerControlBasedRotateInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetKeyDown("r");
        return Input.GetKeyDown("r");
    }

    public bool readRotate() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool zoomCamInput() {
        GameObject uiInput = GameObject.Find(baseControlFind + "ZoomButton");
        if (uiInput == null) return computerControlBasedZoomInput(out bool b);
        
        ButtonControl control = uiInput.GetComponent<ButtonControl>();
        return basicButtonInput(control, computerControlBasedZoomInput, readZoom);
    }

    public bool computerControlBasedZoomInput(out bool buttonsTouched) {
        buttonsTouched = Input.GetMouseButtonDown(1);
        return Input.GetMouseButtonDown(1);
    }

    public bool readZoom() {
        return false;
    }

//-------------------------------------------------------------------------------------------------
    public bool shootGunnerInput() {
        return Input.GetMouseButton(0) || (Input.touchCount > 0 ? Input.GetTouch(0).phase != TouchPhase.Ended : false);
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
        Vector3 totalPos = Vector3.zero;
        for (int i = 0; i < Input.touchCount; i++) {
            totalPos += GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.GetTouch(i).position.x, Input.GetTouch(i).position.y, -transform.position.z));
        }
        return totalPos / Input.touchCount;
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

