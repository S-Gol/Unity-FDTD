using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElasticFDTD;
using System.Diagnostics;

public class Elastic3DManager : MonoBehaviour
{


    public ComputeShader FDTDShader;

    public bool restart;
    public class ElasticModel3D
    {
        const int threadGroups = 512;
        #region var declarations
        public float dx, dy, dz, ds, t;
        public int nx, ny, nz, nx2, ny2, nz2;
        public int nt, ntDisplay;
        float[] rho, vp, vs, lam, mu;
        public int[] materialGrid;
        public ElasticFDTD.Material[] materials;
        public float maxVP, dt;
        List<Vector3Int> sourcePoints;
        List<float> sourceFreqs;
        Vector3[] sourceVals;

        int absThick;
        float absRate;

        //Pseudo-3D arrays
        public Vector3[] u1, u2, u3;
        public float[] weights;

        //Shader data
        ComputeShader FDTDShader;
        ComputeBuffer u1Buffer, u2Buffer, u3Buffer;
        ComputeBuffer weightBuffer;
        ComputeBuffer matData, matGrid;
        RenderTexture velTexture;
        ComputeBuffer sourceValBuffer, sourcePosBuffer;

        int differentialKernel;
        int sourceKernel;
        int copyKernel;

        #endregion
        int to1d(int x, int y, int z, int xSize, int ySize)
        {
            return (z * xSize * ySize) + (y * xSize) + x;
        }
        Vector3Int to3d(int idx, int xSize, int ySize, int zSize)
        {
            int z = idx / (xSize * ySize);
            idx -= (z * xSize * ySize);
            int y = idx / xSize;
            int x = idx % xSize;
            return new Vector3Int(x, y, z);
        }

        public ElasticModel3D(List<Source3D> sources, int[,,] matGrid, float ds, ElasticFDTD.Material[] Mats, ComputeShader FDTDShader)
        {
            dx = dy = dz = ds;

            t = 0;
            nt = 0;
            materials = Mats;

            //Fill the grid if it does not exist
            nx = matGrid.GetLength(0);
            ny = matGrid.GetLength(1);
            nz = matGrid.GetLength(2);

            nx2 = nx + 2;
            ny2 = ny + 2;
            nz2 = nz + 2;

            materialGrid = new int[nx2 * ny2 * nz2];
            for (int x = 0; x < nx; x++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        materialGrid[this.to1d(x, y, z, nx2, ny2)] = matGrid[x, y, z];
                    }
                }
            }

            // Max primary wave speed dictates DT

            maxVP = 0;
            foreach (ElasticFDTD.Material mat in materials)
            {
                if (mat.vp > maxVP)
                    maxVP = mat.vp;
            }

            dt = 0.8f / (maxVP * Mathf.Sqrt(1.0f / Mathf.Pow(dx, 2) + 1.0f / Mathf.Pow(dy, 2) + 1.0f / Mathf.Pow(dz,2)));

            // Boundary - no reflections 
            absThick = Mathf.RoundToInt(Mathf.Min(Mathf.Min(Mathf.Floor(0.15f * nx), Mathf.Floor(0.15f * ny)), 0.15f*nz));
            absRate = 0.3f / absThick;
            // Field setup 

            weights = new float[nx2 * ny2 * nz2];

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = 1;
            }

            for (int x = 0; x < nx2; x++)
            {
                for (int y = 0; y < ny2; y++)
                {
                    for (int z = 0; z < nz2; z++)
                    {
                        int i = 0;
                        int j = 0;
                        int k = 0;

                        if (x < absThick + 1)
                            i = absThick + 1 - x;
                        if (y < absThick + 1)
                            j = absThick + 1 - y;
                        if (z < absThick + 1)
                            k = absThick + 1 - z;

                        if (nx - absThick < x)
                            i = x - nx + absThick;
                        if (ny - absThick < y)
                            j = y - ny + absThick;
                        if (nz - absThick < z)
                            k = z - nz + absThick;

                        if (i != 0 && j != 0 && k != 0) 
                        {
                            float rr = absRate * absRate * (i * i + j * j + k * k);
                            weights[to1d(x,y,z,nx2,ny2)] = Mathf.Exp(-rr);
                        }
                    }
                }
            }

            //Array allocation
            u1 = new Vector3[nx2 * ny2 * nz2];
            u2 = new Vector3[nx2 * ny2 * nz2];
            u3 = new Vector3[nx2 * ny2 * nz2];

            sourcePoints = new List<Vector3Int>();
            sourceFreqs = new List<float>();

            //Assign sources
            for (int i = 0; i < sources.Count; i++)
            {
                sourcePoints.Add(sources[i].point);
                sourceFreqs.Add(sources[i].f);
            }
            sourceVals = new Vector3[sources.Count];

            this.FDTDShader = FDTDShader;
            shaderInit();
        }

        public void shaderInit()
        {
            UnityEngine.Debug.Log("Initializing 3D FDTD compute");

            differentialKernel = FDTDShader.FindKernel("Elastic3DDifferentials");
            sourceKernel = FDTDShader.FindKernel("SetSourcePoint");
            copyKernel = FDTDShader.FindKernel("CopyBuffers");


            //Buffers
            u1Buffer = new ComputeBuffer(nx2 * ny2 * nz2, 12);
            u2Buffer = new ComputeBuffer(nx2 * ny2 * nz2, 12);
            u3Buffer = new ComputeBuffer(nx2 * ny2 * nz2, 12);
            weightBuffer = new ComputeBuffer(nx2 * ny2 * nz2, 4);

            weightBuffer.SetData(weights);

            //Send material data to buffers
            matData = new ComputeBuffer(materials.Length, 4 * 5);
            matData.SetData(materials);

            matGrid = new ComputeBuffer(nx2 * ny2 * nz2, 4);
            matGrid.SetData(materialGrid);

            //Source buffers
            sourcePosBuffer = new ComputeBuffer(sourcePoints.Count, 12);
            sourceValBuffer = new ComputeBuffer(sourcePoints.Count, 12);

            sourcePosBuffer.SetData(sourcePoints);

            Shader.SetGlobalBuffer("u1Buffer", u1Buffer);
            Shader.SetGlobalBuffer("u2Buffer", u2Buffer);
            Shader.SetGlobalBuffer("u3Buffer", u3Buffer);

            Shader.SetGlobalBuffer("weightBuffer", weightBuffer);
            Shader.SetGlobalBuffer("matGridBuffer", matGrid);
            Shader.SetGlobalBuffer("matDataBuffer", matData);


            Shader.SetGlobalTexture("velTexture", velTexture);

            Shader.SetGlobalFloat("co_dx", 1f / (2f * dx));
            Shader.SetGlobalFloat("co_dy", 1f / (2f * dy));
            Shader.SetGlobalFloat("co_dz", 1f / (2f * dz));

            Shader.SetGlobalFloat("dt", dt);

            Shader.SetGlobalInt("nx", nx);
            Shader.SetGlobalInt("nx2", nx2);
            Shader.SetGlobalInt("ny", ny);
            Shader.SetGlobalInt("ny2", ny2);
            Shader.SetGlobalInt("nz", nz);
            Shader.SetGlobalInt("nz2", nz2);

            Shader.SetGlobalVector("size", new Vector4(nx2,ny2,nz2,0));


            Shader.SetGlobalBuffer("sourcePosBuffer", sourcePosBuffer);

        }
        public void timeStep()
        {
            t += dt;
            nt++;
            Shader.SetGlobalFloat("t", t);
            int numThreadGroups = Mathf.CeilToInt((float)nx * (float)ny * (float)nz / threadGroups);
            //Dispatch the differential calcs
            FDTDShader.Dispatch(differentialKernel, numThreadGroups, 1, 1);
            
            //Dispatch the source updates
            for (int i = 0; i < sourceVals.Length; i++)
            {
                //Gaussian pulse - change later to abstracted source functions
                float f0 = sourceFreqs[i];
                float t0 = 1f / f0;
                float tempV = Mathf.Exp(-((Mathf.Pow((2 * (t - 2 * t0) / (t0)), 2)))) * Mathf.Sin(2 * Mathf.PI * f0 * t);
                sourceVals[i] = new Vector3(0, -tempV, 0);
            }

            sourceValBuffer.SetData(sourceVals);
            Shader.SetGlobalBuffer("sourceValBuffer", sourceValBuffer);
            FDTDShader.Dispatch(sourceKernel, 1, 1, 1);
            
            //Move the values to their next buffer
            FDTDShader.Dispatch(copyKernel, numThreadGroups, 1, 1);
        }
    }

    ElasticModel3D model;

    // Start is called before the first frame update
    void Start()
    {
        ElasticFDTD.Material[] matArr = new ElasticFDTD.Material[2];
        matArr[0] = ElasticMaterials.materials["steel"];
        matArr[1] = ElasticMaterials.materials["Nylon"];

        int[,,] matGrid = new int[300, 300, 300];
        
        for (int x = 0; x < 300; x++)
        {
            for (int z = 0; z < 300; z++)
            {
                for (int y = 200; y < 250; y++)
                {
                    matGrid[x, y, z] = 1;
                }
            }
        }

        List<Source3D> sources = new List<Source3D>();

        sources.Add(new Source3D(150, 100, 150, 10000));


        model = new ElasticModel3D(sources, matGrid, 0.01f, matArr, FDTDShader);
        nsTotal = 0;
    }

    Stopwatch stopWatch = new Stopwatch();
    double nsTotal;
    // Update is called once per frame
    void Update()
    {

        if (restart)
        {
            restart = false;
            Start();
        }
        stopWatch.Reset();
        stopWatch.Start();

        model.timeStep();

        stopWatch.Stop();

        double nanosecExTime = ((double)stopWatch.ElapsedTicks / Stopwatch.Frequency) * 1e9;
        nsTotal += nanosecExTime;
        UnityEngine.Debug.Log("Average timestep, microseconds: " + nsTotal / (model.nt * 1000));
    }
}
