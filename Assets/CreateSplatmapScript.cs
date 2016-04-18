﻿using UnityEngine;
using System.Collections;
using System.Linq;  //used for Sum of array

public class CreateSplatmapScript : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        // Get the attached terrain component
        Terrain terrain = GetComponent<Terrain>();

        // Get a reference to the terrain data
        TerrainData terrainData = terrain.terrainData;

        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y / (float)terrainData.alphamapHeight;
                float x_01 = (float)x / (float)terrainData.alphamapWidth;

                // Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
                float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight), Mathf.RoundToInt(x_01 * terrainData.heightmapWidth));

                // Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
                Vector3 normal = terrainData.GetInterpolatedNormal(y_01, x_01);

                // Calculate the steepness of the terrain
                float steepness = terrainData.GetSteepness(y_01, x_01);

                // Setup an array to record the mix of texture weights at this point
                float[] splatWeights = new float[terrainData.alphamapLayers];

                // Texture[0] has constant influence
                splatWeights[0] = 1f;
                splatWeights[2] = 0f;
                splatWeights[3] = 0f;

                // Texture[1] is stronger at lower altitudes

                //if altitude 0
                if (height == 0)
                {
                    splatWeights[0] = 0f;
                    splatWeights[6] = 0F;
                    splatWeights[1] = 1;//(height+0.01f);//Mathf.Clamp01((terrainData.heightmapHeight - height));
                    splatWeights[4] = 0;
                }
                else
                {
                    splatWeights[1] = 1 / height * 2;
                    splatWeights[6] = Mathf.Clamp01(steepness / (terrainData.heightmapHeight / 5.0f));

                    //if altitude greater than 10% of the map height
                    if (height > terrainData.heightmapHeight / 10)
                    {
                        if (height < (terrainData.heightmapHeight / 10) + (terrainData.heightmapHeight / 20))
                        {
                            splatWeights[4] = height * 0.5f * Random.Range(0.3f, 1.0f) / (steepness * 1.5f);
                        }
                        else
                        {
                            splatWeights[4] = height * 0.5f / (steepness * 1.5f);
                        }

                    }


                    if (splatWeights[6] > 0.5f)
                    {
                        splatWeights[0] = 0.1f;
                    }
                    else
                    {
                        splatWeights[0] = 1 - splatWeights[1] - splatWeights[6] - splatWeights[4];
                    }

                }

                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
