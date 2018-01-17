using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTree
{
   /// <summary>
   /// 2 Dimensional for now
   /// </summary>
   class RTreeRectangle
   {
      // Bottom Left Point
      public int X1 = 0;
      public int Y1 = 0;

      // Upper Right Point
      public int X2 = 0;
      public int Y2 = 0;

      public int GetArea()
      {
         return (X2 - X1) * (Y2 - Y1);
      }

      public void Extend(RTreeRectangle b)
      {
         X1 = Math.Min(X1, b.X1);
         Y1 = Math.Min(Y1, b.Y1);
         X2 = Math.Max(X2, b.X2);
         Y2 = Math.Max(Y2, b.Y2);
      }

      public bool Overlaps(RTreeRectangle b)
      {
         return b.X1 <= X2 &&
                b.Y1 <= Y2 &&
                b.X2 >= X1 &&
                b.Y2 >= Y1;
      }
   }

   abstract class IndexRecord<T>
   {
      public RTreeRectangle BBox = new RTreeRectangle();
   }

   class LeafRecord<T> : IndexRecord<T>
   {
      public T Data;
   }

   class NodeRecord<T> : IndexRecord<T>
   {
      public RTreeNode<T> Node;

      public bool TryInsert(IndexRecord<T> item)
      {
         bool success = Node.TryInsert(item);
         if( success )
         {
            ResizeBBox();
         }
         return success;
      }

      // Modifies this's record. 
      public NodeRecord<T> Split(IndexRecord<T> item)
      {
         NodeRecord<T> newNode = Node.SplitQuadradic(item);
         ResizeBBox();

         // newNode's bounding box should be the right size already.
         return newNode;
      }

      public void ResizeBBox()
      {
         RTreeRectangle newBBox = new RTreeRectangle();

         // Setting the values like this will guarantee the first
         // extend picks real values.
         newBBox.X1 = int.MaxValue;
         newBBox.Y1 = int.MaxValue;
         newBBox.X2 = int.MinValue;
         newBBox.Y2 = int.MinValue;

         foreach(var childNode in Node.GetRecords())
         {
            newBBox.Extend(childNode.BBox);
         }

         BBox = newBBox;
      }
   }

   class RTreeNode<T>
   {
      // Refer to paper for naming convention.
      public const uint M = 9;
      public const uint m = 4;

      private bool isLeaf;
      private List<IndexRecord<T>> records;

      public RTreeNode(bool Leaf)
      {
         // Since the tree structure grows up, the leaf
         // nodes are always leaf nodes.
         isLeaf = Leaf;
         records = new List<IndexRecord<T>>();
      }

      public bool IsLeaf()
      {
         return isLeaf;
      }

      public List<IndexRecord<T>> GetRecords()
      {
         return records;
      }

      public int GetRecordCount()
      {
         return records.Count;
      }

      public bool TryInsert(IndexRecord<T> item)
      {
         if(records.Count < M)
         {
            records.Add(item);
            // TODO: Extend the bounding box.
            return true;
         }
         else
         {
            // Need to split.
            return false;
         }
      }

      public NodeRecord<T> SplitQuadradic(IndexRecord<T> newItem)
      {
         List<IndexRecord<T>> itemsToBeAssigned = records;
         itemsToBeAssigned.Add(newItem);

         IndexRecord<T> A, B;
         // TODO handle case of m < 2;
         pickSeeds(out A, out B);
         itemsToBeAssigned.Remove(A);
         itemsToBeAssigned.Remove(B);

         NodeRecord<T> groupOne = new NodeRecord<T>() { BBox = A.BBox, Node = new RTreeNode<T>(isLeaf) };
         NodeRecord<T> groupTwo = new NodeRecord<T>() { BBox = B.BBox, Node = new RTreeNode<T>(isLeaf) };

         if( groupOne.Node.GetRecordCount() + itemsToBeAssigned.Count <= m )
         {
            while( itemsToBeAssigned.Count > 0 )
            {
               groupOne.TryInsert(itemsToBeAssigned.First());
               itemsToBeAssigned.Remove(itemsToBeAssigned.First());
            }
         }

         if( groupTwo.Node.GetRecordCount() + itemsToBeAssigned.Count <= m )
         {
            while( itemsToBeAssigned.Count > 0 )
            {
               groupTwo.TryInsert(itemsToBeAssigned.First());
               itemsToBeAssigned.Remove(itemsToBeAssigned.First());
            }
         }

         while( itemsToBeAssigned.Count > 0)
         {
            int d1, d2;
            var next = pickNext(itemsToBeAssigned, groupOne, groupTwo, out d1, out d2);
            if (d1 < d2)
            {
               groupOne.TryInsert(next);
            }
            else if (d2 < d1)
            {
               groupTwo.TryInsert(next);
            }
            else
            {
               // Insert to whichever is smaller.
               if (groupOne.Node.GetRecordCount() < groupTwo.Node.GetRecordCount())
               {
                  groupOne.TryInsert(next);
               }
               else
               {
                  groupTwo.TryInsert(next);
               }
            }

            itemsToBeAssigned.Remove(next);
         }

         // Set this equal to group two.
         records = groupTwo.Node.records;
         return groupOne;
      }

      private void pickSeeds(out IndexRecord<T> a, out IndexRecord<T> b)
      {
         int maxD = int.MinValue;
         a = null;
         b = null;

         foreach (var childNodeA in records)
         {
            foreach (var childNodeB in records)
            {
               if (childNodeA == childNodeB) { continue; }
               int D = RTree<T>.EnclosingArea(childNodeA.BBox, childNodeB.BBox);
               D = D - childNodeA.BBox.GetArea() - childNodeB.BBox.GetArea();
               if (D > maxD)
               {
                  maxD = D;
                  a = childNodeA;
                  b = childNodeB;
               }

            }
         }
      }

      private static IndexRecord<T> pickNext( List<IndexRecord<T>> remainingList,
                                              NodeRecord<T> groupOne, NodeRecord<T> groupTwo,
                                              out int D1, out int D2 )
      {
         IndexRecord<T> nextRecord = null;
         int maxDiff = int.MinValue;
         D1 = 0;
         D2 = 0;

         int diff, d1, d2;
         foreach( var record in remainingList )
         {
            d1 = RTree<T>.EnclosingArea(groupOne.BBox, record.BBox) - groupOne.BBox.GetArea();
            d2 = RTree<T>.EnclosingArea(groupTwo.BBox, record.BBox) - groupTwo.BBox.GetArea();
            diff = Math.Abs(d2 - d1);
            if(diff > maxDiff)
            {
               maxDiff = diff;
               D1 = d1;
               D2 = d2;
               nextRecord = record;
            }
         }

         return nextRecord;
      }
   }

}
