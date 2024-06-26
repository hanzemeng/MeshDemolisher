using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public class ClippedVoronoi
{
    private DelaunayTetrahedralization dt = new DelaunayTetrahedralization();

    public List<Point3D> voronoiPoints = new List<Point3D>();
    public List<List<int>> voronoiFaces = new List<List<int>>();
    public List<Point3D> voronoiFacesCenters = new List<Point3D>();
    public List<List<(int, bool)>> voronoiCells = new List<List<(int, bool)>>(); // true if face normal (left hand rule) points towrad point
    public List<List<int>> voronoiCellsNeighbors = new List<List<int>>();
    public List<Point3D> voronoiCellsCenters = new List<Point3D>();

    public List<IPointLocation> clipPoints;
    public List<int> clipTetrahedrons;
    public List<(int,int)> clipBoundFaces = new List<(int,int)>(); // (tetrahedronIndex, faceIndex), {0,1,2}, {0,2,3}, {2,1,3}, {0,3,1}}
    public List<int> clipBoundFacesNeighbors = new List<int>(); // {0,1}, {1,2}, {2,0}

    public Dictionary<int, HashSet<List<int>>> clipVoronoiCellsExterior = new Dictionary<int, HashSet<List<int>>>();
    public Dictionary<int, HashSet<List<int>>> clipVoronoiCellsInterior = new Dictionary<int, HashSet<List<int>>>();
    public List<int> interiorVoronoiCells = new List<int>();

    private Dictionary<int, (int,int,int,double,double,double)> exteriorPointsCDTMappings = new Dictionary<int, (int, int, int, double, double, double)>();
    public Dictionary<int,List<(List<(int,Point3D)>,double)>> exteriorPointsMappings = new Dictionary<int, List<(List<(int, Point3D)>, double)>>();

    public void Reset()
    {
        voronoiPoints.Clear();
        voronoiFaces.Clear();
        voronoiFacesCenters.Clear();
        voronoiCells.Clear();
        voronoiCellsNeighbors.Clear();
        voronoiCellsCenters.Clear();

        clipBoundFaces.Clear();
        clipBoundFacesNeighbors.Clear();

        clipVoronoiCellsExterior.Clear();
        clipVoronoiCellsInterior.Clear();
        interiorVoronoiCells.Clear();

        exteriorPointsCDTMappings.Clear();
        exteriorPointsMappings.Clear();
    }

    public void CalculateClippedVoronoi(List<Vector3> voronoiPoints, List<Vector3> clipPoints, List<int> clipBound)
    {
        //voronoiPoints = voronoiPoints.DeepCopy().ToList();
        //clipPoints = clipPoints.DeepCopy().ToList();
        //clipBound = clipBound.DeepCopy().ToList();
        Reset();
        CalculateVoronoi(voronoiPoints);
        TetrahedralizeClipObject(clipPoints,clipBound);
        Clip();
        Filter();
        RemapEexteriorPoints();
    }

    private void RemapEexteriorPoints()
    {
        Dictionary<int, List<(int,Point3D)>> originalPointsMappings = dt.originalPointsMappings;
        Dictionary<int, (int,int,double)> insertedPointsMappings = dt.insertedPointsMappings;

        int[] valIndex = new int[3];
        double[] valWeight = new double[3];
        foreach(var kvp in exteriorPointsCDTMappings)
        {
            List<(List<(int,Point3D)>, double)> mapping = new List<(List<(int,Point3D)>, double)>();
            
            valIndex[0] = kvp.Value.Item1;
            valIndex[1] = kvp.Value.Item2;
            valIndex[2] = kvp.Value.Item3;
            valWeight[0] = kvp.Value.Item4;
            valWeight[1] = kvp.Value.Item5;
            valWeight[2] = kvp.Value.Item6;

            for(int i=0; i<3; i++)
            {
                if(-1 == valIndex[i])
                {
                    break;
                }
                if(insertedPointsMappings.ContainsKey(valIndex[i]))
                {
                    var cdtMapping = insertedPointsMappings[valIndex[i]];
                    //mapping.Add((originalPointsMappings[cdtMapping.Item1], valWeight[i]*(cdtMapping.Item3)));
                    //mapping.Add((originalPointsMappings[cdtMapping.Item2], valWeight[i]*(1d-cdtMapping.Item3)));
                    mapping.Add((originalPointsMappings[cdtMapping.Item1], valWeight[i]*(1d-cdtMapping.Item3)));
                    mapping.Add((originalPointsMappings[cdtMapping.Item2], valWeight[i]*(cdtMapping.Item3)));
                }
                else
                {
                    mapping.Add((originalPointsMappings[valIndex[i]], valWeight[i]));
                }
            }
            exteriorPointsMappings[kvp.Key] = mapping;
        }
    }

    private void Filter()
    {
        for(int i=0; i<voronoiCells.Count; i++)
        {
            if(clipVoronoiCellsExterior.ContainsKey(i))
            {
                continue;
            }
            Point3D center = voronoiCellsCenters[i];
            for(int j=0; j<clipTetrahedrons.Count; j+=4)
            {
                if(-1 == clipTetrahedrons[j])
                {
                    continue;
                }

                bool inTet = true;
                for(int k=0; k<4; k++)
                {
                    Int3 tri = dt.GetTetrahedronPoints(j/4,k);
                    if(Sign.NEGATIVE == PointComputation.Orient(clipPoints[tri[0]],clipPoints[tri[1]],clipPoints[tri[2]],center))
                    {
                        inTet = false;
                        break;
                    }
                }

                if(inTet)
                {
                    interiorVoronoiCells.Add(i);
                    break;
                }
            }
        }
    }

    private void Clip()
    {
        List<HashSet<int>> voronoiCellsIncidentTris = Enumerable.Range(0,voronoiCells.Count).Select(x=>new HashSet<int>()).ToList();
        Dictionary<(int,int,int), int> edgeFaceIntersections = new Dictionary<(int, int, int), int>();
        Dictionary<(int,int,int,int), int> edgeTriangleIntersections = new Dictionary<(int, int, int,int), int>();

        Queue<(int,int)> incidentPairs = new Queue<(int, int)>();
        {
            Int3 bound = dt.GetTetrahedronPoints(clipBoundFaces[0].Item1,clipBoundFaces[0].Item2);
            Point3D centroid = (clipPoints[bound[0]].ToPoint3D()+clipPoints[bound[1]].ToPoint3D()+clipPoints[bound[2]].ToPoint3D()) / 3d;
            int cell = 0;
            double distance = Point3D.SquareMagnitude(voronoiCellsCenters[0] - centroid);
            for(int i=1; i<voronoiCellsCenters.Count; i++)
            {
                double newDistance =  Point3D.SquareMagnitude(voronoiCellsCenters[i] - centroid);
                {
                    if(newDistance < distance)
                    {
                        cell = i;
                        distance = newDistance;
                    }
                }
            }
            incidentPairs.Enqueue((cell, 0));
            voronoiCellsIncidentTris[cell].Add(0);
        }
        
        while(0 != incidentPairs.Count)
        {
            var current = incidentPairs.Dequeue();
            Int3 triangle = dt.GetTetrahedronPoints(clipBoundFaces[current.Item2].Item1, clipBoundFaces[current.Item2].Item2);
            List<int> intersections = TriangleVoronoiCellIntersection(clipBoundFaces[current.Item2].Item1, clipBoundFaces[current.Item2].Item2, voronoiCells[current.Item1], edgeFaceIntersections, edgeTriangleIntersections);

            if(!clipVoronoiCellsExterior.ContainsKey(current.Item1))
            {
                clipVoronoiCellsExterior[current.Item1] = new HashSet<List<int>>(new ListIntComparator());
            }
            clipVoronoiCellsExterior[current.Item1].Add(intersections);

            for(int i=0; i<intersections.Count; i++)
            {
                int e0 = intersections[i];
                int e1 = intersections[(i+1)%intersections.Count];
                if(e0>e1)
                {
                    int temp = e0;
                    e0 = e1;
                    e1 = temp;
                }

                List<int> e0Face = GetOriginFace(e0, clipBoundFaces[current.Item2].Item1, clipBoundFaces[current.Item2].Item2, voronoiCells[current.Item1], edgeTriangleIntersections);
                List<int> e1Face = GetOriginFace(e1, clipBoundFaces[current.Item2].Item1, clipBoundFaces[current.Item2].Item2, voronoiCells[current.Item1], edgeTriangleIntersections);
                bool shouldSkip = false;
                for(int j=0; j<e0Face.Count; j++)
                {
                    shouldSkip = true;
                    int neighbor = voronoiCellsNeighbors[current.Item1][e0Face[j]];
                    if(-1 != neighbor && !voronoiCellsIncidentTris[neighbor].Contains(current.Item2))
                    {
                        incidentPairs.Enqueue((neighbor, current.Item2));
                        voronoiCellsIncidentTris[neighbor].Add(current.Item2);
                    }
                }
                for(int j=0; j<e1Face.Count; j++)
                {
                    shouldSkip = true;
                    int neighbor = voronoiCellsNeighbors[current.Item1][e1Face[j]];
                    if(-1 != neighbor && !voronoiCellsIncidentTris[neighbor].Contains(current.Item2))
                    {
                        incidentPairs.Enqueue((neighbor, current.Item2));
                        voronoiCellsIncidentTris[neighbor].Add(current.Item2);
                    }
                }
                if(shouldSkip)
                {
                    continue;
                }

                bool e0b = triangle.Contains(e0);
                bool e1b = triangle.Contains(e1);
                if(e0b && e1b)
                {
                    CheckTriangleNeighbor(e0,e1,triangle,current,incidentPairs, voronoiCellsIncidentTris);
                }
                else if(e0b && !e1b)
                {
                    var e1Origin = GetOrigin(e1, triangle, voronoiCells[current.Item1], edgeFaceIntersections);
                    if(e0 == e1Origin.e0)
                    {
                        CheckTriangleNeighbor(e0,e1Origin.e1,triangle,current,incidentPairs, voronoiCellsIncidentTris);
                    }
                    else if(e0 == e1Origin.e1)
                    {
                        CheckTriangleNeighbor(e0,e1Origin.e0,triangle,current,incidentPairs, voronoiCellsIncidentTris);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if(!e0b && e1b)
                {
                    var e0Origin = GetOrigin(e0, triangle, voronoiCells[current.Item1], edgeFaceIntersections);
                    if(e1 == e0Origin.e0)
                    {
                        CheckTriangleNeighbor(e1,e0Origin.e1,triangle,current,incidentPairs, voronoiCellsIncidentTris);
                    }
                    else if(e1 == e0Origin.e1)
                    {
                        CheckTriangleNeighbor(e1,e0Origin.e0,triangle,current,incidentPairs, voronoiCellsIncidentTris);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    var e0Origin = GetOrigin(e0, triangle, voronoiCells[current.Item1], edgeFaceIntersections);
                    var e1Origin = GetOrigin(e1, triangle, voronoiCells[current.Item1], edgeFaceIntersections);
                    if(e0Origin.face == e1Origin.face)
                    {
                        CheckVoronoiCellNeighbor(e0Origin.face,current,incidentPairs, voronoiCellsIncidentTris);
                    }
                    else
                    {
                        CheckTriangleNeighbor(e0Origin.e0,e0Origin.e1,triangle,current,incidentPairs, voronoiCellsIncidentTris);
                    }
                }
            }
        }


        List<HashSet<(int,int)>> voronoiCellsBoundFace = Enumerable.Range(0,voronoiCells.Count).Select(x=>new HashSet<(int,int)>()).ToList();
        List<HashSet<int>> voronoiCellsIncidentTets = Enumerable.Range(0,voronoiCells.Count).Select(x=>new HashSet<int>()).ToList();
        for(int i=0; i<voronoiCellsIncidentTris.Count; i++)
        {
            HashSet<int> tris = voronoiCellsIncidentTris[i];
            foreach(int tri in tris)
            {
                voronoiCellsBoundFace[i].Add((clipBoundFaces[tri].Item1,clipBoundFaces[tri].Item2));

                int tet = clipBoundFaces[tri].Item1;
                voronoiCellsIncidentTets[i].Add(tet);
                incidentPairs.Enqueue((i, tet));
            }
        }

        while(0 != incidentPairs.Count)
        {
            var current = incidentPairs.Dequeue();

            List<(int,bool)> cellFaces = voronoiCells[current.Item1];
            List<int> cellNeighbors = voronoiCellsNeighbors[current.Item1];
            Int4 tetNeighbors = dt.GetTetrahedronNeighbors(current.Item2);

            for(int i=0; i<cellFaces.Count; i++)
            {
                var intersectRes = VoronoiFaceTetrahedronIntersection(current.Item2, cellFaces[i].Item1, cellFaces[i].Item2, edgeFaceIntersections,edgeTriangleIntersections);
                List<int> intersections = intersectRes.Item1;
                Int4 intersectTetFace = intersectRes.Item2;
                if(0 == intersections.Count)
                {
                    continue;
                }
                if(!clipVoronoiCellsInterior.ContainsKey(current.Item1))
                {
                    clipVoronoiCellsInterior[current.Item1] = new HashSet<List<int>>(new ListIntComparator());
                }
                clipVoronoiCellsInterior[current.Item1].Add(intersections);

                if(intersectTetFace.Contains(1))
                {
                    int neighbor = cellNeighbors[i];
                    if(-1 != neighbor && 0 != voronoiCellsIncidentTris[neighbor].Count && !voronoiCellsIncidentTets[neighbor].Contains(current.Item2))
                    {
                        incidentPairs.Enqueue((neighbor, current.Item2));
                        voronoiCellsIncidentTets[neighbor].Add(current.Item2);
                    }
                }

                for(int j=0; j<4; j++)
                {
                    if(1 != intersectTetFace[j])
                    {
                        continue;
                    }

                    int neighbor = tetNeighbors[j];
                    if(-1 != neighbor && !dt.GetTetrahedronPoints(neighbor).Contains(-1) && !voronoiCellsIncidentTets[current.Item1].Contains(neighbor))
                    {
                        incidentPairs.Enqueue((current.Item1,neighbor));
                        voronoiCellsIncidentTets[current.Item1].Add(neighbor);
                    }
                }
            }
        }
    }

    private (List<int>, Int4) VoronoiFaceTetrahedronIntersection(int tetIndex, int voronoiFaceIndex, bool voronoiFaceOrient,  Dictionary<(int,int,int), int> edgeFaceIntersections, Dictionary<(int,int,int, int), int> edgeTriangleIntersections)
    {
        List<int> intersections = new List<int>();
        Int4 intersectTetFace = new Int4(0,0,0,0);

        List<int> face = voronoiFaces[voronoiFaceIndex];
        for(int i=0; i<4; i++)
        {
            Int3 triangle = dt.GetTetrahedronPoints(tetIndex, i);

            for(int j=0; j<3; j++)
            {
                int e0 = triangle[j];
                int e1 = triangle[(j+1)%3];
                if(e0>e1)
                {
                    int temp = e0;
                    e0 = e1;
                    e1 = temp;
                }

                if(!edgeFaceIntersections.ContainsKey((e0,e1,voronoiFaceIndex)))
                {
                    
                    Sign orient0 = PointComputation.Orient(voronoiPoints[face[0]],voronoiPoints[face[1]],voronoiPoints[face[2]], clipPoints[e0]);
                    Sign orient1 = PointComputation.Orient(voronoiPoints[face[0]],voronoiPoints[face[1]],voronoiPoints[face[2]], clipPoints[e1]);
                    if(Sign.ZERO == orient0 || Sign.ZERO == orient1 || orient0 == orient1)
                    {
                        edgeFaceIntersections[(e0,e1,voronoiFaceIndex)] = -1;
                    }
                    else
                    {
                        bool intersects = false;
                        for(int k=0; k<face.Count; k++)
                        {
                            if(Sign.POSITIVE != PointComputation.LineCrossTriangle(clipPoints[e0],clipPoints[e1],
                                voronoiPoints[face[k]],voronoiPoints[face[(k+1)%face.Count]],voronoiFacesCenters[voronoiFaceIndex]))
                            {
                                continue;
                            }

                            intersects = true;
                            break;
                        }
                        if(intersects)
                        {
                            var intersection = PointComputation.LinePlaneIntersection(clipPoints[e0],clipPoints[e1],voronoiPoints[face[0]],voronoiPoints[face[1]],voronoiPoints[face[2]]);
                            edgeFaceIntersections[(e0,e1,voronoiFaceIndex)] = clipPoints.Count;
                            intersections.Add(clipPoints.Count);
                            clipPoints.Add(intersection.Item1);

                            intersectTetFace[i] = 1;
                        }
                        else
                        {
                            edgeFaceIntersections[(e0,e1,voronoiFaceIndex)] = -1;
                        }
                    }
                }
                else
                {
                    int n = edgeFaceIntersections[(e0,e1,voronoiFaceIndex)];
                    if(-1 != n)
                    {
                        intersections.Add(n);
                        intersectTetFace[i] = 1;
                    }
                }
            }
        }

        for(int i=0; i<face.Count; i++)
        {
            bool isInCell = true;
            for(int j=0; j<4; j++)
            {
                Int3 triangle = dt.GetTetrahedronPoints(tetIndex, j);
                Sign orient = PointComputation.Orient(clipPoints[triangle[0]],clipPoints[triangle[1]],clipPoints[triangle[2]],voronoiPoints[face[i]]);
                if(Sign.NEGATIVE == orient)
                {
                    isInCell = false;
                    break;
                }
            }
            if(isInCell)
            {
                intersections.Add(clipPoints.Count);
                clipPoints.Add(voronoiPoints[face[i]]);
            }
        }
        

        for(int i=0; i<4; i++)
        {
            Int3 triangle = dt.GetTetrahedronPoints(tetIndex, i);
            for(int j=0; j<face.Count; j++)
            {
                int e0 = face[j];
                int e1 = face[(j+1)%face.Count];
                if(e0>e1)
                {
                    int temp = e0;
                    e0 = e1;
                    e1 = temp;
                }

                if(!edgeTriangleIntersections.ContainsKey((e0,e1,tetIndex, i)))
                {
                    if(Sign.POSITIVE == PointComputation.InnerSegmentCrossInnerTriangle(voronoiPoints[e0],voronoiPoints[e1],clipPoints[triangle[0]],clipPoints[triangle[1]],clipPoints[triangle[2]]))
                    {
                        var intersection = PointComputation.LinePlaneIntersection(voronoiPoints[e0],voronoiPoints[e1],clipPoints[triangle[0]],clipPoints[triangle[1]],clipPoints[triangle[2]]);
                        edgeTriangleIntersections[(e0,e1,tetIndex, i)] = clipPoints.Count;
                        intersections.Add(clipPoints.Count);
                        clipPoints.Add(intersection.Item1);
                        intersectTetFace[i] = 1;
                    }
                    else
                    {
                        edgeTriangleIntersections[(e0,e1,tetIndex, i)] = -1;
                    }
                }
                else
                {
                    int n = edgeTriangleIntersections[(e0,e1,tetIndex, i)];
                    if(-1 != n && !intersections.Contains(n))
                    {
                        intersections.Add(n);
                        intersectTetFace[i] = 1;
                    }
                }
            }
        }

        if(0 == intersections.Count)
        {
            return (intersections, intersectTetFace);
        }

        Point3D normal = Point3D.Cross(voronoiPoints[face[1]].ToPoint3D() - voronoiPoints[face[0]].ToPoint3D(),
                                       voronoiPoints[face[2]].ToPoint3D() - voronoiPoints[face[1]].ToPoint3D());
        if(!voronoiFaceOrient)
        {
            normal = normal * -1d;
        }

        List<Point3D> intersectionsPoints = intersections.Select(x=>clipPoints[x].ToPoint3D()).ToList();
        var sortRes = PointComputation.RotationIndexSort(intersectionsPoints, normal);
        intersections = intersections.Select((x,i)=>intersections[sortRes.mapping[i]]).ToList();
        
        return (intersections, intersectTetFace);
    }

    private void CheckVoronoiCellNeighbor(int face, (int,int) current, Queue<(int,int)> incidentPairs, List<HashSet<int>> voronoiCellsIncident)
    {
        for(int i=0; i<voronoiCells[current.Item1].Count; i++)
        {
            if(face == voronoiCells[current.Item1][i].Item1)
            {
                int neighbor = voronoiCellsNeighbors[current.Item1][i];
                if(-1 != neighbor && !voronoiCellsIncident[neighbor].Contains(current.Item2))
                {
                    incidentPairs.Enqueue((neighbor, current.Item2));
                    voronoiCellsIncident[neighbor].Add(current.Item2);
                }
                return;
            }
        }
        throw new Exception();
    }

    private void CheckTriangleNeighbor(int e0, int e1, Int3 triangle, (int,int) current, Queue<(int,int)> incidentPairs, List<HashSet<int>> voronoiCellsIncidentTris)
    {
        for(int j=0; j<3; j++)
        {
            if((e0 == triangle[j] && e1 == triangle[(j+1)%3]) || (e1 == triangle[j] && e0 == triangle[(j+1)%3]))
            {
                int neighbor = clipBoundFacesNeighbors[3*current.Item2+j];
                if(!voronoiCellsIncidentTris[current.Item1].Contains(neighbor))
                {
                    incidentPairs.Enqueue((current.Item1, neighbor));
                    voronoiCellsIncidentTris[current.Item1].Add(neighbor);
                }
                return;
            }
        }
        throw new Exception();
    }

    private (int e0, int e1, int face) GetOrigin(int p, Int3 triangle, List<(int, bool)> faces, Dictionary<(int,int,int), int> edgeFaceIntersections)
    {
        for(int i=0; i<faces.Count; i++)
        {
            for(int j=0; j<3; j++)
            {
                int e0 = triangle[j];
                int e1 = triangle[(j+1)%3];
                if(e0>e1)
                {
                    int temp = e0;
                    e0 = e1;
                    e1 = temp;
                }

                if(p == edgeFaceIntersections[(e0,e1,faces[i].Item1)])
                {
                    return (e0,e1, faces[i].Item1);
                }
            }
        }
        throw new Exception();
    }

    private List<int> GetOriginFace(int p, int tetIndex, int faceIndex, List<(int, bool)> faces, Dictionary<(int,int,int,int), int> edgeTriangleIntersections)
    {
        List<int> res = new List<int>();
        for(int i=0; i<faces.Count; i++)
        {
            List<int> face = voronoiFaces[faces[i].Item1];
            for(int j=0; j<face.Count; j++)
            {
                int e0 = face[j];
                int e1 = face[(j+1)%face.Count];
                if(e0>e1)
                {
                    int temp = e0;
                    e0 = e1;
                    e1 = temp;
                }
                if(p == edgeTriangleIntersections[(e0,e1,tetIndex,faceIndex)])
                {
                    res.Add(i);
                    break;
                }
            }
        }
        return res;
    }

    private List<int> TriangleVoronoiCellIntersection(int tetIndex, int faceIndex, List<(int, bool)> faces, Dictionary<(int,int,int), int> edgeFaceIntersections, Dictionary<(int,int,int, int), int> edgeTriangleIntersections)
    {
        Int3 triangle = dt.GetTetrahedronPoints(tetIndex, faceIndex);

        List<int> intersections = new List<int>();

        for(int i=0; i<faces.Count; i++)
        {
            for(int j=0; j<3; j++)
            {
                int e0 = triangle[j];
                int e1 = triangle[(j+1)%3];
                if(e0>e1)
                {
                    int temp = e0;
                    e0 = e1;
                    e1 = temp;
                }

                if(!edgeFaceIntersections.ContainsKey((e0,e1,faces[i].Item1)))
                {
                    List<int> face = voronoiFaces[faces[i].Item1];
                    Sign orient0 = PointComputation.Orient(voronoiPoints[face[0]],voronoiPoints[face[1]],voronoiPoints[face[2]], clipPoints[e0]);
                    Sign orient1 = PointComputation.Orient(voronoiPoints[face[0]],voronoiPoints[face[1]],voronoiPoints[face[2]], clipPoints[e1]);
                    if(Sign.ZERO == orient0 || Sign.ZERO == orient1 || orient0 == orient1)
                    {
                        edgeFaceIntersections[(e0,e1,faces[i].Item1)] = -1;
                    }
                    else
                    {
                        bool intersects = false;
                        for(int k=0; k<face.Count; k++)
                        {
                            if(Sign.POSITIVE != PointComputation.LineCrossTriangle(clipPoints[e0],clipPoints[e1],
                                voronoiPoints[face[k]],voronoiPoints[face[(k+1)%face.Count]],voronoiFacesCenters[faces[i].Item1]))
                            {
                                continue;
                            }

                            intersects = true;
                            break;
                        }
                        if(intersects)
                        {
                            var intersection = PointComputation.LinePlaneIntersection(clipPoints[e0],clipPoints[e1],voronoiPoints[face[0]],voronoiPoints[face[1]],voronoiPoints[face[2]]);
                            edgeFaceIntersections[(e0,e1,faces[i].Item1)] = clipPoints.Count;
                            intersections.Add(clipPoints.Count);
                            exteriorPointsCDTMappings[clipPoints.Count] = (e0,e1,-1,1d-intersection.Item2,intersection.Item2,0d);
                            clipPoints.Add(intersection.Item1);
                        }
                        else
                        {
                            edgeFaceIntersections[(e0,e1,faces[i].Item1)] = -1;
                        }
                    }
                }
                else
                {
                    int n = edgeFaceIntersections[(e0,e1,faces[i].Item1)];
                    if(-1 != n)
                    {
                        intersections.Add(n);
                    }
                }
            }
        }
        
        for(int j=0; j<3; j++)
        {
            bool isInCell = true;
            for(int i=0; i<faces.Count; i++)
            {
                List<int> face = voronoiFaces[faces[i].Item1];
                Sign orient = PointComputation.Orient(voronoiPoints[face[0]],voronoiPoints[face[1]],voronoiPoints[face[2]],clipPoints[triangle[j]]);
                if(faces[i].Item2 && Sign.NEGATIVE == orient)
                {
                    isInCell = false;
                    break;
                }
                else if(!faces[i].Item2 && Sign.POSITIVE == orient)
                {
                    isInCell = false;
                    break;
                }
            }
            if(isInCell)
            {
                intersections.Add(triangle[j]);
                exteriorPointsCDTMappings[triangle[j]] = (triangle[j],-1,-1,1d,0d,0d);
            }
        }

        for(int i=0; i<faces.Count; i++)
        {
            List<int> face = voronoiFaces[faces[i].Item1];
            for(int j=0; j<face.Count; j++)
            {
                int e0 = face[j];
                int e1 = face[(j+1)%face.Count];
                if(e0>e1)
                {
                    int temp = e0;
                    e0 = e1;
                    e1 = temp;
                }

                if(!edgeTriangleIntersections.ContainsKey((e0,e1,tetIndex, faceIndex)))
                {
                    if(Sign.POSITIVE == PointComputation.InnerSegmentCrossInnerTriangle(voronoiPoints[e0],voronoiPoints[e1],clipPoints[triangle[0]],clipPoints[triangle[1]],clipPoints[triangle[2]]))
                    {
                        var intersection = PointComputation.LinePlaneIntersection(voronoiPoints[e0],voronoiPoints[e1],clipPoints[triangle[0]],clipPoints[triangle[1]],clipPoints[triangle[2]]);
                        edgeTriangleIntersections[(e0,e1,tetIndex, faceIndex)] = clipPoints.Count;
                        intersections.Add(clipPoints.Count);

                        Point3D a = clipPoints[triangle[0]].ToPoint3D();
                        Point3D b = clipPoints[triangle[1]].ToPoint3D();
                        Point3D c = clipPoints[triangle[2]].ToPoint3D();
                        Point3D p = intersection.Item1;
                        double triArea2 = Point3D.Magnitude(Point3D.Cross(b-a,c-a));

                        double aWeight = Point3D.Magnitude(Point3D.Cross(p-b,p-c)) / triArea2;
                        double bWeight = Point3D.Magnitude(Point3D.Cross(p-c,p-a)) / triArea2;
                        double cWeight = 1d-aWeight-bWeight;
                        exteriorPointsCDTMappings[clipPoints.Count] = (triangle[0],triangle[1],triangle[2],aWeight,bWeight,cWeight);

                        clipPoints.Add(intersection.Item1);
                    }
                    else
                    {
                        edgeTriangleIntersections[(e0,e1,tetIndex, faceIndex)] = -1;
                    }
                }
                else
                {
                    int n = edgeTriangleIntersections[(e0,e1,tetIndex, faceIndex)];
                    if(-1 != n && !intersections.Contains(n))
                    {
                        intersections.Add(n);
                    }
                }
            }
        }

        if(0 == intersections.Count)
        {
            return intersections;
        }

        Point3D normal =  Point3D.Cross(clipPoints[triangle[1]].ToPoint3D() - clipPoints[triangle[0]].ToPoint3D(),
                                        clipPoints[triangle[2]].ToPoint3D() - clipPoints[triangle[1]].ToPoint3D());

        List<Point3D> intersectionsPoints = intersections.Select(x=>clipPoints[x].ToPoint3D()).ToList();
        var sortRes = PointComputation.RotationIndexSort(intersectionsPoints, normal);
        intersections = intersections.Select((x,i)=>intersections[sortRes.mapping[i]]).ToList();
        
        return intersections;
    }

    private void CalculateVoronoi(List<Vector3> inputPoints)
    {
        dt.DelaunayTetrahedralize(inputPoints);

        List<IPointLocation> points = dt.points;
        List<int> tetrahedrons = dt.tetrahedrons;

        for(int i=0; i<tetrahedrons.Count; i+=4)
        {
            if(-1 == tetrahedrons[i])
            {
                voronoiPoints.Add(new Point3D(0d,0d,0d));
            }
            else
            {
                voronoiPoints.Add(PointComputation.CircumsphereFromFourPoints(points[tetrahedrons[i]],points[tetrahedrons[i+1]],points[tetrahedrons[i+2]],points[tetrahedrons[i+3]]));
            }
        }

        Dictionary<List<int>, (int, int)> calculatedFace = new Dictionary<List<int>, (int, int)>(new ListIntComparator());

        for(int i=0; i<points.Count; i++)
        {
            List<(int, bool)> voronoiCell = new List<(int, bool)>();
            List<int> voronoiCellNeighbors = new List<int>();

            List<int> incidentTetrahedrons = dt.FindTetrahedronIncident(i);
            List<Int4> incidentTetrahedronsPoints = incidentTetrahedrons.Select(x=>dt.GetTetrahedronPoints(x)).ToList();
            HashSet<int> uniqueEdge = new HashSet<int>();
            for(int j=0; j<incidentTetrahedronsPoints.Count; j++)
            {
                uniqueEdge.Add(incidentTetrahedronsPoints[j][0]);
                uniqueEdge.Add(incidentTetrahedronsPoints[j][1]);
                uniqueEdge.Add(incidentTetrahedronsPoints[j][2]);
                uniqueEdge.Add(incidentTetrahedronsPoints[j][3]);
            }
            uniqueEdge.Remove(i);

            foreach(int edge in uniqueEdge)
            {
                List<int> edgeTetrahedrons = incidentTetrahedrons.Where((x,i)=>incidentTetrahedronsPoints[i].Contains(edge)).ToList();

                if(edgeTetrahedrons.Count<3)
                {
                    Debug.Log("Not enough vertices");
                    continue;
                }

                if(!calculatedFace.ContainsKey(edgeTetrahedrons))
                {
                    int faceIndex = voronoiFaces.Count;
                    calculatedFace[edgeTetrahedrons] = (faceIndex, i);

                    List<Point3D> centers3D = edgeTetrahedrons.Select(x=>voronoiPoints[x]).ToList();
                    var sortRes = PointComputation.RotationIndexSort(centers3D);
                    voronoiFaces.Add(edgeTetrahedrons.Select((x,i)=>edgeTetrahedrons[sortRes.mapping[i]]).ToList());

                    if(Sign.POSITIVE == PointComputation.Orient(centers3D[sortRes.mapping[0]],centers3D[sortRes.mapping[1]],centers3D[sortRes.mapping[2]],points[i]))
                    {
                        voronoiCell.Add((faceIndex, true));
                    }
                    else
                    {
                        voronoiCell.Add((faceIndex, false));
                    }
                    voronoiCellNeighbors.Add(-1);
                }
                else
                {
                    var temp = calculatedFace[edgeTetrahedrons];
                    //calculatedFace.Remove(edgeTetrahedrons);
                    int faceIndex = temp.Item1;
                    int neighborIndex = temp.Item2;
                    for(int j=0; j<voronoiCells[neighborIndex].Count; j++)
                    {
                        if(faceIndex != voronoiCells[neighborIndex][j].Item1)
                        {
                            continue;
                        }

                        voronoiCell.Add((faceIndex, !voronoiCells[neighborIndex][j].Item2));
                        voronoiCellNeighbors.Add(neighborIndex);
                        voronoiCellsNeighbors[neighborIndex][j] = i;
                        break;
                    }
                }
            }
            voronoiCells.Add(voronoiCell);
            voronoiCellsNeighbors.Add(voronoiCellNeighbors);
        }
        voronoiCellsCenters = points.Select(x=>x.ToPoint3D()).ToList();
        voronoiFacesCenters = voronoiFaces.Select(x=>x.Aggregate(new Point3D(0d,0d,0d), (sum,next)=>sum + voronoiPoints[next]) / x.Count).ToList();
    }

    private void TetrahedralizeClipObject(List<Vector3> inputPoints, List<int> inputBound)
    {
        dt.ConstrainedDelaunayTetrahedralize(inputPoints, inputBound);

        clipPoints = dt.points;
        clipTetrahedrons = dt.tetrahedrons;

        Dictionary<(int,int), int> calculatedBoundEdge = new Dictionary<(int, int), int>();

        for(int i=0; i<dt.tetrahedrons.Count; i+=4)
        {
            if(-1 == dt.tetrahedrons[i])
            {
                continue;
            }

            for(int j=0; j<4; j++)
            {
                if(!dt.neighborSeparation[i+j])
                {
                    continue;
                }

                int faceIndex = clipBoundFaces.Count;
                clipBoundFaces.Add((i/4,j));
                
                Int3 facePoints = dt.GetTetrahedronPoints(i/4, j);
                for(int k=0; k<3; k++)
                {
                    int e0 = facePoints[k];
                    int e1 = facePoints[(k+1)%3];
                    if(e0>e1)
                    {
                        int temp = e0;
                        e0 = e1;
                        e1 = temp;
                    }

                    if(!calculatedBoundEdge.ContainsKey((e0,e1)))
                    {
                        calculatedBoundEdge[(e0,e1)] = faceIndex;
                        clipBoundFacesNeighbors.Add(-1);
                    }
                    else
                    {
                        int neighbor = calculatedBoundEdge[(e0,e1)];
                        //calculatedBoundEdge.Remove((e0,e1));
                        clipBoundFacesNeighbors.Add(neighbor);
                        for(int l=0; l<3; l++)
                        {
                            Int3 neighborPoints = dt.GetTetrahedronPoints(clipBoundFaces[neighbor].Item1, clipBoundFaces[neighbor].Item2);
                            int ne0 = neighborPoints[l];
                            int ne1 = neighborPoints[(l+1)%3];
                            if(ne0>ne1)
                            {
                                int temp = ne0;
                                ne0 = ne1;
                                ne1 = temp;
                            }
                            if(ne0 == e0 && ne1 == e1)
                            {
                                clipBoundFacesNeighbors[3*neighbor+l] = faceIndex;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}

}
