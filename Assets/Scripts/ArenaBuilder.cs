using UnityEngine;
using UnityEngine.Rendering;

// Konseptteki "Hourglass Arena" haritasini runtime'da olusturur.
// Eski dikdortgen zemin/duvarlari gizler; renkli bolgeler, diyagonal duvarlar,
// cep guvenli alanlari ve siperleri kurar. RoleZoneLimiter referanslarini otomatik baglar.
[DefaultExecutionOrder(-100)]
public class ArenaBuilder : MonoBehaviour
{
    [Header("Legacy")]
    public bool hideLegacyArena = true;

    [Header("Boyutlar")]
    public float arenaHalfWidth = 22f;
    public float arenaHalfLength = 17f;
    public float throwerZoneDepth = 7f;
    public float neckHalfWidth = 8.5f;
    public float wallHeight = 2.4f;
    public float wallThickness = 0.55f;

    [Header("Cep (Pocket Safe Area)")]
    public float pocketHalfWidth = 3.5f;
    public float pocketHalfDepth = 3f;
    public float pocketCenterX = 17.5f;

    [Header("Siper")]
    public Vector2 coverSize = new Vector2(2.6f, 1.2f);
    public float coverHeight = 1.35f;
    public float coverCenterZ = 3.5f;

    [Header("Spawn")]
    public Vector3 runnerSpawn = new Vector3(0f, 1f, 5f);
    public Vector3 saverSpawn = new Vector3(-7f, 1f, -3f);
    public Vector3 throwerSpawn = new Vector3(0f, 1f, -13.5f);
    public Vector3 runnerBotSpawn = new Vector3(11f, 1f, 0f);

    [Header("Visual Polish")]
    public bool enableVisualPolish = true;
    public float gridTileWorldSize = 2f;
    public float ceilingHeight = 9f;
    public bool enableFog = true;

    public Collider RunnerZoneCollider { get; private set; }
    public Collider ThrowerZoneTop { get; private set; }
    public Collider ThrowerZoneBottom { get; private set; }

    private Transform arenaRoot;
    private Material matRunner;
    private Material matThrower;
    private Material matPocket;
    private Material matWall;
    private Material matWallTrim;
    private Material matNeon;
    private Material matNeonBlue;
    private Material matCover;
    private Material matLine;
    private Material matCeiling;

    void Awake()
    {
        if (hideLegacyArena)
        {
            HideLegacyObjects();
        }

        BuildArena();
    }

    void Start()
    {
        WireGameplayReferences();
        RepositionPlayers();
    }

    void HideLegacyObjects()
    {
        // Legacy arena artik Disabled_TestObjects altinda ve kapali olabilir.
        // Yine de isimle bulup kapat (eski sahneler / aktif kalanlar icin).
        string[] legacyNames = { "ArenaWalls", "ArenaZones_New", "zemin1" };

        Transform disabledFolder = SceneFolders.Find(SceneFolders.DisabledTestObjects);

        foreach (string objectName in legacyNames)
        {
            GameObject legacy = null;

            if (disabledFolder != null)
            {
                Transform child = disabledFolder.Find(objectName);
                if (child != null)
                {
                    legacy = child.gameObject;
                }
            }

            if (legacy == null)
            {
                legacy = GameObject.Find(objectName);
            }

            if (legacy != null)
            {
                legacy.SetActive(false);
            }
        }
    }

    void BuildArena()
    {
        arenaRoot = new GameObject("HourglassArena").transform;

        // Hierarchy: Scene/Arena/HourglassArena (yoksa GameManager altina dusmesin).
        Transform arenaFolder = SceneFolders.Find(SceneFolders.Arena);
        arenaRoot.SetParent(arenaFolder != null ? arenaFolder : transform, false);

        CreateMaterials();

        float runnerBoundaryZ = arenaHalfLength - throwerZoneDepth;

        BuildFloors(runnerBoundaryZ);
        BuildOuterWalls();
        BuildHourglassWalls(runnerBoundaryZ);
        BuildPockets();
        BuildCoverBlocks();
        BuildCenterLine();

        if (enableVisualPolish)
        {
            BuildZoneBoundaryLines(runnerBoundaryZ);
            BuildWallPanels();
            BuildCornerPillars();
            BuildArenaCeiling();
            BuildArenaLighting(runnerBoundaryZ);
            ApplyAtmosphere();
        }

        BuildZoneTriggers(runnerBoundaryZ);
    }

    void CreateMaterials()
    {
        // Daha koyu, doygun renkler: konseptteki spor salonu / arena hissi.
        matRunner = CreateFloorMaterial(
            new Color(0.06f, 0.38f, 0.18f),
            new Color(0.10f, 0.52f, 0.26f),
            0.42f
        );

        matThrower = CreateFloorMaterial(
            new Color(0.42f, 0.08f, 0.07f),
            new Color(0.62f, 0.14f, 0.12f),
            0.40f
        );

        matPocket = CreateFloorMaterial(
            new Color(0.08f, 0.22f, 0.42f),
            new Color(0.14f, 0.38f, 0.68f),
            0.48f
        );

        matWall = CreateLitMaterial(new Color(0.11f, 0.12f, 0.14f), 0.28f);
        matWallTrim = CreateLitMaterial(new Color(0.07f, 0.08f, 0.09f), 0.15f);
        matNeon = CreateLitMaterial(new Color(1f, 0.18f, 0.12f), 0.95f, new Color(2.2f, 0.25f, 0.08f));
        matNeonBlue = CreateLitMaterial(new Color(0.25f, 0.65f, 1f), 0.95f, new Color(0.35f, 0.85f, 1.6f));
        matCover = CreateLitMaterial(new Color(0.26f, 0.28f, 0.32f), 0.62f);
        matLine = CreateLitMaterial(new Color(0.95f, 0.97f, 1f), 0.2f, new Color(0.4f, 0.45f, 0.55f));
        matCeiling = CreateLitMaterial(new Color(0.04f, 0.05f, 0.07f), 0.05f);
    }

    Material CreateFloorMaterial(Color dark, Color line, float smoothness)
    {
        Material material = CreateLitMaterial(dark, smoothness);

        if (enableVisualPolish)
        {
            Texture2D grid = CreateGridTexture(dark, line);
            ApplyTexture(material, grid);
        }

        return material;
    }

    Material CreateLitMaterial(Color color, float smoothness, Color? emission = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (emission.HasValue)
        {
            Color emissive = emission.Value;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissive);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        return material;
    }

    Texture2D CreateGridTexture(Color background, Color line)
    {
        const int size = 128;
        const int cell = 16;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Repeat;

        Color faintLine = Color.Lerp(background, line, 0.55f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool majorX = x % cell == 0;
                bool majorY = y % cell == 0;
                bool minorX = x % (cell / 2) == 0;
                bool minorY = y % (cell / 2) == 0;

                Color pixel = background;

                if (majorX || majorY)
                {
                    pixel = faintLine;
                }
                else if (minorX || minorY)
                {
                    pixel = Color.Lerp(background, faintLine, 0.18f);
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    void ApplyTexture(Material material, Texture2D texture)
    {
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        material.mainTexture = texture;
    }

    void SetFloorTiling(Renderer renderer, float worldWidth, float worldDepth)
    {
        if (!enableVisualPolish || renderer == null) return;

        Material mat = renderer.material;
        float tile = Mathf.Max(0.5f, gridTileWorldSize);
        Vector2 scale = new Vector2(worldWidth / tile, worldDepth / tile);

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTextureScale("_BaseMap", scale);
        }

        mat.mainTextureScale = scale;
    }

    void BuildFloors(float runnerBoundaryZ)
    {
        Transform floors = CreateChild("Floors");

        float throwerCenterZ = runnerBoundaryZ + throwerZoneDepth * 0.5f;
        float floorWidth = arenaHalfWidth * 2f;

        GameObject throwerTop = CreateFloor(
            floors,
            "ThrowerFloor_Top",
            new Vector3(0f, 0.01f, throwerCenterZ),
            new Vector3(floorWidth, 1f, throwerZoneDepth),
            matThrower
        );
        SetFloorTiling(throwerTop.GetComponent<Renderer>(), floorWidth, throwerZoneDepth);

        GameObject throwerBottom = CreateFloor(
            floors,
            "ThrowerFloor_Bottom",
            new Vector3(0f, 0.01f, -throwerCenterZ),
            new Vector3(floorWidth, 1f, throwerZoneDepth),
            matThrower
        );
        SetFloorTiling(throwerBottom.GetComponent<Renderer>(), floorWidth, throwerZoneDepth);

        float runnerDepth = runnerBoundaryZ * 2f;

        GameObject runnerFloor = CreateFloor(
            floors,
            "RunnerFloor",
            new Vector3(0f, 0.015f, 0f),
            new Vector3(floorWidth, 1f, runnerDepth),
            matRunner
        );
        SetFloorTiling(runnerFloor.GetComponent<Renderer>(), floorWidth, runnerDepth);
    }

    void BuildOuterWalls()
    {
        Transform walls = CreateChild("Walls");

        CreateWall(
            walls,
            "Wall_North",
            new Vector3(0f, wallHeight * 0.5f, arenaHalfLength + wallThickness * 0.5f),
            new Vector3(arenaHalfWidth * 2f + wallThickness * 2f, wallHeight, wallThickness),
            matWall
        );

        CreateWall(
            walls,
            "Wall_South",
            new Vector3(0f, wallHeight * 0.5f, -arenaHalfLength - wallThickness * 0.5f),
            new Vector3(arenaHalfWidth * 2f + wallThickness * 2f, wallHeight, wallThickness),
            matWall
        );

        CreateWall(
            walls,
            "Wall_East",
            new Vector3(arenaHalfWidth + wallThickness * 0.5f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, arenaHalfLength * 2f),
            matWall
        );

        CreateWall(
            walls,
            "Wall_West",
            new Vector3(-arenaHalfWidth - wallThickness * 0.5f, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, arenaHalfLength * 2f),
            matWall
        );

        AddNeonStrip(walls, new Vector3(0f, wallHeight * 0.88f, arenaHalfLength + wallThickness * 0.42f), new Vector3(arenaHalfWidth * 2f, 0.14f, 0.1f), matNeon);
        AddNeonStrip(walls, new Vector3(0f, wallHeight * 0.88f, -arenaHalfLength - wallThickness * 0.42f), new Vector3(arenaHalfWidth * 2f, 0.14f, 0.1f), matNeon);
        AddNeonStrip(walls, new Vector3(arenaHalfWidth + wallThickness * 0.42f, wallHeight * 0.88f, 0f), new Vector3(0.1f, 0.14f, arenaHalfLength * 2f), matNeon);
        AddNeonStrip(walls, new Vector3(-arenaHalfWidth - wallThickness * 0.42f, wallHeight * 0.88f, 0f), new Vector3(0.1f, 0.14f, arenaHalfLength * 2f), matNeon);

        // Duvar taban borduru.
        AddNeonStrip(walls, new Vector3(0f, 0.08f, arenaHalfLength + wallThickness * 0.38f), new Vector3(arenaHalfWidth * 2f, 0.06f, 0.06f), matWallTrim);
        AddNeonStrip(walls, new Vector3(0f, 0.08f, -arenaHalfLength - wallThickness * 0.38f), new Vector3(arenaHalfWidth * 2f, 0.06f, 0.06f), matWallTrim);
    }

    void BuildWallPanels()
    {
        Transform panels = CreateChild("WallPanels");
        float panelHeight = 0.18f;
        float panelGap = 0.08f;
        int panelCount = 5;

        for (int i = 0; i < panelCount; i++)
        {
            float y = 0.35f + i * (panelHeight + panelGap);

            AddNeonStrip(
                panels,
                new Vector3(0f, y, arenaHalfLength + wallThickness * 0.35f),
                new Vector3(arenaHalfWidth * 1.85f, panelHeight, 0.05f),
                matWallTrim
            );

            AddNeonStrip(
                panels,
                new Vector3(0f, y, -arenaHalfLength - wallThickness * 0.35f),
                new Vector3(arenaHalfWidth * 1.85f, panelHeight, 0.05f),
                matWallTrim
            );
        }
    }

    void BuildCornerPillars()
    {
        Transform pillars = CreateChild("CornerPillars");
        float pillarSize = 0.7f;
        float pillarHeight = wallHeight + 0.35f;

        Vector3[] corners =
        {
            new Vector3(arenaHalfWidth, 0f, arenaHalfLength),
            new Vector3(-arenaHalfWidth, 0f, arenaHalfLength),
            new Vector3(arenaHalfWidth, 0f, -arenaHalfLength),
            new Vector3(-arenaHalfWidth, 0f, -arenaHalfLength)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "CornerPillar_" + i;
            pillar.transform.SetParent(pillars, false);
            pillar.transform.position = corners[i] + Vector3.up * pillarHeight * 0.5f;
            pillar.transform.localScale = new Vector3(pillarSize, pillarHeight, pillarSize);
            pillar.GetComponent<Renderer>().sharedMaterial = matWall;

            AddNeonStrip(
                pillars,
                corners[i] + new Vector3(0f, pillarHeight * 0.55f, 0f),
                new Vector3(0.1f, pillarHeight * 0.75f, 0.1f),
                matNeon
            );
        }
    }

    void BuildZoneBoundaryLines(float runnerBoundaryZ)
    {
        Transform lines = CreateChild("ZoneLines");
        float lineWidth = arenaHalfWidth * 2f;

        AddNeonStrip(lines, new Vector3(0f, 0.04f, runnerBoundaryZ), new Vector3(lineWidth, 0.04f, 0.12f), matLine);
        AddNeonStrip(lines, new Vector3(0f, 0.04f, -runnerBoundaryZ), new Vector3(lineWidth, 0.04f, 0.12f), matLine);
    }

    void BuildHourglassWalls(float runnerBoundaryZ)
    {
        Transform walls = CreateChild("HourglassWalls");

        float pocketGapZ = pocketHalfDepth + 0.6f;
        float pocketEdgeX = pocketCenterX - pocketHalfWidth + 0.4f;

        CreateDiagonalWall(walls, -arenaHalfWidth, runnerBoundaryZ, -pocketEdgeX, pocketGapZ);
        CreateDiagonalWall(walls, -pocketEdgeX, pocketGapZ, -neckHalfWidth, 0f);
        CreateDiagonalWall(walls, -neckHalfWidth, 0f, -pocketEdgeX, -pocketGapZ);
        CreateDiagonalWall(walls, -pocketEdgeX, -pocketGapZ, -arenaHalfWidth, -runnerBoundaryZ);

        CreateDiagonalWall(walls, arenaHalfWidth, runnerBoundaryZ, pocketEdgeX, pocketGapZ);
        CreateDiagonalWall(walls, pocketEdgeX, pocketGapZ, neckHalfWidth, 0f);
        CreateDiagonalWall(walls, neckHalfWidth, 0f, pocketEdgeX, -pocketGapZ);
        CreateDiagonalWall(walls, pocketEdgeX, -pocketGapZ, arenaHalfWidth, -runnerBoundaryZ);
    }

    void BuildPockets()
    {
        Transform pockets = CreateChild("Pockets");

        CreatePocket(pockets, "Pocket_Left", -pocketCenterX);
        CreatePocket(pockets, "Pocket_Right", pocketCenterX);
    }

    void CreatePocket(Transform parent, string objectName, float centerX)
    {
        float pocketWidth = pocketHalfWidth * 2f;
        float pocketDepth = pocketHalfDepth * 2f;

        GameObject pocket = CreateFloor(
            parent,
            objectName + "_Floor",
            new Vector3(centerX, 0.02f, 0f),
            new Vector3(pocketWidth, 1f, pocketDepth),
            matPocket
        );
        SetFloorTiling(pocket.GetComponent<Renderer>(), pocketWidth, pocketDepth);

        Renderer[] rimRenderers = null;

        if (enableVisualPolish)
        {
            Transform rim = CreateChild(objectName + "_Rim", parent);
            float rimY = 0.05f;

            AddNeonStrip(rim, new Vector3(centerX, rimY, pocketHalfDepth), new Vector3(pocketWidth, 0.05f, 0.08f), matNeonBlue);
            AddNeonStrip(rim, new Vector3(centerX, rimY, -pocketHalfDepth), new Vector3(pocketWidth, 0.05f, 0.08f), matNeonBlue);
            AddNeonStrip(rim, new Vector3(centerX - pocketHalfWidth, rimY, 0f), new Vector3(0.08f, 0.05f, pocketDepth), matNeonBlue);
            AddNeonStrip(rim, new Vector3(centerX + pocketHalfWidth, rimY, 0f), new Vector3(0.08f, 0.05f, pocketDepth), matNeonBlue);
            rimRenderers = rim.GetComponentsInChildren<Renderer>();
        }

        GameObject triggerObject = new GameObject(objectName);
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.position = new Vector3(centerX, 0.5f, 0f);

        BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(pocketWidth, 2f, pocketDepth);
        trigger.center = Vector3.zero;

        SafeZone safeZone = triggerObject.AddComponent<SafeZone>();
        safeZone.floorRenderer = pocket.GetComponent<Renderer>();
        safeZone.rimRenderers = rimRenderers;
    }

    Transform CreateChild(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    void BuildCoverBlocks()
    {
        Transform coverRoot = CreateChild("Cover");

        CreateCover(coverRoot, "Cover_North", new Vector3(0f, coverHeight * 0.5f, coverCenterZ));
        CreateCover(coverRoot, "Cover_South", new Vector3(0f, coverHeight * 0.5f, -coverCenterZ));
    }

    void CreateCover(Transform parent, string objectName, Vector3 position)
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = objectName;
        cover.transform.SetParent(parent, false);
        cover.transform.position = position;
        cover.transform.localScale = new Vector3(coverSize.x, coverHeight, coverSize.y);
        cover.GetComponent<Renderer>().sharedMaterial = matCover;

        if (enableVisualPolish)
        {
            AddNeonStrip(
                parent,
                position + Vector3.up * (coverHeight * 0.5f + 0.03f),
                new Vector3(coverSize.x * 1.02f, 0.05f, coverSize.y * 1.02f),
                matWallTrim
            );
        }
    }

    void BuildCenterLine()
    {
        Transform decor = CreateChild("Decor");
        float runnerBoundaryZ = arenaHalfLength - throwerZoneDepth;

        for (int i = -4; i <= 4; i++)
        {
            if (i == 0) continue;

            float z = i * (runnerBoundaryZ / 4.5f);

            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = "CenterDash_" + i;
            dash.transform.SetParent(decor, false);
            dash.transform.position = new Vector3(0f, 0.035f, z);
            dash.transform.localScale = new Vector3(0.2f, 0.02f, 1.6f);
            dash.GetComponent<Renderer>().sharedMaterial = matLine;

            Destroy(dash.GetComponent<Collider>());
        }

        // Orta cizgi oklari (kosu yonu ipucu).
        CreateDirectionArrow(decor, new Vector3(0f, 0.04f, runnerBoundaryZ * 0.35f), 0f);
        CreateDirectionArrow(decor, new Vector3(0f, 0.04f, -runnerBoundaryZ * 0.35f), 180f);
    }

    void CreateDirectionArrow(Transform parent, Vector3 position, float yaw)
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.name = "DirectionArrow";
        arrow.transform.SetParent(parent, false);
        arrow.transform.position = position;
        arrow.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        arrow.transform.localScale = new Vector3(0.9f, 0.02f, 0.35f);
        arrow.GetComponent<Renderer>().sharedMaterial = matLine;
        Destroy(arrow.GetComponent<Collider>());
    }

    void BuildArenaCeiling()
    {
        Transform ceilingRoot = CreateChild("Ceiling");

        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ceiling.name = "ArenaCeiling";
        ceiling.transform.SetParent(ceilingRoot, false);
        ceiling.transform.position = new Vector3(0f, ceilingHeight, 0f);
        ceiling.transform.localScale = new Vector3(arenaHalfWidth * 0.22f, 1f, arenaHalfLength * 0.22f);
        ceiling.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        ceiling.GetComponent<Renderer>().sharedMaterial = matCeiling;
        Destroy(ceiling.GetComponent<Collider>());

        // Tavanda ince neon cerceve.
        float halfW = arenaHalfWidth * 0.98f;
        float halfL = arenaHalfLength * 0.98f;
        float y = ceilingHeight - 0.05f;

        AddNeonStrip(ceilingRoot, new Vector3(0f, y, halfL), new Vector3(halfW * 2f, 0.06f, 0.06f), matNeon);
        AddNeonStrip(ceilingRoot, new Vector3(0f, y, -halfL), new Vector3(halfW * 2f, 0.06f, 0.06f), matNeon);
        AddNeonStrip(ceilingRoot, new Vector3(halfW, y, 0f), new Vector3(0.06f, 0.06f, halfL * 2f), matNeon);
        AddNeonStrip(ceilingRoot, new Vector3(-halfW, y, 0f), new Vector3(0.06f, 0.06f, halfL * 2f), matNeon);
    }

    void BuildArenaLighting(float runnerBoundaryZ)
    {
        Transform lighting = CreateChild("Lighting");

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light sceneLight in lights)
        {
            if (sceneLight.type != LightType.Directional) continue;

            sceneLight.color = new Color(0.78f, 0.84f, 1f);
            sceneLight.intensity = 1.05f;
            sceneLight.shadows = LightShadows.Soft;
        }

        Vector3[] cornerPositions =
        {
            new Vector3(arenaHalfWidth * 0.85f, wallHeight + 1.2f, arenaHalfLength * 0.85f),
            new Vector3(-arenaHalfWidth * 0.85f, wallHeight + 1.2f, arenaHalfLength * 0.85f),
            new Vector3(arenaHalfWidth * 0.85f, wallHeight + 1.2f, -arenaHalfLength * 0.85f),
            new Vector3(-arenaHalfWidth * 0.85f, wallHeight + 1.2f, -arenaHalfLength * 0.85f)
        };

        for (int i = 0; i < cornerPositions.Length; i++)
        {
            CreatePointLight(
                lighting,
                "CornerLight_" + i,
                cornerPositions[i],
                new Color(1f, 0.35f, 0.22f),
                28f,
                0.7f
            );
        }

        CreatePointLight(
            lighting,
            "RunnerFill",
            new Vector3(0f, wallHeight + 2.5f, 0f),
            new Color(0.55f, 0.85f, 1f),
            38f,
            0.45f
        );

        CreatePointLight(
            lighting,
            "ThrowerFillTop",
            new Vector3(0f, wallHeight + 2f, runnerBoundaryZ + throwerZoneDepth * 0.45f),
            new Color(1f, 0.45f, 0.35f),
            22f,
            0.35f
        );

        CreatePointLight(
            lighting,
            "ThrowerFillBottom",
            new Vector3(0f, wallHeight + 2f, -(runnerBoundaryZ + throwerZoneDepth * 0.45f)),
            new Color(1f, 0.45f, 0.35f),
            22f,
            0.35f
        );
    }

    void CreatePointLight(Transform parent, string objectName, Vector3 position, Color color, float range, float intensity)
    {
        GameObject lightObject = new GameObject(objectName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
    }

    void ApplyAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.14f, 0.16f, 0.22f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.11f, 0.13f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.07f);

        if (!enableFog) return;

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.07f, 0.09f, 0.13f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
    }

    void BuildZoneTriggers(float runnerBoundaryZ)
    {
        Transform zones = CreateChild("Zones");

        float throwerCenterZ = runnerBoundaryZ + throwerZoneDepth * 0.5f;

        RunnerZoneCollider = CreateZoneTrigger(
            zones,
            "RunnerZone",
            "RunnerZone",
            new Vector3(0f, 0.5f, 0f),
            new Vector3(arenaHalfWidth * 2f, 1f, runnerBoundaryZ * 2f)
        );

        ThrowerZoneTop = CreateZoneTrigger(
            zones,
            "ThrowerZone_Top",
            "ThrowerZone",
            new Vector3(0f, 0.5f, throwerCenterZ),
            new Vector3(arenaHalfWidth * 2f, 1f, throwerZoneDepth)
        );

        ThrowerZoneBottom = CreateZoneTrigger(
            zones,
            "ThrowerZone_Bottom",
            "ThrowerZone",
            new Vector3(0f, 0.5f, -throwerCenterZ),
            new Vector3(arenaHalfWidth * 2f, 1f, throwerZoneDepth)
        );
    }

    Collider CreateZoneTrigger(Transform parent, string objectName, string tag, Vector3 position, Vector3 size)
    {
        GameObject zoneObject = new GameObject(objectName);
        zoneObject.transform.SetParent(parent, false);
        zoneObject.transform.position = position;
        zoneObject.tag = tag;

        BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;

        return collider;
    }

    GameObject CreateFloor(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = objectName;
        floor.transform.SetParent(parent, false);
        floor.transform.position = position;
        floor.transform.localScale = new Vector3(scale.x * 0.1f, 1f, scale.z * 0.1f);
        floor.GetComponent<Renderer>().sharedMaterial = material;

        return floor;
    }

    void CreateWall(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = objectName;
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = material;
    }

    void CreateDiagonalWall(Transform parent, float x1, float z1, float x2, float z2)
    {
        float midX = (x1 + x2) * 0.5f;
        float midZ = (z1 + z2) * 0.5f;
        float dx = x2 - x1;
        float dz = z2 - z1;
        float length = Mathf.Sqrt(dx * dx + dz * dz);
        float angleY = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "HourglassWall";
        wall.transform.SetParent(parent, false);
        wall.transform.position = new Vector3(midX, wallHeight * 0.5f, midZ);
        wall.transform.rotation = Quaternion.Euler(0f, angleY, 0f);
        wall.transform.localScale = new Vector3(wallThickness, wallHeight, length + wallThickness);
        wall.GetComponent<Renderer>().sharedMaterial = matWall;

        if (enableVisualPolish)
        {
            // Dis duvarlarla ayni neon taci: hourglass kenarlari da okunur olsun.
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "HourglassNeon";
            strip.transform.SetParent(parent, false);
            strip.transform.position = new Vector3(midX, wallHeight * 0.88f, midZ);
            strip.transform.rotation = Quaternion.Euler(0f, angleY, 0f);
            strip.transform.localScale = new Vector3(0.1f, 0.12f, length + wallThickness);
            strip.GetComponent<Renderer>().sharedMaterial = matNeon;
            Object.Destroy(strip.GetComponent<Collider>());
        }
    }

    void AddNeonStrip(Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "NeonStrip";
        strip.transform.SetParent(parent, false);
        strip.transform.position = position;
        strip.transform.localScale = scale;
        strip.GetComponent<Renderer>().sharedMaterial = material;

        Destroy(strip.GetComponent<Collider>());
    }

    Transform CreateChild(string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(arenaRoot, false);
        return child.transform;
    }

    void WireGameplayReferences()
    {
        RoleZoneLimiter[] limiters = FindObjectsByType<RoleZoneLimiter>(FindObjectsSortMode.None);

        foreach (RoleZoneLimiter limiter in limiters)
        {
            limiter.runnerZone = RunnerZoneCollider;
            limiter.throwerZoneA = ThrowerZoneTop;
            limiter.throwerZoneB = ThrowerZoneBottom;
        }

        RunnerBot[] bots = FindObjectsByType<RunnerBot>(FindObjectsSortMode.None);

        foreach (RunnerBot bot in bots)
        {
            bot.wanderZone = RunnerZoneCollider;
        }
    }

    void RepositionPlayers()
    {
        MovePlayer(RoleType.Runner, runnerSpawn);
        MovePlayer(RoleType.Saver, saverSpawn);
        MovePlayer(RoleType.Thrower, throwerSpawn);

        RunnerBot bot = FindFirstObjectByType<RunnerBot>();

        if (bot != null)
        {
            bot.transform.position = runnerBotSpawn;
        }
    }

    void MovePlayer(RoleType role, Vector3 position)
    {
        PlayerRole player = PlayerManager.GetFirst(role);

        if (player == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.position = position;
    }
}
