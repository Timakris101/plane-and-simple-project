using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;
using UnityEngine.EventSystems;

public class BaseControl : MonoBehaviour, IPointerDownHandler {
    public void OnPointerDown(PointerEventData eventData) {
        parentWithScript<CustomInputs>(gameObject).GetComponent<CustomInputs>().setModeOf(gameObject, "mobile");
    }
}
