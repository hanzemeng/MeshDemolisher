//#define CHECK_DELAUNARY_AFTER_INSERT
//#define CHECK_FLAT_TETRAHEDRON_AFTER_FLIP
//#define CHECK_INSERT_SAME_POINT

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public partial class DelaunayTetrahedralization
{
    private void RemoveDuplicatePoints()
    {
        SortedDictionary<Vector3, int> uniqueVertices = new SortedDictionary<Vector3, int>(new Vector3Comparator());
        int uniqueVerticesIndex = 0;
        Point3D currentNormal = new Point3D(0d,0d,0d);

        for(int i=0; i<inputTriangles.Count; i++)
        {
            if(0 == i%3)
            {
                Point3D a = new Point3D(inputPoints[inputTriangles[i+0]]);
                Point3D b = new Point3D(inputPoints[inputTriangles[i+1]]);
                Point3D c = new Point3D(inputPoints[inputTriangles[i+2]]);
                currentNormal = Point3D.Cross(b-a,c-b);
                currentNormal.Normalize();
            }
            if(!uniqueVertices.ContainsKey(inputPoints[inputTriangles[i]]))
            {
                uniqueVertices[inputPoints[inputTriangles[i]]] = uniqueVerticesIndex;
                originalPointsMappings[uniqueVerticesIndex+4] = new List<(int, Point3D)>(); // 4 corners are inserted first
                uniqueVerticesIndex++;
            }
            originalPointsMappings[uniqueVertices[inputPoints[inputTriangles[i]]]+4].Add((inputTriangles[i], currentNormal));
            inputTriangles[i] = uniqueVertices[inputPoints[inputTriangles[i]]]+4;
        }

        inputPoints = new List<Vector3>(new Vector3[uniqueVerticesIndex]);
        foreach(var kvp in uniqueVertices)
        {
            inputPoints[kvp.Value] = kvp.Key;
        }
    }

    private void InsertInputPoints()
    {
        inputPoints.
            Select(x => CreateNewPoint(x))
            .ToList()
            .ForEach(x=>AddNewPoint(x));

        //Debug.Log(VerifyDelaunay());
    }

    private void AddNewPoint(int p)
    {
        #if CHECK_INSERT_SAME_POINT
        for(int i=0; i<points.Count; i++)
        {
            if(i == p || (points[i] is Point3D && points[p] is Point3DImplicit) || (points[i] is Point3DImplicit && points[p] is Point3D))
            {
                continue;
            }
            if(points[i] is Point3D)
            {
                if(points[p] is Point3DImplicit)
                {
                    continue;
                }
                if(((Point3D)points[i]).Equals((Point3D)points[p]))
                {
                    Debug.LogWarning($"{p} is same as {i}");
                    return;
                }
            }
            if(points[i] is Point3DImplicit)
            {
                if(points[p] is Point3D)
                {
                    continue;
                }
                if(((Point3DImplicit)points[i]).Equals((Point3DImplicit)points[p]))
                {
                    Debug.LogWarning($"{p} is same as {i}");
                    return;
                }
            }
            
        }
        #endif

        HashSet<int> s = new HashSet<int>();

        {
            (int, Sign,Sign,Sign,Sign) search = FindTetrahedronContainPoint(p);
            bool onEdge = false;
            bool onFacet = false;
            int current = search.Item1;
            Int4 currentPoints = GetTetrahedronPoints(current);

            if(Sign.ZERO == search.Item2)
            {
                if(Sign.ZERO == search.Item3)
                {
                    SetBaseAndPivot(current, currentPoints[0],currentPoints[2]);
                    onEdge = true;
                }
                else if(Sign.ZERO == search.Item4)
                {
                    SetBaseAndPivot(current, currentPoints[1],currentPoints[2]);
                    onEdge = true;
                }
                else if(Sign.ZERO == search.Item5)
                {
                    SetBaseAndPivot(current, currentPoints[0],currentPoints[1]);
                    onEdge = true;
                }
                else
                {
                    //SetBase(current, currentPoints[0],currentPoints[1],currentPoints[2]);
                    onFacet = true;
                }
            }
            else if(Sign.ZERO == search.Item3)
            {
                if(Sign.ZERO == search.Item4)
                {
                    SetBaseAndPivot(current, currentPoints[2],currentPoints[3]);
                    onEdge = true;
                }
                else if(Sign.ZERO == search.Item5)
                {
                    //SetBaseAndPivot(current, currentPoints[0],currentPoints[3]);
                    onEdge = true;
                }
                else
                {
                    SetBase(current, currentPoints[0],currentPoints[2],currentPoints[3]);
                    onFacet = true;
                }
            }
            else if(Sign.ZERO == search.Item4)
            {
                if(Sign.ZERO == search.Item5)
                {
                    SetBaseAndPivot(current, currentPoints[1],currentPoints[3]);
                    onEdge = true;
                }
                else
                {
                    SetBase(current, currentPoints[2],currentPoints[1],currentPoints[3]);
                    onFacet = true;
                }
            }
            else if(Sign.ZERO == search.Item5)
            {
                SetBase(current, currentPoints[0],currentPoints[3],currentPoints[1]);
                onFacet = true;
            }

            if(onEdge)
            {
                //2->2n flip

                List<int> t0s = new List<int>();
                List<int> t1s = new List<int>();

                Int4 currentNeighbors = GetTetrahedronNeighbors(current);
                int last = currentNeighbors[1];
                while(true)
                {
                    currentPoints = GetTetrahedronPoints(current);
                    currentNeighbors = GetTetrahedronNeighbors(current);
                    RemoveTetrahedron(current);
                    int t0 = CreateNewTetrahedron(currentPoints[0], currentPoints[1],currentPoints[2],p);
                    int t1 = CreateNewTetrahedron(currentPoints[3], currentPoints[2],currentPoints[1],p);

                    incidentTetrahedrons[currentPoints[0]] = t0;
                    incidentTetrahedrons[currentPoints[1]] = t0;
                    incidentTetrahedrons[currentPoints[2]] = t0;
                    incidentTetrahedrons[currentPoints[3]] = t1;
                    incidentTetrahedrons[p] = t0;

                    SetTetrahedronNeighbors(t0, 0, currentNeighbors[0]);
                    SetTetrahedronNeighbors(t1, 0, currentNeighbors[2]);
                    if(-1 != currentNeighbors[0])
                    {
                        SetBase(currentNeighbors[0], currentPoints[0], currentPoints[1], currentPoints[2]);
                        SetTetrahedronNeighbors(currentNeighbors[0], 0, t0);
                    }
                    if(-1 != currentNeighbors[2])
                    {
                        SetBase(currentNeighbors[2], currentPoints[3], currentPoints[2], currentPoints[1]);
                        SetTetrahedronNeighbors(currentNeighbors[2], 0, t1);
                    }
                    t0s.Add(t0);
                    t1s.Add(t1);

                    if(current == last)
                    {
                        break;
                    }
                    int next = currentNeighbors[3];
                    SetBaseAndPivot(next, currentPoints[0],currentPoints[3]);
                    current = next;
                }
                int n = t0s.Count;
                for(int i=0; i<t0s.Count; i++)
                {
                    SetTetrahedronNeighbors(t0s[i], 2, t1s[i]);
                    SetTetrahedronNeighbors(t1s[i], 2, t0s[i]);

                    SetTetrahedronNeighbors(t0s[i], 3, t0s[(i+1)%n]);
                    SetTetrahedronNeighbors(t0s[(i+1)%n], 1, t0s[i]);

                    SetTetrahedronNeighbors(t1s[i], 1, t1s[(i+1)%n]);
                    SetTetrahedronNeighbors(t1s[(i+1)%n], 3, t1s[i]);

                    s.Add(t0s[i]);
                    s.Add(t1s[i]);

                    #if CHECK_FLAT_TETRAHEDRON_AFTER_FLIP
                    if(Sign.ZERO == Orient(t0s[i]) || Sign.ZERO == Orient(t1s[i]))
                    {
                        Debug.LogWarning($"{p}, n2n flat");
                        Debug.Log(points[currentPoints[0]]);
                        Debug.Log(points[currentPoints[3]]);
                        Debug.Log(points[p]);
                        return;
                    }
                    #endif
                }
                
            }
            else if(onFacet)
            {
                //2->6 flip
                
                currentPoints = GetTetrahedronPoints(current);
                Int4 currentNeighbors = GetTetrahedronNeighbors(current);
                int neighbor0 = currentNeighbors[0];
                SetBase(neighbor0, currentPoints[0],currentPoints[1],currentPoints[2]);
                Int4 neighbor0Points = GetTetrahedronPoints(neighbor0);
                Int4 neighbor0Neighbors = GetTetrahedronNeighbors(neighbor0);
                RemoveTetrahedron(current);
                RemoveTetrahedron(neighbor0);

                int t0 = CreateNewTetrahedron(currentPoints[0], currentPoints[3],currentPoints[1],p);
                int t1 = CreateNewTetrahedron(currentPoints[1], currentPoints[3],currentPoints[2],p);
                int t2 = CreateNewTetrahedron(currentPoints[2], currentPoints[3],currentPoints[0],p);
                int t3 = CreateNewTetrahedron(currentPoints[0], currentPoints[1],neighbor0Points[3],p);
                int t4 = CreateNewTetrahedron(currentPoints[1], currentPoints[2],neighbor0Points[3],p);
                int t5 = CreateNewTetrahedron(currentPoints[2], currentPoints[0],neighbor0Points[3],p);

                incidentTetrahedrons[currentPoints[0]] = t0;
                incidentTetrahedrons[currentPoints[1]] = t0;
                incidentTetrahedrons[currentPoints[2]] = t1;
                incidentTetrahedrons[currentPoints[3]] = t0;
                incidentTetrahedrons[neighbor0Points[3]] = t3;
                incidentTetrahedrons[p] = t0;

                SetTetrahedronNeighbors(t0, currentNeighbors[3], t3, t1, t2);
                SetTetrahedronNeighbors(t1, currentNeighbors[2], t4, t2, t0);
                SetTetrahedronNeighbors(t2, currentNeighbors[1], t5, t0, t1);
                SetTetrahedronNeighbors(t3, neighbor0Neighbors[1], t5, t4, t0);
                SetTetrahedronNeighbors(t4, neighbor0Neighbors[2], t3, t5, t1);
                SetTetrahedronNeighbors(t5, neighbor0Neighbors[3], t4, t3, t2);
                //{0,1,2}, {0,2,3}, {2,1,3}, {0,3,1}
                if(-1 != currentNeighbors[3])
                {
                    SetBase(currentNeighbors[3], currentPoints[0], currentPoints[3], currentPoints[1]);
                    SetTetrahedronNeighbors(currentNeighbors[3], 0, t0);
                }
                if(-1 != currentNeighbors[2])
                {
                    SetBase(currentNeighbors[2], currentPoints[1], currentPoints[3], currentPoints[2]);
                    SetTetrahedronNeighbors(currentNeighbors[2], 0, t1);
                }
                if(-1 != currentNeighbors[1])
                {
                    SetBase(currentNeighbors[1], currentPoints[2], currentPoints[3], currentPoints[0]);
                    SetTetrahedronNeighbors(currentNeighbors[1], 0, t2);
                }

                if(-1 != neighbor0Neighbors[1])
                {
                    SetBase(neighbor0Neighbors[1], currentPoints[0], currentPoints[1], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[1], 0, t3);
                }
                if(-1 != neighbor0Neighbors[2])
                {
                    SetBase(neighbor0Neighbors[2], currentPoints[1], currentPoints[2], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[2], 0, t4);
                }
                if(-1 != neighbor0Neighbors[3])
                {
                    SetBase(neighbor0Neighbors[3], currentPoints[2], currentPoints[0], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[3], 0, t5);
                }

                s.Add(t0);
                s.Add(t1);
                s.Add(t2);
                s.Add(t3);
                s.Add(t4);
                s.Add(t5);

                #if CHECK_FLAT_TETRAHEDRON_AFTER_FLIP
                if(Sign.ZERO == Orient(t0) ||Sign.ZERO == Orient(t1)||Sign.ZERO == Orient(t2)||Sign.ZERO == Orient(t3)||Sign.ZERO == Orient(t4)||Sign.ZERO == Orient(t5))
                {
                    Debug.LogWarning("26 flat");
                    return;
                }
                #endif
            }
            else
            {   // 1->4 flip
                currentPoints = GetTetrahedronPoints(current);
                Int4 currentNeighbors = GetTetrahedronNeighbors(current);
                RemoveTetrahedron(current);

                int t0 = CreateNewTetrahedron(currentPoints[0], currentPoints[1], currentPoints[2], p);
                int t1 = CreateNewTetrahedron(currentPoints[0], currentPoints[2], currentPoints[3], p);
                int t2 = CreateNewTetrahedron(currentPoints[2], currentPoints[1], currentPoints[3], p);
                int t3 = CreateNewTetrahedron(currentPoints[0], currentPoints[3], currentPoints[1], p);

                incidentTetrahedrons[currentPoints[0]] = t0;
                incidentTetrahedrons[currentPoints[1]] = t0;
                incidentTetrahedrons[currentPoints[2]] = t0;
                incidentTetrahedrons[currentPoints[3]] = t1;
                incidentTetrahedrons[p] = t0;

                SetTetrahedronNeighbors(t0, currentNeighbors[0], t1, t2, t3);
                SetTetrahedronNeighbors(t1, currentNeighbors[1], t3, t2, t0);
                SetTetrahedronNeighbors(t2, currentNeighbors[2], t1, t3, t0);
                SetTetrahedronNeighbors(t3, currentNeighbors[3], t0, t2, t1);

                if(-1 != currentNeighbors[0])
                {
                    SetBase(currentNeighbors[0], currentPoints[0], currentPoints[1], currentPoints[2]);
                    SetTetrahedronNeighbors(currentNeighbors[0], 0, t0);
                }
                if(-1 != currentNeighbors[1])
                {
                    SetBase(currentNeighbors[1], currentPoints[0], currentPoints[2], currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[1], 0, t1);
                }
                if(-1 != currentNeighbors[2])
                {
                    SetBase(currentNeighbors[2], currentPoints[2], currentPoints[1], currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[2], 0, t2);
                }
                if(-1 != currentNeighbors[3])
                {
                    SetBase(currentNeighbors[3], currentPoints[0], currentPoints[3], currentPoints[1]);
                    SetTetrahedronNeighbors(currentNeighbors[3], 0, t3);
                }

                s.Add(t0);
                s.Add(t1);
                s.Add(t2);
                s.Add(t3);

                #if CHECK_FLAT_TETRAHEDRON_AFTER_FLIP
                if(Sign.ZERO == Orient(t0) ||Sign.ZERO == Orient(t1)||Sign.ZERO == Orient(t2)||Sign.ZERO == Orient(t3))
                {
                    Debug.LogWarning("14 flat");
                    return;
                }
                #endif
            }
        }
        

        while(0 != s.Count)
        {
            int current = s.First();
            s.Remove(current);
            SetPivot(current, p);
            int neighbor0 = GetTetrahedronNeighbors(current)[0];
            if(-1 == neighbor0)
            {
                continue;
            }    
            Int4 currentPoints = GetTetrahedronPoints(current);
            SetBase(neighbor0, currentPoints[0], currentPoints[1], currentPoints[2]);
            Int4 neighbor0Points = GetTetrahedronPoints(neighbor0);

            if(Sign.POSITIVE != InCircumsphereSymbolicPerturbation(current, neighbor0Points[3]))
            {
                continue;
            }

            Sign orient023 = Orient(currentPoints[0],currentPoints[2],currentPoints[3],neighbor0Points[3]);
            Sign orient213 = Orient(currentPoints[2],currentPoints[1],currentPoints[3],neighbor0Points[3]);
            Sign orient031 = Orient(currentPoints[0],currentPoints[3],currentPoints[1],neighbor0Points[3]);

            if(Sign.POSITIVE == orient023 && Sign.POSITIVE == orient213 && Sign.POSITIVE == orient031)
            {
                // 2->3 flip
                Int4 currentNeighbors = GetTetrahedronNeighbors(current);
                Int4 neighbor0Neighbors = GetTetrahedronNeighbors(neighbor0);

                s.Remove(current);
                s.Remove(neighbor0);
                RemoveTetrahedron(current);
                RemoveTetrahedron(neighbor0);

                int t0 = CreateNewTetrahedron(currentPoints[0],currentPoints[1],neighbor0Points[3],currentPoints[3]);
                int t1 = CreateNewTetrahedron(currentPoints[1],currentPoints[2],neighbor0Points[3],currentPoints[3]);
                int t2 = CreateNewTetrahedron(currentPoints[2],currentPoints[0],neighbor0Points[3],currentPoints[3]);

                incidentTetrahedrons[currentPoints[0]] = t0;
                incidentTetrahedrons[currentPoints[1]] = t0;
                incidentTetrahedrons[currentPoints[2]] = t1;
                incidentTetrahedrons[currentPoints[3]] = t0;
                incidentTetrahedrons[neighbor0Points[3]] = t0;

                SetTetrahedronNeighbors(t0, neighbor0Neighbors[1], t2, t1, currentNeighbors[3]);
                SetTetrahedronNeighbors(t1, neighbor0Neighbors[2], t0, t2, currentNeighbors[2]);
                SetTetrahedronNeighbors(t2, neighbor0Neighbors[3], t1, t0, currentNeighbors[1]);

                if(-1 != neighbor0Neighbors[1])
                {
                    SetBase(neighbor0Neighbors[1], currentPoints[0], currentPoints[1], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[1], 0, t0);
                }
                if(-1 != currentNeighbors[3])
                {
                    SetBase(currentNeighbors[3], currentPoints[0], currentPoints[1], currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[3], 0, t0);
                }
                if(-1 != neighbor0Neighbors[2])
                {
                    SetBase(neighbor0Neighbors[2], currentPoints[1], currentPoints[2], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[2], 0, t1);
                }
                if(-1 != currentNeighbors[2])
                {
                    SetBase(currentNeighbors[2], currentPoints[1], currentPoints[2], currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[2], 0, t1);
                }
                if(-1 != neighbor0Neighbors[3])
                {
                    SetBase(neighbor0Neighbors[3], currentPoints[2], currentPoints[0], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[3], 0, t2);
                }
                if(-1 != currentNeighbors[1])
                {
                    SetBase(currentNeighbors[1], currentPoints[2], currentPoints[0], currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[1], 0, t2);
                }

                s.Add(t0);
                s.Add(t1);
                s.Add(t2);

                #if CHECK_FLAT_TETRAHEDRON_AFTER_FLIP
                if(Sign.ZERO == Orient(t0) ||Sign.ZERO == Orient(t1)||Sign.ZERO == Orient(t2))
                {
                    Debug.LogWarning("23 flat");
                    return;
                }
                #endif
                continue;
            }

            //{0,1,2}, {0,2,3}, {2,1,3}, {0,3,1}
            bool try23 = false;
            if(Sign.NEGATIVE == orient023)
            {
                if(Sign.NEGATIVE == orient213 || Sign.NEGATIVE == orient031)
                {
                    continue;
                }
                SetBase(current, currentPoints[2], currentPoints[0], currentPoints[1]);
                SetBase(neighbor0, currentPoints[2], currentPoints[0], currentPoints[1]);
                try23 = true;
            }
            else if(Sign.NEGATIVE == orient213)
            {
                if(Sign.NEGATIVE == orient023 || Sign.NEGATIVE == orient031)
                {
                    continue;
                }
                SetBase(current, currentPoints[1], currentPoints[2], currentPoints[0]);
                SetBase(neighbor0, currentPoints[1], currentPoints[2], currentPoints[0]);
                try23 = true;
            }
            else if(Sign.NEGATIVE == orient031)
            {
                if(Sign.NEGATIVE == orient023 || Sign.NEGATIVE == orient213)
                {
                    continue;
                }
                //SetBase(current, currentPoints[0], currentPoints[1], currentPoints[2]);
                //SetBase(neighbor0, currentPoints[0], currentPoints[1], currentPoints[2]);
                try23 = true;
            }

            if(try23)
            {
                currentPoints = GetTetrahedronPoints(current);
                neighbor0Points = GetTetrahedronPoints(neighbor0);
                Int4 currentNeighbors = GetTetrahedronNeighbors(current);
                Int4 neighbor0Neighbors = GetTetrahedronNeighbors(neighbor0);
                int neighbor1 = -1;
                if(-1 == currentNeighbors[3] || currentNeighbors[3] != neighbor0Neighbors[1])
                {
                    continue;
                }
                // 3->2 flip
                neighbor1 = currentNeighbors[3];
                SetBase(neighbor1, currentPoints[0],currentPoints[1],currentPoints[3]);
                Int4 neighbor1Neighbors = GetTetrahedronNeighbors(neighbor1);

                s.Remove(current);
                s.Remove(neighbor0);
                s.Remove(neighbor1);
                RemoveTetrahedron(current);
                RemoveTetrahedron(neighbor0);
                RemoveTetrahedron(neighbor1);

                int t0 = CreateNewTetrahedron(currentPoints[2],currentPoints[0],neighbor0Points[3],currentPoints[3]);
                int t1 = CreateNewTetrahedron(currentPoints[2],neighbor0Points[3],currentPoints[1],currentPoints[3]);

                incidentTetrahedrons[currentPoints[0]] = t0;
                incidentTetrahedrons[currentPoints[1]] = t1;
                incidentTetrahedrons[currentPoints[2]] = t0;
                incidentTetrahedrons[currentPoints[3]] = t0;
                incidentTetrahedrons[neighbor0Points[3]] = t0;


                SetTetrahedronNeighbors(t0, neighbor0Neighbors[3], t1, neighbor1Neighbors[1], currentNeighbors[1]);
                SetTetrahedronNeighbors(t1, neighbor0Neighbors[2], currentNeighbors[2], neighbor1Neighbors[2], t0);

                if(-1 != neighbor0Neighbors[3])
                {
                    SetBase(neighbor0Neighbors[3], currentPoints[2],currentPoints[0],neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[3], 0, t0);
                }
                if(-1 != neighbor1Neighbors[1])
                {
                    SetBase(neighbor1Neighbors[1], currentPoints[0],neighbor0Points[3],currentPoints[3]);
                    SetTetrahedronNeighbors(neighbor1Neighbors[1], 0, t0);
                }
                if(-1 != currentNeighbors[1])
                {
                    SetBase(currentNeighbors[1], currentPoints[2],currentPoints[0],currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[1], 0, t0);
                }

                if(-1 != neighbor0Neighbors[2])
                {
                    SetBase(neighbor0Neighbors[2], currentPoints[2],neighbor0Points[3],currentPoints[1]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[2], 0, t1);
                }
                if(-1 != currentNeighbors[2])
                {
                    SetBase(currentNeighbors[2], currentPoints[2],currentPoints[1],currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[2], 0, t1);
                }
                if(-1 != neighbor1Neighbors[2])
                {
                    SetBase(neighbor1Neighbors[2], currentPoints[1],neighbor0Points[3],currentPoints[3]);
                    SetTetrahedronNeighbors(neighbor1Neighbors[2], 0, t1);
                }

                s.Add(t0);
                s.Add(t1);

                #if CHECK_FLAT_TETRAHEDRON_AFTER_FLIP
                if(Sign.ZERO == Orient(t0) ||Sign.ZERO == Orient(t1))
                {
                    Debug.LogWarning("32 flat");
                    return;
                }
                #endif
                continue;
            }

            bool try44 = false;
            if(Sign.ZERO == orient023)
            {
                if(Sign.ZERO == orient213 || Sign.ZERO == orient031)
                {
                    continue;
                }
                SetBase(current, currentPoints[2], currentPoints[0], currentPoints[1]);
                SetBase(neighbor0, currentPoints[2], currentPoints[0], currentPoints[1]);
                try44 = true;
            }
            else if(Sign.ZERO == orient213)
            {
                if(Sign.ZERO == orient023 || Sign.ZERO == orient031)
                {
                    continue;
                }
                SetBase(current, currentPoints[1], currentPoints[2], currentPoints[0]);
                SetBase(neighbor0, currentPoints[1], currentPoints[2], currentPoints[0]);
                try44 = true;
            }
            else if(Sign.ZERO == orient031)
            {
                if(Sign.ZERO == orient023 || Sign.ZERO == orient213)
                {
                    continue;
                }
                //SetBase(current, currentPoints[0], currentPoints[1], currentPoints[2]);
                //SetBase(neighbor0, currentPoints[0], currentPoints[1], currentPoints[2]);
                try44 = true;
            }

            if(try44)
            {
                currentPoints = GetTetrahedronPoints(current);
                neighbor0Points = GetTetrahedronPoints(neighbor0);
                Int4 currentNeighbors = GetTetrahedronNeighbors(current);
                Int4 neighbor0Neighbors = GetTetrahedronNeighbors(neighbor0);
                int neighbor1 = currentNeighbors[3];
                int neighbor2 = neighbor0Neighbors[1];
                SetBase(neighbor1, currentPoints[0], currentPoints[1], currentPoints[3]);
                SetBase(neighbor2, currentPoints[0], neighbor0Points[3], currentPoints[1]);
                Int4 neighbor1Points = GetTetrahedronPoints(neighbor1);
                Int4 neighbor2Points = GetTetrahedronPoints(neighbor2);

                if(neighbor1Points[3] != neighbor2Points[3] ||
                    Sign.ZERO == Orient(currentPoints[0],currentPoints[3],neighbor0Points[3],neighbor1Points[3]) ||
                    Sign.ZERO == Orient(currentPoints[1],currentPoints[3],neighbor0Points[3],neighbor1Points[3]))
                {
                    continue;
                }

                // 4->4 flip
                Int4 neighbor1Neighbors = GetTetrahedronNeighbors(neighbor1);
                Int4 neighbor2Neighbors = GetTetrahedronNeighbors(neighbor2);

                s.Remove(current);
                s.Remove(neighbor0);
                s.Remove(neighbor1);
                s.Remove(neighbor2);
                RemoveTetrahedron(current);
                RemoveTetrahedron(neighbor0);
                RemoveTetrahedron(neighbor1);
                RemoveTetrahedron(neighbor2);

                int t0 = CreateNewTetrahedron(currentPoints[2], currentPoints[0], neighbor0Points[3], currentPoints[3]);
                int t1 = CreateNewTetrahedron(currentPoints[2], neighbor0Points[3], currentPoints[1], currentPoints[3]);
                int t2 = CreateNewTetrahedron(neighbor1Points[3], neighbor0Points[3], currentPoints[0], currentPoints[3]);
                int t3 = CreateNewTetrahedron(neighbor1Points[3], currentPoints[1], neighbor0Points[3], currentPoints[3]);

                incidentTetrahedrons[currentPoints[0]] = t0;
                incidentTetrahedrons[currentPoints[1]] = t1;
                incidentTetrahedrons[currentPoints[2]] = t0;
                incidentTetrahedrons[currentPoints[3]] = t0;
                incidentTetrahedrons[neighbor0Points[3]] = t0;
                incidentTetrahedrons[neighbor1Points[3]] = t2;
                    
                SetTetrahedronNeighbors(t0, neighbor0Neighbors[3], t1, t2, currentNeighbors[1]);
                SetTetrahedronNeighbors(t1, neighbor0Neighbors[2], currentNeighbors[2], t3, t0);
                SetTetrahedronNeighbors(t2, neighbor2Neighbors[3], neighbor1Neighbors[1], t0, t3);
                SetTetrahedronNeighbors(t3, neighbor2Neighbors[2], t2, t1, neighbor1Neighbors[2]);

                if(-1 != neighbor0Neighbors[3])
                {
                    SetBase(neighbor0Neighbors[3], currentPoints[2], currentPoints[0], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[3], 0, t0);
                }
                if(-1 != currentNeighbors[1])
                {
                    SetBase(currentNeighbors[1], currentPoints[2], currentPoints[0], currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[1], 0, t0);
                }

                if(-1 != neighbor0Neighbors[2])
                {
                    SetBase(neighbor0Neighbors[2], currentPoints[2], neighbor0Points[3], currentPoints[1]);
                    SetTetrahedronNeighbors(neighbor0Neighbors[2], 0, t1);
                }
                if(-1 != currentNeighbors[2])
                {
                    SetBase(currentNeighbors[2], currentPoints[2], currentPoints[1], currentPoints[3]);
                    SetTetrahedronNeighbors(currentNeighbors[2], 0, t1);
                }

                if(-1 != neighbor2Neighbors[3])
                {
                    SetBase(neighbor2Neighbors[3], neighbor1Points[3], neighbor0Points[3], currentPoints[0]);
                    SetTetrahedronNeighbors(neighbor2Neighbors[3], 0, t2);
                }
                if(-1 != neighbor1Neighbors[1])
                {
                    SetBase(neighbor1Neighbors[1], neighbor1Points[3], currentPoints[0], currentPoints[3]);
                    SetTetrahedronNeighbors(neighbor1Neighbors[1], 0, t2);
                }

                if(-1 != neighbor2Neighbors[2])
                {
                    SetBase(neighbor2Neighbors[2], neighbor1Points[3], currentPoints[1], neighbor0Points[3]);
                    SetTetrahedronNeighbors(neighbor2Neighbors[2], 0, t3);
                }
                if(-1 != neighbor1Neighbors[2])
                {
                    SetBase(neighbor1Neighbors[2], neighbor1Points[3], currentPoints[1], currentPoints[3]);
                    SetTetrahedronNeighbors(neighbor1Neighbors[2], 0, t3);
                }

                s.Add(t0);
                s.Add(t1);
                s.Add(t2);
                s.Add(t3);

                #if CHECK_FLAT_TETRAHEDRON_AFTER_FLIP
                if(Sign.ZERO == Orient(t0) ||Sign.ZERO == Orient(t1) ||Sign.ZERO == Orient(t2) ||Sign.ZERO == Orient(t3))
                {
                    Debug.LogWarning("44 flat");
                    return;
                }
                #endif
                continue;
                
            }

            #if CHECK_DELAUNARY_AFTER_INSERT
            VerifyDelaunay();
            #endif
        }
    }

    private (int, Sign, Sign, Sign, Sign) FindTetrahedronContainPoint(int p)
    {
        int res = -1;
        Sign orient012 = Sign.POSITIVE;
        Sign orient023 = Sign.POSITIVE;
        Sign orient213 = Sign.POSITIVE;
        Sign orient031 = Sign.POSITIVE;
        for(int i=0; i<tetrahedrons.Count; i+=4)
        {
            if(-1 != tetrahedrons[i])
            {
                res = i/4;
                break;
            }
        }
        int prev = res;
        bool found = false;
        while(!found)
        {
            Int4 points = GetTetrahedronPoints(res);
            Int4 neighbors = GetTetrahedronNeighbors(res);
            if(Sign.NEGATIVE == (orient012=Orient(points[0],points[1],points[2],p)) && -1 != neighbors[0] && prev != neighbors[0])
            {
                prev = res;
                res = neighbors[0];
            }
            else
            {
                if(Sign.NEGATIVE == (orient023=Orient(points[0],points[2],points[3],p)) && -1 != neighbors[1] && prev != neighbors[1])
                {
                    prev = res;
                    res = neighbors[1];
                }
                else
                {
                    if(Sign.NEGATIVE == (orient213=Orient(points[2],points[1],points[3],p)) && -1 != neighbors[2] && prev != neighbors[2])
                    {
                        prev = res;
                        res = neighbors[2];
                    }
                    else
                    {
                        if(Sign.NEGATIVE == (orient031=Orient(points[0],points[3],points[1],p)) && -1 != neighbors[3] && prev != neighbors[3])
                        {
                            prev = res;
                            res = neighbors[3];
                        }
                        else
                        {
                            found = true;
                        }
                    }
                }
            }
        }
        return (res, orient012, orient023, orient213, orient031);
    }
}

}
