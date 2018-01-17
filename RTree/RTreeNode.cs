using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTree
{
   /// <summary>
   /// 2 Dimensional for now. Can be generalized to N dimensions.
   /// </summary>
   class RTreeRectangle
   {
      // Bottom Left Point
      public int X1 = 0;
      public int Y1 = 0;

      // Upper Right Point
      public int X2 = 0;
      public int Y2 = 0;

      /// <summary>
      /// Returns the area of this rectangle.
      /// </summary>
      /// <returns></returns>
      public int GetArea()
      {
         return (X2 - X1) * (Y2 - Y1);
      }

      /// <summary>
      /// Extends the size of this rectangle to include b.
      /// </summary>
      /// <param name="b"></param>
      public void Extend(RTreeRectangle b)
      {
         X1 = Math.Min(X1, b.X1);
         Y1 = Math.Min(Y1, b.Y1);
         X2 = Math.Max(X2, b.X2);
         Y2 = Math.Max(Y2, b.Y2);
      }

      /// <summary>
      /// Returns whether two rectangles intersect.
      /// </summary>
      /// <param name="b"></param>
      /// <returns></returns>
      public bool Overlaps(RTreeRectangle b)
      {
         return b.X1 <= X2 &&
                b.Y1 <= Y2 &&
                b.X2 >= X1 &&
                b.Y2 >= Y1;
      }
   }

   /// <summary>
   /// Base class for the tree node elements.
   /// Maintains a bounding box.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   abstract class IndexRecord<T>
   {
      public RTreeRectangle BBox = new RTreeRectangle();
   }

   /// <summary>
   /// Record that stores data in leaf nodes.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   class LeafRecord<T> : IndexRecord<T>
   {
      public T Data;
   }

   /// <summary>
   /// Record that stores children nodes.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   class NodeRecord<T> : IndexRecord<T>
   {
      public RTreeNode<T> Node;

      /// <summary>
      /// Attempts to insert a record.
      /// If this succeeds, it resizes the bouding box.
      /// Otherwise return false.
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      public bool TryInsert(IndexRecord<T> item)
      {
         bool success = Node.TryInsert(item);
         if( success )
         {
            ResizeBBox();
         }
         return success;
      }

      /// <summary>
      /// Splits this node into two based on the chosen algorithm.
      /// Returns a new node with some of the records. This node
      /// retains some of the records as well.
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      public NodeRecord<T> Split(IndexRecord<T> item)
      {
         NodeRecord<T> newNode = Node.SplitQuadradic(item);
         ResizeBBox();

         // newNode's bounding box should be the right size already.
         return newNode;
      }

      /// <summary>
      /// Calculates the minimum bounding box required for this node.
      /// </summary>
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

   /// <summary>
   /// Maintains a list of records.
   /// This is synonymous to the "Child pointer" from
   /// the RTree paper.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   class RTreeNode<T>
   {
      // TODO: This should not be constant.
      // Refer to paper for naming convention.
      public const uint M = 4;
      public const uint m = 2;

      private bool isLeaf;
      private List<IndexRecord<T>> records;

      /// <summary>
      /// Initializes this class.
      /// </summary>
      /// <param name="Leaf"></param>
      public RTreeNode(bool Leaf)
      {
         // Since the tree structure grows up, the leaf
         // nodes are always leaf nodes.
         isLeaf = Leaf;
         records = new List<IndexRecord<T>>();
      }

      /// <summary>
      /// Returns whether this node is designated as a leaf node.
      /// </summary>
      /// <returns></returns>
      public bool IsLeaf()
      {
         return isLeaf;
      }

      /// <summary>
      /// Returns the list of records stored by this node.
      /// </summary>
      /// <returns></returns>
      public List<IndexRecord<T>> GetRecords()
      {
         return records;
      }

      /// <summary>
      /// Returns the number of records currently held.
      /// </summary>
      /// <returns></returns>
      public int GetRecordCount()
      {
         return records.Count;
      }

      /// <summary>
      /// Attempts to insert an item into this's list of records.
      /// If it fails, returns false (the node needs to split).
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
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

      /// <summary>
      /// Utilizes the Quadradic split algorithm to determine where each
      /// of the records go - either to group one or group two.
      /// </summary>
      /// <param name="newItem"></param>
      /// <returns></returns>
      public NodeRecord<T> SplitQuadradic(IndexRecord<T> newItem)
      {
         List<IndexRecord<T>> itemsToBeAssigned = records;
         itemsToBeAssigned.Add(newItem);

         IndexRecord<T> A, B;
         // QS1.
         pickSeeds(itemsToBeAssigned, out A, out B);
         itemsToBeAssigned.Remove(A);
         itemsToBeAssigned.Remove(B);

         NodeRecord<T> groupOne = new NodeRecord<T>() { BBox = A.BBox, Node = new RTreeNode<T>(isLeaf) };
         NodeRecord<T> groupTwo = new NodeRecord<T>() { BBox = B.BBox, Node = new RTreeNode<T>(isLeaf) };
         groupOne.Node.TryInsert(A);
         groupTwo.Node.TryInsert(B);

         // QS2.
         while( itemsToBeAssigned.Count > 0)
         {
            // QS3.
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

            // QS2. 
            if (groupOne.Node.GetRecordCount() + itemsToBeAssigned.Count <= m)
            {
               while (itemsToBeAssigned.Count > 0)
               {
                  groupOne.TryInsert(itemsToBeAssigned.First());
                  itemsToBeAssigned.Remove(itemsToBeAssigned.First());
               }
            }

            // QS2.
            if (groupTwo.Node.GetRecordCount() + itemsToBeAssigned.Count <= m)
            {
               while (itemsToBeAssigned.Count > 0)
               {
                  groupTwo.TryInsert(itemsToBeAssigned.First());
                  itemsToBeAssigned.Remove(itemsToBeAssigned.First());
               }
            }
         }

         // Set this equal to group two.
         records = groupTwo.Node.records;
         return groupOne;
      }

      /// <summary>
      /// Returns the two index records in the remaininglist that, if put into a node,
      /// would have the largest bouding box of any two records.
      /// </summary>
      /// <param name="a"></param>
      /// <param name="b"></param>
      private void pickSeeds(List<IndexRecord<T>> remainingList, out IndexRecord<T> a, out IndexRecord<T> b)
      {
         int maxD = int.MinValue;
         a = null;
         b = null;

         foreach (var childNodeA in remainingList)
         {
            foreach (var childNodeB in remainingList)
            {
               if (childNodeA == childNodeB) { continue; }
               int D = RTree<T>.EnclosingArea(childNodeA.BBox, childNodeB.BBox);
               // PS1.
               // This is a measure inefficiency of grouping items together.
               D = D - childNodeA.BBox.GetArea() - childNodeB.BBox.GetArea();
               if (D > maxD)
               {
                  // PS2.
                  maxD = D;
                  a = childNodeA;
                  b = childNodeB;
               }

            }
         }
      }

      /// <summary>
      /// Returns the record in remaininglist with the largest affinity for
      /// one group over the other.
      /// </summary>
      /// <param name="remainingList"></param>
      /// <param name="groupOne"></param>
      /// <param name="groupTwo"></param>
      /// <param name="D1"></param>
      /// <param name="D2"></param>
      /// <returns></returns>
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
            // PN1.
            d1 = RTree<T>.EnclosingArea(groupOne.BBox, record.BBox) - groupOne.BBox.GetArea();
            d2 = RTree<T>.EnclosingArea(groupTwo.BBox, record.BBox) - groupTwo.BBox.GetArea();
            diff = Math.Abs(d2 - d1);
            if(diff > maxDiff)
            {
               maxDiff = diff;
               // PN2.
               D1 = d1;
               D2 = d2;
               nextRecord = record;
            }
         }

         return nextRecord;
      }
   }

}
