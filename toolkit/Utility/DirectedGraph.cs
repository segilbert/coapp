//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Eric Schultz. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Utility {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CoApp.Toolkit.Extensions;
    using Exceptions;

    /// <summary>
    /// A directed graph implementation. Currently is used for cycle checking but could
    /// be used in other situations.
    /// </summary>
    /// <typeparam name="TNode">The class of node in the directed graph.</typeparam>
    public class DirectedGraph<TNode> : IEnumerable<TNode> where TNode : class
    {
        /// <summary>
        /// An enumerable of all the nodes in the graph.
        /// </summary>
        private readonly IEnumerable<TNode> _list;

        /// <summary>
        /// A function that when given the list of nodes and the current node will return all the
        /// direct child nodes.
        /// </summary>
        private readonly Func<IEnumerable<TNode>,TNode, IEnumerable<TNode>> _exportNodeFunc;

        /// <summary>
        /// Constructor for DirectedGraph
        /// </summary>
        /// <param name="list">an <see cref="IEnumerable{T1}"/> of <typeparamref name="TNode"/> containing the nodes in the graph.</param>
        /// <param name="exportNodeFunc"> an <see cref="Func{T1, TReturn}"/> of 
        /// <typeparamref name="TNode"/> and 
        /// <see cref="IEnumerable{T}"/> of <typeparamref name="TNode"/> that when given the current node will return all the
        /// direct child nodes.</param>
        public DirectedGraph(IEnumerable<TNode> list, Func<TNode,IEnumerable<TNode>> exportNodeFunc) : 
            this(list, (node, n) => exportNodeFunc.Invoke(n))
        {
        }


        /// <summary>
        /// Constructor for DirectedGraph
        /// </summary>
        /// <param name="list">an <see cref="IEnumerable{T1}"/> of <typeparamref name="TNode"/> containing the nodes in the graph.</param>
        /// <param name="exportNodeFunc"> an <see cref="Func{T1, TReturn}"/> of 
        /// <see cref="IEnumerable{T}"/> of <typeparamref name="TNode"/>, 
        /// <typeparamref name="TNode"/> and 
        /// <see cref="IEnumerable{T}"/> of <typeparamref name="TNode"/> that when given the the list of nodes and 
        /// the current node will return all the direct child nodes.</param>
        public DirectedGraph(IEnumerable<TNode> list, Func<IEnumerable<TNode>,TNode,IEnumerable<TNode>> exportNodeFunc)
        {
            _list = list;
            _exportNodeFunc = exportNodeFunc;
        }


        /// <summary>
        /// All the cycles in the directed graph.
        /// </summary>
        /// <returns>an <see cref="IEnumerable{T}"/> with elements of type <see cref="IEnumerable{T}"/> of <typeparamref name="TNode"/>.
        /// Each element describes a cycle in the graph. Starting from a given node, the element follows the path back to the first element.
        /// For example if there is a a cycle from A->B->A, an element in returns will consist of an enumerable with nodes A, B and A in that
        /// order.
        /// </returns>
        public IEnumerable<IEnumerable<TNode>> AllCycles
        {
            get
            {
                return _list.Select(DoesCycleExistFrom).Where(n => !n.IsNullOrEmpty());
            }
        }

        /// <summary>
        /// Checks if a cycle exists in the graph.
        /// </summary>
        /// <returns>true if a cycle exists, false otherwise.</returns>
        public bool DoesCycleExist()
        {
            return AllCycles.Any();
        }
        
        /// <summary>
        /// Finds a cycle in the graph starting at <paramref name="start"/>.
        /// </summary>
        /// <param name="start">the <typeparamref name="TNode"/> to start from.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TNode"/> of a cycle from <paramref name="start"/> back
        /// to <paramref name="start"/>. For example if there is a a cycle from A->B->A, and A is <paramref name="start"/>
        /// the enumerable returned will consist of nodes A, B and A in that
        /// order.
        /// 
        /// If no cycle exists, the enumerable will be empty.
        /// </returns>
        public IEnumerable<TNode> DoesCycleExistFrom(TNode start)
        {
            return new CycleChecker(this).DoesCycleExistFrom(start);
        }
    
        /// <summary>
        /// An enumerator of all the nodes.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> of <typeparamref name="TNode"/></returns>
        public IEnumerator<TNode> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        /// An enumerator of all the nodes.
        /// </summary>
        /// <returns>An <see cref="System.Collections.IEnumerator"/></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class CycleChecker
        {
            private readonly DirectedGraph<TNode> _graph;
 

            public CycleChecker(DirectedGraph<TNode> graph)
            {
                _graph = graph;
            }


            public IEnumerable<TNode> DoesCycleExistFrom(TNode start)
            {
                return DoesCycleExistFrom(start, new List<TNode>());
            }

            private IEnumerable<TNode> DoesCycleExistFrom(TNode start, List<TNode> visited)
            {
                if (!_graph.Contains(start))
                    throw new CoAppException();

                var newVisit = new List<TNode>(visited) { start };

                if (visited.Contains(start))
                {
                    if (visited.First() == start)
                    {
                        return newVisit;
                    }
                    else
                    {
                        return Enumerable.Empty<TNode>();
                    }
                }

                foreach (var n in _graph._exportNodeFunc(_graph, start))
                {
                    var path = DoesCycleExistFrom(n, newVisit);

                    if (!path.IsNullOrEmpty())
                        return path;
                }

                return Enumerable.Empty<TNode>();
            }
        }
    
    }


    
}
