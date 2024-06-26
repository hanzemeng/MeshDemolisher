//#define VERBOSE
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public partial class DelaunayTetrahedralization
{
    public DelaunayTetrahedralization()
    {
        PointComputation.Init();
    }

    public void DelaunayTetrahedralize(List<Vector3> inputPoints)
    {
        SortedSet<Vector3> uniqueVertices = new SortedSet<Vector3>(new Vector3Comparator());
        inputPoints.ForEach(x=>uniqueVertices.Add(x));
        inputPoints = uniqueVertices.ToList();

        Reset();
        inputPoints.
            Select(x=>CreateNewPoint(x))
            .ToList()
            .ForEach(x=>AddNewPoint(x));
    }

    public void ConstrainedDelaunayTetrahedralize(List<Vector3> inputPoints, List<int> inputTriangles)
    {
        StringBuilder stringBuilder = new StringBuilder();
        this.inputPoints = inputPoints;
        this.inputTriangles = inputTriangles;
        stringBuilder.Append($"Ver: {inputPoints.Count}, Tri: {inputTriangles.Count/3}\n");

        Reset();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        RemoveDuplicatePoints();
        InsertInputPoints();
        watch.Stop();
        stringBuilder.Append($"Insert Point: {watch.ElapsedMilliseconds}ms\n");
        watch.Reset();

        watch.Start();
        CalculateEdgeInformation();
        SegmentRecovery();
        watch.Stop();
        stringBuilder.Append($"Segment Recovery: {watch.ElapsedMilliseconds}ms\n");
        watch.Reset();

        watch.Start();
        CalculateFaceInformation();
        FaceRecovery();
        watch.Stop();
        stringBuilder.Append($"Face Recovery: {watch.ElapsedMilliseconds}ms\n");
        watch.Reset();

        watch.Start();
        FilterOuterTetrahedron();
        watch.Stop();
        stringBuilder.Append($"Filter: {watch.ElapsedMilliseconds}ms");
        watch.Reset();
        #if VERBOSE
        Debug.Log(stringBuilder.ToString());
        #endif
    }

    public static bool VerifyDelaunayTetrahedralizeInput(List<Vector3> inputPoints)
    {
        SortedSet<Vector3> uniqueVertices = new SortedSet<Vector3>(new Vector3Comparator());
        inputPoints.ForEach(x=>uniqueVertices.Add(x));
        inputPoints = uniqueVertices.ToList();

        Point3D[] boundPoints = new Point3D[4]
        {
            new Point3D(new Vector3(-RANGE,-RANGE,-RANGE)),
            new Point3D(new Vector3(0,-RANGE, RANGE)),
            new Point3D(new Vector3(RANGE,-RANGE,-RANGE)),
            new Point3D(new Vector3(0,RANGE,0))
        };
        foreach(Vector3 point in inputPoints)
        {
            Point3D p = new Point3D(point);
            for(int i=0; i<4; i++)
            {
                if(Sign.POSITIVE != PointComputation.Orient(boundPoints[TETRAHEDRON_FACET[i,0]],boundPoints[TETRAHEDRON_FACET[i,1]],boundPoints[TETRAHEDRON_FACET[i,2]],p))
                {
                    Debug.LogWarning("A input point is too far from the world space origin.");
                    return false;
                }
            }
        }

        return true;
    }

    // need a way to check if part of the modle is flat
    public static bool VerifyConstrainedDelaunayTetrahedralizeInput(List<Vector3> inputPoints, List<int> inputTriangles)
    {
        {
            SortedDictionary<Vector3, int> uniqueVertices = new SortedDictionary<Vector3, int>(new Vector3Comparator());
            int uniqueVerticesIndex = 0;
            for(int i=0; i<inputTriangles.Count; i++)
            {
                if(!uniqueVertices.ContainsKey(inputPoints[inputTriangles[i]]))
                {
                    uniqueVertices[inputPoints[inputTriangles[i]]] = uniqueVerticesIndex;
                    uniqueVerticesIndex++;
                }
                inputTriangles[i] = uniqueVertices[inputPoints[inputTriangles[i]]];
            }

            inputPoints = new List<Vector3>(new Vector3[uniqueVerticesIndex]);
            foreach(var kvp in uniqueVertices)
            {
                inputPoints[kvp.Value] = kvp.Key;
            }
        }

        List<Point3D> points = new List<Point3D>();
        {
            Point3D[] boundPoints = new Point3D[4]
            {
                new Point3D(new Vector3(-RANGE,-RANGE,-RANGE)),
                new Point3D(new Vector3(0,-RANGE, RANGE)),
                new Point3D(new Vector3(RANGE,-RANGE,-RANGE)),
                new Point3D(new Vector3(0,RANGE,0))
            };
            foreach(Vector3 point in inputPoints)
            {
                Point3D p = new Point3D(point);
                points.Add(p);
                for(int i=0; i<4; i++)
                {
                    if(Sign.POSITIVE != PointComputation.Orient(boundPoints[TETRAHEDRON_FACET[i,0]],boundPoints[TETRAHEDRON_FACET[i,1]],boundPoints[TETRAHEDRON_FACET[i,2]],p))
                    {
                        Debug.LogWarning("A input point is too far from the world space origin.");
                        return false;
                    }
                }
            }
        }

        {
            Dictionary<(int,int), List<int>> edges = new Dictionary<(int, int), List<int>>();
            for(int i=0; i<inputTriangles.Count; i+=3)
            {
                for(int j=0; j<3; j++)
                {
                    (int,int) edge;
                    if(inputTriangles[i+j] < inputTriangles[i+((j+1)%3)])
                    {
                        edge = (inputTriangles[i+j], inputTriangles[i+((j+1)%3)]);
                    }
                    else
                    {
                        edge = (inputTriangles[i+((j+1)%3)], inputTriangles[i+j]);
                    }
                    if(!edges.ContainsKey(edge))
                    {
                        edges[edge] = new List<int>();
                    }
                    edges[edge].Add(i/3);
                }
            }
            foreach(var kvp in edges)
            {
                if(2 != kvp.Value.Count)
                {
                    Debug.LogWarning("Input triangles do not enclose a volume.");
                    return false;
                }
            }
        }

        {
            for(int i=0; i<inputTriangles.Count; i+=3)
            {
                int ti0 = inputTriangles[i+0];
                int ti1 = inputTriangles[i+1];
                int ti2 = inputTriangles[i+2];

                for(int j=i+3; j<inputTriangles.Count; j+=3)
                {
                    int tj0 = inputTriangles[j+0];
                    int tj1 = inputTriangles[j+1];
                    int tj2 = inputTriangles[j+2];
                    if(
                        Sign.POSITIVE == PointComputation.InnerSegmentCrossInnerTriangle(points[ti0],points[ti1],points[tj0],points[tj1],points[tj2]) ||
                        Sign.POSITIVE == PointComputation.InnerSegmentCrossInnerTriangle(points[ti1],points[ti2],points[tj0],points[tj1],points[tj2]) ||
                        Sign.POSITIVE == PointComputation.InnerSegmentCrossInnerTriangle(points[ti2],points[ti0],points[tj0],points[tj1],points[tj2])
                        )
                    {
                        Debug.LogWarning("Input triangles intersect.");
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public bool VerifyDelaunay()
    {
        for(int i=0; i<tetrahedrons.Count; i+=4)
        {
            if(-1 == tetrahedrons[i])
            {
                continue;
            }

            if(Sign.ZERO == Orient(i/4))
            {
                Debug.Log($"{tetrahedrons[i]}_{tetrahedrons[i+1]}_{tetrahedrons[i+2]}_{tetrahedrons[i+3]} is flat.");
                return false;
            }

            for(int j=0; j<points.Count; j++)
            {
                if(Sign.POSITIVE == InCircumsphereSymbolicPerturbation(i/4, j))
                {
                    Debug.Log($"{tetrahedrons[i]}_{tetrahedrons[i+1]}_{tetrahedrons[i+2]}_{tetrahedrons[i+3]} contains {j}; {points[j].ToPoint3D()}.");
                    return false;
                }
            }
        }
        return true;
    }
}

}
