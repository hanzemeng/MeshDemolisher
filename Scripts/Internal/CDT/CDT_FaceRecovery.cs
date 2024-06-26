using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public partial class DelaunayTetrahedralization
{
    private void CalculateFaceInformation()
    {
        HashSet<int> addedTriangle = new HashSet<int>();
        DisjointSet<int> flatTriangles = new DisjointSet<int>(EqualityComparer<int>.Default);
        foreach((int, int) edge in flatInputEdges)
        {
            Int4 triangles = inputEdges[edge];
            flatTriangles.MakeSet(triangles[0]);
            flatTriangles.MakeSet(triangles[2]);
            flatTriangles.Union(triangles[0],triangles[2]);
            addedTriangle.Add(triangles[0]);
            addedTriangle.Add(triangles[2]);
        }
        inputFaces = flatTriangles.GetAllDisjointSet();

        for(int i=0; i<inputTriangles.Count; i+=3)
        {
            if(addedTriangle.Contains(i/3))
            {
                continue;
            }
            inputFaces.Add(new List<int>{i/3});
        }
        
        inputFacesFlatEdges = Enumerable.Range(0, inputFaces.Count).Select(i=>new HashSet<(int,int)>()).ToList();

        int[] triPs = new int[3];

        for(int i=0; i<inputFaces.Count; i++)
        {
            List<int> face = inputFaces[i];
            HashSet<(int,int)> faceEdges = new HashSet<(int, int)>();
            HashSet<int> facePoints = new HashSet<int>();

            foreach(int tri in face)
            {
                triPs[0] = inputTriangles[3*tri];
                triPs[1] = inputTriangles[3*tri+1];
                triPs[2] = inputTriangles[3*tri+2];
                for(int j=0; j<3; j++)
                {
                    int e0 = triPs[j];
                    int e1 = triPs[(j+1)%3];
                    facePoints.Add(e0);
                    facePoints.Add(e1);
                    if(e0>e1)
                    {
                        int temp = e0;
                        e0 = e1;
                        e1 = temp;
                    }

                    if(faceEdges.Contains((e0,e1)))
                    {
                        faceEdges.Remove((e0,e1));
                        inputFacesFlatEdges[i].Add((e0,e1));
                    }
                    else
                    {
                        faceEdges.Add((e0,e1));
                    }
                }
            }
            inputFacesBoundEdges.Add(faceEdges);

            HashSet<int> boundPoints = new HashSet<int>();
            foreach(var edge in faceEdges)
            {
                facePoints.Remove(edge.Item1);
                facePoints.Remove(edge.Item2);
                boundPoints.Add(edge.Item1);
                boundPoints.Add(edge.Item2);
            }
            inputFacesBoundPoints.Add(boundPoints);
            inputFacesFlatPoints.Add(facePoints);
        }

        //Transform par = GameObject.Find("f").transform;
        //GameObject p = GameObject.Find("p");
        //for(int i = 22; i<23; i++)
        //{
        //    List<int> face = inputFaces[i];
        //    List<Vector3> vector3s = new List<Vector3>();
        //    //Debug.Log(face.Count);
        //    foreach(int tri in face)
        //    {
        //        vector3s.Add(points[inputTriangles[tri*3]].ToPoint3D().ToVector3());
        //        vector3s.Add(points[inputTriangles[tri*3+1]].ToPoint3D().ToVector3());
        //        vector3s.Add(points[inputTriangles[tri*3+2]].ToPoint3D().ToVector3());
        //    }
        //    List<int> tris = Enumerable.Range(0,vector3s.Count).Select(i=>i).ToList();

        //    Mesh m = new Mesh();
        //    m.vertices = vector3s.ToArray();
        //    m.triangles = tris.ToArray();
        //    GameObject g = new GameObject();
        //    g.name = i.ToString();
        //    g.AddComponent<MeshRenderer>();
        //    (g.AddComponent<MeshFilter>()).mesh = m;
        //    g.transform.SetParent(par);

        //    foreach(int bp in inputFacesBoundPoints[i])
        //    {
        //        g = Object.Instantiate(p);
        //        g.transform.position = points[bp].ToPoint3D().ToVector3();
        //        g.transform.SetParent(par,true);
        //    }
        //}
    }

    private void FaceRecovery()
    {
        Dictionary<int, Sign> pointsOrient = new Dictionary<int, Sign>();
        HashSet<int> topPoints = new HashSet<int>();
        HashSet<int> bottomPoints = new HashSet<int>();
        HashSet<int> facePoints = new HashSet<int>();
        Dictionary<(int,int,int),(int,int)> topBoundingTriangles = new Dictionary<(int, int, int), (int, int)>(new Tuple3IntComparator()); // key: triangle, value: tetrahedron, face index
        Dictionary<(int,int,int),(int,int)> bottomBoundingTriangles = new Dictionary<(int, int, int), (int, int)>(new Tuple3IntComparator()); // key: triangle, value: tetrahedron, face index

        for(int f=0; f<inputFaces.Count; f++)
        {
            List<int> intersectTetrahedron = FindIntersectTetrahedron(f);
            if(0 == intersectTetrahedron.Count)
            {
                continue;
            }

            //if(22 == f)
            //{
            //    Transform par = GameObject.Find("f").transform;

            //    foreach(int t in intersectTetrahedron)
            //    {
            //        Int4 ps = GetTetrahedronPoints(t);
            //        List<Vector3> vector3s = new List<Vector3>();
            //        vector3s.Add(points[ps[0]].ToPoint3D().ToVector3());
            //        vector3s.Add(points[ps[1]].ToPoint3D().ToVector3());
            //        vector3s.Add(points[ps[2]].ToPoint3D().ToVector3());
            //        vector3s.Add(points[ps[3]].ToPoint3D().ToVector3());

            //        Mesh m = new Mesh();
            //        List<int> tris = new List<int>{2,1,0, 3,2,0, 3,1,2, 1,3,0};
            //        m.vertices = vector3s.ToArray();
            //        m.triangles = tris.ToArray();
            //        GameObject g = new GameObject();
            //        g.AddComponent<MeshRenderer>();
            //        (g.AddComponent<MeshFilter>()).mesh = m;
            //        g.transform.SetParent(par);
            //    }

            //    //HashSet<int> ts = new HashSet<int>();
            //    //foreach(int t in intersectTetrahedron)
            //    //{
            //    //    Int4 n = GetTetrahedronNeighbors(t);
            //    //    ts.Add(n[0]);
            //    //    ts.Add(n[1]);
            //    //    ts.Add(n[2]);
            //    //    ts.Add(n[3]);
            //    //}

            //    //    //par = GameObject.Find("t").transform;
            //    //    //ts.Remove(-1);
            //    //    //foreach(int t in intersectTetrahedron)
            //    //    //{
            //    //    //    ts.Remove(t);
            //    //    //}
            //    //    //foreach(int t in ts)
            //    //    //{
            //    //    //    Int4 ps = GetTetrahedronPoints(t);
            //    //    //    List<Vector3> vector3s = new List<Vector3>();
            //    //    //    vector3s.Add(points[ps[0]].ToPoint3D().ToVector3());
            //    //    //    vector3s.Add(points[ps[1]].ToPoint3D().ToVector3());
            //    //    //    vector3s.Add(points[ps[2]].ToPoint3D().ToVector3());
            //    //    //    vector3s.Add(points[ps[3]].ToPoint3D().ToVector3());

            //    //    //    Mesh m = new Mesh();
            //    //    //    List<int> tris = new List<int>{2,1,0, 3,2,0, 3,1,2, 1,3,0};
            //    //    //    m.vertices = vector3s.ToArray();
            //    //    //    m.triangles = tris.ToArray();
            //    //    //    GameObject g = new GameObject();
            //    //    //    g.AddComponent<MeshRenderer>();
            //    //    //    (g.AddComponent<MeshFilter>()).mesh = m;
            //    //    //    g.transform.SetParent(par);
            //    //    //}

            //    //    //throw new System.Exception();

            //}

            int tri0 = inputTriangles[3*inputFaces[f][0]];
            int tri1 = inputTriangles[3*inputFaces[f][0]+1];
            int tri2 = inputTriangles[3*inputFaces[f][0]+2];

            pointsOrient.Clear();
            topPoints.Clear();
            bottomPoints.Clear();
            facePoints.Clear();
            topBoundingTriangles.Clear();
            bottomBoundingTriangles.Clear();

            foreach(int tetrahedron in intersectTetrahedron)
            {
                Int4 tetrahedronPoints = GetTetrahedronPoints(tetrahedron);
                for(int i=0; i<4; i++)
                {
                    if(pointsOrient.ContainsKey(tetrahedronPoints[i]))
                    {
                        continue;
                    }

                    pointsOrient[tetrahedronPoints[i]] = Orient(tri0, tri1, tri2, tetrahedronPoints[i]);
                    if(Sign.POSITIVE == pointsOrient[tetrahedronPoints[i]])
                    {
                        topPoints.Add(tetrahedronPoints[i]);
                    }
                    else if(Sign.NEGATIVE == pointsOrient[tetrahedronPoints[i]])
                    {
                        bottomPoints.Add(tetrahedronPoints[i]);
                    }
                    else
                    {
                        facePoints.Add(tetrahedronPoints[i]);
                    }
                }
            }

            foreach(int tetrahedron in intersectTetrahedron)
            {
                Int4 tetrahedronPoints = GetTetrahedronPoints(tetrahedron);
                Int4 tetrahedronNeighbors = GetTetrahedronNeighbors(tetrahedron);
                for(int i=0; i<4; i++)
                {
                    if(intersectTetrahedron.Contains(tetrahedronNeighbors[i]))
                    {
                        continue;
                    }

                    int p0 = tetrahedronPoints[TETRAHEDRON_FACET[i,0]];
                    int p1 = tetrahedronPoints[TETRAHEDRON_FACET[i,1]];
                    int p2 = tetrahedronPoints[TETRAHEDRON_FACET[i,2]];

                    if(0 <= (int)pointsOrient[p0] && 0 <= (int)pointsOrient[p1] && 0 <= (int)pointsOrient[p2])
                    {
                        for(int j = 0; j<4; j++)
                        {
                            Int3 neighborFace = GetTetrahedronPoints(tetrahedronNeighbors[i], j);
                            if(!neighborFace.Contains(p0) || !neighborFace.Contains(p1) || !neighborFace.Contains(p2))
                            {
                                continue;
                            }
                            topBoundingTriangles[(p0, p1, p2)] = (tetrahedronNeighbors[i], j);
                            break;
                        }
                    }
                    else if(0 >= (int)pointsOrient[p0] && 0 >= (int)pointsOrient[p1] && 0 >= (int)pointsOrient[p2])
                    {
                        for(int j = 0; j<4; j++)
                        {
                            Int3 neighborFace = GetTetrahedronPoints(tetrahedronNeighbors[i], j);
                            if(!neighborFace.Contains(p0) || !neighborFace.Contains(p1) || !neighborFace.Contains(p2))
                            {
                                continue;
                            }
                            bottomBoundingTriangles[(p0, p1, p2)] = (tetrahedronNeighbors[i], j);
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("bounding face in the middle");
                        return;
                    }
                }
            }
            List<(int,int,int)> originalTopBoundingTriangles = topBoundingTriangles.Keys.ToList();
            List<(int,int,int)> originalBottomBoundingTriangles = bottomBoundingTriangles.Keys.ToList();

            foreach(int tetrahedron in intersectTetrahedron)
            {
                RemoveTetrahedron(tetrahedron);
            }

            List<int> boundPoints = topPoints.Union(facePoints).ToList();
            Dictionary<int, List<Sign>> pointsBoundFaceOrient = new Dictionary<int, List<Sign>>();

            while(0 != topBoundingTriangles.Count)
            {
                //using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter("/Users/hanzemeng/Desktop/out.txt"))
                //{
                //    outputFile.WriteLine(topBoundingTriangles.Count);
                //}

                var current = topBoundingTriangles.First();
                topBoundingTriangles.Remove(current.Key);
                if(facePoints.Contains(current.Key.Item1) && facePoints.Contains(current.Key.Item2) && facePoints.Contains(current.Key.Item3))
                {
                    bottomBoundingTriangles[current.Key] = current.Value;
                    continue;
                }

                ConnectBoundTriangle(current, topBoundingTriangles, boundPoints, originalTopBoundingTriangles, pointsBoundFaceOrient);
            }

            //foreach(var v in bottomBoundingTriangles)
            //{
            //    Transform par = GameObject.Find("t").transform;
            //    List<Vector3> vector3s = new List<Vector3>();
            //    vector3s.Add(points[v.Key.Item3].ToPoint3D().ToVector3());
            //    vector3s.Add(points[v.Key.Item2].ToPoint3D().ToVector3());
            //    vector3s.Add(points[v.Key.Item1].ToPoint3D().ToVector3());

            //    Mesh m = new Mesh();
            //    List<int> tris = new List<int>{0,1,2};
            //    m.vertices = vector3s.ToArray();
            //    m.triangles = tris.ToArray();
            //    GameObject g = new GameObject();
            //    g.AddComponent<MeshRenderer>();
            //    (g.AddComponent<MeshFilter>()).mesh = m;
            //    g.transform.SetParent(par);
            //}
            //throw new System.Exception();

            boundPoints = bottomPoints.Union(facePoints).ToList();
            pointsBoundFaceOrient.Clear();

            while(0 != bottomBoundingTriangles.Count)
            {
                var current = bottomBoundingTriangles.First();
                bottomBoundingTriangles.Remove(current.Key);

                ConnectBoundTriangle(current, bottomBoundingTriangles, boundPoints, originalBottomBoundingTriangles, pointsBoundFaceOrient);
            }
        }
    }

    private List<int> FindIntersectTetrahedron(int face)
    {
        int tri0 = inputTriangles[3*inputFaces[face][0]];
        int tri1 = inputTriangles[3*inputFaces[face][0]+1];
        int tri2 = inputTriangles[3*inputFaces[face][0]+2];

        Dictionary<int,Sign> orientCache = new Dictionary<int, Sign>();

        (int, int) e0 = inputFacesBoundEdges[face].First();
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

            Sign orient3 = OrientCache(p3, tri0, tri1, tri2, orientCache);
            Sign orient4 = OrientCache(p4, tri0, tri1, tri2, orientCache);
            if(Sign.ZERO != orient3 && orient3 == orient4)
            {
                continue;
            }

            if(((int)orient3 * (int)orient4) < 0)
            {
                foreach(int triangle in inputFaces[face])
                {
                    if(Sign.POSITIVE == LineCrossTriangle(p3,p4,inputTriangles[3*triangle],inputTriangles[3*triangle+1],inputTriangles[3*triangle+2]))
                    {
                        startTetrahedron = tetrahedron;
                        goto FOUND;
                    }
                }
            }

            if(Sign.ZERO == orient3 && TriangleInFace(e0.Item1, e0.Item2, p3, face))
            {
                startTetrahedron = tetrahedron;
                goto FOUND;
            }
            if(Sign.ZERO == orient4 && TriangleInFace(e0.Item1, e0.Item2, p4, face))
            {
                startTetrahedron = tetrahedron;
                goto FOUND;
            }
        }
        FOUND:

        HashSet<int> possibleTetrahedorns = new HashSet<int>();
        searchQueue.Clear();
        searchQueue.Enqueue(startTetrahedron);
        possibleTetrahedorns.Add(startTetrahedron);
        foreach(int p in inputFacesFlatPoints[face])
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
                    inputFacesBoundPoints[face].Contains(t0) || inputFacesFlatPoints[face].Contains(t0),
                    inputFacesBoundPoints[face].Contains(t1) || inputFacesFlatPoints[face].Contains(t1),
                    inputFacesBoundPoints[face].Contains(t2) || inputFacesFlatPoints[face].Contains(t2)};
                List<int> pointOrient  = new List<int>
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
                    if(!inputFacesBoundEdges[face].Contains((t0,t1)) && !inputFacesBoundEdges[face].Contains((t1,t0)))
                    {
                        searchQueue.Enqueue(currentNeighbors[j]);
                        possibleTetrahedorns.Add(currentNeighbors[j]);
                    }
                }
                else if(onBound[1] && onBound[2])
                {
                    if(!inputFacesBoundEdges[face].Contains((t1,t2)) && !inputFacesBoundEdges[face].Contains((t2,t1)))
                    {
                        searchQueue.Enqueue(currentNeighbors[j]);
                        possibleTetrahedorns.Add(currentNeighbors[j]);
                    }
                }
                else if(onBound[2] && onBound[0])
                {
                    if(!inputFacesBoundEdges[face].Contains((t2,t0)) && !inputFacesBoundEdges[face].Contains((t0,t2)))
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

        List<int> res = new List<int>();

        possibleTetrahedorns.Remove(-1);
        foreach(int tetrahedron in possibleTetrahedorns)
        {
            //if(HasGhostPoint(tetrahedron))
            //{
            //    continue;
            //}
            Int4 tetrahedronPoints = GetTetrahedronPoints(tetrahedron);
            List<int> pointOrient = new List<int>
            {
                (int)OrientCache(tetrahedronPoints[0], tri0, tri1, tri2, orientCache),
                (int)OrientCache(tetrahedronPoints[1], tri0, tri1, tri2, orientCache),
                (int)OrientCache(tetrahedronPoints[2], tri0, tri1, tri2, orientCache),
                (int)OrientCache(tetrahedronPoints[3], tri0, tri1, tri2, orientCache)
            };
            if(!(
                (pointOrient[0]>=0 && pointOrient[1]>=0 && pointOrient[2]>=0 && pointOrient[3]>=0) ||
                (pointOrient[0]<=0 && pointOrient[1]<=0 && pointOrient[2]<=0 && pointOrient[3]<=0)
                ))
            {
                res.Add(tetrahedron);
            }
        }

        return res;
    }

    private void ConnectBoundTriangle(KeyValuePair<(int,int,int),(int,int)> boundTriangle, Dictionary<(int,int,int),(int,int)> currentBound, List<int> originalBoundPoints, List<(int,int,int)> originalBound, Dictionary<int, List<Sign>> pointsOrient)
    {
        int p0 = boundTriangle.Key.Item1;
        int p1 = boundTriangle.Key.Item2;
        int p2 = boundTriangle.Key.Item3;
        int pRes = -1;


        bool[,] triSameTet = new bool[3,4];
        Sign[,] triOrientFace = new Sign[3,4];
        int index;
        for(index=0; index<originalBoundPoints.Count; index++)
        {
            if(p0 == originalBoundPoints[index] || p1 == originalBoundPoints[index] || p2 == originalBoundPoints[index])
            {
                continue;
            }

            int p3 = originalBoundPoints[index];
            if(Sign.POSITIVE != Orient(p0,p1,p2,p3) || IsIntersecting(p0,p1,p2,p3, currentBound,triSameTet,triOrientFace) || !IsLocallyDelaunay(p0,p1,p2,p3, originalBoundPoints, originalBound, pointsOrient))
            {
                continue;
            }
            pRes = p3;
            break;
        }
        if(-1 == pRes)
        {
            Debug.LogWarning("Gift Wrapping no point");
            throw new System.Exception();
        }
        for(index=index+1; index<originalBoundPoints.Count; index++)
        {
            if(p0 == originalBoundPoints[index] || p1 == originalBoundPoints[index] || p2 == originalBoundPoints[index])
            {
                continue;
            }

            int p3 = originalBoundPoints[index];
            if(Sign.POSITIVE != InCircumsphere(p0,p1,p2,pRes,p3))
            {
                continue;
            }
            if(Sign.POSITIVE != Orient(p0,p1,p2,p3) || IsIntersecting(p0,p1,p2,p3, currentBound,triSameTet,triOrientFace) || !IsLocallyDelaunay(p0,p1,p2,p3, originalBoundPoints, originalBound, pointsOrient))
            {
                continue;
            }
            pRes = p3;
        }


        //{0,1,2}, {0,2,3}, {2,1,3}, {0,3,1}
        int t0 = CreateNewTetrahedron(p0,p1,p2,pRes);

        //Transform par = GameObject.Find("t").transform;
        //Int4 ps = GetTetrahedronPoints(t0);
        //List<Vector3> vector3s = new List<Vector3>();
        //vector3s.Add(points[ps[0]].ToPoint3D().ToVector3());
        //vector3s.Add(points[ps[1]].ToPoint3D().ToVector3());
        //vector3s.Add(points[ps[2]].ToPoint3D().ToVector3());
        //vector3s.Add(points[ps[3]].ToPoint3D().ToVector3());

        //Mesh m = new Mesh();
        //List<int> tris = new List<int>{2,1,0, 3,2,0, 3,1,2, 1,3,0};
        //m.vertices = vector3s.ToArray();
        //m.triangles = tris.ToArray();
        //GameObject g = new GameObject();
        //g.AddComponent<MeshRenderer>();
        //(g.AddComponent<MeshFilter>()).mesh = m;
        //g.transform.SetParent(par);

        incidentTetrahedrons[p0] = t0;
        incidentTetrahedrons[p1] = t0;
        incidentTetrahedrons[p2] = t0;
        incidentTetrahedrons[pRes] = t0;
        SetTetrahedronNeighbors(boundTriangle.Value.Item1, boundTriangle.Value.Item2, t0);
        SetTetrahedronNeighbors(t0, 0, boundTriangle.Value.Item1);

        Int4 t0Points = GetTetrahedronPoints(t0);
        for(int i=1; i<4; i++)
        {
            p0 = t0Points[TETRAHEDRON_FACET[i, 0]];
            p1 = t0Points[TETRAHEDRON_FACET[i, 1]];
            p2 = t0Points[TETRAHEDRON_FACET[i, 2]];

            if(currentBound.ContainsKey((p0,p1,p2)))
            {
                (int,int) neighbor = currentBound[(p0,p1,p2)];
                SetTetrahedronNeighbors(neighbor.Item1, neighbor.Item2, t0);
                SetTetrahedronNeighbors(t0, i, neighbor.Item1);

                currentBound.Remove((p0,p1,p2));
            }
            else
            {
                currentBound[(p2,p1,p0)] = (t0, i);
            }
        }
    }

    private bool IsIntersecting(int p0, int p1, int p2, int p3, Dictionary<(int,int,int),(int,int)> currentBound, bool[,] triSameTet, Sign[,] triOrientFace)
    {
        Int4 tetPs = new Int4(p0,p1,p2,p3);
        Group4<Sign> face0OrientTri = new Group4<Sign>();

        foreach(var bound in currentBound)
        {
            Int3 triPs = new Int3(bound.Key.Item1,bound.Key.Item2,bound.Key.Item3);
            
            for(int i=0; i<3; i++)
            {
                for(int j=0; j<4; j++)
                {
                    triSameTet[i,j] = triPs[i] == tetPs[j];
                }
            }

            int onFace = 0;
            for(int i=0; i<3; i++)
            {
                for(int j=0; j<4; j++)
                {
                    if(triSameTet[i,j])
                    {
                        onFace++;
                        break;
                    }
                }
            }
            if(3 == onFace)
            {
                goto NEXT_BOUND;
            }

            //{0,1,2}, {0,2,3}, {2,1,3}, {0,3,1}
            //{3,1,0,2}
            
            for(int i=0; i<4; i++)
            {
                int tetP0 = tetPs[TETRAHEDRON_FACET[i,0]];
                int tetP1 = tetPs[TETRAHEDRON_FACET[i,1]];
                int tetP2 = tetPs[TETRAHEDRON_FACET[i,2]];

                for(int j=0; j<3; j++)
                {
                    if(triSameTet[j, TETRAHEDRON_FACET[i,0]] || triSameTet[j, TETRAHEDRON_FACET[i,1]] || triSameTet[j, TETRAHEDRON_FACET[i,2]])
                    {
                        triOrientFace[j,i] = Sign.ZERO;
                    }
                    else if(triSameTet[j, TETRAHEDRON_FACET_PIVOT[i]])
                    {
                        triOrientFace[j,i] = Sign.POSITIVE;
                    }
                    else
                    {
                        triOrientFace[j,i] = Orient(tetP0, tetP1, tetP2, triPs[j]);
                    }
                }

                bool inHalfSpace = true;
                if(0 == i)
                {
                    for(int j=0; j<3; j++)
                    {
                        if(Sign.POSITIVE == triOrientFace[j,i])
                        {
                            inHalfSpace = false;
                            break;
                        }
                    }
                }
                else
                {
                    for(int j=0; j<3; j++)
                    {
                        if(Sign.NEGATIVE != triOrientFace[j,i])
                        {
                            inHalfSpace = false;
                            break;
                        }
                    }
                }
                if(inHalfSpace)
                {
                    goto NEXT_BOUND;
                }
            }

            
            //Sign[] face0OrientTri = new Sign[4];
            for(int i=0; i<4; i++)
            {
                if(triSameTet[0,i] || triSameTet[1,i] || triSameTet[2,i])
                {
                    face0OrientTri[i] = Sign.ZERO;
                }
                else
                {
                    face0OrientTri[i] = Orient(triPs[0],triPs[1],triPs[2],tetPs[i]);
                }
            }

            bool inHalfSpaceNegative = true;
            bool inHalfSpacePositive = true;
            for(int i=0; i<4; i++)
            {
                if(Sign.POSITIVE == face0OrientTri[i])
                {
                    inHalfSpaceNegative = false;
                }
                if(Sign.NEGATIVE == face0OrientTri[i])
                {
                    inHalfSpacePositive = false;
                }
            }
            if(inHalfSpaceNegative || inHalfSpacePositive)
            {
                goto NEXT_BOUND;
            }


            for(int i=0; i<3; i++)
            {
                for(int j=0; j<3; j++)
                {
                    if(Sign.ZERO == Orient(triPs[j],triPs[(j+1)%3], tetPs[i], tetPs[3]) && Sign.POSITIVE == SegmentCrossInnerSegment(triPs[j],triPs[(j+1)%3], tetPs[i], tetPs[3]))
                    {
                        return true;
                    }
                }
            }

            //{0,1,2}, {0,2,3}, {2,1,3}, {0,3,1}
            for(int i=1; i<4; i++)
            {
                int i0 = TETRAHEDRON_FACET[i,0];
                int i1 = TETRAHEDRON_FACET[i,1];
                int i2 = TETRAHEDRON_FACET[i,2];
                for(int j=0; j<3; j++)
                {
                    if(Sign.POSITIVE == InnerSegmentCrossInnerTriangle(triPs[j], triPs[(j+1)%3], tetPs[i0], tetPs[i1], tetPs[i2]))
                    {
                        return true;
                    }
                }
            }

            for(int i=0; i<3; i++)
            {
                if(Sign.POSITIVE == InnerSegmentCrossInnerTriangle(tetPs[i], tetPs[3], triPs[0], triPs[1], triPs[2]))
                {
                    return true;
                }
            }

            //Sign[,] triOrientFace = new Sign[3,4];
            for(int i=0; i<3; i++)
            {
                for(int j=1; j<4; j++)
                {
                    if(Sign.ZERO == triOrientFace[i, j])
                    {
                        int i0 = TETRAHEDRON_FACET[j,0];
                        int i1 = TETRAHEDRON_FACET[j,1];
                        int i2 = TETRAHEDRON_FACET[j,2];
                        if(Sign.POSITIVE == PointInInnerTriangle(triPs[i], tetPs[i0], tetPs[i1], tetPs[i2]))
                        {
                            return true;
                        }    
                    }
                }
            }

            NEXT_BOUND:
            continue;
        }

        return false;
    }

    private bool IsLocallyDelaunay(int p0, int p1, int p2, int p3, List<int> originalBoundPoints, List<(int,int,int)> originalBound, Dictionary<int, List<Sign>> pointsOrient)
    {
        Int4 tetPs = new Int4(p0,p1,p2,p3);

        foreach(int point in originalBoundPoints)
        {
            for(int i=0; i<4; i++)
            {
                if(point == tetPs[i])
                {
                    goto NEXT_POINT;
                }
            }

            Sign inCircumsphere = InCircumsphereSymbolicPerturbation(p0,p1,p2,p3, point);
            if(Sign.ZERO == inCircumsphere)
            {
                Debug.LogWarning("Symbolic Perturbatio problem");
                return false;
            }
            if(Sign.NEGATIVE == inCircumsphere)
            {
                goto NEXT_POINT;
            }

            if(Sign.NEGATIVE == Orient(tetPs[0],tetPs[1],tetPs[2],point))
            {
                goto NEXT_POINT;
            }

            for(int i=0; i<originalBound.Count; i++)
            {
                if(!pointsOrient.ContainsKey(point))
                {
                    FillPointsOrient(point,originalBound,pointsOrient);
                }
                if(Sign.ZERO == pointsOrient[point][i])
                {
                    continue;
                }

                for(int j=0; j<4; j++)
                {
                    if(!pointsOrient.ContainsKey(tetPs[j]))
                    {
                        FillPointsOrient(tetPs[j],originalBound,pointsOrient);
                    }
                }
                if(
                    pointsOrient[tetPs[0]][i] == pointsOrient[tetPs[1]][i] &&
                    pointsOrient[tetPs[0]][i] == pointsOrient[tetPs[2]][i] &&
                    pointsOrient[tetPs[0]][i] == pointsOrient[tetPs[3]][i] &&
                    pointsOrient[tetPs[0]][i] == pointsOrient[point][i]
                    )
                {
                    continue;
                }

                for(int j=0; j<4; j++)
                {
                    if(Sign.POSITIVE == FastInnerSegmentCrossInnerTriangle(point, tetPs[j], i, originalBound, pointsOrient))
                    {
                        goto NEXT_POINT;
                    }
                }

                Int3 triPs = new Int3(originalBound[i].Item1,originalBound[i].Item2,originalBound[i].Item3);
                for(int j=0; j<3; j++)
                {
                    if(Sign.POSITIVE == InnerTriangleSideCrossInnerTriangle(triPs[0],triPs[1],triPs[2], tetPs[j], tetPs[(j+1)%3], point))
                    {
                        goto NEXT_POINT;
                    }
                    if(Sign.POSITIVE == InnerTriangleSideCrossInnerTriangle(triPs[0],triPs[1],triPs[2], tetPs[j], tetPs[3], point))
                    {
                        goto NEXT_POINT;
                    }
                }
                for(int j=0; j<3; j++)
                {
                    if(Sign.POSITIVE == IsLocallyDelaunayTest3(triPs[0],triPs[1],triPs[2], tetPs[j], tetPs[(j+1)%3], tetPs[3], point, Sign.NEGATIVE))
                    {
                        goto NEXT_POINT;
                    }
                }
            }

            return false;

            NEXT_POINT:
            continue;
        }
        return true;
    }

    private void FillPointsOrient(int p, List<(int,int,int)> originalBound, Dictionary<int, List<Sign>> pointsOrient)
    {
        pointsOrient[p] = new List<Sign>(originalBound.Count);

        for(int i=0; i<originalBound.Count; i++)
        {
            (int,int,int) bound = originalBound[i];
            if(p == bound.Item1 || p == bound.Item2 || p == bound.Item3)
            {
                pointsOrient[p].Add(Sign.ZERO);
            }
            else
            {
                pointsOrient[p].Add(Orient(bound.Item1, bound.Item2, bound.Item3, p));
            }
        }
    }

    private Sign FastInnerSegmentCrossInnerTriangle(int s0, int s1, int boundIndex, List<(int,int,int)> originalBound, Dictionary<int, List<Sign>> pointsOrient)
    {
        if(!pointsOrient.ContainsKey(s0))
        {
            FillPointsOrient(s0, originalBound, pointsOrient);
        }
        if(!pointsOrient.ContainsKey(s1))
        {
            FillPointsOrient(s1, originalBound, pointsOrient);
        }

        Sign orient0 = pointsOrient[s0][boundIndex];
        Sign orient1 = pointsOrient[s1][boundIndex];

        if(orient0 == orient1)
        {
            return Sign.NEGATIVE;
        }
        if(Sign.ZERO == orient0 || Sign.ZERO == orient1)
        {
            return Sign.NEGATIVE;
        }

        (int,int,int) bound = originalBound[boundIndex];
        return LineCrossInnerTriangle(s0,s1,bound.Item1,bound.Item2,bound.Item3);
    }
    private Sign FastInnerSegmentCrossInnerTriangle(int s0, int s1, int t0, int t1, int t2)
    {
        Sign orient0 =  Orient(t0,t1,t2,s0);
        Sign orient1 =  Orient(t0,t1,t2,s1);

        if(orient0 == orient1)
        {
            return Sign.NEGATIVE;
        }
        if(Sign.ZERO == orient0 || Sign.ZERO == orient1)
        {
            return Sign.NEGATIVE;
        }
        return LineCrossInnerTriangle(s0,s1,t0,t1,t2);
    }

    private Sign InnerTriangleSideCrossInnerTriangle(int s0, int s1, int s2, int t0, int t1, int t2)
    {
        if(Sign.POSITIVE == FastInnerSegmentCrossInnerTriangle(s0,s1,t0,t1,t2))
        {
            return Sign.POSITIVE;
        }
        if(Sign.POSITIVE == FastInnerSegmentCrossInnerTriangle(s1,s2,t0,t1,t2))
        {
            return Sign.POSITIVE;
        }
        if(Sign.POSITIVE == FastInnerSegmentCrossInnerTriangle(s2,s0,t0,t1,t2))
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }

    private Sign IsLocallyDelaunayTest3(int tri0, int tri1, int tri2, int tet0, int tet1, int tet2, int tet3, Sign tet3Orient)
    {
        Sign sign = Orient(tet0, tet1, tet2, tet3);
            
        if(sign == tet3Orient)
        {
            if(Sign.POSITIVE == PointInTetrahedron(tet0, tet1, tet2, tet3, tri0))
            {
                return Sign.POSITIVE;
            }
            if(Sign.POSITIVE == PointInTetrahedron(tet0, tet1, tet2, tet3, tri1))
            {
                return Sign.POSITIVE;
            }
            if(Sign.POSITIVE == PointInTetrahedron(tet0, tet1, tet2, tet3, tri2))
            {
                return Sign.POSITIVE;
            }
        }
        else
        {
            if(Sign.POSITIVE == PointInTetrahedron(tet1, tet0, tet2, tet3, tri0))
            {
                return Sign.POSITIVE;
            }
            if(Sign.POSITIVE == PointInTetrahedron(tet1, tet0, tet2, tet3, tri1))
            {
                return Sign.POSITIVE;
            }
            if(Sign.POSITIVE == PointInTetrahedron(tet1, tet0, tet2, tet3, tri2))
            {
                return Sign.POSITIVE;
            }
        }
        return Sign.NEGATIVE;
    }

    private Sign PointInTetrahedron(int t0, int t1, int t2, int t3, int p)
    {
        Sign sign = Orient(t0, t1, t2, p);
        if(Sign.ZERO == sign)
        {
            return Sign.NEGATIVE;
        }
        if(sign == Orient(t0, t1, p, t3) && sign == Orient(t0, p, t2, t3) && sign == Orient(p, t1, t2, t3))
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }
}

}
