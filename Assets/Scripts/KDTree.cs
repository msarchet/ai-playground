using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class KDTree
{
    public static Stack<KDNode> nodeCache = new Stack<KDNode>();
    private KDNode rootNode;
    private int dimensions;
    public KDTree(int dimensions, int[][] items)
    {
        this.dimensions = dimensions;
        if (items.Length > 0)
        {
            this.rootNode = this.BuildSubTree(items, 0, 0, items.Length - 1);
        }
    }

    public void ReleaseTree()
    {
        this.ReleaseNode(rootNode);
    }

    public void BuildTree(int[][] items)
    {
        this.rootNode = this.BuildSubTree(items, 0, 0, items.Length - 1);
    }

    private void SortItems(int[][] items, int low, int high, int depth)
    {
        if (low >= high || low < 0)
        {
            return;
        }

        int pivot = this.Partition(items, low, high, depth);

        SortItems(items, low, pivot - 1, depth);
        SortItems(items, pivot + 1, high, depth);
    }

    private int Partition(int[][] items, int low, int high, int depth)
    {
        int pivot = items[high][depth % this.dimensions];

        int i = low - 1;
        for (int j = low; j <= high - 1; j++)
        {
            if (items[j][depth % this.dimensions] <= pivot)
            {
                i += 1;
                var temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        i += 1;
        var temp2 = items[i];
        items[i] = items[high];
        items[high] = temp2;

        return i;
    }

    private KDNode BuildSubTree(int[][] items, int depth, int low, int high)
    {
        this.SortItems(items, low, high, depth);

        int mid = (low + high) / 2;
        KDNode node = this.AllocateNode(items[mid], depth);
        if (low <= mid - 1)
        {
            node.Children[0] = BuildSubTree(items, depth + 1, low, mid - 1);
        }

        if (mid + 1 <= high)
        {
            node.Children[1] = BuildSubTree(items, depth + 1, mid + 1, high);
        }
        return node;
    }

    private KDNode AllocateNode(int[] item, int depth)
    {
        if (nodeCache.Count == 0)
        {
            return new KDNode(item, depth);
        }

        var node = nodeCache.Pop();
        node.Item = item;
        node.Depth = depth;
        return node;
    }

    private void ReleaseNode(KDNode node)
    {
        if (node != null)
        {
            ReleaseNode(node.Children[0]);
            ReleaseNode(node.Children[1]);
            node.Children[0] = null;
            node.Children[1] = null;
        }
    }

    public KDNode Nearest(int[] point)
    {
        return FindNearestNeighbor(rootNode, rootNode, point);
    }

    private KDNode FindNearestNeighbor(KDNode currentNode, KDNode closestNode, int[] point)
    {
        if (currentNode == null)
        {
            return closestNode;
        }

        var currentNodeDistance = Math.Pow(currentNode.Item[0] - point[0], 2) + Math.Pow(currentNode.Item[1] - point[1], 2);
        var closestDistance = Math.Pow(closestNode.Item[0] - point[0], 2) + Math.Pow(closestNode.Item[1] - point[1], 2);

        if (currentNodeDistance <= closestDistance)
        {
            closestNode = currentNode;
            closestDistance = currentNodeDistance;
        }

        int dimension = currentNode.Depth % this.dimensions;

        int splitDistance = point[dimension] - currentNode.Item[dimension];

        KDNode first = splitDistance <= 0 ? currentNode.Children[0] : currentNode.Children[1];
        KDNode second = splitDistance <= 0 ? currentNode.Children[1] : currentNode.Children[0];


        closestNode = FindNearestNeighbor(first, closestNode, point);
        closestDistance = Math.Pow(closestNode.Item[0] - point[0], 2) + Math.Pow(closestNode.Item[1] - point[1], 2);
        if (splitDistance * splitDistance < closestDistance)
        {
            closestNode = FindNearestNeighbor(second, closestNode, point);
        }

        return closestNode;
    }
}

public class KDNode
{
    public int[] Item;
    public int Depth;

    public KDNode[] Children;

    public KDNode(int[] item, int depth)
    {
        Item = item;
        Depth = depth;
        Children = new KDNode[2];
    }
}

