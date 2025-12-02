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
        if (GetComponent<SpriteRenderer>() != null) {
            GetComponent<SpriteRenderer>().enabled = !b;
        }
        if (GetComponent<Image>() != null) {
            GetComponent<Image>().enabled = !b;
        }
        if (GetComponent<TMP_Text>() != null) {
            GetComponent<TMP_Text>().enabled = !b;
        }
        foreach (GameObject imgObj in progenyWithScript<Image>(gameObject)) {
            imgObj.GetComponent<Image>().enabled = !b;
        }
        foreach (GameObject spriteObj in progenyWithScript<SpriteRenderer>(gameObject)) {
            spriteObj.GetComponent<SpriteRenderer>().enabled = !b;
        }
        foreach (GameObject textObj in progenyWithScript<TMP_Text>(gameObject)) {
            textObj.GetComponent<TMP_Text>().enabled = !b;
        }
    }
}
