using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public class TextToMesh : MonoBehaviour
{
    [Header("This script creates a mesh from a text file.\nTo create the mesh, right click on the script and click To Mesh.\n\nShould only be used for testing the Delaunay Tetrahedralization Class.")]

    [SerializeField] private TextAsset inputMeshFile;
    [SerializeField] private MeshFilter outputMeshFilter;


    /*
    Expected inputMeshFile format:
    <number of vertices>
    <number of triangles>
    <x coordinate of vertex 0> <y coordinate of vertex 0> <z coordinate of vertex 0>
    ...
    3 <a vertex that defines the triangle> <a vertex that defines the triangle> <a vertex that defines the triangle>
    ...
    */


    [ContextMenu("To Mesh")]
    public void ToMesh()
    {
        char[] delimiters = new char[]{' ', '\n'};
        string[] meshParameters = inputMeshFile.text.Split(delimiters);

        int pointCount = int.Parse(meshParameters[0]);
        int triCount = int.Parse(meshParameters[1]);
        int index = 2;

        List<Vector3> points = new List<Vector3>();
        for(int i=0; i<pointCount; i++)
        {
            points.Add(new Vector3(float.Parse(meshParameters[index]),float.Parse(meshParameters[index+1]),float.Parse(meshParameters[index+2])));
            index += 3;
        }

        List<int> tri = new List<int>();
        for(int i = 0; i<triCount; i++)
        {
            tri.Add(int.Parse(meshParameters[index+1]));
            tri.Add(int.Parse(meshParameters[index+2]));
            tri.Add(int.Parse(meshParameters[index+3]));
            index += 4;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = points.ToArray();
        mesh.triangles = tri.ToArray();

        outputMeshFilter.mesh = mesh;
    }
}

}
