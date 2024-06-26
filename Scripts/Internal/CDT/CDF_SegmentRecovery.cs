using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public partial class DelaunayTetrahedralization
{
    private void CalculateEdgeInformation()
    {
        List<HashSet<int>> inputPointsNeighbors = Enumerable.Range(0,points.Count).Select(i=>new HashSet<int>()).ToList();
        
        for(int i=0; i<inputTriangles.Count; i+=3)
        {
            for(int j=0; j<3; j++)
            {
                int isReverse;
                (int,int) edge;
                if(inputTriangles[i+j] < inputTriangles[i+((j+1)%3)])
                {
                    isReverse = 0;
                    edge = (inputTriangles[i+j], inputTriangles[i+((j+1)%3)]);
                }
                else
                {
                    isReverse = 1;
                    edge = (inputTriangles[i+((j+1)%3)], inputTriangles[i+j]);
                }

                inputPointsNeighbors[edge.Item1].Add(edge.Item2);
                inputPointsNeighbors[edge.Item2].Add(edge.Item1);
                if(!inputEdges.ContainsKey(edge))
                {
                    inputEdges[edge] = new Int4(i/3, isReverse, -1,-1);
                }
                else
                {
                    Int4 old = inputEdges[edge];
                    inputEdges[edge] = new Int4(old[0], old[1], i/3, isReverse);
                }
            }
        }
        foreach(var kvp in inputEdges)
        {
            Int4 incidentTriangle = kvp.Value;

            int p2 = -1;
            int p3 = -1;
            for(int i=0; i<3; i++)
            {
                if(kvp.Key.Item1 != inputTriangles[incidentTriangle[0]*3+i] && kvp.Key.Item2 != inputTriangles[incidentTriangle[0]*3+i])
                {
                    p2 = inputTriangles[incidentTriangle[0]*3+i];
                }
                if(kvp.Key.Item1 != inputTriangles[incidentTriangle[2]*3+i] && kvp.Key.Item2 != inputTriangles[incidentTriangle[2]*3+i])
                {
                    p3 = inputTriangles[incidentTriangle[2]*3+i];
                }
            }
            if(Sign.ZERO == Orient(kvp.Key.Item1,kvp.Key.Item2, p2,p3))
            {
                flatInputEdges.Add(kvp.Key);
            }
            else
            {
                nonFlatEdges.Add(kvp.Key);
            }
        }

        pointsIsAcute = Enumerable.Range(0,points.Count).Select(i=>false).ToList();
        for(int i=4; i<inputPointsNeighbors.Count; i++)
        {
            List<int> neighbors = inputPointsNeighbors[i].ToList();
            for(int j=0; j<neighbors.Count; j++)
            {
                for(int k=j+1; k<neighbors.Count; k++)
                {
                    if(Sign.POSITIVE == DotProductSign(neighbors[j],neighbors[k],i))
                    {
                        pointsIsAcute[i] = true;
                        goto NEXT;
                    }
                }
            }
            NEXT:
            continue;
        }
    }
        
    private void SegmentRecovery()
    {
        void AddIncidentTriangle((int,int) edge, int triangle, int isReversed)
        {
            Int4 old = inputEdges[edge];
            if(-1 == old[0])
            {
                inputEdges[edge] = new Int4(triangle,isReversed,-1,-1);
            }
            else
            {
                inputEdges[edge] = new Int4(old[0], old[1],triangle,isReversed);
            }
        }

        int allowedIteration = 100;
        int count = 0;
        List<(int,int)> missEdges = new List<(int, int)>();
        List<int> searchTetrahedron = new List<int>();
        do
        {
            if(--allowedIteration<0)
            {
                Debug.Log($"{count}, Too many iteration");
                throw new Exception();
            }
            missEdges.Clear();
            foreach(var edge in nonFlatEdges)
            {
                if(0 != FindTetrahedronIncident(edge.Item1,edge.Item2).Count)
                {
                    continue;
                }
                missEdges.Add(edge);
            }
            //missEdges.Shuffle();
            foreach((int, int) edge in missEdges)
            {
                count++;
                int p = SplitEdge(edge.Item1,edge.Item2,searchTetrahedron);
                if(-1 == p)
                {
                    Debug.Log("Split edge problem");
                    continue;
                }

                AddNewPoint(p);
                nonFlatEdges.Remove(edge);
                nonFlatEdges.Add((edge.Item1, p));
                nonFlatEdges.Add((edge.Item2, p));

                Int4 impactTriangles = inputEdges[edge];
                inputEdges.Remove(edge);
                inputEdges[(edge.Item1, p)] = new Int4(-1,-1,-1,-1);
                inputEdges[(edge.Item2, p)] = new Int4(-1,-1,-1,-1);

                for(int i=0; i<4; i+=2) // note i+=2
                {
                    int p2 = -1;
                    for(int j=0; j<3; j++)
                    {
                        if(edge.Item1 != inputTriangles[3*impactTriangles[i] + j] && edge.Item2 != inputTriangles[3*impactTriangles[i] + j])
                        {
                            p2 = inputTriangles[3*impactTriangles[i] + j];
                            break;
                        }
                    }

                    int n = inputTriangles.Count / 3;
                    if(1 == impactTriangles[i+1])
                    {
                        inputTriangles[3*impactTriangles[i]+0] = p;
                        inputTriangles[3*impactTriangles[i]+1] = edge.Item1;
                        inputTriangles[3*impactTriangles[i]+2] = p2;
                        AddIncidentTriangle((edge.Item1, p), impactTriangles[i], 1);
                        //inputEdges[(edge.Item1, p)].Add((impactTriangles[i].Item1, true));

                        inputTriangles.Add(edge.Item2);
                        inputTriangles.Add(p);
                        inputTriangles.Add(p2);
                        AddIncidentTriangle((edge.Item2, p), n, 0);
                        //inputEdges[(edge.Item2, p)].Add((n,false));

                        inputEdges[(p2, p)] = new Int4(impactTriangles[i], 0, n, 1);
                        //inputEdges[(p2, p)] = new List<(int, bool)>();
                        //inputEdges[(p2, p)].Add((impactTriangles[i].Item1, false));
                        //inputEdges[(p2, p)].Add((n, true));
                    }
                    else
                    {
                        inputTriangles[3*impactTriangles[i]+0] = edge.Item1;
                        inputTriangles[3*impactTriangles[i]+1] = p;
                        inputTriangles[3*impactTriangles[i]+2] = p2;
                        AddIncidentTriangle((edge.Item1, p), impactTriangles[i], 0);
                        //inputEdges[(edge.Item1, p)].Add((impactTriangles[i].Item1, false));

                        inputTriangles.Add(p);
                        inputTriangles.Add(edge.Item2);
                        inputTriangles.Add(p2);
                        AddIncidentTriangle((edge.Item2, p), n, 1);
                        //inputEdges[(edge.Item2, p)].Add((n,true));

                        inputEdges[(p2, p)] = new Int4(impactTriangles[i], 1, n, 0);
                        //inputEdges[(p2, p)] = new List<(int, bool)>();
                        //inputEdges[(p2, p)].Add((impactTriangles[i].Item1, true));
                        //inputEdges[(p2, p)].Add((n, false));
                    }
                    flatInputEdges.Add((p2, p));

                    int p1 = edge.Item2;
                    if(p2>p1)
                    {
                        int temp = p2;
                        p2 = p1;
                        p1 = temp;
                    }
                    Int4 old = inputEdges[(p2,p1)];
                    if(impactTriangles[i] == old[0])
                    {
                        inputEdges[(p2,p1)] = new Int4(n,old[1],old[2],old[3]);
                    }
                    else
                    {
                        inputEdges[(p2,p1)] = new Int4(old[0],old[1],n,old[3]);
                    }
                }
            }
        }while(0 != missEdges.Count);
    }

    private int SplitEdge(int p0, int p1, List<int> searchTetrahedron)
    {
        //if(0 != FindTetrahedronIncident(p0,p1).Count)
        //{
        //    return -1;
        //}

        if(!edgesTypes.ContainsKey((p0, p1)))
        {
            if(!edgesTypes.ContainsKey((p1, p0)))
            {
                bool p0IsAcute = pointsIsAcute[p0];
                bool p1IsAcute = pointsIsAcute[p1];
                if(p0IsAcute && p1IsAcute)
                {
                    //double t0 = GetT(op0, p0);
                    //double t1 = GetT(op1, p1);
                    //int res = CreateNewPoint(op0,op1,(t0+t1)/2d);
                    int res = CreateNewPoint(p0,p1,0.5d);
                    edgesTypes[(p0, res)] = 1;
                    edgesTypes[(p1, res)] = 1;
                    originalEdges[(p0, res)] = (p0, p1);
                    originalEdges[(p1, res)] = (p1, p0);
                    return res;
                }
                else if(p0IsAcute && !p1IsAcute)
                {
                    edgesTypes[(p0, p1)] = 1;
                    originalEdges[(p0, p1)] = (p0, p1);
                }
                else if(!p0IsAcute && p1IsAcute)
                {
                    int temp = p0;
                    p0 = p1;
                    p1 = temp;
                    edgesTypes[(p0, p1)] = 1;
                    originalEdges[(p0, p1)] = (p0, p1);
                }
                else
                {
                    edgesTypes[(p0, p1)] = 0;
                    originalEdges[(p0, p1)] = (p0, p1);
                    //edgesTypes[(p1, p0)] = 0;
                    //originalEdges[(p1, p0)] = (p0, p1);
                }
            }
            else
            {
                int temp = p0;
                p0 = p1;
                p1 = temp;
            }
        }
        

        {
            int acuteVerticesCount = edgesTypes[(p0, p1)];
            int op0 = originalEdges[(p0,p1)].Item1;
            int op1 = originalEdges[(p0,p1)].Item2;
            
            Point3D pp0 = points[p0].ToPoint3D();
            Point3D pp1 = points[p1].ToPoint3D();
            Point3D opp0 = (Point3D)points[op0];
            Point3D opp1 = (Point3D)points[op1];

            int pRef = -1;
            Point3D ppRef = null;
            {
                FindTetrahedronIncident(p0, searchTetrahedron);
                double segmentLength = Point3D.SquareMagnitude(pp1-pp0);

                visitedPoints.Clear();
                visitedPoints.Add(p0);
                visitedPoints.Add(p1);
                visitedTetrahedrons.Clear();
                visitedTetrahedrons.Add(-1);

                for(int i=0; i<searchTetrahedron.Count; i++)
                {
                    if(visitedTetrahedrons.Contains(searchTetrahedron[i]))
                    {
                        continue;
                    }
                    visitedTetrahedrons.Add(searchTetrahedron[i]);

                    bool shouldCheckNeighbors = false;
                    Int4 tetrahedronPoints = GetTetrahedronPoints(searchTetrahedron[i]);
                    for(int j=0; j<4; j++)
                    {
                        if(visitedPoints.Contains(tetrahedronPoints[j]))
                        {
                            //shouldCheckNeighbors = true;
                            continue;
                        }
                        visitedPoints.Add(tetrahedronPoints[j]);

                        Point3D pp2 = points[tetrahedronPoints[j]].ToPoint3D();
                        if((Point3D.SquareMagnitude(pp2-pp0) + Point3D.SquareMagnitude(pp2-pp1)) <= segmentLength)
                        {
                            shouldCheckNeighbors = true;
                            if(-1 == pRef || HasLargerSphere(pp0,pp1,pp2,ppRef))
                            {
                                pRef = tetrahedronPoints[j];
                                ppRef = points[pRef].ToPoint3D();
                            }
                        }
                    }

                    if(shouldCheckNeighbors)
                    {
                        Int4 neighbors = GetTetrahedronNeighbors(searchTetrahedron[i]);
                        searchTetrahedron.Add(neighbors[0]);
                        searchTetrahedron.Add(neighbors[1]);
                        searchTetrahedron.Add(neighbors[2]);
                        searchTetrahedron.Add(neighbors[3]);
                    }
                }
            }

            int res;
            double t0 = GetT(op0, p0);
            double t1 = GetTReverse(op1, p1);
            if(0 == acuteVerticesCount)
            {
                if(-1 == pRef) // should never happen
                {
                    Debug.LogWarning("No reference point found");
                    res = CreateNewPoint(op0,op1,(t0+t1)/2d);
                }
                else if(4d*Point3D.SquareMagnitude(pp0-ppRef) < Point3D.SquareMagnitude(pp0-pp1))
                {
                    double discr = Math.Sqrt(Point3D.SquareMagnitude(ppRef-pp0) / Point3D.SquareMagnitude(opp1-opp0));
                    discr += t0;
                    if(discr >= t1)
                    {
                        discr = (t0+t1) / 2d;
                    }
                    res = CreateNewPoint(op0,op1,discr);
                }
                else if(4d*Point3D.SquareMagnitude(pp1-ppRef) < Point3D.SquareMagnitude(pp0-pp1))
                {
                    double discr = Math.Sqrt(Point3D.SquareMagnitude(ppRef-pp1) / Point3D.SquareMagnitude(opp1-opp0));
                    discr = t1 - discr;
                    if(discr <= t0)
                    {
                        discr = (t0+t1) / 2d;
                    }
                    res = CreateNewPoint(op0,op1,discr);
                }
                else
                {
                    res = CreateNewPoint(op0,op1,(t0+t1)/2d);
                }

                edgesTypes.Remove((p0,p1));
                originalEdges.Remove((p0,p1));

                edgesTypes[(p0, res)] = 0;
                edgesTypes[(p1, res)] = 0;
                originalEdges[(p0, res)] = (op0, op1);
                originalEdges[(p1, res)] = (op1, op0);

                //edgesTypes[(res, p0)] = 0;
                //edgesTypes[(res, p1)] = 0;
                //originalEdges[(res, p0)] = (op1, op0);
                //originalEdges[(res, p1)] = (op0, op1);
            }
            else
            {
                if(-1 == pRef) // should never happen
                {
                    Debug.LogWarning("No reference point found");
                    res = CreateNewPoint(op0,op1,(t0+t1)/2d);
                }
                else
                {
                    double discr = Math.Sqrt(Point3D.SquareMagnitude(ppRef-opp0) / Point3D.SquareMagnitude(opp1-opp0));
                    double dv = (t1 - t0) * 0.2d;
                    if(discr <= t0 + dv || discr >= t1 - dv)
                    {
                        discr = (t0 + t1) / 2d;
                    }
                    Point3DImplicit ppRes = new Point3DImplicit(opp0, opp1, discr);

                    if(Point3D.SquareMagnitude(ppRes.ToPoint3D()-pp1) < Point3D.SquareMagnitude(ppRes.ToPoint3D()-ppRef))
                    {
                        res = CreateNewPoint(op0,op1,(t0+t1)/2d);
                    }
                    else
                    {
                        res = CreateNewPoint(op0,op1,discr);
                    }
                }

                edgesTypes.Remove((p0,p1));
                originalEdges.Remove((p0,p1));

                edgesTypes[(p0, res)] = 1;
                edgesTypes[(res, p1)] = 1;
                originalEdges[(p0, res)] = (op0, op1);
                originalEdges[(res, p1)] = (op0, op1);

            }
            return res;
        }
        
    }

    // true if smallest sphere by p,q,r is larger than smallest sphere by p,q,s
    private bool HasLargerSphere(Point3D pv, Point3D qv, Point3D rv, Point3D sv)
    {
        Point3D pms = pv - sv, qms = qv - sv, pmr = pv - rv, qmr = qv - rv;
        double lens = Point3D.SquareMagnitude(pms) * Point3D.SquareMagnitude(qms);
        if (lens == 0d)
        {
            return true;
        }
        double lenr = Point3D.SquareMagnitude(pmr) * Point3D.SquareMagnitude(qmr);
        if (lenr == 0d)
        {
            return false;
        }
        double dots = Point3D.Dot(pms, qms);
        double dotr = Point3D.Dot(pmr, qmr);

        return (dots * dots) * lenr < (dotr * dotr) * lens;
    }

    private double GetT(int op, int p)
    {
        if(op == p)
        {
            return 0d;
        }
        if(((Point3DImplicit)points[p]).p0 == points[op])
        {
            return ((Point3DImplicit)points[p]).t;
        }
        return 1d - ((Point3DImplicit)points[p]).t;
    }
    private double GetTReverse(int op, int p)
    {
        if(op == p)
        {
            return 1d;
        }
        if(((Point3DImplicit)points[p]).p1 == points[op])
        {
            return ((Point3DImplicit)points[p]).t;
        }
        return 1d - ((Point3DImplicit)points[p]).t;
    }
}

}
