using System.Linq;
using System.Collections.Generic;

namespace Hanzzz.MeshDemolisher
{

public class DisjointSet<T>
{
    private class Node<Q>
    {
        public Q data { get; set; }
        public Node<Q> parent { get; set; }

        public Node(Q data)
        {
            this.data = data;
            parent = this;
        }
    }


    private Dictionary<T, Node<T>> nodes;
    private IEqualityComparer<T> equalityComparer;

    public DisjointSet(IEqualityComparer<T> equalityComparer)
    {
        nodes = new Dictionary<T, Node<T>>(equalityComparer);
        this.equalityComparer = equalityComparer;
    }

    public bool MakeSet(T data)
    {
        if(nodes.ContainsKey(data))
        {
            return false;
        }
        nodes.Add(data, new Node<T>(data));
        return true;
    }

    public bool SameSet(T data1, T data2)
    {
        return equalityComparer.Equals(FindSet(data1), FindSet(data2));
    }
    public T FindSet(T data)
    {
        return FindSet(nodes[data]).data;
    }
    private Node<T> FindSet(Node<T> node)
    {
        var parent = node.parent;
        while(parent != node)
        {
            node.parent = parent.parent;
            node = parent;
            parent = parent.parent;
        }
        return parent;
    }

    public bool Union(T dataA, T dataB)
    {
        var parentA = FindSet(nodes[dataA]);
        var parentB = FindSet(nodes[dataB]);

        parentA.parent = parentB;
        return true;
    }

    public List<List<T>> GetAllDisjointSet()
    {
        List<List<T>> res = new List<List<T>>();

        //foreach(var node in nodes)
        //{
        //    bool isNewSet = true;
        //    for(int i=0; i<res.Count; i++)
        //    {
        //        if(res[i].Any(x => SameSet(node.Key, x)))
        //        {
        //            res[i].Add(node.Key);
        //            isNewSet = false;
        //            break;
        //        }
        //    }
        //    if(isNewSet)
        //    {
        //        res.Add(new List<T>{node.Key});
        //    }
        //}

        Dictionary<Node<T>, List<T>> resSet = new Dictionary<Node<T>, List<T>>();

        foreach(var node in nodes)
        {
            Node<T> nodeSet = FindSet(node.Value);
            if(!resSet.ContainsKey(nodeSet))
            {
                resSet[nodeSet] = new List<T>();
            }
            resSet[nodeSet].Add(node.Key);
        }
        foreach(var set in resSet)
        {
            res.Add(set.Value);
        }

        return res;
    }

    public void Clear()
    {
        nodes.Clear();
    }
}

}
