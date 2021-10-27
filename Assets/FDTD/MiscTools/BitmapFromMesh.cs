using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;

public static class BitmapFromMesh 
{
    public static Bitmap3 getBitmap(Mesh mesh, int maxDim)
    {
        DMesh3 gMesh = new DMesh3();
        //Convert Unity mesh to Geometry3Sharp mesh
        foreach (Vector3 v in mesh.vertices)
        {
            gMesh.AppendVertex(new g3.Vector3d(v.x, v.y, v.z));
        }
        for (int n = 0; n < mesh.triangles.Length / 3; n++)
        {
            int t = n * 3;
            gMesh.AppendTriangle(mesh.triangles[t], mesh.triangles[t + 1], mesh.triangles[t + 2]);
        }

        gMesh.GetBounds();
        //Convert mesh to a signed distance field
        MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(gMesh, gMesh.CachedBounds.MaxDim / (maxDim));
        sdf.Compute();

        //Convert SDF to a bitmap 
        Bitmap3 bmp = new Bitmap3(sdf.Dimensions);

        foreach (Vector3i idx in bmp.Indices())
        {
            float f = sdf[idx.x, idx.y, idx.z];
            bmp.Set(idx, (f < 0) ? true : false);
        }
        Debug.Log("Computed SDF. Size: " + sdf.Dimensions);

        //Send the bitmap to the GPU.
        return bmp;

    }
}
