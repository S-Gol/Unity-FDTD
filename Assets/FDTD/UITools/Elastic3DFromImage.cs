using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElasticFDTD;

public class Elastic3DFromImage : MonoBehaviour
{
    public ComputeShader FDTDShader;
    public bool restart;
    public Texture2D image;
    public int maxCells;

    ElasticModel3D model;
    // Start is called before the first frame update
    void Start()
    {

        ElasticFDTD.Material[] matArr = new ElasticFDTD.Material[2];
        
        matArr[0] = ElasticMaterials.materials["steel"];
        matArr[1] = ElasticMaterials.materials["Void"];

        int sizeX = image.width;
        int sizeY = image.height;
        int sizeZ = Mathf.FloorToInt((float)maxCells / (sizeX * sizeY));
        print("Intializing from image - size " + sizeX + ", " + sizeY + ", " + sizeZ);

        int[,,] matGrid = new int[sizeX, sizeY, sizeZ];


        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Color col = image.GetPixel(x, y);
                    if (col.r + col.g + col.b > 0.9f)
                    {
                        matGrid[x, y, z] = 1;
                    }
                }
            }
        }



        List<Source3D> sources = new List<Source3D>();

        sources.Add(new Source3D(200, 390, 200, 10000));


        model = new ElasticModel3D(sources, matGrid, 0.01f, matArr, FDTDShader);
    }

    // Update is called once per frame
    void Update()
    {

        if (restart)
        {
            restart = false;
            Start();
        }
        if (model.asyncStepReady)
        {
            StopAllCoroutines();
            StartCoroutine(model.asyncTimestep());
        }
    }
}
