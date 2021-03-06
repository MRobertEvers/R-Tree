﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTree
{
   class RTree<T>
   {
      /// <summary>
      /// Starting point of this data structure.
      /// </summary>
      private NodeRecord<T> root;

      /// <summary>
      /// RTree constructor. Initializes the root.
      /// </summary>
      public RTree()
      {
         root = new NodeRecord<T> { Node = new RTreeNode<T>(Leaf: true) };
      }

      /// <summary>
      /// Remove the input item from the list.
      /// </summary>
      /// <param name="item">Item to remove</param>
      public void Delete(LeafRecord<T> item)
      {
         NodeRecord<T> node = root;
         List<NodeRecord<T>> nodePath = new List<NodeRecord<T>>();
         if( findLeaf( item, ref node, nodePath) )
         {
            nodePath.Insert(0, root);
            node.Node.GetRecords().Remove(item);
            condenseTree(node, nodePath);
         }
      }

      /// <summary>
      /// Wraps the search function. Eliminates the need to specify a node.
      /// </summary>
      /// <param name="SearchBox">Area to retrieve data from</param>
      /// <returns>List of all LeafRecords within that area.</returns>
      public List<LeafRecord<T>> Search(RTreeRectangle SearchBox)
      {
         return search(SearchBox, root);
      }

      /// <summary>
      /// Inserts Data into the tree structure at point (X,Y)
      /// </summary>
      /// <param name="Data">Data to insert</param>
      /// <param name="X">X Coord of data</param>
      /// <param name="Y">Y Coord of data</param>
      public void Insert(T Data, int X, int Y)
      {
         LeafRecord<T> insert = new LeafRecord<T>();
         insert.Data = Data;
         insert.BBox.X1 = X;
         insert.BBox.Y1 = Y;
         insert.BBox.X2 = X;
         insert.BBox.Y2 = Y;

         Insert(insert);
      }

      /// <summary>
      /// Performs the insertion algorithm.
      /// </summary>
      /// <param name="item">Item to insert</param>
      public void Insert(LeafRecord<T> item)
      {
         List<NodeRecord<T>> leafPath = new List<NodeRecord<T>>();

         // I1.
         // Track the nodes traversed to get to the point that we
         // want to add the node. This list of nodes will be used
         // to propagate changes up the tree.
         NodeRecord<T> insertNode = chooseLeaf(item, leafPath);
         NodeRecord<T> splitNode = null;

         // I2.
         // Attempts to insert the item in the given node.
         // If it fails, we split the node. Store the new
         // node in splitNode, so it can be propagated up
         // later.
         if( !insertNode.TryInsert(item) )
         {
            // Split.
            splitNode = insertNode.Split(item);
         }

         // I3. 
         // Propagate resizing up the tree. Propagate split node
         // if necessary.
         if( adjustTree(leafPath, insertNode, ref splitNode) )
         {
            // I4.
            // Create a new root if the root was split. This new root is not a leaf.
            NodeRecord<T> newRoot = new NodeRecord<T>() { Node = new RTreeNode<T>(Leaf: false) };
            newRoot.TryInsert(root);
            newRoot.TryInsert(splitNode);
            root = newRoot;
         }
      }

      /// <summary>
      /// Finds the leaf node that contains the leaf record if able.
      /// </summary>
      /// <param name="item">Item to find</param>
      /// <param name="rRoot">Tree to search</param>
      /// <param name="rLeafPath">Path to node from root</param>
      /// <returns>true if leaf is found</returns>
      private bool findLeaf(LeafRecord<T> item, ref NodeRecord<T> rRoot, List<NodeRecord<T>> rLeafPath)
      {
         NodeRecord<T> node = rRoot;
         if( !node.Node.IsLeaf() )
         {
            foreach( var childNode in node.Node.GetRecords() )
            {
               if( childNode.BBox.Overlaps(item.BBox) )
               {
                  node = (NodeRecord<T>)childNode;
                  if( findLeaf(item, ref node, rLeafPath))
                  {
                     rLeafPath.Insert(0, node);
                     rRoot = node;
                     return true;
                  }
               }
            }
         }
         else
         {
            foreach( var childNode in node.Node.GetRecords() )
            {
               LeafRecord<T> leaf = (LeafRecord<T>)childNode;
               if( leaf.IsEqual(item) )
               {
                  return true;
               }
            }
         }

         return false;
      }

      /// <summary>
      /// Removes unfull nodes and shrinks BBoxs along the leafpath.
      /// </summary>
      /// <param name="deletedRecordNode">node with leaf deleted</param>
      /// <param name="leafPath">Path from root to node with deletion</param>
      private void condenseTree(NodeRecord<T> deletedRecordNode, List<NodeRecord<T>> leafPath)
      {
         List<NodeRecord<T>> Q = new List<NodeRecord<T>>();
         NodeRecord<T> N = deletedRecordNode;
         while(true)
         {
            if( N == root ) { break; }

            int level = leafPath.IndexOf(N);
            NodeRecord<T> P = leafPath[level - 1];
            if (N.Node.GetRecordCount() < RTreeNode<T>.m)
            {
               P.Node.GetRecords().Remove(N);
               Q.Add(N);
            }
            else
            {
               N.ResizeBBox();
            }

            N = P;
         }

         // We don't have to worry about doubling down because 
         // at each level, we separated the removed node from its
         // parents. So even if an 'ex-parent' is present with its 
         // 'ex-child' in the list, we won't double down.
         foreach( var orphan in Q )
         {
            reinsertNode(orphan);
         }
      }

      /// <summary>
      /// Goes to the leaf nodes of the input tree and re-adds them
      /// to this tree.
      /// </summary>
      /// <param name="node"></param>
      private void reinsertNode(NodeRecord<T> node)
      {
         if (node.Node.IsLeaf())
         {
            foreach (var leafRecord in node.Node.GetRecords())
            {
               Insert((LeafRecord<T>)leafRecord);
            }
         }
         else
         {
            foreach (var nodeRecord in node.Node.GetRecords())
            {
               reinsertNode((NodeRecord<T>)nodeRecord);
            }
         }
      }

      /// <summary>
      /// Searches for all data points below the input root for points that
      /// fall within the searchbox
      /// </summary>
      /// <param name="SearchBox">Area to search</param>
      /// <param name="aRoot">Node to start looking</param>
      /// <returns>List of all leafRecords that reside within the searchbox</returns>
      private List<LeafRecord<T>> search(RTreeRectangle SearchBox, NodeRecord<T> aRoot)
      {
         var matches = new List<LeafRecord<T>>();

         if (!SearchBox.Overlaps(aRoot.BBox))
         {
            // If there is no overlap, return nothing.
            return matches;
         }
         else if (!aRoot.Node.IsLeaf())
         {
            // If the node is not a leaf, recursively call this function
            // for each of its children.
            foreach (var childNode in aRoot.Node.GetRecords())
            {
               NodeRecord<T> node = (NodeRecord<T>)childNode;
               matches = matches.Concat(search(SearchBox, node)).ToList();
            }
            return matches;
         }
         else
         {
            // If this node is a leaf, add each data point that resides in the
            // search box to the match list.
            foreach (var childNode in aRoot.Node.GetRecords())
            {
               LeafRecord<T> node = (LeafRecord<T>)childNode;
               if (SearchBox.Overlaps(node.BBox))
               {
                  matches.Add(node);
               }
            }
            return matches;
         }
      }

      /// <summary>
      /// Returns the node that the item should be added to. Additionally,
      /// it tracks each node it traversed to find the proper location. That list
      /// is called the leafPath and is used for split, and resize propagation.
      /// </summary>
      /// <param name="item">Item to insert</param>
      /// <param name="rLeafPath">Path from root to node where the item should be added.</param>
      /// <returns></returns>
      private NodeRecord<T> chooseLeaf(LeafRecord<T> item, List<NodeRecord<T>> rLeafPath)
      {
         // See Paper for naming
         // CL1.
         NodeRecord<T> N = root;
         rLeafPath.Add(N);

         while (true)
         {
            // CL2.
            if (N.Node.IsLeaf()) { return N; }

            int minEnlargementArea = int.MaxValue;
            int minArea = int.MaxValue;

            // CL3.
            // Find the node that would require the least enlargement area to 
            // add the new item. If there is a tie, use the node with the 
            // smallest area.
            int enlargementArea;
            int nodeArea;
            foreach (var childNode in N.Node.GetRecords())
            {
               NodeRecord<T> nodeRecord = (NodeRecord<T>)childNode;
               NodeRecord<T> F = nodeRecord; // See Paper for naming.
               enlargementArea = EnclosingArea(nodeRecord.BBox, item.BBox) - item.BBox.GetArea();
               nodeArea = nodeRecord.BBox.GetArea();

               if (enlargementArea < minEnlargementArea)
               {
                  minEnlargementArea = enlargementArea;
                  minArea = nodeArea;
                  // CL4.
                  N = F;
               }
               // Resolve ties using the node with the smallest size.
               else if (enlargementArea == minEnlargementArea)
               {
                  if (nodeArea < minArea)
                  {
                     minArea = nodeArea;
                     // CL4.
                     N = F;
                  }
               }
            }

            // N is now the child needing the least enlargement.
            rLeafPath.Add(N);
         }
      }

      /// <summary>
      /// Propagates resizing of the nodes upwards. Additionally, performs
      /// any splitting of nodes along the way if a node needed to split.
      /// Returns whether the root node was split.
      /// </summary>
      /// <param name="adjustPath">Path along which the nodes need be adjusted</param>
      /// <param name="node">Node above which needs to be adjusted</param>
      /// <param name="splitNode">Node that needs to be inserted.</param>
      /// <returns>True if the root node was split</returns>
      private bool adjustTree( List<NodeRecord<T>> adjustPath,
                               NodeRecord<T> node, ref NodeRecord<T> splitNode )
      {
         // AT1.
         NodeRecord<T> N = node;
         NodeRecord<T> NN = splitNode;

         while(true)
         {
            // AT2.
            if (N == root) { return NN != null; }

            // AT3.
            N.ResizeBBox();

            // AT4.
            // Try to add the extra node to this parent node.
            // If the parent has no more room, split the parent,
            // and propagate the split node upwards to continue
            // looking for an insertion point.
            int level = adjustPath.IndexOf(N);
            NodeRecord<T> P = adjustPath[level - 1];

            if (NN != null)
            {
               if (!P.TryInsert(NN))
               {
                  NodeRecord<T> PP = P.Split(NN);
                  NN = PP;
               }
               else
               {
                  NN = null;
               }
            }

            N = P;
         }
      }

      /// <summary>
      /// Cacluates the smallest rectangle that encloses both a and b.
      /// </summary>
      /// <param name="a"></param>
      /// <param name="b"></param>
      /// <returns>Size of the smallest rectangle</returns>
      public static int EnclosingArea(RTreeRectangle a, RTreeRectangle b)
      {
         int minX1 = Math.Min(a.X1, b.X1);
         int minY1 = Math.Min(a.Y1, b.Y1);

         int maxX2 = Math.Max(a.X2, b.X2);
         int maxY2 = Math.Max(a.Y2, b.Y2);

         return (maxX2 - minX1) * (maxY2 - minY1);
      }

   }
}
