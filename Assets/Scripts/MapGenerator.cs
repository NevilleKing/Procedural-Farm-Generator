using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

// Inspector Script to allow editing of values
public class MapGenerator : MonoBehaviour {

    public enum DrawMode
    {
        NoiseMap,
        ColourMap,
        Mesh
    }
    public DrawMode drawMode;

    const int mapChunkSize = 241; // for level of detail
    [Range(0,6)]
    public int levelOfDetail;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    // define how different height values are affected by multiplier
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;
    public bool showBorder;

    public TerrainType[] regions;

    // When button is pressed, generate the map
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        float heightCheck = regions[0].height;

        // loop through the noise map
        for (int y = 0; y < mapChunkSize; ++y)
        {
            for (int x = 0; x < mapChunkSize; ++x)
            {
                float currentHeight = noiseMap[x, y];

                // see what region the current height falls within
                for (int i = 0; i < regions.Length; ++i)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].colour;

                        break;
                    }
                }
            }
        }

        for (int i = 0; i < colourMap.Length; ++i)
        {
            try
            {
                if ((i % mapChunkSize) != 0 && noiseMap[i * mapChunkSize - 1, i % mapChunkSize] <= heightCheck)
                    colourMap[i * mapChunkSize + (i % mapChunkSize) - 1] = Color.black;
            } catch (Exception e)
            {
                Debug.Log("test");
            }
        }

        //for (int y2 = 0; y2 < mapChunkSize; ++y2)
        //{
        //    for (int x2 = 0; x2 < mapChunkSize; ++x2)
        //    {
        //        for (int i2 = 0; i2 < regions.Length; ++i2)
        //        {
        //            if (i2 == 1 && showBorder)
        //            {
        //                // check left
        //                if (x2 > 0 && noiseMap[x2 - 1, y2] <= heightCheck)
        //                    colourMap[y2 * mapChunkSize + x2 - 1] = Color.black;
        //                //// check right
        //                //if (x2 < mapChunkSize - 1 && noiseMap[x2 + 1, y2] <= heightCheck)
        //                //    colourMap[y2 * mapChunkSize + x2 + 1] = Color.black;
        //                //// check top
        //                //if (y2 > 0 && noiseMap[x2, y2 - 1] <= heightCheck)
        //                //    colourMap[(y2 - 1) * mapChunkSize + x2] = Color.black;
        //                //// check bottom
        //                //if (y2 < mapChunkSize - 1 && noiseMap[x2, y2 + 1] <= heightCheck)
        //                //    colourMap[(y2 + 1) * mapChunkSize + x2] = Color.black;
        //            }
        //        }
        //    }
        //}

        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
    }

    // make sure that values don't go out of range
    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 0)
            octaves = 0;
    }

    private void Start()
    {
        // generate the map on play
        GenerateMap();
    }
}

// Hold information on terrain types
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}
