using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private Renderer textureRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Transform objectParent;
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
                //float currentHeight = noiseMap[x, y];

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

        if (drawMode == DrawMode.NoiseMap)
        {
            DisplayMap(MyTextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            DisplayMap(MyTextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrain(noiseMap, 1f), MyTextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
            for (int i = 0; i < objectParent.childCount; i++)
            {
                DestroyImmediate(objectParent.GetChild(i).gameObject);
            }
            ObjectGeneration.PlaceObjectAtPosition(noiseMap, normalizedHeightMap, biomes, objectParent);
        }
    }

    public void DisplayMap(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
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