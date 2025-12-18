using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, 
        float persistance, float lacunarity, Vector2 offset, bool generateIslands, AnimationCurve islandFalloffCurve,
        bool generatePlains, float plainsScale, float plainsThreshold, float plainsBlendRange, float plainsHeight)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        Vector2 plainsOffset = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0)scale = 0.0001f; // Prevent division by zero by clamping scale to a small value
        if (plainsScale <= 0) plainsScale = 0.0001f; // Prevent division by zero by clamping plainsScale to a small value

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float centerWidth = mapWidth / 2f;
        float centerHeight = mapHeight / 2f;

        //float maxDistanceFromCenter = Vector2.Distance(new Vector2(centerWidth, centerHeight), new Vector2(0, 0)); // For island generation

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - centerWidth) / scale * frequency + octaveOffsets[i].x * frequency;
                    float sampleY = (y - centerHeight) / scale * frequency - octaveOffsets[i].y * frequency;

                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += noiseValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        // Apply plains and island generation
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float normalizedHeight = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]); // Normalize height between 0 and 1

                // Apply plains generation
                if (generatePlains)
                {
                    float plainsSampleX = (x - centerWidth) / plainsScale + plainsOffset.x;
                    float plainsSampleY = (y - centerHeight) / plainsScale + plainsOffset.y;
                    float plainsNoiseValue = Mathf.PerlinNoise(plainsSampleX, plainsSampleY);

                    float blendStart = plainsThreshold;
                    float blendEnd = plainsThreshold + plainsBlendRange;
                    float blendFactor = Mathf.InverseLerp(blendStart, blendEnd, plainsNoiseValue);

                    normalizedHeight = Mathf.Lerp(normalizedHeight, plainsHeight, blendFactor);
                }

                // Apply island generation
                if (generateIslands)
                {
                    float islandRadious = mapWidth / 2f;
                    float distanceFromCenter = Vector2.Distance(new Vector2(centerWidth, centerHeight), new Vector2(x, y));
                    float normalizedDistance = distanceFromCenter / islandRadious;
                    //float normalizedDistance = distanceFromCenter / maxDistanceFromCenter;
                    float falloff = islandFalloffCurve.Evaluate(normalizedDistance);

                    normalizedHeight *= Mathf.Clamp01(1 - falloff);
                }

                noiseMap[x, y] = normalizedHeight;
            }
        }

        float minFinalHeight = float.MaxValue;
        float maxFinalHeight = float.MinValue;
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (noiseMap[x, y] > maxFinalHeight) maxFinalHeight = noiseMap[x, y];
                if (noiseMap[x, y] < minFinalHeight) minFinalHeight = noiseMap[x, y];
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (maxFinalHeight <= minFinalHeight)
                {
                    noiseMap[x, y] = 0;
                }
                else
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minFinalHeight, maxFinalHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }
}
