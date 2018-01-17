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

         foreach(var leaf in testTree.Find(searchArea))
         {
            Console.WriteLine(leaf.Data);
         }
         Console.Read();
      }
   }
}
