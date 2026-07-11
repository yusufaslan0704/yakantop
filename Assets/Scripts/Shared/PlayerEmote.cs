using System.Collections;
using UnityEngine;

// Emote: kafanin ustunde kisa sureli konusma baloncugu + dans.
// Secim: EmoteWheelUI (T basili tut) veya 1-4 / D-Pad.
public class PlayerEmote : MonoBehaviour
{
    [Header("Emotes")]
    public string[] emotes =
    {
        "Haha!",
        "Buraya!",
        "Iskaladin!",
        "<3",
        "GG",
        "Kos!",
        "Wow!",
        "..."
    };

    [Header("Display")]
    public float displayDuration = 1.6f;
    public float heightOffset = 2.55f;
    public float textSize = 0.16f;
    public int fontSize = 64;
    public Color textColor = new Color(0.95f, 0.98f, 1f, 1f);
    public Color outlineColor = new Color(0.02f, 0.04f, 0.08f, 0.95f);
    public float cooldown = 0.75f;

    // Gorsel katman (orn. dans animasyonu) emote'lari buradan dinler.
    public event System.Action<int> OnEmotePlayed;

    private PlayerInputHandler inputHandler;
    private PlayerHealth playerHealth;

    private TextMesh bubbleText;
    private TextMesh outlineText;
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

        // Outline (arkada, hafif buyuk) — TextMesh'de gercek outline yok.
        GameObject outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(bubble, false);
        outlineObject.transform.localPosition = new Vector3(0.012f, -0.012f, 0.01f);
        outlineText = outlineObject.AddComponent<TextMesh>();
        outlineText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        outlineObject.GetComponent<MeshRenderer>().material = outlineText.font.material;
        outlineText.anchor = TextAnchor.MiddleCenter;
        outlineText.alignment = TextAlignment.Center;
        outlineText.fontSize = fontSize;
        outlineText.characterSize = textSize * 1.02f;
        outlineText.color = outlineColor;
        outlineText.text = "";

        bubbleText = bubbleObject.AddComponent<TextMesh>();
        bubbleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bubbleObject.GetComponent<MeshRenderer>().material = bubbleText.font.material;

        bubbleText.anchor = TextAnchor.MiddleCenter;
        bubbleText.alignment = TextAlignment.Center;
        bubbleText.fontSize = fontSize;
        bubbleText.characterSize = textSize;
        bubbleText.color = textColor;
        bubbleText.fontStyle = FontStyle.Bold;
        bubbleText.text = "";

        bubbleObject.SetActive(false);
    }

    void Update()
    {
        if (inputHandler == null) return;

        // Cember acikken 1-4 hotkey'leri yutma; secim cemberden gelir.
        if (EmoteWheelUI.IsOpen) return;

        int index = inputHandler.EmoteIndex;
        if (index < 0) return;

        PlayEmote(index);
    }

    public string[] GetWheelLabels()
    {
        return emotes != null ? emotes : System.Array.Empty<string>();
    }

    public bool PlayEmote(int index)
    {
        if (index < 0 || emotes == null || index >= emotes.Length)
        {
            return false;
        }

        if (Time.time < nextEmoteTime)
        {
            return false;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return false;
        }

        Show(emotes[index]);
        OnEmotePlayed?.Invoke(index);
        return true;
    }

    void LateUpdate()
    {
        if (bubble == null || !bubble.gameObject.activeSelf) return;

        // Baloncuk karakterle donmesin: hep kafanin ustunde, kameraya donuk.
        bubble.position = transform.position + Vector3.up * heightOffset;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0)
            {
                cam = cams[0];
            }
        }

        if (cam != null)
        {
            bubble.rotation = Quaternion.LookRotation(bubble.position - cam.transform.position);
        }
    }

    public void Show(string text)
    {
        if (bubbleText == null) return;

        nextEmoteTime = Time.time + cooldown;

        bubbleText.text = text;
        if (outlineText != null)
        {
            outlineText.text = text;
        }

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
