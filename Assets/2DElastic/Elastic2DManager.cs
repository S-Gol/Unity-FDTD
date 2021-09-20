using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElasticFDTD;
using System.Diagnostics;

public class Elastic2DManager : MonoBehaviour
{
    /// <summary>
    /// Class representing an elastic wave simulation scenario
    /// </summary>

    public ComputeShader FDTDShader;

    public bool restart;
    public class ElasticModel2D
    {
        #region var declarations
        public float dx, dy, ds, t;
        public int nx, ny, nx2, ny2;
        public int nt, ntDisplay;
        float[] rho, vp, vs, lam, mu;
        public int[] materialGrid;
        public ElasticFDTD.Material[] materials;
        public float maxVP, CFL, dt;
        List<Vector2Int> sourcePoints;
        List<float> sourceFreqs;
        Vector2[] sourceVals;

        int abs_thick;
        float abs_rate;

        //Pseudo-2D arrays
        public Vector2[] u1, u2, u3;
        public float[] weights;

        //Shader data
        ComputeShader FDTDShader;
        ComputeBuffer u1Buffer, u2Buffer, u3Buffer;
        ComputeBuffer weightBuffer;
        ComputeBuffer matData, matGrid;
        RenderTexture velTexture;
        ComputeBuffer sourceValBuffer, sourcePosBuffer;

        int differentialKernel;
        int patternKernel;
        int sourceKernel;
        int copyKernel;
        
        #endregion
        public int xyToIdx(int x, int y)
        {
            return (x + nx * y);
        }
        public int xyToIdx(int x, int y, int nx, int nz)
        {
            return (x + nx * y);
        }
        public Vector2Int idxToXY(int idx)
        {
            return new Vector2Int(idx % nx, idx / nx);
        }
        public ElasticModel2D(List<Source2D> sources, int[,] matGrid, float ds, ElasticFDTD.Material[] Mats, ComputeShader FDTDShader)
        {
            dx = ds;
            dy = ds;

            t = 0;
            nt = 0;
            materials = Mats;

            //Fill the grid if it does not exist
            nx = matGrid.GetLength(0);
            ny = matGrid.GetLength(1);
            nx2 = nx + 2;
            ny2 = ny + 2;

            materialGrid = new int[nx2 * ny2];
            for (int x = 0; x < nx; x++)
            {
                for (int y = 0; y < ny; y++)
                {
                    materialGrid[this.xyToIdx(x+1, y+1, nx2, ny2)] = matGrid[x, y];
                }
            }

            // Max primary wave speed dictates DT

            maxVP = 0;
            foreach (ElasticFDTD.Material mat in materials)
            {
                if (mat.vp > maxVP)
                    maxVP = mat.vp;
            }

            dt = 0.8f / (maxVP * Mathf.Sqrt(1.0f / Mathf.Pow(dx, 2) + 1.0f / Mathf.Pow(dy, 2)));
            CFL = maxVP * dt * Mathf.Sqrt(1.0f / Mathf.Pow(dx, 2) + 1.0f / Mathf.Pow(dy, 2));

            // Boundary - no reflections 
            abs_thick = Mathf.RoundToInt(Mathf.Min(Mathf.Floor(0.15f * nx), Mathf.Floor(0.15f * ny)));
            abs_rate = 0.3f / abs_thick;
            // Field setup 
            weights = new float[(nx + 2) * (ny + 2)];
           
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = 1;
            }
            
            for (int iy = 0; iy < ny + 2; iy++)
            {
                for (int ix = 0; ix < nx + 2; ix++)
                {
                    int i = 0;
                    int k = 0;

                    if (ix < abs_thick + 1)
                        i = abs_thick + 1 - ix;

                    if (iy < abs_thick + 1)
                        k = abs_thick + 1 - iy;

                    if (nx - abs_thick < ix)
                        i = ix - nx + abs_thick;

                    if (ny - abs_thick < iy)
                        k = iy - ny + abs_thick;

                    if (i != 0 && k != 0)
                    {
                        float rr = abs_rate * abs_rate * (i * i + k * k);
                        weights[this.xyToIdx(ix, iy, nx2, ny2)] = Mathf.Exp(-rr);
                    }

                }
            }
            //Array allocation
            u1 = new Vector2[(nx + 2) * (ny + 2)];
            u2 = new Vector2[(nx + 2) * (ny + 2)];
            u3 = new Vector2[(nx + 2) * (ny + 2)];

            sourcePoints = new List<Vector2Int>();
            sourceFreqs = new List<float>();

            //Assign sources
            for (int i = 0; i < sources.Count; i++)
            {
                sourcePoints.Add(sources[i].point);
                sourceFreqs.Add(sources[i].f);
            }
            sourceVals = new Vector2[sources.Count];

            this.FDTDShader = FDTDShader;
            shaderInit();
        }
       
        public void shaderInit()
        {
            UnityEngine.Debug.Log("Initializing 2D FDTD compute");
            differentialKernel = FDTDShader.FindKernel("Elastic2DDifferentials");
            patternKernel = FDTDShader.FindKernel("FillPatternedNoise");
            sourceKernel = FDTDShader.FindKernel("SetSourcePoint");
            copyKernel = FDTDShader.FindKernel("CopyBuffers");


            //Buffers
            u1Buffer = new ComputeBuffer((nx + 2) * (ny + 2), 8);
            u2Buffer = new ComputeBuffer((nx + 2) * (ny + 2), 8);
            u3Buffer = new ComputeBuffer((nx + 2) * (ny + 2), 8);
            weightBuffer = new ComputeBuffer((nx + 2) * (ny + 2), 4);
            weightBuffer.SetData(weights);

            //Result texture
            velTexture = new RenderTexture(nx + 2, ny + 2, 32, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            velTexture.enableRandomWrite = true;
            velTexture.Create();

            //Send material data to buffers
            matData = new ComputeBuffer(materials.Length, 4 * 5);
            matData.SetData(materials);

            matGrid = new ComputeBuffer(nx2 * ny2, 4);
            matGrid.SetData(materialGrid);

            //Source buffers
            sourcePosBuffer = new ComputeBuffer(sourcePoints.Count, 8);
            sourceValBuffer = new ComputeBuffer(sourcePoints.Count, 8);

            sourcePosBuffer.SetData(sourcePoints);

            Shader.SetGlobalBuffer("u1Buffer", u1Buffer);
            Shader.SetGlobalBuffer("u2Buffer", u2Buffer);
            Shader.SetGlobalBuffer("u3Buffer", u3Buffer);

            Shader.SetGlobalBuffer("weightBuffer", weightBuffer);
            Shader.SetGlobalBuffer("matGridBuffer", matGrid);
            Shader.SetGlobalBuffer("matDataBuffer", matData);


            Shader.SetGlobalTexture("velTexture", velTexture);
            
            Shader.SetGlobalFloat("co_dxx", 1 / (dx * dx));
            Shader.SetGlobalFloat("co_dyy", 1 / (dy * dy));
            Shader.SetGlobalFloat("co_dxy", 1 / (4f * dx * dy));

            Shader.SetGlobalFloat("dt", dt);


            Shader.SetGlobalInt("nx", nx);
            Shader.SetGlobalInt("nx2", nx2);
            Shader.SetGlobalInt("ny", ny);
            Shader.SetGlobalInt("ny2", ny2);

            Shader.SetGlobalBuffer("sourcePosBuffer", sourcePosBuffer);



        }
        public void timeStep()
        {
            t += dt;
            nt++;
            Shader.SetGlobalFloat("t", t);
            //todo add a iteration-time measurement

            //Dispatch the differential calcs
            FDTDShader.Dispatch(differentialKernel, nx / 8, ny / 8, 1);

            //Dispatch the source updates
            for (int i = 0; i < sourceVals.Length; i++)
            {
                //Gaussian pulse - change later to abstracted source functions
                float f0 = sourceFreqs[i];
                float t0 = 1f / f0;
                float tempV = Mathf.Exp(-((Mathf.Pow((2 * (t - 2 * t0) / (t0)), 2)))) * Mathf.Sin(2 * Mathf.PI * f0 * t);
                sourceVals[i]=new Vector2(tempV, 0);
            }

            sourceValBuffer.SetData(sourceVals);
            Shader.SetGlobalBuffer("sourceValBuffer", sourceValBuffer);
            FDTDShader.Dispatch(sourceKernel, 1, 1, 1);

            //Move the values to their next buffer
            FDTDShader.Dispatch(copyKernel, nx / 8, ny / 8, 1);

        }
    }

    ElasticModel2D model;

    // Start is called before the first frame update
    void Start()
    {
        ElasticFDTD.Material[] matArr = new ElasticFDTD.Material[2];
        matArr[0] = ElasticMaterials.materials["steel"];
        matArr[1] = ElasticMaterials.materials["steel"];

        int[,] matGrid = new int[400,400];

        for (int x = 0; x < 400; x++)
        {
            for (int y = 100; y < 130; y++)
            {
                matGrid[x, y] = 1;
            }
        }

        List<Source2D> sources = new List<Source2D>();

        sources.Add(new Source2D(200, 50, 10));


        model = new ElasticModel2D(sources, matGrid, 10f, matArr, FDTDShader);
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
        UnityEngine.Debug.Log("Average timestep, microseconds: " + nsTotal / (model.nt*1000));
    }
}
