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
    public bool placeModels;
    public bool placeFields;

    public TerrainType[] regions;
    public SpawningInfo Models;

    // colours for fields
    public Color[] fieldColours;

    // parent of model objects
    private GameObject modelParent;

    // When button is pressed, generate the map
    public void GenerateMap()
    {
        deleteModels();

        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        bool[] fieldSquareDone = new bool[mapChunkSize * mapChunkSize];

        float heightCheck = regions[0].height;

        System.Random prng = new System.Random(seed);
        List<int> treePositions = new List<int>();

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
                        Color colour = regions[i].colour;

                        if (i != 0) fieldSquareDone[y * mapChunkSize + x] = true;

                        if (i == 1 && showBorder)
                        {
                            // check left
                            if (x > 0 && noiseMap[x - 1, y] <= heightCheck)
                                colourMap[y * mapChunkSize + x - 1] = Color.black;
                            // check top
                            if (y > 0 && noiseMap[x, y - 1] <= heightCheck)
                                colourMap[(y-1) * mapChunkSize + x] = Color.black;
                        }
                        else if (i == 0 && showBorder)
                        {
                            // check left
                            if (x > 0 && noiseMap[x - 1, y] > heightCheck)
                                colour = Color.black;
                            // check top
                            if (y > 0 && noiseMap[x, y - 1] > heightCheck)
                                colour = Color.black;
                        }
                        else if (i == 3 && placeModels)
                        {
                                treePositions.Add(y * mapChunkSize + x);
                        }

                        colourMap[y * mapChunkSize + x] = colour;
                        
                        break;
                    }
                }
            }
        }

        int t = 0;

        // loop through array again to add fields
        if (placeFields)
        {
            for (int y = 0; y < mapChunkSize; ++y)
            {
                for (int x = 0; x < mapChunkSize; ++x)
                {
                    ++t;
                    if (!fieldSquareDone[y * mapChunkSize + x])
                    {
                        int fieldHeight = 10;
                        int fieldWidth = 5;

                        int fX = x;
                        int fY = y * mapChunkSize;

                        int fh = fY + (fieldHeight * mapChunkSize);
                        int fw = fX + fieldWidth;

                        // pick random colour
                        Color c = fieldColours[prng.Next(0, fieldColours.Length)];

                        while (fY < fh && fY < fieldSquareDone.Length)
                        {
                            fX = x;
                            while (fX < fw && fX < mapChunkSize)
                            {
                                if (!fieldSquareDone[fY + fX])
                                {
                                    fieldSquareDone[fY + fX] = true;
                                    colourMap[fY + fX] = c;
                                }

                                ++fX;
                            }

                            fY += mapChunkSize;
                        }
                    }
                }
            }
        }

        Debug.Log(t);

        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
        {
            MeshData md = MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
            display.DrawMesh(md, TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));

            modelParent = new GameObject();
            modelParent.name = "Models";

            for (int i = 0; i < treePositions.Count; ++i)
            {
                Vector3 pos = md.vertices[treePositions[i]];
                pos.x *= 10;
                pos.z *= 10;

                // number to spawn
                int numToSpawn = prng.Next(1, 4);
                for (int treeNum = 0; treeNum < numToSpawn; ++treeNum)
                {
                    Vector3 currentTreePos = pos;
                    currentTreePos.x += (prng.Next(-5, 5));
                    currentTreePos.z += (prng.Next(-5, 5));

                    // random rotation
                    int rot = prng.Next(0, 360);

                    GameObject tree = Instantiate(Models.treeModels[0], currentTreePos, Quaternion.Euler(new Vector3(270, rot, 0)));
                    tree.transform.SetParent(modelParent.transform);
                }
            }
        }
    }

    public void deleteModels()
    {
        if (modelParent != null)
        {
            DestroyImmediate(modelParent);
            modelParent = null;
        }
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

// Holds spawning information
[System.Serializable]
public struct SpawningInfo
{
    public GameObject[] treeModels;
}
