# Unity-FDTD

A Unity, compute shader-based implementation of the Finite Difference Time Domain (FDTD) method for simulation of elastic waves in solids. The simulation allows multiple materials, multiple sources, and any arbitrary geometry - including realtime mesh importing. 


Since the simulation is run purely on the GPU, it can render the result in real-time. This is performed using two different volumetric rendering methods - the Direct Volume Method and the Maximum Intensity Projection. 

# Acknowledgments
* [Assimp](http://assimp.org/) - Used for runtime model loading
* [Unity Simple File Browser](https://github.com/yasirkula/UnitySimpleFileBrowser) - Runtime file browser
* [Unity Mesh Importer](https://github.com/eastskykang/UnityMeshImporter) - Examples of Assimp use in C# 
* [G3 Sharp](https://github.com/gradientspace/geometry3Sharp) - Used for signed distance fields, bitmaps, mesh voxelization
* [Outline Effect](https://forum.unity3d.com/threads/free-open-source-outline-image-effect.314362) - Rendering outline effects

# References

* Rendering
    * [Unity Volume Rendering - Matias Lavik](https://github.com/mlavik1/UnityVolumeRendering)
    * [Volumetric Rendering - Alan Zucconi](https://www.alanzucconi.com/2016/07/01/volumetric-rendering)
* FDTD Implementations
    * [WaveProp in Matlab](https://github.com/ovcharenkoo/WaveProp_in_MATLAB)