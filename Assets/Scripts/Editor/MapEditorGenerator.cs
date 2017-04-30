using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Editor Script - add button control in inspector
[CustomEditor (typeof(MapGenerator))]
public class NewBehaviourScript : Editor {

    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        // if values have changed in the inspector
        if (DrawDefaultInspector())
        {
            // genertate the map if auto update is selected
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateMap();
            }
        }

        // if the button is pressed, generate map
        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap();
        }
    }

}
