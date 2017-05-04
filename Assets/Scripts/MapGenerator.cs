using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

// Inspector Script to allow editing of values
public class MapGenerator : MonoBehaviour
{

    public enum DrawMode
    {
        NoiseMap,
        ColourMap,
        Mesh
    }
    public DrawMode drawMode;

    const int mapChunkSize = 241; // for level of detail
    [Range(0, 6)]
    public int levelOfDetail;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
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
        List<ModelPlacementInfo> modelPositions = new List<ModelPlacementInfo>();

        // loop through the noise map
        for (int y = 0; y < mapChunkSize; ++y) {
            for (int x = 0; x < mapChunkSize; ++x) {
                float currentHeight = noiseMap[x, y];

                // see what region the current height falls within
                for (int i = 0; i < regions.Length; ++i) {
                    if (currentHeight <= regions[i].height) {
                        Color colour = regions[i].colour;

                        if (i != 0) fieldSquareDone[y * mapChunkSize + x] = true;

                        if (i == 1 && showBorder) {
                            // check left
                            if (x > 0 && noiseMap[x - 1, y] <= heightCheck) {
                                colourMap[y * mapChunkSize + x - 1] = Color.black;
                                modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.wall, y * mapChunkSize + x, ModelPlacementInfo.Rotation.left));
                            }
                            // check top
                            if (y > 0 && noiseMap[x, y - 1] <= heightCheck) {
                                colourMap[(y - 1) * mapChunkSize + x] = Color.black;
                                modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.wall, y * mapChunkSize + x, ModelPlacementInfo.Rotation.top));
                            }
                        } else if (i == 0 && showBorder) {
                            // check left
                            if (x > 0 && noiseMap[x - 1, y] > heightCheck) {
                                colour = Color.black;
                                modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.wall, y * mapChunkSize + x, ModelPlacementInfo.Rotation.left));
                            }
                            // check top
                            if (y > 0 && noiseMap[x, y - 1] > heightCheck) {
                                colour = Color.black;
                                modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.wall, y * mapChunkSize + x, ModelPlacementInfo.Rotation.top));
                            }
                        } else if (i == 3 && placeModels) {
                            modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.tree, (y * mapChunkSize + x)));
                        }

                        colourMap[y * mapChunkSize + x] = colour;

                        break;
                    }
                }
            }
        }

        // loop through array again to add fields
        if (placeFields) {
            float[,] fieldNoise = Noise.GenerateFieldMap(mapChunkSize, mapChunkSize, seed);

            float noiseInterval = 1.0f / (float)fieldColours.Length;

            for (int y = 0; y < mapChunkSize; ++y) {
                for (int x = 0; x < mapChunkSize; ++x) {
                    if (!fieldSquareDone[y * mapChunkSize + x]) {
                        //fieldSquareDone[y * mapChunkSize + x] = true;
                        float currentNoiseValue = fieldNoise[x, y];

                        int index = 0;
                        for (float j = noiseInterval; j <= 1; j += noiseInterval, ++index) {
                            if (currentNoiseValue <= j) {
                                colourMap[y * mapChunkSize + x] = fieldColours[index];
                                break;
                            }
                        }

                        if (placeModels && showBorder) {
                            // check left
                            if (x > 0 && fieldNoise[x - 1, y] != currentNoiseValue) {
                                modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.wall, y * mapChunkSize + x, ModelPlacementInfo.Rotation.left));
                            }
                            // check top
                            if (y > 0 && fieldNoise[x, y - 1] != currentNoiseValue) {
                                modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.wall, y * mapChunkSize + x, ModelPlacementInfo.Rotation.top));
                            }
                        }
                    }


                }
            }

            // crop generation
            for (int y = 0; y < mapChunkSize; ++y) {
                for (int x = 0; x < mapChunkSize; ++x) {
                    if (!fieldSquareDone[y * mapChunkSize + x]) {

                        List<int> result = RecursiveField(x, y, ref fieldSquareDone, ref fieldNoise);
                        int crop = prng.Next(0, Models.crops.Length);
                        
                        for (int i = 0; i < result.Count; ++i)
                            modelPositions.Add(new ModelPlacementInfo(ModelPlacementInfo.PlacementType.crops, result[i], crop));
                    }
                }
            }
        }



        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh) {
            MeshData md = MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
            display.DrawMesh(md, TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));

            modelParent = new GameObject();
            modelParent.name = "Models";

            for (int i = 0; i < modelPositions.Count; ++i) {
                Vector3 pos = md.vertices[modelPositions[i].meshIndex];
                pos.x *= 10;
                pos.z *= 10;

                switch (modelPositions[i].type) {

                    case ModelPlacementInfo.PlacementType.tree:

                        int treeModelIndex = prng.Next(0, Models.treeModels.Length);

                        // number to spawn
                        int numToSpawn = prng.Next(1, 4);
                        for (int treeNum = 0; treeNum < numToSpawn; ++treeNum) {
                            Vector3 currentTreePos = pos;
                            currentTreePos.x += (prng.Next(-5, 5));
                            currentTreePos.z += (prng.Next(-5, 5));

                            // random rotation
                            int rot = prng.Next(0, 360);

                            GameObject tree = Instantiate(Models.treeModels[treeModelIndex], currentTreePos, Quaternion.Euler(new Vector3(270, rot, 0)));
                            tree.transform.SetParent(modelParent.transform);
                        }
                        break;

                    case ModelPlacementInfo.PlacementType.wall:

                        GameObject wall;

                        if (modelPositions[i].rotation == ModelPlacementInfo.Rotation.left) {
                            wall = Instantiate(Models.wallModels[0], pos - new Vector3(0, -1, 5), Quaternion.Euler(new Vector3(270, 90, 0)));
                            wall.transform.SetParent(modelParent.transform);
                        } else if (modelPositions[i].rotation == ModelPlacementInfo.Rotation.top) {
                            wall = Instantiate(Models.wallModels[0], pos + new Vector3(Models.wallModels[0].GetComponent<MeshRenderer>().bounds.size.x / 2, 1, 0), Quaternion.Euler(new Vector3(270, 0, 0)));
                            wall.transform.SetParent(modelParent.transform);
                        }

                        break;

                    case ModelPlacementInfo.PlacementType.crops:

                        GameObject crop = Instantiate(Models.crops[modelPositions[i].gameObjectIndex], pos + new Vector3(5, 0, -5), Quaternion.identity);
                        crop.transform.SetParent(modelParent.transform);
                        break;
                }
            }
        }
    }

    public void deleteModels()
    {
        if (modelParent != null) {
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

    private List<int> RecursiveField(int x, int y, ref bool[] fieldSquareDone, ref float[,] fieldNoise)
    {
        List<int> result = new List<int>();

        FRecurse(x, y, ref result, ref fieldSquareDone, ref fieldNoise);

        return result;
    }

    private void FRecurse(int x, int y, ref List<int> result, ref bool[] fieldSquareDone, ref float[,] fieldNoise)
    {
        int iPos = y * mapChunkSize + x;
        fieldSquareDone[iPos] = true;
        result.Add(iPos);

        float currentNoiseValue = fieldNoise[x, y];

        // check left
        if (x > 0 && !fieldSquareDone[iPos-1] && fieldNoise[x - 1, y] == currentNoiseValue)
            FRecurse(x - 1, y, ref result, ref fieldSquareDone, ref fieldNoise);


        // check right
        if (x < (mapChunkSize-1) && !fieldSquareDone[iPos + 1] && fieldNoise[x + 1, y] == currentNoiseValue)
            FRecurse(x + 1, y, ref result, ref fieldSquareDone, ref fieldNoise);


        // check top
        if (y > 0 && !fieldSquareDone[iPos - mapChunkSize] && fieldNoise[x, y - 1] == currentNoiseValue)
            FRecurse(x, y - 1, ref result, ref fieldSquareDone, ref fieldNoise);

        // check bottom
        if (y < (mapChunkSize - 1) && !fieldSquareDone[iPos + mapChunkSize] && fieldNoise[x, y + 1] == currentNoiseValue)
            FRecurse(x, y + 1, ref result, ref fieldSquareDone, ref fieldNoise);

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
    public GameObject[] wallModels;
    public GameObject[] crops;
}

public class ModelPlacementInfo
{
    public enum PlacementType
    {
        tree,
        wall,
        crops
    }

    public enum Rotation
    {
        left,
        top
    }

    public PlacementType type;
    public int meshIndex;
    public Rotation rotation;

    public int gameObjectIndex;

    public ModelPlacementInfo(PlacementType t,
                              int meshI)
    {
        type = t;
        meshIndex = meshI;
    }

    public ModelPlacementInfo(PlacementType t,
                              int meshI,
                              int go_loc)
    {
        type = t;
        meshIndex = meshI;
        gameObjectIndex = go_loc;
    }

    public ModelPlacementInfo(PlacementType t,
                             int meshI,
                             Rotation rot)
    {
        type = t;
        meshIndex = meshI;
        rotation = rot;
    }
}
