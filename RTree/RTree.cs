using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTree
{
   class RTree<T>
   {
      private NodeRecord<T> root;

      public RTree()
      {
         root = new NodeRecord<T> { Node = new RTreeNode<T>(Leaf: true) };
      }

      public List<LeafRecord<T>> Find(RTreeRectangle SearchBox)
      {
         return search(SearchBox, root);
      }

      private List<LeafRecord<T>> search(RTreeRectangle SearchBox, NodeRecord<T> aRoot)
      {
         var matches = new List<LeafRecord<T>>();
         if( !SearchBox.Overlaps(aRoot.BBox) )
         {
            return matches;
         }
         else if( !aRoot.Node.IsLeaf() )
         {
            foreach (var childNode in aRoot.Node.GetRecords())
            {
               NodeRecord<T> node = (NodeRecord<T>)childNode;
               matches = matches.Concat(search(SearchBox, node)).ToList();
            }
            return matches;
         }
         else
         {
            foreach (var childNode in aRoot.Node.GetRecords())
            {
               LeafRecord<T> node = (LeafRecord<T>)childNode;
               if( SearchBox.Overlaps(node.BBox) )
               {
                  matches.Add(node);
               }
            }
            return matches;
         }
      }

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

      public void Insert(LeafRecord<T> item)
      {
         List<NodeRecord<T>> leafPath = new List<NodeRecord<T>>();

         // I1.
         NodeRecord<T> insertNode = chooseLeaf(item, leafPath);
         NodeRecord<T> splitNode = null;

         // I2.
         if( !insertNode.TryInsert(item) )
         {
            // Split.
            splitNode = insertNode.Split(item);
         }

         // I3. Split node is the node that will need to be put
         //     under the new root node if there is one.
         if( adjustTree(leafPath, insertNode, ref splitNode) )
         {
            // I4.
            NodeRecord<T> newRoot = new NodeRecord<T>() { Node = new RTreeNode<T>(Leaf: false) };
            newRoot.TryInsert(root);
            newRoot.TryInsert(splitNode);
            root = newRoot;
         }
      }

      private NodeRecord<T> chooseLeaf(LeafRecord<T> item, List<NodeRecord<T>> rLeafPath)
      {
         // See Paper for naming
         NodeRecord<T> N = root;
         rLeafPath.Add(N);

         while (true)
         {
            if (N.Node.IsLeaf()) { return N; }

            int itemArea = item.BBox.GetArea();
            int minEnlargementArea = int.MaxValue;
            int minArea = int.MaxValue;

            int enlargementArea;
            foreach (var childNode in N.Node.GetRecords())
            {
               NodeRecord<T> nodeRecord = (NodeRecord<T>)childNode;
               NodeRecord<T> F = nodeRecord; // See Paper for naming.
               enlargementArea = EnclosingArea(nodeRecord.BBox, item.BBox) - itemArea;

               if (enlargementArea < minEnlargementArea)
               {
                  minEnlargementArea = enlargementArea;
                  N = F;
               }
               else if (enlargementArea == minEnlargementArea)
               {
                  if (itemArea < minArea)
                  {
                     minArea = itemArea;
                     N = F;
                  }
               }
            }

            // N is now the child needing the least enlargement.
            rLeafPath.Add(N);
         }
      }

      // True indicates we need to add a node to the root.
      private bool adjustTree( List<NodeRecord<T>> adjustPath,
                               NodeRecord<T> node, ref NodeRecord<T> splitNode )
      {
         NodeRecord<T> N = node;
         NodeRecord<T> NN = splitNode;

         while(true)
         {
            if (N == root) { return NN != null; }

            N.ResizeBBox();
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
