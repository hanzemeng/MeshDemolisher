using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public class Vector3Comparator : IComparer<Vector3>
{
    //public int Compare(Vector3 a, Vector3 b)
    //{
    //    if(a.x < b.x-Constant.EPSILON_F)
    //    {
    //        return -1;
    //    }
    //    if(a.x > b.x+Constant.EPSILON_F)
    //    {
    //        return 1;
    //    }
    //    if(a.y < b.y-Constant.EPSILON_F)
    //    {
    //        return -1;
    //    }
    //    if(a.y > b.y+Constant.EPSILON_F)
    //    {
    //        return 1;
    //    }
    //    if(a.z < b.z-Constant.EPSILON_F)
    //    {
    //        return -1;
    //    }
    //    if(a.z > b.z+Constant.EPSILON_F)
    //    {
    //        return 1;
    //    }

    //    return 0;
    //}

    public int Compare(Vector3 a, Vector3 b)
    {
        if((a-b).magnitude < Constant.EPSILON_F)
        {
            return 0;
        }

        if(a.x < b.x)
        {
            return -1;
        }
        if(a.x > b.x)
        {
            return 1;
        }
        if(a.y < b.y)
        {
            return -1;
        }
        if(a.y > b.y)
        {
            return 1;
        }
        if(a.z < b.z)
        {
            return -1;
        }
        if(a.z > b.z)
        {
            return 1;
        }

        throw new Exception();
    }
}

public class Tuple3IntComparator : IEqualityComparer<(int,int,int)>
{
    private static HashSet<int> hashSet = new HashSet<int>();

    public bool Equals((int,int,int) x, (int,int,int) y)
    {
        hashSet.Clear();
        hashSet.Add(x.Item1);
        hashSet.Add(x.Item2);
        hashSet.Add(x.Item3);
        hashSet.Remove(y.Item1);
        hashSet.Remove(y.Item2);
        hashSet.Remove(y.Item3);
        return 0 == hashSet.Count;
    }

    public int GetHashCode((int,int,int) obj)
    {
        return obj.Item1.GetHashCode() ^ obj.Item2.GetHashCode() ^ obj.Item3.GetHashCode();
    }
}

public class ListIntComparator : IEqualityComparer<List<int>>
{
    private static HashSet<int> hashSet = new HashSet<int>();

    public bool Equals(List<int> l1, List<int> l2)
    {
        if(l1.Count != l2.Count)
        {
            return false;
        }

        hashSet.Clear();
        l1.ForEach(x=>hashSet.Add(x));
        l2.ForEach(x=>hashSet.Remove(x));
        return 0 == hashSet.Count;
    }

    public int GetHashCode(List<int> obj)
    {
        return obj.Aggregate(0, (res, next)=>res^next);
    }
}

}
