 using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

public class HeightmapGen : MonoBehaviour {

    private float[,] HeightMap;
    private float[] weights = { 1f, 1 / 2f, 1 / 4f, 1 / 8f, 1 / 16f, 1 / 32f, 1 / 64f, 1 / 128f};//{ 1f, 1 / 4f, 1/16f, 1/64f, 1/256f, 1/1024f};//new float[30];
    private float[] frequencies = { 1f, 2f, 4f, 8f, 16f, 32f, 64f, 128f}; //{ 1f, 2f, 4f, 8f, 16f, 32f};//new float[30];
    private int xTerrainRes;
	private int yTerrainRes;
	private GameObject myTerrain;
	private TerrainData tData;
	public float xOrg = 0;
	public float yOrg = 0;
	public float frequency = 1.0F;
	private float previousFrequency = 0.0F;
	private int i = 0;
	private int j = 0;
	public Texture2D[] terrainTex;
    public Texture2D[] terrainNormals;
	public int passes = 2;
	public float amplitude = 1.0F;  //terrain height
	private float previousAmplitude = 1.0F;
    public Material terrainMat;

	// Use this for initialization
	void Start ()
	{
		previousFrequency = frequency;
		previousAmplitude = amplitude;
        //for (int i = 0; i < 60; i++)
        //{
        //    frequencies[i] = (i + 1);
        //    weights[i] = Mathf.Pow(frequencies[i], (-1f));
        //}
        generateNewTerrain();
	}
		
	void generateNewTerrain()
	{
		//set the data of the terrain we will create
		tData = new TerrainData();
		tData.size = new Vector3(32, 500, 32);
		tData.heightmapResolution = 513;
		tData.SetDetailResolution(512, 8);
		HeightMap = tData.GetHeights(0, 0, tData.heightmapWidth, tData.heightmapHeight); //set the HeightMap tab to make it correspond to the actual height tab for our terrain
        float lowestValue = 500f;

        
        for (int i = 0; i < passes; i++)
        {
            int y = 0;
            while (y < tData.heightmapHeight)
            {
                int x = 0;
                while (x < tData.heightmapWidth)
                {
                    float xCoord = Convert.ToSingle(x) / Convert.ToSingle(tData.heightmapWidth - 1); //need to convert everything to float in order for the Perling Noise function to work
                    float yCoord = Convert.ToSingle(y) / Convert.ToSingle(tData.heightmapHeight - 1); // same
                    float sample = Mathf.PerlinNoise(xCoord * frequencies[i] * frequency, yCoord * frequencies[i] * frequency); //generate height value for a vertex of the terrain with the Perlin Noise function
                    HeightMap[x, y] += sample * weights[i] * amplitude; //put the generated value in the corresponding table of heights
                    if(i == (passes - 1))
                    {
                        if (HeightMap[x, y] < lowestValue)
                        {
                            lowestValue = HeightMap[x, y];
                        }
                    }
                    x++;
                }
                y++;
            }
        }
        Debug.Log("lowest value : " + lowestValue);

        int q = 0;
        while (q < tData.heightmapHeight)
        {
            int x = 0;
            while (x < tData.heightmapWidth)
            {
                HeightMap[x, q] = HeightMap[x, q] - lowestValue;//put the generated value in the corresponding table of heights
                //Debug.Log(HeightMap[x, p]);
                x++;
            }
            q++;
        }

        for (int coordy = 0; coordy < tData.heightmapHeight; coordy++)
        {
            for (int coordx = 0; coordx < tData.heightmapWidth; coordx++)
            {
                if (HeightMap[coordx, coordy] < 0.4f)
                {
                    HeightMap[coordx, coordy] = SurroundingAverage(coordx, coordy);
                }
            }
        }

        tData.SetHeights(0, 0, HeightMap); //put the generated heights into our terrain data

		//set the texture of the terrain
		SplatPrototype[] tex = new SplatPrototype[terrainTex.Length];
        
        for(int i = 0; i < terrainTex.Length; i++)
        {
            tex[i] = new SplatPrototype();
            tex[i].texture = terrainTex[i];
            tex[i].normalMap = terrainNormals[i];
            tex[i].tileSize = new Vector2(15, 15);
        }
		
		tData.splatPrototypes = tex;

		//create the terrain data object as an asset
		AssetDatabase.CreateAsset(tData, "Assets/testTerrain.asset");

		//create the terrain
		myTerrain = Terrain.CreateTerrainGameObject(tData);
		myTerrain.transform.position = new Vector3(-250f, 0f, -250f);
        myTerrain.AddComponent<AssignSplatMap>();
        myTerrain.GetComponent<Terrain>().materialType = Terrain.MaterialType.Custom;
        myTerrain.GetComponent<Terrain>().materialTemplate = terrainMat;

    }
	
	// Update is called once per frame
	void Update ()
	{
		//if the scale has changed, then destroy the previous terrain and generate a new one with the current scale.
		if(frequency != previousFrequency || amplitude != previousAmplitude)
		{
			Destroy(myTerrain);
			generateNewTerrain();
			previousFrequency = frequency;
			previousAmplitude = amplitude;
		}
			
	}

    float SurroundingAverage(int x, int y)
    {
        float surroudingsAverage = HeightMap[x,y];
        int divider = 1;
        if(x == 0)
        {
            surroudingsAverage += HeightMap[x + 1, y];
            divider += 1;
        }
        else if(x == (tData.heightmapWidth-1))
        {
            surroudingsAverage += HeightMap[x - 1, y];
            divider += 1;
        }
        else
        {
            surroudingsAverage += HeightMap[x + 1, y] + HeightMap[x - 1, y];
            divider += 2;
        }


        if(y == 0)
        {
            surroudingsAverage += HeightMap[x, y + 1];
            divider += 1;
        }
        else if(y == (tData.heightmapHeight - 1))
        {
            surroudingsAverage += HeightMap[x, y - 1];
            divider += 1;
        }
        else
        {
            surroudingsAverage += HeightMap[x, y + 1] + HeightMap[x, y - 1];
            divider += 2;
        }

        return (surroudingsAverage / divider);
    }
}
