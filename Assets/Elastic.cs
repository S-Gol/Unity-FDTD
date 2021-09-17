using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElasticFDTD
{
    public struct Source2D
    {
        public int x, z;
        public float f;
        public Source2D(int X, int Z, float F)
        {
            x = X;
            z = Z;
            f = F;
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
