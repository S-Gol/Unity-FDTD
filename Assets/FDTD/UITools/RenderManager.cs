using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class RenderManager : MonoBehaviour
{
    public Material[] materials;
    public Dropdown materialDropdown;
    public GameObject SimRenderTarget;
    public Text colorMin;
    public Text colorMax;
    MeshRenderer meshRenderer;
    int currentMat = 0;
    public GameObject[] panels;
    // Start is called before the first frame update
    void Start()
    {
        Shader.SetGlobalFloat("scale_factor", ElasticModel3D.scaleFactor);

        meshRenderer = SimRenderTarget.GetComponent<MeshRenderer>();
        ChangeRenderMode(currentMat);
        setMipVelMult(1);
    }

    // Update is called once per frame
    public void ChangeRenderMode(int id)
    {
        meshRenderer.material = materials[id];
        currentMat = id;
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == id);
        }
    }
    public void setMipMin(float val)
    {
        materials[currentMat].SetFloat("_MinVal", val);
    }
    public void setMipMax(float val)
    {
        materials[currentMat].SetFloat("_MaxVal", val);
    }
    public void setMipVelMult(float val)
    {
        materials[currentMat].SetFloat("_VelMult", val);
        colorMax.text = string.Format("{0:E2}", ElasticModel3D.scaleFactor / val) + " Pa";
    }
    public void setMipOpacityMult(float val)
    {
        materials[currentMat].SetFloat("_OpacityMult", val);
    }
}
