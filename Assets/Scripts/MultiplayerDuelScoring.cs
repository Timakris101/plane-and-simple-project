using UnityEngine;

public class MultiplayerDuelScoring : MonoBehaviour {
    public static void applyScoringToPlayer(int enemyElo, float score) {
        float k = 30f;
        float expected = 1f / (Mathf.Pow(10f, (float) (enemyElo - PlayerPrefs.GetInt("Elo")) / 400f) + 1f);
        float change = k * (score - expected);
        PlayerPrefs.SetInt("Elo", PlayerPrefs.GetInt("Elo") + (int) change);
        Debug.Log("New Elo: " + PlayerPrefs.GetInt("Elo"));
    }
}
