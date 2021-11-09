using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElasticFDTD;


public class Stats : MonoBehaviour
{
    public Text text;
    public Text currentTime;
    float avg;
    public ElasticModel3D sim;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Frame time, ms: \r\n" + (Time.smoothDeltaTime * 1000).ToString("#.");
        if(sim!= null)
        {
            currentTime.text = "Current sim time, us: \r\n" + (sim.t * 1e6).ToString("#.");
        }
        else
        {
            currentTime.text = "";
        }
    }
}
