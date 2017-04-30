using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {

    // Create a texture from the colour map
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; // fix blurry issue
        texture.wrapMode = TextureWrapMode.Clamp; // don't repeat from other side of texture
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    // get texture from 2D height map
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        // get sizes from array
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        // create a 1D array to hold the colour values for the texture
        Color[] colourMap = new Color[width * height];

        // loop through the noise map
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                // use the noise map to get a colour between black and white
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }
	
}
