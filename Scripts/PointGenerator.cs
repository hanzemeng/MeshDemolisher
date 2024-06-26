using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public enum PointDomainType
{
    SPHERE = 0,
    CUBE = 1,
    CYLINDER = 2,

    MESH = 100,
}

public class PointGenerator : MonoBehaviour
{
    [Header("This script generates points inside a defined domain.\n\nUncheck Generate On Change for performance,\nespecially if the domain type is MESH.")]

    public bool generateOnChange;

    public GameObject pointPrefab;
    public Transform pointsParent;
    [Range(0, 1000)] public int pointCount;
    [Range(0f, 0.1f)] public float pointScale;

    public PointDomainType pointDomainType;
    [Range(0f, 5f)] public float length0;
    [Range(0f, 5f)] public float length1;
    [Range(0f, 5f)] public float length2;
    public GameObject domainGameObject;

    private List<Transform> tempPoints;
    private bool dtIsClean;
    private DelaunayTetrahedralization dt;
    private Vector3 lowerBound;
    private Vector3 upperBound;

    public void OnEnable()
    {
        dt = new DelaunayTetrahedralization();
        dtIsClean = true;
        tempPoints = new List<Transform>();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();
        for(int i=0; i<pointCount; i++)
        {
            GenerateNewPoint(i);
        }
        dtIsClean = true;
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        GetCurrentPoints();
        foreach(Transform point in tempPoints)
        {
            DestroyImmediate(point.gameObject);
        }
    }

    private void GetCurrentPoints()
    {
        tempPoints.Clear();
        if(null == pointsParent)
        {
            return;
        }

        foreach(Transform child in pointsParent)
        {
            tempPoints.Add(child);
        }
    }

    public void OnPointPrefabChange(GameObject newPointPrefab)
    {
        GetCurrentPoints();

        foreach(Transform point in tempPoints)
        {
            Transform newPoint = Instantiate(newPointPrefab,pointsParent).transform;
            newPoint.localPosition = point.localPosition;
            newPoint.localScale = point.localScale;
        }
        foreach(Transform point in tempPoints)
        {
            DestroyImmediate(point.gameObject);
        }
    }
    public void OnPointsParentChange(Transform newPointsParent)
    {
        //GetCurrentPoints();

        //foreach(Transform point in tempPoints)
        //{
        //    point.SetParent(newPointsParent,false);
        //}
    }
    public void OnPointCountChange(int newPointCount)
    {
        if(!generateOnChange)
        {
            return;
        }
        if(newPointCount<0 || pointCount<0)
        {
            return;
        }

        GetCurrentPoints();

        for(int i=newPointCount; i<pointCount; i++)
        {
            if(i>=tempPoints.Count)
            {
                break;
            }
            DestroyImmediate(tempPoints[i].gameObject);
        }
        for(int i=pointCount; i<newPointCount; i++)
        {
            GenerateNewPoint(i);
        }
    }
    public void OnPointScaleChange(float newPointScale)
    {
        if(newPointScale < 0f)
        {
            return;
        }

        GetCurrentPoints();
        foreach(Transform point in tempPoints)
        {
            point.localScale = newPointScale * Vector3.one;
        }
    }
    public void OnGenerationSchemeChange()
    {
        if(!generateOnChange)
        {
            return;
        }

        Clear();
        for(int i=0; i<pointCount; i++)
        {
            GenerateNewPoint(i);
        }
    }

    private void GenerateNewPoint(int index)
    {
        Transform newPoint = Instantiate(pointPrefab,pointsParent).transform;
        newPoint.name = index.ToString();
        newPoint.localScale = pointScale * Vector3.one;

        switch(pointDomainType)
        {
            case PointDomainType.SPHERE:
                newPoint.localPosition = length0*Random.insideUnitSphere;
                break;
            case PointDomainType.CUBE:
                newPoint.localPosition = new Vector3(Random.Range(-length0,length0),Random.Range(-length1,length1),Random.Range(-length2,length2));
                break;
            case PointDomainType.CYLINDER:
                Vector2 v = length0*Random.insideUnitCircle;
                float h = Random.Range(-length1,length1);
                newPoint.localPosition = new Vector3(v.x,h,v.y);
                break;
            case PointDomainType.MESH:
                GenerateNewPointFromGameObject(newPoint);
                break;
        }
        newPoint.localPosition -= pointsParent.position;
    }

    private void GenerateNewPointFromGameObject(Transform np)
    {
        if(dtIsClean)
        {
            dtIsClean = false;

            List<Vector3> meshVertices = new List<Vector3>();
            List<int> meshTriangles = new List<int>();
            domainGameObject.GetComponent<MeshFilter>().sharedMesh.GetVertices(meshVertices);
            Transform domainTransform = domainGameObject.transform;
            meshVertices = meshVertices.Select(x=>domainTransform.TransformPoint(x)).ToList();
            meshTriangles = domainGameObject.GetComponent<MeshFilter>().sharedMesh.triangles.ToList();

            lowerBound = meshVertices[0];
            upperBound = meshVertices[0];
            for(int i=1; i<meshVertices.Count; i++)
            {
                float x = meshVertices[i].x;
                float y = meshVertices[i].y;
                float z = meshVertices[i].z;

                lowerBound = new Vector3(Mathf.Min(lowerBound.x,x),Mathf.Min(lowerBound.y,y),Mathf.Min(lowerBound.z,z));
                upperBound = new Vector3(Mathf.Max(upperBound.x,x),Mathf.Max(upperBound.y,y),Mathf.Max(upperBound.z,z));
            }

            dt.ConstrainedDelaunayTetrahedralize(meshVertices, meshTriangles);
        }


        List<IPointLocation> points = dt.points;
        List<int> tetrahedrons = dt.tetrahedrons;

        Vector3 newPoint = Vector3.zero;
        bool found = false;
        while(!found)
        {
            newPoint = new Vector3(Random.Range(lowerBound.x,upperBound.x),Random.Range(lowerBound.y,upperBound.y),Random.Range(lowerBound.z,upperBound.z));
            Point3D newPointP = new Point3D(newPoint);

            for(int i=0; i<tetrahedrons.Count; i+=4)
            {
                if(-1 == tetrahedrons[i])
                {
                    continue;
                }

                bool isInside = true;
                for(int j=0; j<4; j++)
                {
                    int i0 = DelaunayTetrahedralization.TETRAHEDRON_FACET[j,0];
                    int i1 = DelaunayTetrahedralization.TETRAHEDRON_FACET[j,1];
                    int i2 = DelaunayTetrahedralization.TETRAHEDRON_FACET[j,2];
                    if(Sign.POSITIVE != PointComputation.Orient(points[tetrahedrons[i+i0]],points[tetrahedrons[i+i1]],points[tetrahedrons[i+i2]],newPointP))
                    {
                        isInside = false;
                        break;
                    }
                }
                if(!isInside)
                {
                    continue;
                }
                found = true;
                break;
            }
        }
        
        np.localPosition = newPoint;
    }
}

}
