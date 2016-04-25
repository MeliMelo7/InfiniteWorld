using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  //used for Sum of array

public class CreateSplatmapScript : MonoBehaviour
{

    public Texture2D grassTexture;
    public Texture2D flowerTexture;
    public float limitGrass = 0.6f;

	// Use this for initialization
	void Start ()
    {
        // Get the attached terrain component
        Terrain terrain = GetComponent<Terrain>();

        // Get a reference to the terrain data
        TerrainData terrainData = terrain.terrainData;
        Debug.Log("Terrain Height : " + terrainData.size.y);
        Debug.Log("terraindata height : " + terrainData.heightmapHeight);

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
                    if (height > terrainData.size.y / 10)
                    {
                        if (height < (terrainData.size.y / 10) + (terrainData.size.y / 10))
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

        //add some grass to the terrain where the grass ground texture is greater than limitGrass
        int[,] grassDetailMap = new int[terrainData.heightmapWidth, terrainData.heightmapHeight];
        int[,] flowerDetailMap = new int[terrainData.heightmapWidth, terrainData.heightmapHeight];
        DetailPrototype grassPrototype = new DetailPrototype();
        DetailPrototype flowerPrototype = new DetailPrototype();
        grassPrototype.renderMode = DetailRenderMode.GrassBillboard;
        grassPrototype.prototypeTexture = grassTexture;
        flowerPrototype.renderMode = DetailRenderMode.GrassBillboard;
        flowerPrototype.prototypeTexture = flowerTexture;
        DetailPrototype[] terrainDetailPrototypes = terrainData.detailPrototypes;
        List<DetailPrototype> prototypesList = new List<DetailPrototype>();
        for (int i = 0; i < terrainDetailPrototypes.Length; i++)
        {
            prototypesList.Add(terrainDetailPrototypes[i]);
        }
        prototypesList.Add(grassPrototype);
        prototypesList.Add(flowerPrototype);

        DetailPrototype[] terrainProto = prototypesList.ToArray();
        terrainData.detailPrototypes = terrainProto;


        for (int yIt = 0; yIt < terrainData.alphamapHeight; yIt++)
        {
            for (int xIt = 0; xIt < terrainData.alphamapWidth; xIt++)
            {
                if(splatmapData[xIt, yIt, 0] > limitGrass)
                {
                    grassDetailMap[xIt, yIt] = 1;
                    flowerDetailMap[xIt, yIt] = Random.Range(0, 2);                   
                }
                else
                {
                    grassDetailMap[xIt, yIt] = 0;
                    flowerDetailMap[xIt, yIt] = 0;
                }
                
            }
        }
        terrainData.SetDetailLayer(0, 0, 0, grassDetailMap);
        terrainData.SetDetailLayer(0, 0, 1, flowerDetailMap);


        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
