using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;

    [Header("Display Settings")]
    [SerializeField] private Renderer textureRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Transform objectParent;
    [SerializeField] private MeshCollider meshCollider;
    public enum DrawMode { NoiseMap, ColourMap, Mesh }
    public DrawMode drawMode;

    [Header("Map Settings")]
    public bool autoUpdate;
    public int mapWidth;
    public int mapHeight;
    public float meshHeightMultiplier;
    public int seed;
    public Vector2 offset;

    [Header("Noise Settings")]
    public float noiseScale;
    public int octaves;
    [Range(0, 1)]
    public float persistence;
    public float lacunarity;

    [Header("Plains Generation")]
    public bool generatePlains;
    public float plainsScale = 150f;
    [Range(0, 1)]
    public float plainsThreshold = 0.7f;
    [Range(0, 1)]
    public float plainsBlendRange = 0.1f;
    [Range(0, 1)]
    public float plainsHeight = 0.3f;

    [Header("Island Generation")]
    public bool generateIslands;
    public AnimationCurve islandFalloffCurve;

    [Header("Terrain Types")]
    public TerrainType[] biomes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, 
            lacunarity, offset, generateIslands, islandFalloffCurve, generatePlains, plainsScale, plainsThreshold, plainsBlendRange, plainsHeight);

        float minNoiseHeight = float.MaxValue;
        float maxNoiseHeight = float.MinValue;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (noiseMap[x, y] > maxNoiseHeight)
                    maxNoiseHeight = noiseMap[x, y];
                if (noiseMap[x, y] < minNoiseHeight)
                    minNoiseHeight = noiseMap[x, y];
            }
        }

        Color[] colorMap = new Color[mapWidth * mapHeight];
        float[,] normalizedHeightMap = new float[mapWidth, mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float normalizedHeight = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                normalizedHeightMap[x, y] = normalizedHeight;
                // float currentHeight = noiseMap[x, y];

                for (int i = 0; i < biomes.Length; i++)
                {
                    if (normalizedHeight <= biomes[i].height)
                    {
                        colorMap[y * mapWidth + x] = biomes[i].color;
                        noiseMap[x, y] = biomes[i].heightCurve.Evaluate(normalizedHeight) * biomes[i].heightMultiplier;
                        break;
                    }
                }
            }
        }

        if (Application.isPlaying || drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrain(noiseMap, meshHeightMultiplier),
                     MyTextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));

            if (objectParent != null)
            {
                for (int i = objectParent.childCount - 1; i >= 0; i--)
                {
                    if (Application.isPlaying) Destroy(objectParent.GetChild(i).gameObject);
                    else DestroyImmediate(objectParent.GetChild(i).gameObject);
                }

                if (biomes != null && biomes.Length > 0)
                {
                    ObjectGeneration.PlaceObjectAtPosition(noiseMap, normalizedHeightMap, biomes, objectParent);
                }
            }
        }
        else if (drawMode == DrawMode.NoiseMap)
        {
            DisplayMap(MyTextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            DisplayMap(MyTextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        }
    }

    public void DisplayMap(Texture2D texture)
    {
        if (textureRenderer == null) return;

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        Material targetMat = Application.isPlaying ? textureRenderer.material : textureRenderer.sharedMaterial;

        SetTextureSafe(targetMat, texture);

        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }


    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        Mesh createdMesh = meshData.CreateMesh();
        meshFilter.sharedMesh = createdMesh;
        meshRenderer.sharedMaterial.mainTexture = texture;

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();

        Material targetMat = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
        SetTextureSafe(targetMat, texture);

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = createdMesh;
        }
    }

    // Helper for URP vs Standard shader texture names
    private void SetTextureSafe(Material mat, Texture2D texture)
    {
        if (mat.HasProperty("_BaseMap")) // URP
        {
            mat.SetTexture("_BaseMap", texture);
        }
        else if (mat.HasProperty("_MainTex")) // Built-in
        {
            mat.mainTexture = texture;
        }
        else
        {
            // Fallback
            mat.mainTexture = texture;
        }
    }

    void OnValidate()
    {
        if (mapWidth < 1) mapWidth = 1;
        if (mapHeight < 1) mapHeight = 1;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }
}

[System.Serializable]
public struct TerrainType
{
    [Header("Terrain Type Settings")]
    public string name;
    public float height;
    public float heightMultiplier;
    public AnimationCurve heightCurve;
    public Texture2D texture;
    public Color color;
    [Range(0, 1)]
    public float blendValue;

    [Header("Object Placement Settings")]
    public GameObject[] placeableObjects;
    public float positionOffset;
    public float objectDensity;
    [Range(0, 90)]
    public float maxPlacementSteepness;
    public bool alignObjectsToSurfaceNormal;
}