using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElasticFDTD;

public class Elastic2DManager : MonoBehaviour
{
    /// <summary>
    /// Class representing an elastic wave simulation scenario
    /// </summary>

    public ComputeShader FDTDShader;

    public class ElasticModel2D
    {
        #region var declarations
        public float dx, dz, ds, t;
        public int nx, nz, nx2, nz2;
        public int nt, ntDisplay;
        float[] rho, vp, vs, lam, mu;
        public int[] materialGrid;
        public ElasticFDTD.Material[] materials;
        public float maxVP, CFL, dt;
        public List<Source2D> sources = new List<Source2D>();
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

        int differentialKernel;
        int patternKernel;

        #endregion
        public int xzToIdx(int x, int z)
        {
            return (x + nx * z);
        }
        public int xzToIdx(int x, int z, int nx, int nz)
        {
            return (x + nx * z);
        }
        public Vector2Int idxToxz(int idx)
        {
            return new Vector2Int(idx % nx, idx / nx);
        }
        public ElasticModel2D(List<Source2D> sources, int[,] matGrid, float ds, ElasticFDTD.Material[] Mats, ComputeShader FDTDShader)
        {
            dx = dz = ds;
            t = 0;
            nt = 0;
            materials = Mats;

            //Fill the grid if it does not exist
            nx = matGrid.GetLength(0);
            nz = matGrid.GetLength(1);
            nx2 = nx + 2;
            nz2 = nz + 2;

            materialGrid = new int[nx2 * nz2];
            for (int x = 0; x < nx; x++)
            {
                for (int z = 0; z < nz; z++)
                {
                    materialGrid[this.xzToIdx(x+1, z+1, nx2, nz2)] = matGrid[x, z];
                }
            }

            // Max primary wave speed dictates DT

            maxVP = 0;
            foreach (ElasticFDTD.Material mat in materials)
            {
                if (mat.vp > maxVP)
                    maxVP = mat.vp;
            }

            dt = 0.8f / (maxVP * Mathf.Sqrt(1.0f / Mathf.Pow(dx, 2) + 1.0f / Mathf.Pow(dz, 2)));
            CFL = maxVP * dt * Mathf.Sqrt(1.0f / Mathf.Pow(dx, 2) + 1.0f / Mathf.Pow(dz, 2));
            this.sources = sources;

            // Boundary - no reflections 
            abs_thick = Mathf.RoundToInt(Mathf.Min(Mathf.Floor(0.15f * nx), Mathf.Floor(0.15f * nz)));
            abs_rate = 0.3f / abs_thick;

            // Field setup 
            weights = new float[(nx + 2) * (nz + 2)];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = 1;
            }
            for (int iz = 0; iz < nz + 2; iz++)
            {
                for (int ix = 0; ix < nx + 2; ix++)
                {
                    int i = 0;
                    int k = 0;
                    if (ix < abs_thick + 1)
                        i = abs_thick + 1 - ix;

                    if (iz < abs_thick + 1)
                        k = abs_thick + 1 - iz;


                    if (nx - abs_thick < ix)
                        i = ix - nx + abs_thick;

                    if (nz - abs_thick < iz)
                        k = iz - nz + abs_thick;

                    if (i != 0 && k != 0)
                    {
                        float rr = abs_rate * abs_rate * (i * i + k * k);
                        weights[this.xzToIdx(ix, iz, nx + 2, nz + 2)] = Mathf.Exp(-rr);
                    }
                }
            }
            //Array allocation
            //// ALLOCATE MEMORY FOR WAVEFIELD
            u1 = new Vector2[(nx + 2) * (nz + 2)];
            u2 = new Vector2[(nx + 2) * (nz + 2)];
            u3 = new Vector2[(nx + 2) * (nz + 2)];

            this.FDTDShader = FDTDShader;
            shaderInit();
        }
       
        public void shaderInit()
        {
            Debug.Log("Initializing 2D FDTD compute");
            differentialKernel = FDTDShader.FindKernel("Elastic2DDifferentials");
            patternKernel = FDTDShader.FindKernel("FillPatternedNoise");

            //Buffers
            u1Buffer = new ComputeBuffer((nx + 2) * (nz + 2), 8);
            u2Buffer = new ComputeBuffer((nx + 2) * (nz + 2), 8);
            u3Buffer = new ComputeBuffer((nx + 2) * (nz + 2), 8);
            weightBuffer = new ComputeBuffer((nx + 2) * (nz + 2), 4);
            weightBuffer.SetData(weights);

            //Result texture
            velTexture = new RenderTexture(nx + 2, nz + 2, 32, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            velTexture.enableRandomWrite = true;
            velTexture.Create();

            //Send material data to buffers
            matData = new ComputeBuffer(materials.Length, 4 * 5);
            matData.SetData(materials);

            matGrid = new ComputeBuffer(nx2 * nz2, 4);
            matGrid.SetData(materialGrid);


            Shader.SetGlobalBuffer("u1Buffer", u1Buffer);
            Shader.SetGlobalBuffer("u2Buffer", u2Buffer);
            Shader.SetGlobalBuffer("u3Buffer", u3Buffer);

            Shader.SetGlobalBuffer("weightBuffer", weightBuffer);
            Shader.SetGlobalBuffer("matGridBuffer", matGrid);
            Shader.SetGlobalBuffer("matDataBuffer", matData);


            Shader.SetGlobalTexture("velTexture", velTexture);
            
            Shader.SetGlobalFloat("co_dxx", 1 / (dx * dx));
            Shader.SetGlobalFloat("co_dzz", 1 / (dz * dz));
            Shader.SetGlobalFloat("co_dxz", 1 / (4*dx * dz));

            Shader.SetGlobalFloat("dt", dt);


            Shader.SetGlobalInt("nx", nx);
            Shader.SetGlobalInt("nx2", nx2);
            Shader.SetGlobalInt("nz", nz);
            Shader.SetGlobalInt("nz2", nz2);

            FDTDShader.Dispatch(patternKernel, nx2 / 8, nz2 / 8, 1);

        }
        public void timeStep()
        {
            t += dt;
            nt++;
            Shader.SetGlobalFloat("t", t);

            //todo add a iteration-time measurement
            FDTDShader.Dispatch(differentialKernel, nx2 / 8, nz2 / 8, 1);
        }
    }

    ElasticModel2D model;

    // Start is called before the first frame update
    void Start()
    {
        ElasticFDTD.Material[] matArr = new ElasticFDTD.Material[1];
        matArr[0] = ElasticMaterials.materials["steel"];
        List<Source2D> sources = new List<Source2D>();
        sources.Add(new Source2D(200, 200, 100000));

        model = new ElasticModel2D(sources, new int[406, 406],10, matArr, FDTDShader);
    }

    // Update is called once per frame
    void Update()
    {
        model.timeStep();

    }
}
