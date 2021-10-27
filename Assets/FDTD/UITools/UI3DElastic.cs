using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;
using Assimp;
using UnityEngine.UI;
using g3;
using ElasticFDTD;


public class UI3DElastic : MonoBehaviour
{
    public GameObject meshObj;
    public GameObject FDTDRenderObj;
    MeshFilter filter;
    ElasticModel3D sim;
    public InputField meshSizeField;
    public ComputeShader FDTDShader;
    bool runSim = true;
    int padding = 5;
    ElasticFDTD.Material[] matArr = new ElasticFDTD.Material[] {
        ElasticMaterials.materials["Void"],
        ElasticMaterials.materials["steel"],
    };

    IEnumerator waitForFileLoad()
    {

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Files and Folders", "Load");
        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                Debug.Log("Loading file from: " + FileBrowser.Result[i]);
            string path = FileBrowser.Result[0];

            AssimpContext importer = new AssimpContext();
            Scene scene = importer.ImportFile(path);
            if (!scene.HasMeshes)
                yield break;

            Assimp.Mesh aMesh = scene.Meshes[0];

            List<Vector3> uVertices = new List<Vector3>();
            List<int> uIndices = new List<int>();

            // Vertices
            if (aMesh.HasVertices)
            {
                foreach (var v in aMesh.Vertices)
                {
                    uVertices.Add(new Vector3(-v.X, v.Y, v.Z));
                }
            }
            // Triangles
            if (aMesh.HasFaces)
            {
                foreach (var f in aMesh.Faces)
                {
                    // Ignore degenerate faces
                    if (f.IndexCount == 1 || f.IndexCount == 2)
                        continue;

                    for (int i = 0; i < (f.IndexCount - 2); i++)
                    {
                        uIndices.Add(f.Indices[i + 2]);
                        uIndices.Add(f.Indices[i + 1]);
                        uIndices.Add(f.Indices[0]);
                    }
                }
            }

            //Convert to Unity mesh
            UnityEngine.Mesh uMesh = new UnityEngine.Mesh();
            uMesh.vertices = uVertices.ToArray();
            uMesh.triangles = uIndices.ToArray();
            uMesh.RecalculateBounds();
            uMesh.RecalculateNormals();
            
            //Render in world-space, overlay with FDTD
            filter.mesh = uMesh;
            float divScale = 1/Mathf.Max(uMesh.bounds.max.x, uMesh.bounds.max.y, uMesh.bounds.max.z);
            meshObj.transform.localScale = new Vector3(divScale, divScale, divScale);
            meshObj.transform.position = -uMesh.bounds.center*divScale;

            //Find max entents, use for FDTD grid size calcs
            Vector3 relSideLengths = uMesh.bounds.extents * 2 * divScale;
            float sideLengthProduct = relSideLengths.x * relSideLengths.y * relSideLengths.z;
            FDTDRenderObj.transform.localScale = relSideLengths;

            //Scale the FDTD grid
            int maxElementCount = (int)Mathf.Pow(int.Parse(meshSizeField.text),3);
            float rendererDx = Mathf.Pow(maxElementCount / sideLengthProduct,1f/3f);
            Vector3 numElementsF = relSideLengths * rendererDx;
            Vector3Int numElements = new Vector3Int((int)numElementsF.x, (int)numElementsF.y, (int)numElementsF.z);
            Bitmap3 bmp = BitmapFromMesh.getBitmap(uMesh, (int)Mathf.Max(numElements.x, numElements.y, numElements.z));

            //Create the FDTD instance
            if (sim != null)
                sim.tryDispose();

            int[,,] matGrid = new int[numElements.x+2* padding, numElements.y+2* padding, numElements.z+2* padding];
            for (int x = 0; x < numElements.x; x++)
            {
                for (int y = 0; y < numElements.y; y++)
                {
                    for (int z = 0; z < numElements.z; z++)
                    {
                        if (bmp.Get(new Vector3i(x, y, z))) 
                            matGrid[x + padding, y + padding, z + padding] = 1;
                        else
                            matGrid[x + padding, y + padding, z + padding] = 0;
                    }
                }
            }
            List<Source3D> sources = new List<Source3D>();
            sources.Add(new Source3D(numElements.x/2, numElements.y/2, numElements.z/2, 10000));
            sim = new ElasticModel3D(sources, matGrid, 0.01f, matArr, FDTDShader);
        }
        yield break;
    }
    public void startFileBrowser()
    {
        StopAllCoroutines();
        StartCoroutine(waitForFileLoad());
    }
    // Start is called before the first frame update
    void Start()
    {
        filter = meshObj.GetComponent<MeshFilter>();
        FileBrowser.SetFilters(false, new FileBrowser.Filter("3D Models", ".stl", ".dae",".fbx",".obj",".blend"));
    }

    // Update is called once per frame
    void Update()
    {
        if (sim!=null && sim.asyncStepReady && runSim)
        {
            StopAllCoroutines();
            StartCoroutine(sim.asyncTimestep());
        }
    }

    private void OnDestroy()
    {
        if (sim != null)
            sim.tryDispose();
    }
    public void PausePlay(bool val)
    {
        runSim = val;
    }
}
