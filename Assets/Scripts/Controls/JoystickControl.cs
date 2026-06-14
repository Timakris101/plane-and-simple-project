using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;
using UnityEngine.EventSystems;

public class JoystickControl : BaseControl, IPointerUpHandler {
    public bool returns;
    Vector2 startPos;
    Vector2 startFullJoystickPos;
    [SerializeField] private GameObject stick;
    [SerializeField] private float range;

    public void setVal(Vector2 f) {
        query();
        stick.transform.localPosition = new Vector2(Mathf.Cos(f.y), Mathf.Sin(f.y)) * f.x * range;
        normalizeIfOutOfRange();
    }

    public Vector2 getVal() {
        query();
        Vector2 diff = stick.transform.position - transform.position;
        return new Vector2(diff.magnitude, Mathf.Atan2(diff.y, diff.x));
    }

    public override void Start() {
        base.Start();
        startFullJoystickPos = transform.position;
    }

    public override void Update() {
        base.Update();
        normalizeIfOutOfRange();
        if (Input.touchCount == 0) return;
        int index = validTouch();
        if (index == -1) {
            stick.transform.localPosition = new Vector2(0f, 0f);
            return;
        }
        if (Input.GetTouch(index).phase == TouchPhase.Ended) {
            OnPointerUp();
            return;
        }
        if (Input.GetTouch(index).phase == TouchPhase.Began) {
            startPos = Input.GetTouch(index).position;
            transform.position = startPos;
        }
        stick.transform.position = Input.GetTouch(index).position;
        normalizeIfOutOfRange();
    }

    private void normalizeIfOutOfRange() {
        if (stick.transform.localPosition.magnitude > range) {
            stick.transform.localPosition = stick.transform.localPosition.normalized * range;
        }
    }

    public int validTouch() {
        for (int i = 0; i < Input.touchCount; i++) {
            if (Input.GetTouch(i).position.x < Screen.width / 2f) {
                return i;
            }
        }
        return -1;
    }

    public void OnPointerUp(PointerEventData eventData) {
        // if (returns) stick.transform.localPosition = new Vector2(0f, 0f);
        // transform.position = startFullJoystickPos;
        // startPos = new Vector2(0, 0);
    }

     public void OnPointerUp() {
        if (returns) stick.transform.localPosition = new Vector2(0f, 0f);
        transform.position = startFullJoystickPos;
        startPos = new Vector2(0, 0);
    }
}