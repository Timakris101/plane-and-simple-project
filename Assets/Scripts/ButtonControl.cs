using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;
using UnityEngine.EventSystems;

public class ButtonControl : BaseControl, IPointerDownHandler, IPointerUpHandler {
    public bool buttonPressed;
    public bool returns;
    public bool keyDownType;
    public bool whatToReturn;

    void Update() {
        base.Update();
        GetComponent<Image>().color = buttonPressed ? GetComponent<Button>().colors.pressedColor : GetComponent<Button>().colors.normalColor;
    }

    public void setVal(bool b) {
        noQuery = false;
        buttonPressed = b;
    }

    public bool getVal() {
        bool b = keyDownType ? whatToReturn : buttonPressed;
        whatToReturn = false;
        noQuery = false;
        return b;
    }

    public void OnPointerDown(PointerEventData eventData) {
        base.OnPointerDown(eventData);
        if (returns) {
            buttonPressed = true;
        } else {
            buttonPressed = !buttonPressed;
        }
        
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (returns) {
            buttonPressed = false;
            if (keyDownType) whatToReturn = true;
        }
    }
}
