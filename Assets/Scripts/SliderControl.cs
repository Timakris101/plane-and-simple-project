using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;
using UnityEngine.EventSystems;

public class SliderControl : BaseControl, IPointerUpHandler {
    public bool returns;

    public void setVal(float f) {
        noQuery = false;
        GetComponent<Slider>().value = f;
    }

    public float getVal() {
        noQuery = false;
        return GetComponent<Slider>().value;
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (returns) GetComponent<Slider>().value = 0f;
    }
}