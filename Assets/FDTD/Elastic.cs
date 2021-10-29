    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElasticFDTD
{
    public struct Source2D
    {
        public Vector2Int point;
        public float f;
        public Source2D(int X, int Z, float F)
        {
            point = new Vector2Int(X, Z);
            f = F;
        }
        public Source2D(Vector2Int pt, float F)
        {
            point = pt;
            f = F;
        }
    }
    public struct Source3D
    {
        public Vector3Int point;
        public Vector3 normal;
        public float f;
        public Source3D(int X, int Y, int Z, float F, Vector3 dir)
        {
            point = new Vector3Int(X, Y, Z);
            f = F;
            normal = dir;
        }
        public Source3D(Vector3Int pt, float F, Vector3 dir)
        {
            point = pt;
            f = F;
            normal = dir;
        }
    }
    public struct Material
    {
        public float vp, vs, rho, lam, mu;
        public Material(float vp, float vs, float rho)
        {
            this.vp = vp;
            this.vs = vs;
            this.rho = rho;
            this.lam = rho * (Mathf.Pow(vp, 2) - 2 * Mathf.Pow(vs, 2));
            this.mu = rho * Mathf.Pow(vs, 2);
        }
    }
}
