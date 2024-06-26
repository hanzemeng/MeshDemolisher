using System.Linq;
using System.Collections.Generic;

namespace Hanzzz.MeshDemolisher
{

public partial class DelaunayTetrahedralization
{
    private void FilterOuterTetrahedron()
    {   
        neighborSeparation = CalculateNeighborSeparation();   
        List<int> inTetrahedron = Enumerable.Range(0,tetrahedrons.Count/4).Select(i=>0).ToList(); // 0 unknown, 1 in, -1 out

        searchQueue.Clear();
        visitedTetrahedrons.Clear();
        visitedTetrahedrons.Add(-1);
        for(int i=0; i<tetrahedrons.Count; i+=4)
        {
            if(HasGhostPoint(i/4))
            {
                inTetrahedron[i/4] = -1;
                searchQueue.Enqueue(i/4);
            }
        }

        while(0 != searchQueue.Count)
        {
            int current = searchQueue.Dequeue();
            if(visitedTetrahedrons.Contains(current))
            {
                continue;
            }
            visitedTetrahedrons.Add(current);

            Int4 currentPoints = GetTetrahedronPoints(current);
            Int4 currentNeighbors = GetTetrahedronNeighbors(current);
            for(int i=0; i<4; i++)
            {
                if(-1 == currentNeighbors[i] || 0 != inTetrahedron[currentNeighbors[i]])
                {
                    continue;
                }

                if(neighborSeparation[4*current+i])
                {
                    inTetrahedron[currentNeighbors[i]] = -1*inTetrahedron[current];
                }
                else
                {
                    inTetrahedron[currentNeighbors[i]] = inTetrahedron[current];
                }
                searchQueue.Enqueue(currentNeighbors[i]);

            }
        }

        for(int i=0; i<inTetrahedron.Count; i++)
        {
            if(-1 == inTetrahedron[i])
            {
                RemoveTetrahedron(i);
            }
        }
    }

    private List<bool> CalculateNeighborSeparation()
    {
        List<bool> res =  Enumerable.Range(0, tetrahedronNeighbors.Count).Select(i=>false).ToList();

        Dictionary<int,Sign> orientCache = new Dictionary<int, Sign>();

        searchQueue.Clear();
        for(int i=0; i<inputFaces.Count; i++)
        {
            orientCache.Clear();
            int tri0 = inputTriangles[3*inputFaces[i][0]];
            int tri1 = inputTriangles[3*inputFaces[i][0]+1];
            int tri2 = inputTriangles[3*inputFaces[i][0]+2];

            HashSet<int> possibleTetrahedorns = new HashSet<int>();

            (int, int) e0 = inputFacesBoundEdges[i].First();
            int startTetrahedron = -1;
            List<int> edgeTetrahedrons = FindTetrahedronIncident(e0.Item1,e0.Item2);
            foreach(int tetrahedron in edgeTetrahedrons)
            {
                int p3 = -1;
                int p4 = -1;
                Int4 tetrahedronPoints = GetTetrahedronPoints(tetrahedron);
                for(int j=0; j<4; j++)
                {
                    if(e0.Item1 != tetrahedronPoints[j] && e0.Item2 != tetrahedronPoints[j])
                    {
                        if(-1 == p3)
                        {
                            p3 = tetrahedronPoints[j];
                        }
                        else
                        {
                            p4 = tetrahedronPoints[j];
                            break;
                        }
                    }
                }
                if(TriangleInFace(e0.Item1, e0.Item2, p3, i) || TriangleInFace(e0.Item1, e0.Item2, p4, i))
                {
                    startTetrahedron = tetrahedron;
                    break;
                }
            }
            searchQueue.Enqueue(startTetrahedron);
            possibleTetrahedorns.Add(startTetrahedron);
            foreach(int p in inputFacesFlatPoints[i])
            {
                FindTetrahedronIncident(p)
                .ForEach(x => { searchQueue.Enqueue(x); possibleTetrahedorns.Add(x); });
            }

            visitedTetrahedrons.Clear();
            visitedTetrahedrons.Add(-1);

            while(0 != searchQueue.Count)
            {
                int current = searchQueue.Dequeue();
                if(visitedTetrahedrons.Contains(current))
                {
                    continue;
                }
                visitedTetrahedrons.Add(current);

                Int4 currentPoints = GetTetrahedronPoints(current);
                Int4 currentNeighbors = GetTetrahedronNeighbors(current);
                for(int j=0; j<4; j++)
                {
                    int t0 = currentPoints[TETRAHEDRON_FACET[j,0]];
                    int t1 = currentPoints[TETRAHEDRON_FACET[j,1]];
                    int t2 = currentPoints[TETRAHEDRON_FACET[j,2]];
                    List<bool> onBound = new List<bool>{
                        inputFacesBoundPoints[i].Contains(t0) || inputFacesFlatPoints[i].Contains(t0),
                        inputFacesBoundPoints[i].Contains(t1) || inputFacesFlatPoints[i].Contains(t1),
                        inputFacesBoundPoints[i].Contains(t2) || inputFacesFlatPoints[i].Contains(t2)};
                    List<int> pointOrient = new List<int>
                    {
                        (int)OrientCache(t0, tri0, tri1, tri2, orientCache),
                        (int)OrientCache(t1, tri0, tri1, tri2, orientCache),
                        (int)OrientCache(t2, tri0, tri1, tri2, orientCache),
                    };

                    if(onBound[0] && onBound[1] && onBound[2])
                    {
                        searchQueue.Enqueue(currentNeighbors[j]);
                        possibleTetrahedorns.Add(currentNeighbors[j]);
                    }
                    else if(onBound[0] && onBound[1])
                    {
                        if(!inputFacesBoundEdges[i].Contains((t0,t1)) && !inputFacesBoundEdges[i].Contains((t1,t0)))
                        {
                            searchQueue.Enqueue(currentNeighbors[j]);
                            possibleTetrahedorns.Add(currentNeighbors[j]);
                        }
                    }
                    else if(onBound[1] && onBound[2])
                    {
                        if(!inputFacesBoundEdges[i].Contains((t1,t2)) && !inputFacesBoundEdges[i].Contains((t2,t1)))
                        {
                            searchQueue.Enqueue(currentNeighbors[j]);
                            possibleTetrahedorns.Add(currentNeighbors[j]);
                        }
                    }
                    else if(onBound[2] && onBound[0])
                    {
                        if(!inputFacesBoundEdges[i].Contains((t2,t0)) && !inputFacesBoundEdges[i].Contains((t0,t2)))
                        {
                            searchQueue.Enqueue(currentNeighbors[j]);
                            possibleTetrahedorns.Add(currentNeighbors[j]);
                        }
                    }
                    else if(onBound[0])
                    {
                        if(pointOrient[1]*pointOrient[2] < 0)
                        {
                            searchQueue.Enqueue(currentNeighbors[j]);
                            possibleTetrahedorns.Add(currentNeighbors[j]);
                        }
                    }
                    else if(onBound[1])
                    {
                        if(pointOrient[2]*pointOrient[0] < 0)
                        {
                            searchQueue.Enqueue(currentNeighbors[j]);
                            possibleTetrahedorns.Add(currentNeighbors[j]);
                        }
                    }
                    else if(onBound[2])
                    {
                        if(pointOrient[0]*pointOrient[1] < 0)
                        {
                            searchQueue.Enqueue(currentNeighbors[j]);
                            possibleTetrahedorns.Add(currentNeighbors[j]);
                        }
                    }
                    else
                    {
                        if(pointOrient[1]*pointOrient[2] < 0 || pointOrient[2]*pointOrient[0] < 0 || pointOrient[0]*pointOrient[1] < 0)
                        {
                            searchQueue.Enqueue(currentNeighbors[j]);
                            possibleTetrahedorns.Add(currentNeighbors[j]);
                        }
                    }
                }
            }

            possibleTetrahedorns.Remove(-1);
            foreach(int tetrahedron in possibleTetrahedorns)
            {
                Int4 tetrahedronPoints = GetTetrahedronPoints(tetrahedron);
                List<Sign> pointOrient = new List<Sign>
                {
                    OrientCache(tetrahedronPoints[0], tri0, tri1, tri2, orientCache),
                    OrientCache(tetrahedronPoints[1], tri0, tri1, tri2, orientCache),
                    OrientCache(tetrahedronPoints[2], tri0, tri1, tri2, orientCache),
                    OrientCache(tetrahedronPoints[3], tri0, tri1, tri2, orientCache)
                };

                for(int j=0; j<4; j++)
                {
                    int t0 = tetrahedronPoints[TETRAHEDRON_FACET[j,0]];
                    int t1 = tetrahedronPoints[TETRAHEDRON_FACET[j,1]];
                    int t2 = tetrahedronPoints[TETRAHEDRON_FACET[j,2]];
                    //if(TriangleInFace(t0,t1,t2, i))
                    //{
                    //    res[4*tetrahedron+j] = true;
                    //    break;
                    //}
                    if(Sign.ZERO == pointOrient[TETRAHEDRON_FACET[j,0]] && Sign.ZERO == pointOrient[TETRAHEDRON_FACET[j,1]] && Sign.ZERO == pointOrient[TETRAHEDRON_FACET[j,2]])
                    {
                        res[4*tetrahedron+j] = true;
                        break;
                    }
                }
            }
        }

        return res;
    }

    private bool TriangleInFace(int t0, int t1, int t2, int f)
    {
        if((!inputFacesBoundPoints[f].Contains(t0) && !inputFacesFlatPoints[f].Contains(t0)) ||
           (!inputFacesBoundPoints[f].Contains(t1) && !inputFacesFlatPoints[f].Contains(t1)) ||
           (!inputFacesBoundPoints[f].Contains(t2) && !inputFacesFlatPoints[f].Contains(t2)))
        {
            return false;
        }

        foreach(int tri in inputFaces[f])
        {
            List<int> triPs = new List<int>{inputTriangles[3*tri],inputTriangles[3*tri+1],inputTriangles[3*tri+2]};
            if(!triPs.Contains(t0) || !triPs.Contains(t1) || !triPs.Contains(t2))
            {
                continue;
            }
            return true;
        }

        List<int> inTriPs = new List<int>{t0,t1,t2};
        for(int j=0; j<3; j++)
        {
            foreach(var edge in inputFacesFlatEdges[f])
            {
                if((edge.Item1 == inTriPs[j] && edge.Item2 == inTriPs[(j+1)%3]) || (edge.Item2 == inTriPs[j] && edge.Item1 == inTriPs[(j+1)%3]))
                {
                    goto NEXT;
                }
                if(edge.Item1 == inTriPs[j] || edge.Item2 == inTriPs[(j+1)%3] || edge.Item2 == inTriPs[j] || edge.Item1 == inTriPs[(j+1)%3])
                {
                    continue;
                }

                if(Sign.POSITIVE == SegmentCrossInnerSegment(inTriPs[j], inTriPs[(j+1)%3], edge.Item1, edge.Item2))
                {
                    return true;
                }
            }
            NEXT:
            continue;
        }
        if(0 == inputFacesFlatPoints[f].Count)
        {
            return false;
        }
            
        for(int j=0; j<3; j++)
        {
            if(inputFacesFlatPoints[f].Contains(inTriPs[j]))
            {
                return true;
            }
        }
        for(int j=0; j<3; j++)
        {
            foreach(var point in inputFacesFlatPoints[f])
            {
                if(Sign.POSITIVE == PointInInnerSegment(point, inTriPs[j], inTriPs[(j+1)%3]))
                {
                    return true;
                }
            }
        }
        return false;
    }
}

}
