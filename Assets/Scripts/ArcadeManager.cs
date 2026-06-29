using UnityEngine;
using static Utils;
using System.Collections.Generic;
using TMPro;

public class ArcadeManager : MonoBehaviour {
    [SerializeField] private GameObject enemySquadron;
    [SerializeField] private GameObject player;
    [SerializeField] private int score;
    // Update is called once per frame
    void Update() {
        if (player == null) {
            findAndSetPlayer("Allies");
            return;
        }
        updateEnemyList("Axis");
        updateDeadEnemiesAndScore();

        if (PlayerPrefs.GetInt("HighScore", 0) < score) PlayerPrefs.SetInt("HighScore", score);
        if (GameObject.Find("FinalScore") != null) {
            GameObject.Find("FinalScore").GetComponent<TMP_Text>().text = score < PlayerPrefs.GetInt("HighScore", 0) ? "Score: " + score + "\n" + "High Score: " + PlayerPrefs.GetInt("HighScore", 0) : "NEW HIGH SCORE: " + PlayerPrefs.GetInt("HighScore", 0) + "!";
        }

        enemySquadron.transform.position = new Vector3(player.transform.position.x + 400f, 200f, 0f);
        enemySquadron.GetComponent<SquadronSpawner>().setAmount((score > 3) ? 2 : 1);
    }

    private void findAndSetPlayer(string playerAlliance) {
        foreach (GameObject vehicle in allVehiclesOfTags("Plane")) {
            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == playerAlliance) {
                if (!aiControllerOfVehicle(vehicle).enabled) {
                    player = vehicle;
                    return;
                }
            }
        }
    }

    List<GameObject> allEnemy = new List<GameObject>();
    List<GameObject> deadEnemy = new List<GameObject>();
    private void updateEnemyList(string enemyAlliance) {
        foreach (GameObject vehicle in allVehiclesOfTags("Plane")) {
            if (vehicle.GetComponent<AllianceHolder>().getAlliance() == enemyAlliance && !allEnemy.Contains(vehicle)) {
                allEnemy.Add(vehicle);
            }
        }
        for (int i = 0; i < allEnemy.Count; i++) {
            if (allEnemy[i] == null) {
                allEnemy.Remove(allEnemy[i]);
                i--;
            }
        }
    }

    private void updateDeadEnemiesAndScore() {
        foreach (GameObject enemy in allEnemy) {
            if (enemy.GetComponent<VehicleController>().vehicleDead() && !deadEnemy.Contains(enemy)) {
                deadEnemy.Add(enemy);
                score++;
            }
        }

        for (int i = 0; i < deadEnemy.Count; i++) {
            if (deadEnemy[i] == null) {
                deadEnemy.Remove(deadEnemy[i]);
                i--;
            }
        }
    }

    public int getScore() {
        return score;
    }
}
