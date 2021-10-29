using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class RenderManager : MonoBehaviour
{
    public Material[] materials;
    public Dropdown materialDropdown;
    public GameObject SimRenderTarget;
    MeshRenderer meshRenderer;
    int currentMat = 0;
    public GameObject[] panels;
    // Start is called before the first frame update
    void Start()
    {

        meshRenderer = SimRenderTarget.GetComponent<MeshRenderer>();
        ChangeRenderMode(currentMat);
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
    }
    public void setMipOpacityMult(float val)
    {
        materials[currentMat].SetFloat("_OpacityMult", val);
    }
}
