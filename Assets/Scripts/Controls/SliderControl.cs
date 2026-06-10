using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;
using UnityEngine.EventSystems;

public class SliderControl : BaseControl, IPointerUpHandler {
    public bool returns;

    public void setVal(float f) {
        query();
        GetComponent<Slider>().value = f;
    }

    public float getVal() {
        query();
        return GetComponent<Slider>().value;
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (returns) GetComponent<Slider>().value = 0f;
    }
}