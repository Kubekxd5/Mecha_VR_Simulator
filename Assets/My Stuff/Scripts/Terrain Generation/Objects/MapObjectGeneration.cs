using System.Collections.Generic;
using UnityEngine;

public static class ObjectGeneration
{
    public static void PlaceObjectAtPosition(float[,] mapData, float[,] normalizedHeightMap, TerrainType[] biomes, Transform parent)
    {
        List<Vector2> spawnedPoints = new List<Vector2>();

        int mapWidth = mapData.GetLength(0);
        int mapHeight = mapData.GetLength(1);

        float topLeftX = (mapWidth / -2f);
        float topLeftZ = (mapHeight / 2f);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (Random.value < biomes[FindBiomeIndexForHeight(normalizedHeightMap[x, y], biomes)].objectDensity/10)
                {
                    float normalizedHeight = normalizedHeightMap[x, y];
                    TerrainType currentBiome = FindBiomeForHeight(normalizedHeight, biomes);

                    Vector3 surfaceNormal = CalculateSurfaceNormal(x, y, mapData);
                    float steepnessAngle = Vector3.Angle(Vector3.up, surfaceNormal);

                    if (steepnessAngle <= currentBiome.maxPlacementSteepness)
                    {
                        GameObject objectToPlace = currentBiome.placeableObjects[Random.Range(0, currentBiome.placeableObjects.Length)];
                        TerrainObject terrainObject = objectToPlace.GetComponent<TerrainObject>();
                        
                        if (terrainObject == null) continue;

                        if (IsValidPosition(new Vector2(x, y), terrainObject.objectRadius, spawnedPoints, mapData))
                        {
                            float finalMeshHeight = mapData[x, y];
                            Vector3 position = new Vector3(topLeftX + x, finalMeshHeight, topLeftZ - y);
                            position = new Vector3(position.x, position.y + currentBiome.positionOffset, position.z);
                            
                            Quaternion rotation = currentBiome.alignObjectsToSurfaceNormal ? 
                                Quaternion.FromToRotation(Vector3.up, surfaceNormal) : Quaternion.identity;
                                rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);

                            Object.Instantiate(objectToPlace, position, rotation, parent);

                            spawnedPoints.Add(new Vector2(x, y));
                        }
                    }
                }
            }
        }
    }

    private static bool IsValidPosition(Vector2 point, float objectRadius, List<Vector2> spawnedPoints, float[,] mapData)
    {
        if(point.x < objectRadius || point.x > mapData.GetLength(0) - objectRadius ||
            point.y < objectRadius || point.y > mapData.GetLength(1) - objectRadius) return false;
       
        foreach (var spawnedPoint in spawnedPoints)
        {
            if (Vector2.Distance(point, spawnedPoint) < objectRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static TerrainType FindBiomeForHeight(float height, TerrainType[] biomes)
    {
        foreach (var biome in biomes)
        {
            if (height <= biome.height)
            {
                return biome;
            }
        }
        return biomes[biomes.Length - 1];
    }

    private static int FindBiomeIndexForHeight(float height, TerrainType[] biomes)
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            if (height <= biomes[i].height)
            {
                return i;
            }
        }
        return biomes.Length - 1;
    }

    private static Vector3 CalculateSurfaceNormal(int x, int y, float[,] mapData)
    {
        float heightLeft = mapData[Mathf.Max(x - 1, 0), y];
        float heightRight = mapData[Mathf.Min(x + 1, mapData.GetLength(0) - 1), y];
        float heightDown = mapData[x, Mathf.Max(y - 1, 0)];
        float heightUp = mapData[x, Mathf.Min(y + 1, mapData.GetLength(1) - 1)];

        Vector3 normal = new Vector3(heightLeft - heightRight, 2f, heightDown - heightUp);
        return normal.normalized;
    }
}
