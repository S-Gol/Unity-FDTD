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
    //Gameobjects for rendering
    public GameObject meshObj;
    public GameObject FDTDRenderObj;
    MeshFilter filter;
    MeshCollider col;

    //Simulation class
    ElasticModel3D sim;
    //UI
    public InputField meshSizeField;
    public ComputeShader FDTDShader;
    //Is it running? 
    bool runSim = true;

    //Additional spacing on grid at edges
    const int padding = 10;

    //Signed distance field in/out of mesh
    //Used for source direction + bitmap
    MeshSignedDistanceGrid sdf;
    Bitmap3 bmp;

    //Default materials
    //TODO change to user selctions
    ElasticFDTD.Material[] matArr = new ElasticFDTD.Material[] {
        ElasticMaterials.materials["Void"],
        ElasticMaterials.materials["steel"],
    };
    //Sim properties
    List<Source3D> sources = new List<Source3D>();
    int[,,] matGrid;
    Vector3Int fieldSize;
    //raycast vars
    const int numSteps = 1024;
    const float stepSize = 1.732f / numSteps;
    Vector3 halfCube = new Vector3(0.5f, 0.5f, 0.5f);

    public bool raycastMatGrid(UnityEngine.Ray ray, out Vector3Int hitPos)
    {
        RaycastHit firstHit;
        if(Physics.Raycast(ray, out firstHit))
        {
            hitPos = new Vector3Int();
            if (firstHit.transform.name != "FDTDRenderObj")
                return false;

            Debug.DrawLine(ray.origin, firstHit.point, Color.red, 0.1f);

            Vector3 rayStartPos = firstHit.transform.InverseTransformPoint(firstHit.point) + halfCube;
            Vector3 transformDir = firstHit.transform.InverseTransformVector(ray.direction);
            for (uint iStep = 1; iStep < numSteps; iStep++)
            {
                float t = iStep * stepSize;
                Vector3 currPos = rayStartPos + transformDir * t;
#if UNITY_EDITOR
                Debug.DrawLine(firstHit.transform.TransformPoint(currPos- halfCube), firstHit.transform.TransformPoint(rayStartPos + transformDir * (t + stepSize)- halfCube), Color.green, 0.1f);
#endif
                // Stop when we are outside the box
                if (currPos.x < -0.0001f || currPos.x >= 1.0001f || currPos.y < -0.0001f || currPos.y > 1.0001f || currPos.z < -0.0001f || currPos.z > 1.0001f)
                    break;

                Vector3Int idx = new Vector3Int((int)(currPos.x * fieldSize.x), (int)(currPos.y * fieldSize.y), (int)(currPos.z * fieldSize.z));
                if (idx.x < fieldSize.x && idx.y < fieldSize.y && idx.z < fieldSize.z)
                {
                    bool intersect = matGrid[idx.x, idx.y, idx.z] != 0;
                    if (intersect)
                    {
                        hitPos = idx;
                        return true;
                    }
                }

            }
            return false;
        }
        else
        {
            hitPos = new Vector3Int();
            return false;
        }
    }
    public void startSourcePlacement()
    {
        if(matGrid == null)
            return;
        StartCoroutine(sourcePlacement());

    }
    IEnumerator sourcePlacement()
    {
        bool hasHit = false;
        while (!(hasHit && Input.GetMouseButtonDown(0)))
        {
            UnityEngine.Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3Int hitPos;
            hasHit = raycastMatGrid(ray, out hitPos);

            yield return null;
        }
        print("break");
        yield break;
    }
    //Async file explorer opening
    IEnumerator waitForFileLoad()
    {
        //Wait for the load dialog to succeed
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Files and Folders", "Load");
        Debug.Log(FileBrowser.Success);
        //Make sure we loaded a file
        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                Debug.Log("Loading file from: " + FileBrowser.Result[i]);
            string path = FileBrowser.Result[0];

            //Create an ASSIMP context to load the file
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
            col.sharedMesh = uMesh;
            float divScale = 1/Mathf.Max(uMesh.bounds.max.x, uMesh.bounds.max.y, uMesh.bounds.max.z);
            meshObj.transform.localScale = new Vector3(divScale, divScale, divScale);
            meshObj.transform.position = -uMesh.bounds.center*divScale;

            //Find max entents, use for FDTD grid size calcs
            Vector3 relSideLengths = uMesh.bounds.extents * 2 * divScale;
            float sideLengthProduct = relSideLengths.x * relSideLengths.y * relSideLengths.z;
            FDTDRenderObj.transform.localScale = relSideLengths*1.01f;

            //Scale the FDTD grid
            int maxElementCount = (int)Mathf.Pow(int.Parse(meshSizeField.text),3);
            float rendererDx = Mathf.Pow(maxElementCount / sideLengthProduct,1f/3f);
            Vector3 numElementsF = relSideLengths * rendererDx;
            Vector3Int numElements = new Vector3Int((int)numElementsF.x, (int)numElementsF.y, (int)numElementsF.z);

            sdf = BitmapUtils.SDFFromMesh(uMesh, (int)Mathf.Max(numElements.x, numElements.y, numElements.z));
            bmp = BitmapUtils.bmpFromSDF(sdf);

            //Create the FDTD instance
            if (sim != null)
                sim.tryDispose();

            fieldSize = new Vector3Int(numElements.x + 2 * padding, numElements.y + 2 * padding, numElements.z + 2 * padding);
            matGrid = new int[fieldSize.x, fieldSize.y, fieldSize.z];
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
            sources.Clear();
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
        col = meshObj.GetComponent<MeshCollider>();
        FileBrowser.SetFilters(false, new FileBrowser.Filter("3D Models", ".stl", ".dae",".fbx",".obj",".blend"));
    }
    public void initSim()
    {
        sources.Add(new Source3D(150,150,150, 10000, new Vector3(1,0,0)));
        sim = new ElasticModel3D(sources, matGrid, 0.01f, matArr, FDTDShader);

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
