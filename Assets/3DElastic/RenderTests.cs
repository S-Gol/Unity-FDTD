using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTests : MonoBehaviour
{
    float[] rayTestArr;

    int size = 200;
    int xSize, ySize, zSize;
    
    int to1d(int x, int y, int z)
    {
        return (z * xSize * ySize) + (y * xSize) + x;
    }
    Vector3Int to3d(int idx)
    {
        int z = idx / (xSize * ySize);
        idx -= (z * xSize* ySize);
        int y = idx / xSize;
        int x = idx % xSize;
        return new Vector3Int(x, y, z);
    }
    // Start is called before the first frame update
    void Start()
    {
        rayTestArr = new float[size*size*size];
        xSize = ySize = zSize = size;
        int halfSize = size / 2;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    rayTestArr[to1d(x, y, z)] = (float)x/size;
                }
            }
        }

        ComputeBuffer testBuffer = new ComputeBuffer(rayTestArr.Length, 4);
        testBuffer.SetData(rayTestArr);
        Shader.SetGlobalBuffer("testBuffer", testBuffer);
        Shader.SetGlobalVector("_Size", new Vector4(size, size, size, size));
        Shader.SetGlobalVector("_Scale", new Vector4(1, 1, 1, 0));

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
