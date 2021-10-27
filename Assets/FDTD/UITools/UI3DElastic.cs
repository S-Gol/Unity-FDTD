using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;
using Assimp;

public class UI3DElastic : MonoBehaviour
{
    public GameObject meshObj;
    MeshFilter filter;
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
            List<Vector3> uNormals = new List<Vector3>();
            List<Vector2> uUv = new List<Vector2>();
            List<int> uIndices = new List<int>();

            // Vertices
            if (aMesh.HasVertices)
            {
                foreach (var v in aMesh.Vertices)
                {
                    uVertices.Add(new Vector3(-v.X, v.Y, v.Z));
                }
            }

            // Normals
            if (aMesh.HasNormals)
            {
                foreach (var n in aMesh.Normals)
                {
                    uNormals.Add(new Vector3(-n.X, n.Y, n.Z));
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

            // Uv (texture coordinate) 
            if (aMesh.HasTextureCoords(0))
            {
                foreach (var uv in aMesh.TextureCoordinateChannels[0])
                {
                    uUv.Add(new Vector2(uv.X, uv.Y));
                }
            }

            UnityEngine.Mesh uMesh = new UnityEngine.Mesh();
            uMesh.vertices = uVertices.ToArray();
            uMesh.normals = uNormals.ToArray();
            uMesh.triangles = uIndices.ToArray();
            uMesh.uv = uUv.ToArray();
            uMesh.RecalculateBounds();
            uMesh.RecalculateNormals();
                
            filter.mesh = uMesh;
            float divScale = 1/Mathf.Max(uMesh.bounds.max.x, uMesh.bounds.max.y, uMesh.bounds.max.z);
            meshObj.transform.localScale = new Vector3(divScale, divScale, divScale);
            meshObj.transform.position = -uMesh.bounds.center;
            


        }

    }
    public void startFileBrowser()
    {
        StartCoroutine(waitForFileLoad());
    }
    // Start is called before the first frame update
    void Start()
    {
        filter = meshObj.GetComponent<MeshFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
