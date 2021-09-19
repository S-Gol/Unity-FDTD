using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElasticFDTD;

public static class ElasticMaterials
{
    public static readonly Dictionary<string, ElasticFDTD.Material> materials = new Dictionary<string, ElasticFDTD.Material>
    {
        {"steel", new ElasticFDTD.Material(5960,3235,8000)},
        {"Aluminum", new ElasticFDTD.Material(6420,3040,2700)},
        {"Berylium", new ElasticFDTD.Material(12890,8880,1870)},
        {"Brass", new ElasticFDTD.Material(4700,2110,8600)},
        {"Copper", new ElasticFDTD.Material(4760,2325,8930)},
        {"Gold", new ElasticFDTD.Material(3240,1200,19700)},
        {"Iron", new ElasticFDTD.Material(5960,3240,7850)},
        {"Lead", new ElasticFDTD.Material(2160,700,11400)},
        {"Molybdenum", new ElasticFDTD.Material(6250,3350,10100)},
        {"Nickel", new ElasticFDTD.Material(5480,2990,8850)},
        {"Platinum", new ElasticFDTD.Material(3260,1730,21400)},
        {"Silver", new ElasticFDTD.Material(3650,1610,10400)},
        {"Mild steel", new ElasticFDTD.Material(5960,3235,7850)},
        {"Stainless", new ElasticFDTD.Material(5790,3100,7900)},
        {"Tin", new ElasticFDTD.Material(3320,1670,7300)},
        {"Titanium", new ElasticFDTD.Material(6070,3125,4500)},
        {"Tungsten", new ElasticFDTD.Material(5220,2890,19300)},
        {"Tungsten Carbide", new ElasticFDTD.Material(6655,3980,13800)},
        {"Zinc", new ElasticFDTD.Material(4210,2440,7100)},
        {"Fused silica", new ElasticFDTD.Material(5968,3764,2200)},
        {"Pyrex", new ElasticFDTD.Material(5640,3280,2320)},
        {"Glass", new ElasticFDTD.Material(3980,2380,3880)},
        {"Lucite", new ElasticFDTD.Material(2680,1100,1180)},
        {"Nylon", new ElasticFDTD.Material(2620,1070,1110)},
        {"Polyethylene", new ElasticFDTD.Material(1950,540,900)},
        {"Polystyrene", new ElasticFDTD.Material(2350,1120,1060)},



    };

}
