using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public partial class DelaunayTetrahedralization
{
    public static readonly float RANGE = 2000f;
    public static readonly int[,] TETRAHEDRON_FACET = new int[4,3] {{0,1,2}, {0,2,3}, {2,1,3}, {0,3,1}};
    public static readonly int[] TETRAHEDRON_FACET_PIVOT = new int[4] {3,1,0,2};

    private List<Vector3> inputPoints;
    private List<int> inputTriangles;
    public Dictionary<int, List<(int,Point3D)>> originalPointsMappings = new Dictionary<int, List<(int, Point3D)>>();
    public Dictionary<int, (int,int,double)> insertedPointsMappings = new Dictionary<int, (int,int,double)>();

    public List<IPointLocation> points = new List<IPointLocation>();
    private List<int> incidentTetrahedrons = new List<int>();
    public List<int> tetrahedrons = new List<int>(); // positive orientation
    private List<int> tetrahedronNeighbors = new List<int>(); // same order as TETRAHEDRON_FACET
    private Queue<int> tetrahedronsGapIndex = new Queue<int>();

    private Dictionary<(int,int), Int4> inputEdges = new Dictionary<(int,int),Int4>();
    private HashSet<(int,int)> flatInputEdges = new HashSet<(int,int)>();
    private HashSet<(int,int)> nonFlatEdges = new HashSet<(int,int)>();

    private List<bool> pointsIsAcute = new List<bool>();
    private Dictionary<(int,int),int> edgesTypes = new Dictionary<(int, int), int>(); // if value is 1, Item1 is acute
    private Dictionary<(int,int), (int,int)> originalEdges = new Dictionary<(int,int), (int,int)>();

    private List<List<int>> inputFaces = new List<List<int>>();
    private List<HashSet<(int,int)>> inputFacesBoundEdges = new List<HashSet<(int,int)>>();
    private List<HashSet<(int,int)>> inputFacesFlatEdges = new List<HashSet<(int,int)>>();
    private List<HashSet<int>> inputFacesBoundPoints = new List<HashSet<int>>();
    private List<HashSet<int>> inputFacesFlatPoints = new List<HashSet<int>>();

    public List<bool> neighborSeparation = new List<bool>();

    private HashSet<int> visitedPoints = new HashSet<int>();
    private HashSet<int> visitedTetrahedrons = new HashSet<int>();
    private Queue<int> searchQueue = new Queue<int>();

    private void Reset()
    {
        originalPointsMappings.Clear();
        insertedPointsMappings.Clear();

        points.Clear();
        incidentTetrahedrons.Clear();
        tetrahedrons.Clear();
        tetrahedronNeighbors.Clear();
        tetrahedronsGapIndex.Clear();

        inputEdges.Clear();
        flatInputEdges.Clear();
        nonFlatEdges.Clear();
        //pointsIsAcute.Clear();
        edgesTypes.Clear();
        originalEdges.Clear();

        //inputFaces.Clear();
        inputFacesBoundEdges.Clear();
        inputFacesFlatEdges.Clear();
        inputFacesBoundPoints.Clear();
        inputFacesFlatPoints.Clear();

        //neighborSeparation.Clear();

        int p0 = CreateNewPoint(new Vector3(-RANGE,-RANGE,-RANGE));
        int p1 = CreateNewPoint(new Vector3(0,-RANGE, RANGE));
        int p2 = CreateNewPoint(new Vector3(RANGE,-RANGE,-RANGE));
        int p3 = CreateNewPoint(new Vector3(0,RANGE,0));

        int t0 = CreateNewTetrahedron(p0,p1,p2,p3);

        incidentTetrahedrons[p0] = t0;
        incidentTetrahedrons[p1] = t0;
        incidentTetrahedrons[p2] = t0;
        incidentTetrahedrons[p3] = t0;
    }
}

}
