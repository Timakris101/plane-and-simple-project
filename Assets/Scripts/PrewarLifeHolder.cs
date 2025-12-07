using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrewarLifeHolder : MonoBehaviour {
    [Serializable] public enum Nationality {
        British,
        German,
        Soviet,
        American,
        French
    };

    public Nationality nationality;
    public string name;
    public string occupation;
    public int birthyear;

    private int yearOfBattle = 1940;
    private int minAge = 17;
    private int maxAge = 40;

    void Start() {
        string[] firstNames = Resources.Load<TextAsset>(nationality.ToString() + "FirstNames").text.Split("\n");
        string firstName = firstNames[UnityEngine.Random.Range(0, firstNames.Length)];

        string[] lastNames = Resources.Load<TextAsset>(nationality.ToString() + "LastNames").text.Split("\n");
        string lastName = lastNames[UnityEngine.Random.Range(0, lastNames.Length)];

        name = firstName + " " + lastName;

        string[] occupations = Resources.Load<TextAsset>("Occupations").text.Split("\n");
        occupation = occupations[UnityEngine.Random.Range(0, occupations.Length)];
        if (occupation == "University") maxAge = 22;

        birthyear = yearOfBattle - UnityEngine.Random.Range(minAge, maxAge);
    }

    void Update() {
        if (!GetComponent<DamageModel>().isAlive()) {
            Debug.Log(this);
        }
    }

    public override string ToString() {
        return
        name + "\n" +
        occupation + "\n" + 
        birthyear + " - " + yearOfBattle;
    }
}
