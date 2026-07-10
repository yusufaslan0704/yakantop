using UnityEngine;
using UnityEngine.UI;

// Atıcı kamerasini TAMAMEN beyaz isikla kaplar (flashbang).
// Runner kamerasina dokunmaz.
public class FlashBlindOverlay : MonoBehaviour
{
    static FlashBlindOverlay instance;

    Canvas canvas;
    Image whiteout;
    Image starBurst;
    float hideAt;
    float startAt;
    float duration;

    public static void Show(float blindDuration)
    {
        EnsureInstance();
        if (instance == null) return;

        if (SplitScreenManager.Instance != null)
        {
            SplitScreenManager.Instance.EnsureThrowerCameraReady();
        }

        instance.duration = Mathf.Max(0.2f, blindDuration);
        instance.startAt = Time.unscaledTime;
        instance.hideAt = Time.unscaledTime + instance.duration;
        instance.BindToThrowerCamera();
        instance.gameObject.SetActive(true);

        if (instance.whiteout != null)
        {
            instance.whiteout.color = Color.white;
        }

        if (instance.starBurst != null)
        {
            instance.starBurst.color = new Color(0.7f, 0.9f, 1f, 1f);
            instance.starBurst.rectTransform.localScale = Vector3.one * 1.4f;
        }
    }

    static void EnsureInstance()
    {
        if (instance != null) return;

        GameObject root = new GameObject("FlashBlindOverlay");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<FlashBlindOverlay>();
        instance.Build();
    }

    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 0.4f;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>().enabled = false;

        // Tam ekran beyaz.
        whiteout = CreateFullImage("Whiteout", Color.white);

        // Ortada yildiz / flare hissi (buyuk beyaz daire + mavi kenar).
        starBurst = CreateCenteredImage("StarBurst", new Color(0.75f, 0.92f, 1f, 1f), new Vector2(900f, 900f));

        gameObject.SetActive(false);
    }

    Image CreateFullImage(string objectName, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    Image CreateCenteredImage(string objectName, Color color, Vector2 size)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void BindToThrowerCamera()
    {
        if (canvas == null) return;

        Camera throwerCam = SplitScreenManager.Instance != null
            ? SplitScreenManager.Instance.GetThrowerCamera()
            : null;

        if (throwerCam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = throwerCam;
            canvas.planeDistance = 0.35f;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }
    }

    void Update()
    {
        if (!PlayerFlash.AreThrowersBlinded() || Time.unscaledTime >= hideAt)
        {
            gameObject.SetActive(false);
            return;
        }

        BindToThrowerCamera();

        bool throwerView = SplitScreenManager.Instance != null &&
                           SplitScreenManager.Instance.IsThrowerViewVisible();

        if (!throwerView || (canvas.renderMode == RenderMode.ScreenSpaceCamera &&
                             (canvas.worldCamera == null || !canvas.worldCamera.enabled)))
        {
            SetAlpha(0f, 0f);
            return;
        }

        float elapsed = Time.unscaledTime - startAt;
        float t = Mathf.Clamp01(elapsed / duration);

        // Ilk ~35%: tam beyaz (hicbir sey gorunmez).
        // Sonra yavasca acilir ama uzun sure parlak kalir.
        float whiteAlpha;
        if (t < 0.35f)
        {
            whiteAlpha = 1f;
        }
        else
        {
            float fadeT = (t - 0.35f) / 0.65f;
            whiteAlpha = Mathf.Lerp(1f, 0.2f, fadeT * fadeT);
        }

        float starAlpha = t < 0.2f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.2f) / 0.5f);
        float starScale = Mathf.Lerp(1.6f, 3.2f, Mathf.Clamp01(t / 0.35f));

        SetAlpha(whiteAlpha, starAlpha);

        if (starBurst != null)
        {
            starBurst.rectTransform.localScale = Vector3.one * starScale;
        }
    }

    void SetAlpha(float whiteAlpha, float starAlpha)
    {
        if (whiteout != null)
        {
            whiteout.color = new Color(1f, 1f, 1f, whiteAlpha);
        }

        if (starBurst != null)
        {
            starBurst.color = new Color(0.75f, 0.92f, 1f, starAlpha);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        instance = null;
    }
}
