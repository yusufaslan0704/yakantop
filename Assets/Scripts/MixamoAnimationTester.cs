using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Mixamo FBX'lerini hizlica test etmek icin: Resources'taki modeli
// sahneye koyar ve icindeki animasyonu Animator Controller kurulumu
// gerektirmeden (Playables API) dongude oynatir.
public class MixamoAnimationTester : MonoBehaviour
{
    [Tooltip("Resources altindaki yol (uzantisiz). Orn: Models/Chicken Dance")]
    public string resourcePath = "Models/Chicken Dance";

    [Tooltip("Model kac kat buyutulsun (Mixamo modelleri bazen kucuk gelir).")]
    public float scaleMultiplier = 1f;

    private PlayableGraph graph;
    private AnimationClipPlayable clipPlayable;
    private float clipLength;

    void Start()
    {
        GameObject modelPrefab = Resources.Load<GameObject>(resourcePath);

        if (modelPrefab == null)
        {
            Debug.LogWarning("Mixamo test: '" + resourcePath + "' bulunamadi. FBX, Assets/Resources altinda mi?");
            return;
        }

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcePath.Substring(0, resourcePath.LastIndexOf('/') + 1));
        AnimationClip danceClip = FindClipFor(modelPrefab.name, clips);

        GameObject model = Instantiate(modelPrefab, transform.position, transform.rotation);
        model.transform.localScale *= scaleMultiplier;

        if (model.GetComponentInChildren<SkinnedMeshRenderer>() == null)
        {
            Debug.LogWarning("Mixamo test: FBX'te mesh yok (sadece iskelet+animasyon). " +
                             "Mixamo'dan indirirken 'Format: FBX' ve 'Skin: With Skin' secmelisin, " +
                             "yoksa dans eden gorunmez bir iskelet olur.");
        }

        if (danceClip == null)
        {
            Debug.LogWarning("Mixamo test: FBX icinde animasyon klibi bulunamadi.");
            return;
        }

        Animator animator = model.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }

        clipLength = danceClip.length;

        // Controller asset'i olmadan klip oynatma.
        graph = PlayableGraph.Create("MixamoTest");
        clipPlayable = AnimationClipPlayable.Create(graph, danceClip);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Anim", animator);
        output.SetSourcePlayable(clipPlayable);

        graph.Play();

        Debug.Log("Mixamo test: '" + danceClip.name + "' oynuyor (" + clipLength.ToString("F1") + " sn, dongude).");
    }

    void Update()
    {
        // Klip bitince basa sar (dongu).
        if (clipPlayable.IsValid() && clipPlayable.GetTime() >= clipLength)
        {
            clipPlayable.SetTime(0);
        }
    }

    AnimationClip FindClipFor(string modelName, AnimationClip[] clips)
    {
        foreach (AnimationClip clip in clips)
        {
            // Onizleme klipleri "__preview__" ile baslar, onlari atla.
            if (clip != null && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }

    void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
