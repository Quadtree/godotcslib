using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class AStarIndexed<T> where T : IEquatable<T>, IComparable<T>
{
    public interface IModel
    {
        IEnumerable<T> GetNeighbors(T node);
        uint GetMoveCostBetweenNodes(T node1, T node2);
        ulong EstimateCostBetweenNodes(T node1, T node2);
    }

    IModel Model;

    public AStarIndexed(IModel model)
    {
        this.Model = model;
    }

    private class PathingNode : IComparable<PathingNode>
    {
        public ulong CostToHere;
        public T Id;
        public ulong EstimatedCostToDestination;

        public ulong TotalEstimatedCost => CostToHere + EstimatedCostToDestination;

        public PathingNode Prev;

        public override bool Equals(object obj)
        {
            if (obj is PathingNode)
            {
                return ((PathingNode)obj).Id.Equals(Id);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public int CompareTo(PathingNode other)
        {
            var x = this;
            var y = other;

            if (x.TotalEstimatedCost < y.TotalEstimatedCost) return -1;
            else if (x.TotalEstimatedCost > y.TotalEstimatedCost) return 1;
            else return x.Id.CompareTo(y.Id);
        }
    }

    public IList<T> FindPath(T startNode, T endNode, Func<T, bool> conclusionValidator = null, ulong? maxMoveCost = null, long? maxIteration = null, float? timeLimit = null, float? hardCutoffTime = null)
    {
        SortedSet<PathingNode> Open = new SortedSet<PathingNode>();
        HashSet<PathingNode> Closed = new HashSet<PathingNode>();

        Open.Add(new PathingNode
        {
            CostToHere = 0,
            Id = startNode,
            EstimatedCostToDestination = Model.EstimateCostBetweenNodes(startNode, endNode),
            Prev = null
        });

        long iteration = 0;

        var startTime = Time.GetTicksUsec();

        while (Open.Count > 0 && iteration < (maxIteration ?? long.MaxValue))
        {
            if (hardCutoffTime != null && Time.GetTicksUsec() - startTime > hardCutoffTime.Value * 1_000_000) return null;

            AT.TimeLimit(Open, limitSeconds: timeLimit);
            var next = Open.First();
            Open.Remove(next);
            Closed.Add(next);

            //GD.Print($"open={Open.Count} next={next.Id} nextTEC={next.TotalEstimatedCost}");

            if (next.Id.Equals(endNode) || conclusionValidator?.Invoke(next.Id) == true)
            {
                var ret = new List<T>();
                while (next != null)
                {
                    ret.Add(next.Id);
                    next = next.Prev;
                }

                ret.Reverse();
                return ret;
            }

            foreach (var neighbor in Model.GetNeighbors(next.Id))
            {
                var neighborNode = new PathingNode
                {
                    CostToHere = next.CostToHere + Model.GetMoveCostBetweenNodes(next.Id, neighbor),
                    Id = neighbor,
                    EstimatedCostToDestination = Model.EstimateCostBetweenNodes(neighbor, endNode),
                    Prev = next
                };

                if ((neighborNode.TotalEstimatedCost <= (maxMoveCost ?? ulong.MaxValue)) && !Open.Contains(neighborNode) && !Closed.Contains(neighborNode))
                {
                    Open.Add(neighborNode);
                }
            }

            ++iteration;
        }

        return null;
    }
}