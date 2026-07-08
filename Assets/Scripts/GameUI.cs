using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
	public GameManager gameManager;
	public TMP_Text timerText;
	public TMP_Text scoreText;

	void Update()
	{
		if (gameManager == null) return;

		if (timerText != null)
		{
			int seconds = Mathf.CeilToInt(gameManager.GetCurrentTime());
			timerText.text = seconds.ToString();
		}

		if (scoreText != null)
		{
			scoreText.text = "Skor: " + gameManager.GetScore();
		}
	}
}