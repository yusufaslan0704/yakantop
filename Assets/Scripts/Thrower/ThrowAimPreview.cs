using System.Collections.Generic;
using UnityEngine;

// Charge sirasinda topun gidecegi gravity yayini + isabet noktasi + hedef highlight.
// Aim assist yok — sadece okunabilirlik.
public class ThrowAimPreview : MonoBehaviour
{
    [Header("Arc")]
    public int maxSteps = 28;
    public float stepTime = 0.045f;
    public float lineWidth = 0.055f;
    public float impactMarkerScale = 0.42f;

    [Header("Target Highlight")]
    public float highlightRayDistance = 80f;
    public float footRingRadius = 0.55f;
    public float footRingHeight = 0.06f;

    PlayerThrow playerThrow;
    PlayerRole playerRole;
    PlayerInputHandler inputHandler;
    ThrowerBot throwerBot;
    LineRenderer line;
    Transform impactMarker;
    Transform footRing;
    readonly List<Vector3> points = new List<Vector3>(32);
    Material lineMaterial;
    Material markerMaterial;
    Material ringMaterial;

    void Awake()
    {
        playerThrow = GetComponent<PlayerThrow>();
        playerRole = GetComponent<PlayerRole>();
        inputHandler = GetComponent<PlayerInputHandler>();
        throwerBot = GetComponent<ThrowerBot>();
        BuildVisuals();
        SetVisible(false);
    }

    void OnDisable()
    {
        ClearHighlight();
        SetVisible(false);
    }

    void OnDestroy()
    {
        ClearHighlight();
        if (line != null) Destroy(line.gameObject);
        if (impactMarker != null) Destroy(impactMarker.gameObject);
        if (footRing != null) Destroy(footRing.gameObject);
        if (lineMaterial != null) Destroy(lineMaterial);
        if (markerMaterial != null) Destroy(markerMaterial);
        if (ringMaterial != null) Destroy(ringMaterial);
    }

    void LateUpdate()
    {
        if (!IsHumanThrower())
        {
            ClearHighlight();
            SetVisible(false);
            return;
        }

        if (!ShouldShowArc())
        {
            SetVisible(false);
            UpdateTargetHighlight(charging: false);
            return;
        }

        float charge = playerThrow.GetChargePercent();

        // Egri top: mouse ile cizilen serbest yol.
        if (playerThrow.IsDrawingThrowPath && playerThrow.DrawnPathPoints != null &&
            playerThrow.DrawnPathPoints.Count >= 2)
        {
            points.Clear();
            for (int i = 0; i < playerThrow.DrawnPathPoints.Count; i++)
            {
                points.Add(playerThrow.DrawnPathPoints[i]);
            }

            Vector3 tip = points[points.Count - 1];
            DrawArc(charge);
            PlaceImpact(tip, true, charge);
            SetVisible(true);
            UpdateTargetHighlight(charging: true);
            return;
        }

        if (!playerThrow.TryGetAimPreview(out Vector3 origin, out Vector3 direction, out float force))
        {
            SetVisible(false);
            UpdateTargetHighlight(charging: false);
            return;
        }

        SimulateArc(origin, direction, force, out Vector3 impact, out bool hasImpact);
        DrawArc(charge);
        PlaceImpact(impact, hasImpact, charge);
        SetVisible(true);
        UpdateTargetHighlight(charging: true);
    }

    bool IsHumanThrower()
    {
        if (throwerBot != null && throwerBot.enabled)
        {
            return false;
        }

        return inputHandler != null && inputHandler.enabled;
    }

    bool ShouldShowArc()
    {
        if (playerThrow == null || !playerThrow.isActiveAndEnabled)
        {
            return false;
        }

        if (!GameManager.RoundIsActive || EmoteWheelUI.IsOpen)
        {
            return false;
        }

        if (playerRole != null && playerRole.roleType != RoleType.Thrower)
        {
            return false;
        }

        return playerThrow.IsCharging();
    }

    void SimulateArc(Vector3 origin, Vector3 direction, float force, out Vector3 impact, out bool hasImpact)
    {
        float mass = ResolveBallMass();

        ThrowPathBuilder.Build(
            origin,
            direction,
            force,
            mass,
            curveBias: 0f,
            curveForce: 0f,
            points,
            out impact,
            out hasImpact,
            transform,
            maxSteps,
            stepTime);

        // Duz yay (egri-degil toplar) — atis bu izi takip edebilir.
        if (playerThrow != null)
        {
            playerThrow.SetPlannedPath(points, force / Mathf.Max(0.05f, mass));
        }
    }

    float ResolveBallMass()
    {
        if (playerThrow == null || playerThrow.ballData == null || playerThrow.ballData.prefab == null)
        {
            return 1f;
        }

        Rigidbody rb = playerThrow.ballData.prefab.GetComponent<Rigidbody>();
        if (rb == null)
        {
            return 1f;
        }

        return Mathf.Max(0.05f, rb.mass);
    }

    bool IsOwnCollider(Collider col)
    {
        if (col == null) return true;
        return col.transform == transform || col.transform.IsChildOf(transform);
    }

    void DrawArc(float charge)
    {
        if (line == null) return;

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }

        Color c = Color.Lerp(UIColorPalette.AimArc, UIColorPalette.AimArcHot, charge);
        line.startColor = c;
        line.endColor = new Color(c.r, c.g, c.b, c.a * 0.35f);
        float width = Mathf.Lerp(lineWidth * 0.75f, lineWidth, charge);
        line.startWidth = width;
        line.endWidth = width * 0.4f;
    }

    void PlaceImpact(Vector3 impact, bool hasImpact, float charge)
    {
        if (impactMarker == null) return;

        if (!hasImpact)
        {
            impactMarker.gameObject.SetActive(false);
            return;
        }

        impactMarker.gameObject.SetActive(true);
        impactMarker.position = impact + Vector3.up * 0.04f;
        float s = Mathf.Lerp(impactMarkerScale * 0.85f, impactMarkerScale * 1.25f, charge);
        impactMarker.localScale = Vector3.one * s;

        if (markerMaterial != null)
        {
            Color c = Color.Lerp(UIColorPalette.AimImpact, UIColorPalette.AimArcHot, charge);
            markerMaterial.SetColor("_BaseColor", c);
            markerMaterial.color = c;
        }
    }

    void UpdateTargetHighlight(bool charging)
    {
        Camera cam = ResolveAimCamera();
        if (cam == null)
        {
            ClearHighlight();
            return;
        }

        // Her zaman hafif tarama; charge'da daha guclu.
        if (!GameManager.RoundIsActive || EmoteWheelUI.IsOpen)
        {
            ClearHighlight();
            return;
        }

        if (playerThrow == null || !playerThrow.isActiveAndEnabled)
        {
            ClearHighlight();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, highlightRayDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Transform found = null;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || IsOwnCollider(col)) continue;

            PlayerRole role = col.GetComponentInParent<PlayerRole>();
            if (role == null) continue;
            if (role.roleType != RoleType.Runner && role.roleType != RoleType.Saver) continue;

            PlayerHealth health = role.GetComponent<PlayerHealth>();
            if (health != null && health.IsEliminated) continue;

            found = role.transform;
            break;
        }

        if (found == null)
        {
            ClearHighlight();
            return;
        }

        if (footRing == null) return;

        footRing.gameObject.SetActive(true);
        Vector3 p = found.position;
        p.y += 0.05f;
        footRing.position = p;
        float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
        float scale = footRingRadius * 2f * pulse * (charging ? 1.15f : 0.9f);
        footRing.localScale = new Vector3(scale, footRingHeight, scale);

        if (ringMaterial != null)
        {
            Color c = charging ? UIColorPalette.AimTargetHot : UIColorPalette.AimTarget;
            c.a *= charging ? 1f : 0.65f;
            ringMaterial.SetColor("_BaseColor", c);
            ringMaterial.color = c;
            if (ringMaterial.HasProperty("_EmissionColor"))
            {
                ringMaterial.EnableKeyword("_EMISSION");
                ringMaterial.SetColor("_EmissionColor", c * (charging ? 1.8f : 0.9f));
            }
        }
    }

    void ClearHighlight()
    {
        if (footRing != null)
        {
            footRing.gameObject.SetActive(false);
        }
    }

    Camera ResolveAimCamera()
    {
        if (playerThrow != null && playerThrow.playerCamera != null)
        {
            return playerThrow.playerCamera;
        }

        if (SplitScreenManager.Instance != null)
        {
            Camera throwerCam = SplitScreenManager.Instance.GetThrowerCamera();
            if (throwerCam != null) return throwerCam;
        }

        return Camera.main;
    }

    void SetVisible(bool visible)
    {
        if (line != null)
        {
            line.enabled = visible;
            if (!visible)
            {
                line.positionCount = 0;
            }
        }

        if (impactMarker != null && !visible)
        {
            impactMarker.gameObject.SetActive(false);
        }
    }

    void BuildVisuals()
    {
        GameObject lineGo = new GameObject("ThrowAimArc");
        SceneFolders.ParentTo(lineGo.transform, SceneFolders.RuntimeSpawned);
        line = lineGo.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.positionCount = 0;

        lineMaterial = CreateUnlitMaterial(UIColorPalette.AimArc);
        line.material = lineMaterial;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth * 0.5f;
        line.startColor = UIColorPalette.AimArc;
        line.endColor = UIColorPalette.AimArc;

        GameObject markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerGo.name = "ThrowAimImpact";
        Object.Destroy(markerGo.GetComponent<Collider>());
        SceneFolders.ParentTo(markerGo.transform, SceneFolders.RuntimeSpawned);
        impactMarker = markerGo.transform;
        impactMarker.localScale = Vector3.one * impactMarkerScale;
        markerMaterial = CreateUnlitMaterial(UIColorPalette.AimImpact);
        Renderer markerRend = markerGo.GetComponent<Renderer>();
        if (markerRend != null)
        {
            markerRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            markerRend.material = markerMaterial;
        }

        GameObject ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringGo.name = "ThrowAimTargetRing";
        Object.Destroy(ringGo.GetComponent<Collider>());
        SceneFolders.ParentTo(ringGo.transform, SceneFolders.RuntimeSpawned);
        footRing = ringGo.transform;
        footRing.localScale = new Vector3(footRingRadius * 2f, footRingHeight, footRingRadius * 2f);
        ringMaterial = CreateUnlitMaterial(UIColorPalette.AimTarget);
        Renderer ringRend = ringGo.GetComponent<Renderer>();
        if (ringRend != null)
        {
            ringRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRend.material = ringMaterial;
        }

        impactMarker.gameObject.SetActive(false);
        footRing.gameObject.SetActive(false);
    }

    static Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Sprites/Default")
                        ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        mat.color = color;

        // Transparent-ish.
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
        }

        return mat;
    }
}
