using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTree
{
   class Program
   {
      static void Main(string[] args)
      {
         (new Program()).RTest();
         Console.Read();
      }

      private void RTest()
      {
         RInsertionTest();
         RDeletionTest();
      }

      private void RInsertionTest()
      {
         Console.WriteLine("/////Insertion/////");
         // TODO: Actually write a test.
         RTreeRectangle searchArea = new RTreeRectangle();
         searchArea.X1 = -5;
         searchArea.Y1 = -1;
         searchArea.X2 = 0;
         searchArea.Y2 = 3;

         RTree<int> testTree = new RTree<int>();
         // Outside
         testTree.Insert(99, 1, 0);
         testTree.Insert(100, 1, 6);

         // Inside
         testTree.Insert(1, 0, 0);
         testTree.Insert(2, -5, -1);
         testTree.Insert(3, 0, 3);
         testTree.Insert(4, -1, 2);

         foreach (var leaf in testTree.Search(searchArea))
         {
            Console.WriteLine(leaf.Data);
         }

         Console.WriteLine("/////Insertion Done/////");
      }

      private void RDeletionTest()
      {
         Console.WriteLine("/////Deletion/////");
         RTreeRectangle searchArea = new RTreeRectangle();
         searchArea.X1 = -5;
         searchArea.Y1 = -1;
         searchArea.X2 = 0;
         searchArea.Y2 = 3;

         RTree<int> testTree = new RTree<int>();
         // Outside
         testTree.Insert(99, 1, 0);
         testTree.Insert(100, 1, 6);

         var newleaf = new LeafRecord<int> { Data = 1, BBox = new RTreeRectangle() };
         // Inside
         testTree.Insert(newleaf);
         testTree.Insert(2, -5, -1);
         testTree.Insert(3, 0, 3);
         testTree.Insert(4, -1, 2);
         testTree.Delete(newleaf);
         foreach (var leaf in testTree.Search(searchArea))
         {
            Console.WriteLine(leaf.Data);
         }

         Console.WriteLine("/////Deletion Done/////");
      }
   }
}
