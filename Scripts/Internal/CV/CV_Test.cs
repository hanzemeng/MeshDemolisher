using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public class CV_Test : MonoBehaviour
{
    [Range(0.01f, 1f)] public float cellScale;

    public GameObject targetGameObject;
    public Material materialInterior;
    public Material materialExterior;
    public Transform pointsParent;
    public Transform resultParent;

    private ClippedVoronoi cv = new ClippedVoronoi();

    [ContextMenu("Voronoi Test")]
    public void Voronoi()
    {
        UpdateGameObjects();

        {
            List<Vector3> voronoiPoints = Enumerable.Range(0, pointsParent.childCount).Select(i=>pointsParent.GetChild(i)).Select(x=>x.position).ToList();
            List<Vector3> meshVertices = new List<Vector3>();
            List<int> meshTriangles = new List<int>();
            targetGameObject.GetComponent<MeshFilter>().sharedMesh.GetVertices(meshVertices);
            meshTriangles = targetGameObject.GetComponent<MeshFilter>().sharedMesh.triangles.ToList();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            cv.CalculateClippedVoronoi(voronoiPoints, meshVertices, meshTriangles);
            watch.Stop();
            Debug.Log($"Voronoi calculated in {watch.ElapsedMilliseconds}ms.");
        }

        {
            var voronoiPoints = cv.voronoiPoints;
            var voronoiFaces = cv.voronoiFaces;
            var voronoiFacesCenters = cv.voronoiFacesCenters;
            var voronoiCells = cv.voronoiCells;

            int count = 0;
            foreach(var cell in voronoiCells)
            {
                GameObject g = new GameObject();
                g.name = count.ToString();
                count++;
                g.transform.parent = resultParent;
                MeshFilter meshFilter = g.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();

                Mesh mesh = new Mesh();
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();

                int index = 0;
                foreach(var face in cell)
                {
                    List<Point3D> points = voronoiFaces[face.Item1].Select(x=>voronoiPoints[x]).ToList();

                    int n = points.Count;
                    Point3D center = voronoiFacesCenters[face.Item1];
                    points.Add(center);

                    vertices.AddRange(points.Select(x=>x.ToVector3()).ToList());
                    if(face.Item2)
                    {
                        for(int j=0; j<n; j++)
                        {
                            triangles.Add(index+n);
                            triangles.Add(index+(j+1)%n);
                            triangles.Add(index+j);
                        }
                    }
                    else
                    {
                        for(int j=0; j<n; j++)
                        {
                            triangles.Add(index+n);
                            triangles.Add(index+j);
                            triangles.Add(index+(j+1)%n);
                        }
                    }
                    index += n+1;
                }

                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();

                Vector3 oldCenter = mesh.bounds.center;
                for(int i = 0; i<vertices.Count; i++)
                {
                    vertices[i] -= oldCenter;
                }
                mesh.vertices = vertices.ToArray();
                mesh.RecalculateBounds();
                g.transform.position = oldCenter-mesh.bounds.center;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                meshFilter.mesh = mesh;

                meshRenderer.material = materialInterior;
            }
        }
        
        OnValidate();
    }

    private void MapUv(List<IPointLocation> clipPoints, List<int> bound, Dictionary<int, List<(List<(int,Point3D)>,double)>> exteriorPointsMappings, List<Vector2> uv, Vector2[] meshUv)
    {
        Point3D boundNormal = Point3D.Cross(clipPoints[bound[1]].ToPoint3D()-clipPoints[bound[0]].ToPoint3D(),
                                            clipPoints[bound[2]].ToPoint3D()-clipPoints[bound[1]].ToPoint3D());

        boundNormal.Normalize();
        boundNormal = boundNormal*-1d;
        Vector2 centerPointUv = Vector2.zero;
        
        foreach(int boundPoint in bound)
        {
            Vector2 pointUv = Vector2.zero;
            List<(List<(int,Point3D)>,double)> originalPoints = exteriorPointsMappings[boundPoint];
            foreach((List<(int,Point3D)>,double) originalPoint in originalPoints)
            {
                int closestIndex = originalPoint.Item1[0].Item1;
                double closestDot = Point3D.Dot(originalPoint.Item1[0].Item2, boundNormal);
                for(int i=1; i<originalPoint.Item1.Count; i++)
                {
                    double dot = Point3D.Dot(originalPoint.Item1[i].Item2, boundNormal);
                    if(dot > closestDot)
                    {
                        closestDot = dot;
                        closestIndex = originalPoint.Item1[i].Item1;
                    }
                }
                pointUv += (float)originalPoint.Item2*meshUv[closestIndex];
            }
            uv.Add(pointUv);
            centerPointUv += pointUv;
        }
       
        centerPointUv /= bound.Count;
        uv.Add(centerPointUv);
    }


    [ContextMenu("Clip Test")]
    public void ClipVoronoi()
    {
        UpdateGameObjects();
        Mesh targetMesh = targetGameObject.GetComponent<MeshFilter>().sharedMesh;
        Transform targetTransform = targetGameObject.transform;

        {
            List<Vector3> voronoiPoints = Enumerable.Range(0, pointsParent.childCount).Select(i=>pointsParent.GetChild(i)).Select(x=>x.position).ToList();

            List<Vector3> meshVertices = new List<Vector3>();
            targetMesh.GetVertices(meshVertices);
            meshVertices = meshVertices.Select(x=>targetTransform.TransformPoint(x)).ToList();
            List<int> meshTriangles = new List<int>();
            meshTriangles = targetMesh.triangles.ToList();
            cv.CalculateClippedVoronoi(voronoiPoints, meshVertices, meshTriangles);
        }

        {
            Vector2[] meshUv = targetMesh.uv;
            Material meshMaterial = targetGameObject.GetComponent<MeshRenderer>().sharedMaterial;

            List<IPointLocation> clipPoints = cv.clipPoints;
            Dictionary<int, HashSet<List<int>>> clipVoronoiCellsExterior = cv.clipVoronoiCellsExterior;
            Dictionary<int, HashSet<List<int>>> clipVoronoiCellsInterior = cv.clipVoronoiCellsInterior;
            Dictionary<int, List<(List<(int,Point3D)>,double)>> exteriorPointsMappings = cv.exteriorPointsMappings;

            foreach(int cellIndex in clipVoronoiCellsExterior.Keys)
            {
                GameObject g = new GameObject();
                g.name = $"{cellIndex}";
                g.transform.parent = resultParent;
                MeshFilter meshFilter = g.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();
                Mesh mesh = new Mesh();
                List<Vector3> vertices = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<int> trianglesExterior = new List<int>();
                List<int> trianglesInterior = new List<int>();

                int index = 0;
                foreach(var bound in clipVoronoiCellsExterior[cellIndex])
                {
                    int n = bound.Count;
                    Point3D center = bound.Aggregate(new Point3D(0d,0d,0d), (sum,next)=>sum+clipPoints[next].ToPoint3D()) / n;
                    vertices.AddRange(bound.Select(x=>clipPoints[x].ToPoint3D().ToVector3()));
                    vertices.Add(center.ToVector3());
                    MapUv(clipPoints,bound,exteriorPointsMappings,uv,meshUv);
                    for(int i=0; i<n; i++)
                    {
                        trianglesExterior.Add(index+n);
                        trianglesExterior.Add(index+(i+1)%n);
                        trianglesExterior.Add(index+i);
                    }
                    index += n+1;
                }

                foreach(var bound in clipVoronoiCellsInterior[cellIndex])
                {
                    int n = bound.Count;
                    Point3D center = bound.Aggregate(new Point3D(0d,0d,0d), (sum,next)=>sum+clipPoints[next].ToPoint3D()) / n;
                    vertices.AddRange(bound.Select(x=>clipPoints[x].ToPoint3D().ToVector3()));
                    vertices.Add(center.ToVector3());
                    for(int i=0; i<n+1; i++)
                    {
                        uv.Add(Vector2.zero);
                    }
                    
                    for(int i=0; i<n; i++)
                    {
                        trianglesInterior.Add(index+n);
                        trianglesInterior.Add(index+(i+1)%n);
                        trianglesInterior.Add(index+i);
                    }
                    index += n+1;
                }


                mesh.vertices = vertices.ToArray();
                Vector3 oldCenter = mesh.bounds.center;
                for(int j=0; j<vertices.Count; j++)
                {
                    vertices[j] -= oldCenter;
                }
                mesh.vertices = vertices.ToArray();
                mesh.RecalculateBounds();
                g.transform.position = oldCenter-mesh.bounds.center;

                mesh.uv = uv.ToArray();
                mesh.subMeshCount = 2;
                mesh.SetTriangles(trianglesExterior, 0);
                mesh.SetTriangles(trianglesInterior, 1);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                meshFilter.mesh = mesh;
                meshRenderer.materials = new Material[] {meshMaterial, materialInterior};
            }    

            var voronoiPoints = cv.voronoiPoints;
            var voronoiFaces = cv.voronoiFaces;
            var voronoiFacesCenters = cv.voronoiFacesCenters;
            var voronoiCells = cv.voronoiCells;
            var interiorVoronoiCells = cv.interiorVoronoiCells;

            for(int i=0; i<interiorVoronoiCells.Count; i++)
            {
                List<(int,bool)> cell = voronoiCells[interiorVoronoiCells[i]];
                GameObject g = new GameObject();
                g.name = i.ToString();
                g.transform.parent = resultParent;
                MeshFilter meshFilter = g.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();

                Mesh mesh = new Mesh();
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();

                int index = 0;
                foreach(var face in cell)
                {
                    List<Point3D> points = voronoiFaces[face.Item1].Select(x=>voronoiPoints[x]).ToList();

                    int n = points.Count;
                    Point3D center = voronoiFacesCenters[face.Item1];
                    points.Add(center);

                    vertices.AddRange(points.Select(x=>x.ToVector3()).ToList());
                    if(face.Item2)
                    {
                        for(int j=0; j<n; j++)
                        {
                            triangles.Add(index+n);
                            triangles.Add(index+(j+1)%n);
                            triangles.Add(index+j);
                        }
                    }
                    else
                    {
                        for(int j=0; j<n; j++)
                        {
                            triangles.Add(index+n);
                            triangles.Add(index+j);
                            triangles.Add(index+(j+1)%n);
                        }
                    }
                    index += n+1;
                }

                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();

                Vector3 oldCenter = mesh.bounds.center;
                for(int j=0; j<vertices.Count; j++)
                {
                    vertices[j] -= oldCenter;
                }
                mesh.vertices = vertices.ToArray();
                mesh.RecalculateBounds();
                g.transform.position = oldCenter-mesh.bounds.center;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                meshFilter.mesh = mesh;

                meshRenderer.material = materialInterior;
            }
        }
    }


    [ContextMenu("Clear")]
    public void Clear()
    {
        //Enumerable.Range(0, pointsParent.childCount).Select(i=>pointsParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
        Enumerable.Range(0, resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
    }

    private void UpdateGameObjects()
    {
        Enumerable.Range(0, resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
    }

    public void OnValidate()
    {
        Enumerable.Range(0, resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>x.localScale=cellScale*Vector3.one);
    }
}

}
