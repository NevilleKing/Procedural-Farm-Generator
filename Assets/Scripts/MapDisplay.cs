using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour {

    public Renderer textureRenderer;

    public void DrawNoiseMap(float[,] noiseMap)
    {
        // get sizes from array
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        // create texture
        Texture2D texture = new Texture2D(width, height);

        // create a 1D array to hold the colour values for the texture
        Color[] colourMap = new Color[width * height];

        // loop through the noise map
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                // use the noise map to get a colour between black and white
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }

        // set texture pixels to the colour map above
        texture.SetPixels(colourMap);
        texture.Apply();

        // need to use shared material so output can be viewed in edit mode
        // otherwise it won't be shown until play mode is activated
        textureRenderer.sharedMaterial.mainTexture = texture;
        // adjust size
        textureRenderer.transform.localScale = new Vector3(width, 1, height);
    }
}
