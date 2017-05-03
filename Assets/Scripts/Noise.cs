using System.Collections;
using System.Collections.Generic;
using LibNoise.Operator;
using LibNoise.Generator;
using UnityEngine;

public static class Noise {

    // octaves - number of noise maps to add together
    // lacunarity - amount of detail - controls increase in frequency of octaves
    // persistance - how much effect each octave has
    // offset - allow scrolling through the noise
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);

        // each octave sampled from different location
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; ++i)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // hanlde divide by 0 error
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // when zoomed, zoom into middle
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // loop through entire map array
        for (int y = 0; y < mapHeight; ++y)
        {
            for (int x = 0; x < mapWidth; ++x)
            {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; ++i)
                {
                    // choose the points to sample from
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    // get the perlin value at the above point
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // -1 to 1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance; // decreases
                    frequency *= lacunarity; // increases
                }

                // update lowest & highest value
                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                // apply the noise height
                noiseMap[x, y] = noiseHeight;
            }
        }

        // normalize noise map (set between 0-1)
        for (int y = 0; y < mapHeight; ++y)
        {
            for (int x = 0; x < mapWidth; ++x)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
	
    public static float[,] GenerateFieldMap(int mapWidth, int mapHeight, int seed)
    {
        //Perlin perlin = new LibNoise.Generator.Perlin();
        //Voronoi vor = new Voronoi();
        //RidgedMultifractal rmf = new RidgedMultifractal();

        //Const constGen = new Const(0.5f);

        //var noiseGen = new Add(new Multiply(perlin, constGen), new Multiply(rmf, constGen));

        //LibNoise.Noise2D noise2d = new LibNoise.Noise2D(size, noiseGen);

        //noise2d.GeneratePlanar(0.0f, 1.0f, 0.0f, 1.0f);

        //return noise2d.GetData();

        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000);
        float offsetY = prng.Next(-100000, 100000);

        float scale = 10.0f;

        int noiseSize = mapWidth * mapHeight;

        Voronoi vor = new Voronoi();

        LibNoise.Noise2D noise2d = new LibNoise.Noise2D(mapWidth, vor);
        
        noise2d.GeneratePlanar(offsetX, offsetX + scale, offsetY, offsetY + scale);

        float[,] noise = noise2d.GetData();

        // normalise
        for (int y = 0; y < mapHeight; ++y)
        {
            for (int x = 0; x < mapWidth; ++x)
            {
                noise[x, y] = Mathf.InverseLerp(-1, 1, noise[x, y]);
            }
        }

        return noise;
    }
}
