using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public class CDT_Test : MonoBehaviour
{
    [Header("This script tests the Delaunay Tetrahedralization Class.\n\nThe Delaunay Test calculates a Delaunay Tetrahedralization of every point in Points Parent.\nThe Tetrahedralization Test tetrahedralize the Target Game Object.\n\nTo run the test, right click on the script.")]

    [Range(0.01f, 1f)] public float cellScale;

    public GameObject targetGameObject;

    public Material materialInternal;
    public Material materialExternal;
    public Transform pointsParent;
    public Transform tetrahedronsParent;

    private DelaunayTetrahedralization dt = new DelaunayTetrahedralization();

    public void OnValidate()
    {
        Enumerable.Range(0, tetrahedronsParent.childCount).Select(i=>tetrahedronsParent.GetChild(i)).ToList().ForEach(x=>x.localScale=cellScale*Vector3.one);
    }

    [ContextMenu("Delaunay Test")]
    public void Delaunay()
    {
        UpdateGameObjects();

        {
            List<Transform> points = Enumerable.Range(0, pointsParent.childCount).Select(i=>pointsParent.GetChild(i)).ToList();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            dt.DelaunayTetrahedralize(points.Select(x=>x.position).ToList());
            watch.Stop();
        

            string isDelaunay;
            if(dt.VerifyDelaunay())
            {
                isDelaunay = "Delaunay";
            }
            else
            {
                isDelaunay = "Not Delaunay";
            }
            Debug.Log($"Tetrahedralization calculated in {watch.ElapsedMilliseconds}ms. Is {isDelaunay}.");
        }

        {
            List<Point3D> points = dt.points.Select(x=>x.ToPoint3D()).ToList();
            List<int> tetrahedrons = dt.tetrahedrons;

            for(int t=0; t<tetrahedrons.Count; t+=4)
            {
                if(-1 == tetrahedrons[t])
                {
                    continue;
                }
                GameObject g = new GameObject();
                g.name = $"{tetrahedrons[t]}_{tetrahedrons[t+1]}_{tetrahedrons[t+2]}_{tetrahedrons[t+3]}";
                g.transform.parent = tetrahedronsParent;
                MeshFilter meshFilter = g.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();

                Mesh mesh = new Mesh();
                List<Vector3> vertices = new List<Vector3>();
                for(int i=0; i<4; i++)
                {
                    vertices.Add(points[tetrahedrons[t+i]].ToVector3());
                }
                List<int> triangles = new List<int>{2,1,0, 3,2,0, 3,1,2, 1,3,0};
                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();

                Vector3 oldCenter = mesh.bounds.center;
                for(int i=0; i<4; i++)
                {
                    vertices[i] -= oldCenter;
                }
                mesh.vertices = vertices.ToArray();
                mesh.RecalculateBounds();
                g.transform.position = oldCenter-mesh.bounds.center;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                meshFilter.mesh = mesh;

                meshRenderer.material = materialInternal;
            }

            Enumerable.Range(0, tetrahedronsParent.childCount).Select(i=>tetrahedronsParent.GetChild(i)).ToList().ForEach(x=>x.localScale=cellScale*Vector3.one);
        }
    }

    [ContextMenu("Tetrahedralization Test")]
    public void Tetrahedralization()
    {
        UpdateGameObjects();

        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();
        targetGameObject.GetComponent<MeshFilter>().sharedMesh.GetVertices(meshVertices);
        Transform targetTransform = targetGameObject.transform;
        meshVertices = meshVertices.Select(x=>targetTransform.TransformPoint(x)).ToList();
        meshTriangles = targetGameObject.GetComponent<MeshFilter>().sharedMesh.triangles.ToList();

        dt.ConstrainedDelaunayTetrahedralize(meshVertices, meshTriangles);
        List<IPointLocation> points = dt.points;
        List<int> tetrahedrons = dt.tetrahedrons;
        List<bool> neighborSeparation = dt.neighborSeparation;

        for(int t = 0; t<tetrahedrons.Count; t+=4)
        {
            if(-1 == tetrahedrons[t])
            {
                continue;
            }
            GameObject g = new GameObject();
            g.name = $"{tetrahedrons[t]}_{tetrahedrons[t+1]}_{tetrahedrons[t+2]}_{tetrahedrons[t+3]}";
            g.transform.parent = tetrahedronsParent;
            MeshFilter meshFilter = g.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            List<int> triangles = new List<int>{2,1,0, 3,2,0, 3,1,2, 1,3,0};
            List<Vector3> vertices = new List<Vector3>();
            Vector3 oldCenter = Vector3.zero;
            for(int i = 0; i<12; i++)
            {
                vertices.Add(points[tetrahedrons[t+triangles[i]]].ToPoint3D().ToVector3());
                oldCenter += vertices[^1];
                triangles[i] = i;
            }
            oldCenter /= 12f;
            g.transform.position = oldCenter;

            for(int i = 0; i<12; i++)
            {
                vertices[i] -= oldCenter;
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            meshFilter.mesh = mesh;
            if(neighborSeparation[t] || neighborSeparation[t+1] || neighborSeparation[t+2] || neighborSeparation[t+3])
            {
                meshRenderer.material = materialExternal;
            }
            else
            {
                meshRenderer.material = materialInternal;
            }
        }
        Enumerable.Range(0,tetrahedronsParent.childCount).Select(i => tetrahedronsParent.GetChild(i)).ToList().ForEach(x => x.localScale=cellScale*Vector3.one);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        //Enumerable.Range(0, pointsParent.childCount).Select(i=>pointsParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
        Enumerable.Range(0, tetrahedronsParent.childCount).Select(i=>tetrahedronsParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
    }

    private void UpdateGameObjects()
    {
        Enumerable.Range(0, tetrahedronsParent.childCount).Select(i=>tetrahedronsParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
    }
}

}
