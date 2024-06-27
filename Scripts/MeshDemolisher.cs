using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hanzzz.MeshDemolisher
{

public class MeshDemolisher
{
    private static VertexAttribute[] VERTEX_TEXTURE_ATTRIBUTES = new VertexAttribute[]{VertexAttribute.TexCoord0, VertexAttribute.TexCoord1,VertexAttribute.TexCoord2,VertexAttribute.TexCoord3,VertexAttribute.TexCoord4,VertexAttribute.TexCoord5,VertexAttribute.TexCoord6,VertexAttribute.TexCoord7};

    private ClippedVoronoi cv;


    public MeshDemolisher()
    {
        cv = new ClippedVoronoi();
    }

    // need a way to check if part of the modle is flat.
    // need to check if the modle forms more than one closed volume.
    // need to check if the uv layout is that every face is in one chunk.
    public bool VerifyDemolishInput(GameObject targetGameObject, List<Transform> demolishPoints)
    {
        List<Vector3> voronoiPoints = demolishPoints.Select(x=>x.position).ToList();
        if(!DelaunayTetrahedralization.VerifyDelaunayTetrahedralizeInput(voronoiPoints))
        {
            return false;
        }

        Transform targetTransform = targetGameObject.transform;
        if(targetTransform.localScale.x < 0f || targetTransform.localScale.y < 0f || targetTransform.localScale.z < 0f)
        {
            Debug.LogWarning("Target object has negative scale.");
            return false;
        }

        Mesh targetMesh = targetGameObject.GetComponent<MeshFilter>().sharedMesh;
        if(targetMesh.subMeshCount>1)
        {
            Debug.LogWarning("Target object has more than one submesh.");
            return false;
        }

        List<Vector3> meshVertices = new List<Vector3>();
        targetMesh.GetVertices(meshVertices);
        meshVertices = meshVertices.Select(x=>targetTransform.TransformPoint(x)).ToList();
        List<int> meshTriangles = new List<int>();
        meshTriangles = targetMesh.triangles.ToList();
        if(!DelaunayTetrahedralization.VerifyConstrainedDelaunayTetrahedralizeInput(meshVertices, meshTriangles))
        {
            return false;
        }

        return true;
    }

    public List<GameObject> Demolish(GameObject targetGameObject, List<Transform> demolishPoints, Material interiorMaterial)
    {
        Mesh targetMesh = targetGameObject.GetComponent<MeshFilter>().sharedMesh;
        
        List<Vector3> breakPoints = demolishPoints.Select(x=>x.position).ToList();
        List<Vector3> meshVertices = targetMesh.vertices.ToList();
        Transform targetTransform = targetGameObject.transform;
        meshVertices = meshVertices.Select(x=>targetTransform.TransformPoint(x)).ToList();
        List<int> meshTriangles = new List<int>();
        meshTriangles = targetMesh.triangles.ToList();
        cv.CalculateClippedVoronoi(breakPoints, meshVertices, meshTriangles);

        Material targetMeshMaterial = targetGameObject.GetComponent<MeshRenderer>().sharedMaterial;
        return ConstructGameObjects(targetMesh, targetMeshMaterial, interiorMaterial);
    }
    public async Task<List<GameObject>> DemolishAsync(GameObject targetGameObject, List<Transform> demolishPoints, Material interiorMaterial)
    {
        Mesh targetMesh = targetGameObject.GetComponent<MeshFilter>().sharedMesh;

        List<Vector3> breakPoints = demolishPoints.Select(x=>x.position).ToList();
        List<Vector3> meshVertices = targetMesh.vertices.ToList();
        Transform targetTransform = targetGameObject.transform;
        meshVertices = meshVertices.Select(x=>targetTransform.TransformPoint(x)).ToList();
        List<int> meshTriangles = new List<int>();
        meshTriangles = targetMesh.triangles.ToList();
        await Task.Run(()=>cv.CalculateClippedVoronoi(breakPoints, meshVertices, meshTriangles));

        Material targetMeshMaterial = targetGameObject.GetComponent<MeshRenderer>().sharedMaterial;
        return ConstructGameObjects(targetMesh, targetMeshMaterial, interiorMaterial);
    }

    private List<GameObject> ConstructGameObjects(Mesh targetMesh, Material targetMeshMaterial, Material interiorMaterial)
    {
        List<GameObject> res = new List<GameObject>();
        Dictionary<VertexAttribute, List<FloatStruct>> originalVerticesAttributes = GetOriginalVerticesAttributes(targetMesh);

        List<IPointLocation> clipPoints = cv.clipPoints;
        Dictionary<int, HashSet<List<int>>> clipVoronoiCellsExterior = cv.clipVoronoiCellsExterior;
        Dictionary<int, HashSet<List<int>>> clipVoronoiCellsInterior = cv.clipVoronoiCellsInterior;
        Dictionary<int, List<(List<(int,Point3D)>,double)>> exteriorPointsMappings = cv.exteriorPointsMappings;

        foreach(int cellIndex in clipVoronoiCellsExterior.Keys)
        {
            GameObject g = new GameObject();
            g.name = $"{cellIndex}";
            MeshFilter meshFilter = g.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = g.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            Dictionary<VertexAttribute, List<FloatStruct>> newVerticesAttributes = CreateEmptyVerticesAttributes(originalVerticesAttributes);
            List<int> trianglesExterior = new List<int>();
            List<int> trianglesInterior = new List<int>();

            int index = 0;
            foreach(var bound in clipVoronoiCellsExterior[cellIndex])
            {
                int n = bound.Count;
                Point3D center = bound.Aggregate(new Point3D(0d,0d,0d), (sum,next)=>sum+clipPoints[next].ToPoint3D()) / n;
                vertices.AddRange(bound.Select(x=>clipPoints[x].ToPoint3D().ToVector3()));
                vertices.Add(center.ToVector3());
                InterpolateOriginalVerticesAttributes(clipPoints,bound,exteriorPointsMappings,newVerticesAttributes, originalVerticesAttributes);

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
                AddDefaultVerticesAttributes(bound, newVerticesAttributes);
                    
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

            for(int j=0; j<VERTEX_TEXTURE_ATTRIBUTES.Length; j++)
            {
                if(!originalVerticesAttributes.ContainsKey(VERTEX_TEXTURE_ATTRIBUTES[j]))
                {
                    continue;
                }

                int dimension = originalVerticesAttributes[VERTEX_TEXTURE_ATTRIBUTES[j]][0].dimension;

                switch(dimension)
                {
                    case 2:
                    {
                        List<Vector2> temp = newVerticesAttributes[VERTEX_TEXTURE_ATTRIBUTES[j]].Select(x=>x.ToVector2()).ToList();
                        mesh.SetUVs(j,temp);
                        break;
                    }
                    case 3:
                    {
                        List<Vector3> temp = newVerticesAttributes[VERTEX_TEXTURE_ATTRIBUTES[j]].Select(x=>x.ToVector3()).ToList();
                        mesh.SetUVs(j,temp);
                        break;
                    }
                    case 4:
                    {
                        List<Vector4> temp = newVerticesAttributes[VERTEX_TEXTURE_ATTRIBUTES[j]].Select(x=>x.ToVector4()).ToList();
                        mesh.SetUVs(j,temp);
                        break;
                    }
                }
            }
            if(originalVerticesAttributes.ContainsKey(VertexAttribute.Color))
            {
                List<Color> temp = newVerticesAttributes[VertexAttribute.Color].Select(x=>x.ToColor()).ToList();
                mesh.SetColors(temp);
            }


            mesh.subMeshCount = 2;
            mesh.SetTriangles(trianglesExterior, 0);
            mesh.SetTriangles(trianglesInterior, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            meshFilter.mesh = mesh;
            meshRenderer.materials = new Material[] {targetMeshMaterial, interiorMaterial};

            res.Add(g);
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

            meshRenderer.material = interiorMaterial;
            res.Add(g);
        }
        
        return res;
    }

    private Dictionary<VertexAttribute, List<FloatStruct>> GetOriginalVerticesAttributes(Mesh targetMesh)
    {
        Dictionary<VertexAttribute, List<FloatStruct>> originalVerticesAttributes = new Dictionary<VertexAttribute, List<FloatStruct>>();

        for(int i=0; i<VERTEX_TEXTURE_ATTRIBUTES.Length; i++)
        {
            if(!targetMesh.HasVertexAttribute(VERTEX_TEXTURE_ATTRIBUTES[i]))
            {
                continue;
            }

            int vertexAttributeDimension = targetMesh.GetVertexAttributeDimension(VERTEX_TEXTURE_ATTRIBUTES[i]);

            List<FloatStruct> data = null;

            switch(vertexAttributeDimension)
            {
                case 2:
                {
                    List<Vector2> temp = new List<Vector2>();
                    targetMesh.GetUVs(i, temp);
                    data = temp.Select(x=>new FloatStruct(x)).ToList();
                    break;
                }
                    
                case 3:
                {
                    List<Vector3> temp = new List<Vector3>();
                    targetMesh.GetUVs(i, temp);
                    data = temp.Select(x=>new FloatStruct(x)).ToList();
                    break;
                }
                    
                case 4:
                {
                    List<Vector4> temp = new List<Vector4>();
                    targetMesh.GetUVs(i, temp);
                    data = temp.Select(x=>new FloatStruct(x)).ToList();
                    break;
                }
                    
            }
            originalVerticesAttributes[VERTEX_TEXTURE_ATTRIBUTES[i]] = data;
        }

        if(targetMesh.HasVertexAttribute(VertexAttribute.Color))
        {
            List<Color> temp = new List<Color>();
            targetMesh.GetColors(temp);
            originalVerticesAttributes[VertexAttribute.Color] = temp.Select(x=>new FloatStruct(x)).ToList();
        }

        return originalVerticesAttributes;
    }

    private Dictionary<VertexAttribute, List<FloatStruct>> CreateEmptyVerticesAttributes(Dictionary<VertexAttribute, List<FloatStruct>> originalData)
    {
        Dictionary<VertexAttribute, List<FloatStruct>> res = new Dictionary<VertexAttribute, List<FloatStruct>>();
        foreach(VertexAttribute vertexAttribute in originalData.Keys)
        {
            res[vertexAttribute] = new List<FloatStruct>();
        }
        return res;
    }


    private void InterpolateOriginalVerticesAttributes(List<IPointLocation> clipPoints, List<int> bound, Dictionary<int, List<(List<(int,Point3D)>,double)>> exteriorPointsMappings, Dictionary<VertexAttribute, List<FloatStruct>> interpolateData, Dictionary<VertexAttribute, List<FloatStruct>> originalData)
    {
        Point3D boundNormal = Point3D.Cross(clipPoints[bound[1]].ToPoint3D()-clipPoints[bound[0]].ToPoint3D(),
                                            clipPoints[bound[2]].ToPoint3D()-clipPoints[bound[1]].ToPoint3D());
        boundNormal.Normalize();
        boundNormal = boundNormal*-1d;

        Dictionary<VertexAttribute, FloatStruct> initialValue = new Dictionary<VertexAttribute, FloatStruct>();
        foreach(var kvp in originalData)
        {
            initialValue[kvp.Key] = kvp.Value[0].DefaultValue();
        }

        Dictionary<VertexAttribute, FloatStruct> centerValue = new Dictionary<VertexAttribute, FloatStruct>(initialValue);
        
        foreach(int boundPoint in bound)
        {
            Dictionary<VertexAttribute, FloatStruct> currentValue = new Dictionary<VertexAttribute, FloatStruct>(initialValue);

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

                foreach(var kvp in originalData)
                {
                    currentValue[kvp.Key] += (float)originalPoint.Item2 * originalData[kvp.Key][closestIndex];
                }
            }

            foreach(var kvp in originalData)
            {
                interpolateData[kvp.Key].Add(currentValue[kvp.Key]);
                centerValue[kvp.Key] += currentValue[kvp.Key];
            }
        }

        float n = (float)bound.Count;
        foreach(var kvp in originalData)
        {
            interpolateData[kvp.Key].Add(centerValue[kvp.Key] / n);
        }
    }

    private void AddDefaultVerticesAttributes(List<int> bound, Dictionary<VertexAttribute, List<FloatStruct>> interpolateData)
    {
        int n = bound.Count + 1;
        foreach(VertexAttribute vertexAttribute in interpolateData.Keys)
        {
            for(int i=0; i<n; i++)
            {
                interpolateData[vertexAttribute].Add(interpolateData[vertexAttribute][0].DefaultValue());
            }
        }
    }
}

}
