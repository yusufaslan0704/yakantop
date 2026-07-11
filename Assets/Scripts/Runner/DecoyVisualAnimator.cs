using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Decoy icin basit Idle/Run playable mixer.
// CharacterModelVisual'in hafif versiyonu — sadece dolasma animasyonu.
public class DecoyVisualAnimator : MonoBehaviour
{
    public string idleClipPath = "Models/Idle";
    public string runClipPath = "Models/Running";
    public float transitionSpeed = 10f;
    public float runSpeedThreshold = 0.4f;

    DecoyClone decoy;
    Animator animator;
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    readonly List<AnimationClipPlayable> playables = new List<AnimationClipPlayable>();
    readonly List<AnimationClip> clips = new List<AnimationClip>();

    int idleIndex = -1;
    int runIndex = -1;
    bool graphReady;

    public bool TrySetup(DecoyClone owner, GameObject visualRoot)
    {
        decoy = owner;
        if (visualRoot == null) return false;

        SkinnedMeshRenderer skinned = visualRoot.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinned == null)
        {
            return false;
        }

        skinned.updateWhenOffscreen = true;

        animator = visualRoot.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = visualRoot.AddComponent<Animator>();
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (animator.avatar == null || !animator.avatar.isValid)
        {
            // Avatar yoksa humanoid Idle/Run retarget olmaz.
            return false;
        }

        return BuildGraph();
    }

    bool BuildGraph()
    {
        graph = PlayableGraph.Create(name + "_DecoyAnim");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        idleIndex = AddClip(idleClipPath);
        runIndex = AddClip(runClipPath);

        if (idleIndex < 0)
        {
            if (graph.IsValid()) graph.Destroy();
            return false;
        }

        // Run yoksa en azindan Idle oynasin.
        if (runIndex < 0)
        {
            runIndex = idleIndex;
        }

        mixer = AnimationMixerPlayable.Create(graph, playables.Count);
        for (int i = 0; i < playables.Count; i++)
        {
            graph.Connect(playables[i], 0, mixer, i);
            mixer.SetInputWeight(i, i == idleIndex ? 1f : 0f);
        }

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "DecoyAnim", animator);
        output.SetSourcePlayable(mixer);
        graph.Play();
        graphReady = true;
        return true;
    }

    int AddClip(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return -1;

        AnimationClip clip = LoadClip(resourcePath);
        if (clip == null) return -1;

        AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(false);
        playables.Add(playable);
        clips.Add(clip);
        return playables.Count - 1;
    }

    static AnimationClip LoadClip(string resourcePath)
    {
        foreach (Object asset in Resources.LoadAll(resourcePath))
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }

    void Update()
    {
        if (!graphReady || !mixer.IsValid()) return;

        LoopClips();

        bool running = decoy != null && decoy.IsMoving;
        float targetIdle = running ? 0f : 1f;
        float targetRun = running ? 1f : 0f;

        for (int i = 0; i < playables.Count; i++)
        {
            float target = 0f;
            if (i == idleIndex) target += targetIdle;
            if (i == runIndex) target += targetRun;
            target = Mathf.Clamp01(target);

            float current = mixer.GetInputWeight(i);
            mixer.SetInputWeight(i, Mathf.MoveTowards(current, target, transitionSpeed * Time.deltaTime));
        }
    }

    void LoopClips()
    {
        for (int i = 0; i < playables.Count; i++)
        {
            if (clips[i] == null || clips[i].isLooping) continue;

            double time = playables[i].GetTime();
            double length = clips[i].length;
            if (time >= length && length > 0)
            {
                playables[i].SetTime(time % length);
            }
        }
    }

    void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
