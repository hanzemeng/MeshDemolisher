using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public partial class DelaunayTetrahedralization
{
    private Sign Orient(int t)
    {
        Int4 tetrahedronPoints = GetTetrahedronPoints(t);
        return Orient(tetrahedronPoints[0],tetrahedronPoints[1],tetrahedronPoints[2],tetrahedronPoints[3]);
    }
    private Sign Orient(int a, int b, int c, int d)
    {
        return PointComputation.Orient(points[a], points[b], points[c], points[d]);
    }
    private Sign OrientCache(int p0, int p1, int p2, int p3, Dictionary<int,Sign> orientCache)
    {
        if(!orientCache.ContainsKey(p0))
        {
            //Debug.Log($"{p0}_{p1}_{p2}_{p3}");
            orientCache[p0] = Orient(p0,p1,p2,p3);
        }
        return orientCache[p0];
    }
    private Sign InCircumsphere(int a, int b, int c, int d, int e)
    {
        return PointComputation.InCircumsphere(points[a], points[b], points[c], points[d], points[e]);
    }
    private Sign InCircumsphereSymbolicPerturbation(int t, int p)
    {
        Int4 tetPoints = GetTetrahedronPoints(t);
        return InCircumsphereSymbolicPerturbation(tetPoints[0],tetPoints[1],tetPoints[2],tetPoints[3],p);
    }
    private Sign InCircumsphereSymbolicPerturbation(int a, int b, int c, int d, int p)
    {
        Sign sign = InCircumsphere(a,b,c,d,p);
        if(Sign.ZERO != sign)
        {
            return sign;
        }
        Int5 ps = new Int5(a,b,c,d,p);

        int swaps = 0;
        int n = 5;
        int count;
        do
        {
            count = 0;
            n--;
            for(int i=0; i<n; i++)
            {
                if (ps[i] > ps[i+1])
                {
                    int temp = ps[i];
                    ps[i] = ps[i+1];
                    ps[i+1] = temp;
                    count++;
                }
            }
            swaps += count;
        }while(0 != count);

        int signInt = (int)Orient(ps[1],ps[2],ps[3],ps[4]);
        if(0 != signInt)
        {
            if(1 == swaps%2)
            {
                return (Sign)(-1*signInt);
            }
            else
            {
                return (Sign)(signInt);
            }
        }

        signInt = (int)Orient(ps[0],ps[2],ps[3],ps[4]);
        if(1 == swaps%2)
        {
            return (Sign)(signInt);
        }
        else
        {
            return (Sign)(-1*signInt);
        }
    }

    private Sign DotProductSign(int a, int b, int c)
    {
        return PointComputation.DotProductSign(points[a], points[b], points[c]);
    }
    private Sign PointInInnerSegment(int a, int b, int c)
    {
        return PointComputation.PointInInnerSegment(points[a], points[b], points[c]);
    }
    private Sign LineCrossInnerTriangle(int a, int b, int c, int d, int e)
    {
        return PointComputation.LineCrossInnerTriangle(points[a], points[b], points[c], points[d], points[e]);
    }
    private Sign LineCrossTriangle(int a, int b, int c, int d, int e)
    {
        return PointComputation.LineCrossTriangle(points[a], points[b], points[c], points[d], points[e]);
    }
    private Sign SegmentCrossInnerSegment(int a, int b, int c, int d)
    {
        return PointComputation.SegmentCrossInnerSegment(points[a], points[b], points[c], points[d]);
    }
    private Sign InnerSegmentCrossInnerTriangle(int a, int b, int c, int d, int e)
    {
        return PointComputation.InnerSegmentCrossInnerTriangle(points[a], points[b], points[c], points[d], points[e]);
    }
    private Sign PointInInnerTriangle(int a, int b, int c, int d)
    {
        return PointComputation.PointInInnerTriangle(points[a], points[b], points[c], points[d]);
    }
    private Sign PointInTriangle(int a, int b, int c, int d)
    {
        return PointComputation.PointInTriangle(points[a], points[b], points[c], points[d]);
    }
    private Point3D CalculateTetrahedronCenter(int tetrahedron)
    {
        Int4 tetrahedronPoints = GetTetrahedronPoints(tetrahedron);
        return PointComputation.CircumsphereFromFourPoints(points[tetrahedronPoints[0]],points[tetrahedronPoints[1]],points[tetrahedronPoints[2]],points[tetrahedronPoints[3]]);
    }


    public List<int> FindTetrahedronIncident(int p0, int p1)
    {
        return FindTetrahedronIncident(p0).Where(x=>HasPoint(x,p1)).ToList();
    }
    public List<int> FindTetrahedronIncident(int p0)
    {
        List<int> res = new List<int>();
        FindTetrahedronIncident(p0,res);
        return res;
    }
    public void FindTetrahedronIncident(int p0, List<int> res)
    {
        res.Clear();
        visitedTetrahedrons.Clear();
        searchQueue.Clear();
        visitedTetrahedrons.Add(-1);

        Int4 temp;
        searchQueue.Enqueue(incidentTetrahedrons[p0]);
        while(0 != searchQueue.Count)
        {
            int tetrahedron = searchQueue.Dequeue();
            if(visitedTetrahedrons.Contains(tetrahedron))
            {
                continue;
            }
            visitedTetrahedrons.Add(tetrahedron);

            if(!HasPoint(tetrahedron, p0))
            {
                continue;
            }

            res.Add(tetrahedron);
            temp = GetTetrahedronNeighbors(tetrahedron);
            searchQueue.Enqueue(temp[0]);
            searchQueue.Enqueue(temp[1]);
            searchQueue.Enqueue(temp[2]);
            searchQueue.Enqueue(temp[3]);
        }
    }

    public bool HasPoint(int t, int p)
    {
        return
            tetrahedrons[4*t+0] == p ||
            tetrahedrons[4*t+1] == p ||
            tetrahedrons[4*t+2] == p ||
            tetrahedrons[4*t+3] == p;
    }
    public bool HasGhostPoint(int t)
    {
        return
            tetrahedrons[4*t+0] < 4 ||
            tetrahedrons[4*t+1] < 4 ||
            tetrahedrons[4*t+2] < 4 ||
            tetrahedrons[4*t+3] < 4;
    }

    private int CreateNewPoint(Vector3 point)
    {
        int res = points.Count;
        points.Add(new Point3D(point));
        incidentTetrahedrons.Add(-1);
        return res;
    }
    private int CreateNewPoint(int p0, int p1, double t)
    {
        int res = points.Count;
        points.Add(new Point3DImplicit((Point3D)points[p0],(Point3D)points[p1],t));
        incidentTetrahedrons.Add(-1);
        insertedPointsMappings[res] = (p0,p1,t);
        return res;
    }

    private int CreateNewTetrahedron(int p0, int p1, int p2, int p3)
    {
        int res;
        if(0 != tetrahedronsGapIndex.Count)
        {
            res = tetrahedronsGapIndex.Dequeue();
            tetrahedrons[4*res+0] = p0;
            tetrahedrons[4*res+1] = p1;
            tetrahedrons[4*res+2] = p2;
            tetrahedrons[4*res+3] = p3;
        }
        else
        {
            res = tetrahedrons.Count / 4;
            tetrahedrons.Add(p0);
            tetrahedrons.Add(p1);
            tetrahedrons.Add(p2);
            tetrahedrons.Add(p3);
            tetrahedronNeighbors.Add(-1);
            tetrahedronNeighbors.Add(-1);
            tetrahedronNeighbors.Add(-1);
            tetrahedronNeighbors.Add(-1);
        }
        
        return res;
    }
    private void RemoveTetrahedron(int t)
    {
        for(int i=0; i<4; i++)
        {
            tetrahedrons[t*4+i] = -1;
            tetrahedronNeighbors[t*4+i] = -1;
        }
        tetrahedronsGapIndex.Enqueue(t);
    }

    public Int4 GetTetrahedronPoints(int t)
    {
        return new Int4
        (
            tetrahedrons[t*4+0],
            tetrahedrons[t*4+1],
            tetrahedrons[t*4+2],
            tetrahedrons[t*4+3]
        );
    }
    public Int3 GetTetrahedronPoints(int t, int f)
    {
        return new Int3
        (
            tetrahedrons[t*4+TETRAHEDRON_FACET[f,0]],
            tetrahedrons[t*4+TETRAHEDRON_FACET[f,1]],
            tetrahedrons[t*4+TETRAHEDRON_FACET[f,2]]
        );
    }

    private void SetTetrahedronPoints(int t, int p0, int p1, int p2, int p3)
    {
        tetrahedrons[t*4+0] = p0;
        tetrahedrons[t*4+1] = p1;
        tetrahedrons[t*4+2] = p2;
        tetrahedrons[t*4+3] = p3;
    }

    public Int4 GetTetrahedronNeighbors(int t)
    {
        return new Int4
        (
            tetrahedronNeighbors[t*4+0],
            tetrahedronNeighbors[t*4+1],
            tetrahedronNeighbors[t*4+2],
            tetrahedronNeighbors[t*4+3]
        );
    }
    private void SetTetrahedronNeighbors(int t, int n0, int n1, int n2, int n3)
    {
        tetrahedronNeighbors[t*4+0] = n0;
        tetrahedronNeighbors[t*4+1] = n1;
        tetrahedronNeighbors[t*4+2] = n2;
        tetrahedronNeighbors[t*4+3] = n3;
    }
    private void SetTetrahedronNeighbors(int t, int i, int n)
    {
        tetrahedronNeighbors[t*4+i] = n;
    }

    private void SetPivot(int tetrahedron, int p)
    {
        Int4 oldPoints = GetTetrahedronPoints(tetrahedron);
        int pi = -1;
        for(int i=0; i<4; i++)
        {
            if(p == oldPoints[i])
            {
                pi = i;
                break;
            }
        }
        if(-1 == pi)
        {
            throw new Exception();
        }
        ChangePivot(tetrahedron, (pi+1)%4, pi);
    }
    private void SetBase(int tetrahedron, int b0, int b1, int b2)
    {
        Int4 oldPoints = GetTetrahedronPoints(tetrahedron);
        int bi = -1;
        int pi = -1;
        for(int i=0; i<4; i++)
        {
            if(b0 == oldPoints[i])
            {
                bi = i;
            }
            if(b0 != oldPoints[i] && b1 != oldPoints[i] && b2 != oldPoints[i])
            {
                pi = i;
            }
        }
        ChangePivot(tetrahedron, bi, pi);
    }
    private void SetBaseAndPivot(int tetrahedron, int b, int p)
    {
        Int4 oldPoints = GetTetrahedronPoints(tetrahedron);
        int bi = -1;
        int pi = -1;
        for(int i=0; i<4; i++)
        {
            if(b == oldPoints[i])
            {
                bi = i;
            }
            if(p == oldPoints[i])
            {
                pi = i;
            }
        }
        ChangePivot(tetrahedron, bi, pi);
    }
    // move the bith point to the 0th point and the pith point to the 3rd point
    private void ChangePivot(int tetrahedron, int bi, int pi)
    {
        if(bi == pi)
        {
            throw new Exception();
        }

        Int4 oldPoints = GetTetrahedronPoints(tetrahedron);
        Int4 oldNeighbors = GetTetrahedronNeighbors(tetrahedron);

        if(0 == bi)
        {
            if(1 == pi)
            {
                //0->0, 1->3, 2->1, 3->2
                SetTetrahedronPoints(tetrahedron, oldPoints[0], oldPoints[2], oldPoints[3], oldPoints[1]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[1], oldNeighbors[3], oldNeighbors[2], oldNeighbors[0]);
            }
            else if(2 == pi)
            {
                //0->0, 1->2, 2->3, 3->1
                SetTetrahedronPoints(tetrahedron, oldPoints[0], oldPoints[3], oldPoints[1], oldPoints[2]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[3], oldNeighbors[0], oldNeighbors[2], oldNeighbors[1]);
            }
        }
        else if(1 == bi)
        {
            if(0 == pi)
            {
                //0->3, 1->0, 2->2, 3->1
                SetTetrahedronPoints(tetrahedron, oldPoints[1], oldPoints[3], oldPoints[2], oldPoints[0]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[2], oldNeighbors[0], oldNeighbors[1], oldNeighbors[3]);
            }
            else if(2 == pi)
            {
                //0->1, 1->0, 2->3, 3->2
                SetTetrahedronPoints(tetrahedron, oldPoints[1], oldPoints[0], oldPoints[3], oldPoints[2]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[3], oldNeighbors[2], oldNeighbors[1], oldNeighbors[0]);
            }
            else if(3 == pi)
            {
                //0->2, 1->0, 2->1, 3->3
                SetTetrahedronPoints(tetrahedron, oldPoints[1], oldPoints[2], oldPoints[0], oldPoints[3]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[0], oldNeighbors[3], oldNeighbors[1], oldNeighbors[2]);
            }
        }
        else if(2 == bi)
        {
            if(0 == pi)
            {
                //0->3, 1->1, 2->0, 3->2
                SetTetrahedronPoints(tetrahedron, oldPoints[2], oldPoints[1], oldPoints[3], oldPoints[0]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[2], oldNeighbors[1], oldNeighbors[3], oldNeighbors[0]);
            }
            else if(1 == pi)
            {
                //0->2, 1->3, 2->0, 3->1
                SetTetrahedronPoints(tetrahedron, oldPoints[2], oldPoints[3], oldPoints[0], oldPoints[1]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[1], oldNeighbors[0], oldNeighbors[3], oldNeighbors[2]);
            }
            else if(3 == pi)
            {
                //0->1, 1->2, 2->0, 3->3
                SetTetrahedronPoints(tetrahedron, oldPoints[2], oldPoints[0], oldPoints[1], oldPoints[3]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[0], oldNeighbors[2], oldNeighbors[3], oldNeighbors[1]);
            }
        }
        else if(3 == bi)
        {
            if(0 == pi)
            {
                //0->3, 1->2, 2->1, 3->0
                SetTetrahedronPoints(tetrahedron, oldPoints[3], oldPoints[2], oldPoints[1], oldPoints[0]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[2], oldNeighbors[3], oldNeighbors[0], oldNeighbors[1]);
            }
            else if(1 == pi)
            {
                //0->1, 1->3, 2->2, 3->0
                SetTetrahedronPoints(tetrahedron, oldPoints[3], oldPoints[0], oldPoints[2], oldPoints[1]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[1], oldNeighbors[2], oldNeighbors[0], oldNeighbors[3]);
            }
            else if(2 == pi)
            {
                //0->2, 1->1, 2->3, 3->0
                SetTetrahedronPoints(tetrahedron, oldPoints[3], oldPoints[1], oldPoints[0], oldPoints[2]);
                SetTetrahedronNeighbors(tetrahedron, oldNeighbors[3], oldNeighbors[1], oldNeighbors[0], oldNeighbors[2]);
            }
        }
    }
}

}
