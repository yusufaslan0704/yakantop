using TMPro;
using UnityEngine;

public class RoundTimerUI : MonoBehaviour
{
	public GameManager gameManager;
	public TMP_Text timerText;

	void Update()
	{
		if (gameManager == null || timerText == null) return;

		float time = gameManager.GetCurrentTime();

		int seconds = Mathf.CeilToInt(time);

		timerText.text = seconds.ToString();
	}
}