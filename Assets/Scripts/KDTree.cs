using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Vector2IntHasCoordinate : IHasCoordinate
{
    public Vector2Int Position { get; set; }
    public int Id { get;  }
    public Vector2IntHasCoordinate(int id)
    {
        this.Id = id;
    }

    public int GetCoordinate(int dimension)
    {
        if (dimension == 0)
        {
            return Position.x;
        }

        if (dimension == 1)
        {
            return Position.y;
        }

        throw new ArgumentException("Dimension is out of range");
    }

    public bool GetInBounds(int[] min, int[] max)
    {
        return Position.x >= min[0] && Position.x <= max[0] && Position.y >= min[1] && Position.y <= max[1];    
    }
}
public class KDTree<T> where T : IHasCoordinate
{
    public static Stack<KDNode<T>> nodeCache = new Stack<KDNode<T>>();
    private KDNode<T> rootNode;
    private int dimensions;
    public KDTree(int dimensions)
    {
        this.dimensions = dimensions;
    }

    public void Buildtree(List<T> items)
    {
        items.OrderBy(T => T.GetCoordinate(0)).ThenBy(T => T.GetCoordinate(1));
        rootNode = this.BuildSubTree(items, 0, items.Count - 1, 0);
    }

    public void ClearTree()
    {
        ReleaseNode(rootNode);
    }

    public KDNode<T> BuildSubTree(List<T> items, int left, int right, int depth)
    {
        if (left > right)
        {
            return null;
        }

        int mid = (left + right) / 2;
        KDNode<T> node = AllocateNode(items[mid], depth);
        node.LeftChild = BuildSubTree(items, left, mid - 1, depth + 1);
        node.RightChild = BuildSubTree(items, mid + 1, right, depth + 1);

        return node;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SearchRange(int[] min, int[] max, List<T> results)
    {
        this.SearchSubTree(rootNode, min, max, 0, results);
    }

    private KDNode<T> AllocateNode(T item, int depth)
    {
        if (nodeCache.Count == 0)
        {
            return new KDNode<T>(item, depth);
        }

        var node = nodeCache.Pop();
        node.Item = item;
        node.Depth = depth;
        return node;
    }

    private void ReleaseNode(KDNode<T> node)
    {
        if (node != null)
        {
            node.Item = default(T);
            node.Depth = 0;
            ReleaseNode(node.LeftChild);
            ReleaseNode(node.RightChild);
            nodeCache.Push(node);
        }
    }

    private void SearchSubTree(KDNode<T> node, int[] min, int[] max, int depth, List<T> results)
    {
        if (node == null)
        {
            return;
        }

        if (node.Item.GetInBounds(min, max))
        {
            results.Add(node.Item);
        }

        int dimension = depth % this.dimensions;
        int split = node.Item.GetCoordinate(dimension);
        int minDist = min[dimension];
        int maxDistance = max[dimension];

        if (minDist <= split)
        {
            SearchSubTree(node.LeftChild, min, max, depth + 1, results);
        }

        if (maxDistance >= split)
        {
            SearchSubTree(node.RightChild, min, max, depth + 1, results);
        }
    }
}

public class KDNode<T> where T : IHasCoordinate
{
    public T Item { get; set; }
    public int Depth { get; set; }
    public KDNode<T> LeftChild { get; set; }
    public KDNode<T> RightChild { get; set; }   

    public KDNode(T item, int depth) {
        Item = item;
        Depth = depth;
    }
}

public interface IHasCoordinate
{
    public int GetCoordinate(int dimension);
    public bool GetInBounds(int[] min, int[] max);
}
