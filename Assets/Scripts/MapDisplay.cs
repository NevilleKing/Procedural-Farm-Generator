using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour {

    public Renderer textureRenderer;

    public void DrawTexture(Texture2D texture)
    {
        // need to use shared material so output can be viewed in edit mode
        // otherwise it won't be shown until play mode is activated
        textureRenderer.sharedMaterial.mainTexture = texture;
        // adjust size
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
}
