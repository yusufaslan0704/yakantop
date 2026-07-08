using System.Collections;
using UnityEngine;

// Emote: kafanin ustunde kisa sureli konusma baloncugu.
// Atici topu sarj ederken karsisinda dans eden kacan... partinin ruhu.
public class PlayerEmote : MonoBehaviour
{
    [Header("Emotes (1-4 tuslari / D-Pad)")]
    public string[] emotes = { "Haha!", "Buraya at!", "Iskaladin!", "<3" };

    [Header("Display")]
    public float displayDuration = 2f;
    public float heightOffset = 2.7f;
    public float textSize = 0.35f;
    public Color textColor = Color.white;
    public float cooldown = 0.8f;

    // Gorsel katman (orn. dans animasyonu) emote'lari buradan dinler.
    public event System.Action<int> OnEmotePlayed;

    private PlayerInputHandler inputHandler;
    private PlayerHealth playerHealth;

    private TextMesh bubbleText;
    private Transform bubble;
    private Coroutine hideRoutine;
    private float nextEmoteTime;

    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        playerHealth = GetComponent<PlayerHealth>();

        CreateBubble();
    }

    void CreateBubble()
    {
        GameObject bubbleObject = new GameObject("EmoteBubble");

        bubble = bubbleObject.transform;
        bubble.SetParent(transform, false);

        bubbleText = bubbleObject.AddComponent<TextMesh>();
        bubbleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bubbleObject.GetComponent<MeshRenderer>().material = bubbleText.font.material;

        bubbleText.anchor = TextAnchor.MiddleCenter;
        bubbleText.alignment = TextAlignment.Center;
        bubbleText.fontSize = 48;
        bubbleText.characterSize = textSize;
        bubbleText.color = textColor;
        bubbleText.text = "";

        bubbleObject.SetActive(false);
    }

    void Update()
    {
        if (inputHandler == null) return;

        int index = inputHandler.EmoteIndex;

        if (index < 0 || index >= emotes.Length) return;

        if (Time.time < nextEmoteTime) return;

        // Elenmis oyuncu emote atamaz (yerde yatarken konusmak olmaz).
        if (playerHealth != null && playerHealth.IsEliminated) return;

        Show(emotes[index]);

        OnEmotePlayed?.Invoke(index);
    }

    void LateUpdate()
    {
        if (bubble == null || !bubble.gameObject.activeSelf) return;

        // Baloncuk karakterle donmesin: hep kafanin ustunde, kameraya donuk.
        bubble.position = transform.position + Vector3.up * heightOffset;

        if (Camera.main != null)
        {
            bubble.rotation = Quaternion.LookRotation(
                bubble.position - Camera.main.transform.position
            );
        }
    }

    public void Show(string text)
    {
        if (bubbleText == null) return;

        nextEmoteTime = Time.time + cooldown;

        bubbleText.text = text;
        bubble.gameObject.SetActive(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        bubble.gameObject.SetActive(false);
        hideRoutine = null;
    }

    void OnDisable()
    {
        if (bubble != null)
        {
            bubble.gameObject.SetActive(false);
        }
    }
}
