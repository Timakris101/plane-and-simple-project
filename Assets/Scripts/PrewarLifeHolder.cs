using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

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

    [SerializeField] private GameObject grave;

    bool graveSpawned = false;

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
        if (!GetComponent<DamageModel>().isAlive() && parentWithScript<Rigidbody2D>(gameObject).GetComponent<Rigidbody2D>().linearVelocity.magnitude == 0 && !graveSpawned) {
            graveSpawned = true;
            GameObject newGrave = Instantiate(grave, transform.position, Quaternion.identity);
            progenyWithScript<TMP_Text>(newGrave)[0].GetComponent<TMP_Text>().text = this.ToString();
        }
    }

    public override string ToString() {
        return
        name + "\n" +
        nationality.ToString() + "\n" + 
        occupation + "\n" + 
        birthyear + " - " + yearOfBattle;
    }
}
