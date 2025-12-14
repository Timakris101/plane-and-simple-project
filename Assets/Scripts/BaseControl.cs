using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;
using UnityEngine.EventSystems;

public class BaseControl : MonoBehaviour, IPointerDownHandler {
    public bool alwaysUseful;
    public bool noQuery;
    public int noQueryCounter;

    public void Start() {
        hide(true);
        noQuery = true;
    }

    public void OnPointerDown(PointerEventData eventData) {
        parentWithScript<CustomInputs>(gameObject).GetComponent<CustomInputs>().setModeOf(gameObject, "mobile");
    }

    public void Update() {
        if (!alwaysUseful && noQueryCounter > 5) hide(true);
        if (noQuery) noQueryCounter++;
        if (!noQuery) {
            noQueryCounter = 0;
            noQuery = true;
            hide(false);
        }
    }

    public void hide(bool b) {
        if (GetComponent<Graphic>() != null) {
            GetComponent<Graphic>().enabled = !b;
        }
        foreach (GameObject imgObj in progenyWithScript<Graphic>(gameObject)) {
            imgObj.GetComponent<Graphic>().enabled = !b;
        }
    }

    public void query() {
        noQuery = false;
    }
}
