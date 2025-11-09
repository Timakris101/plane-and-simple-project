using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.UI;

public class Utils {
    public static GameObject[] allVehiclesOfTags(params string[] tags) {
        List<GameObject[]> vehiclesByType = new List<GameObject[]>();
        foreach (string tag in tags) {
            vehiclesByType.Add(GameObject.FindGameObjectsWithTag(tag));
        }
        int totalAmt = 0;
        foreach (GameObject[] vehiclesOfTypeA in vehiclesByType) {
            totalAmt += vehiclesOfTypeA.Length;
        }
        GameObject[] vehicles = new GameObject[totalAmt];
        int index = 0;
        foreach (GameObject[] vehiclesOfTypeA in vehiclesByType) {
            foreach (GameObject vehicle in vehiclesOfTypeA) {
                vehicles[index] = vehicle;
                index++;
            }
        }
        return vehicles;
    }

    public static VehicleController aiControllerOfVehicle(GameObject vehicle) {
        VehicleController[] controllers = vehicle.GetComponents<VehicleController>();
        if (controllers.Length == 0) return null;
        if (controllers[0].GetType().IsAssignableFrom(controllers[1].GetType())) {
            return controllers[1];
        } else {
            return controllers[0];
        }
    }

    public static VehicleController nonAiControllerOfVehicle(GameObject vehicle) {
        VehicleController[] controllers = vehicle.GetComponents<VehicleController>();
        if (controllers.Length == 0) return null;
        if (controllers[0].GetType().IsAssignableFrom(controllers[1].GetType())) {
            return controllers[0];
        } else {
            return controllers[1];
        }
    }

    public static List<GameObject> progenyWithScript<T>(GameObject obj) {
        List<GameObject> list = new List<GameObject>();

        for (int i = 0; i < obj.transform.childCount; i++) {
            if (obj.transform.GetChild(i).TryGetComponent<T>(out T component)) {
                list.Add(obj.transform.GetChild(i).gameObject);
            }
            list.AddRange(progenyWithScript<T>(obj.transform.GetChild(i).gameObject));
        }
        return list;
    }

    public static List<GameObject> allObjectsInTreeWith<T>(GameObject obj) {
        List<GameObject> list = progenyWithScript<T>(maxAncestor(obj));
        if (maxAncestor(obj).TryGetComponent<T>(out T component)) list.Add(maxAncestor(obj));
        return list;
    }


    public static GameObject parentWithScript<T>(GameObject obj) {
        if (obj.transform.parent == null) return null;
        if (obj.transform.parent.TryGetComponent<T>(out T component)) {
            return obj.transform.parent.gameObject;
        }
        return parentWithScript<T>(obj.transform.parent.gameObject);
    }

    public static GameObject maxAncestor(GameObject obj) {
        if (obj.transform.parent == null) return obj;
        return maxAncestor(obj.transform.parent.gameObject);
    }

    public static bool isAncestor(GameObject potentialAncestor, GameObject obj) {
        if (obj.transform.parent == null) return false;
        if (obj.transform.parent.gameObject == potentialAncestor) return true;
        return isAncestor(potentialAncestor, obj.transform.parent.gameObject);
    }
    
    public static Vector3 localPositionFrom(GameObject ancestor, GameObject obj) {
        if (obj.transform.parent == null) return new Vector3(0, 0, 0);
        if (obj.transform.parent.gameObject == ancestor) return obj.transform.localPosition;
        return localPositionFrom(ancestor, obj.transform.parent.gameObject) + obj.transform.localPosition;
    }

    public static float newtonRaphson(float startVal, int iterations, float precisionOfValidity, params float[] coefficients) {
        float val = startVal;
        float newVal = 0;
        for (int i = 0; i < iterations; i++) {
            newVal = val - (polynomialEquation(val, coefficients) / polynomialEquation(val, coefficientsOfDerivative(coefficients)));

            float temp = val;
            val = newVal;
            newVal = temp;
        }
        if (Mathf.Abs(polynomialEquation(newVal, coefficients)) < precisionOfValidity) return newVal;
        return -Mathf.Infinity;
    }

    public static float polynomialEquation(float x, params float[] coefficients) {
        float sum = 0;
        for (int i = 0; i < coefficients.Length; i++) {
            sum += coefficients[coefficients.Length - 1 - i] * Mathf.Pow(x, i);
        }
        return sum;
    }

    public static float[] coefficientsOfDerivative(params float[] coefficientsOfRegular) {
        float[] newCoeffs = new float[coefficientsOfRegular.Length - 1];
        for (int i = 0; i < newCoeffs.Length; i++) {
            newCoeffs[i] = (newCoeffs.Length - i) * coefficientsOfRegular[i];
        }
        return newCoeffs;
    }
}
