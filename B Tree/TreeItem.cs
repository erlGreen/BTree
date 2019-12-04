using System;
using System.Collections.Generic;
using System.Text;

namespace B_Tree
{
    class TreeItem
    {
        public int leftChildNumber, rightChildNumber;
        public int key, valueOffset;

        public TreeItem(int k, int v, int left, int right)
        {
            key = k;
            valueOffset = v;
            leftChildNumber = left;
            rightChildNumber = right;
        }

        public static bool operator >(TreeItem item1, TreeItem item2)
        {
            if (item1.key > item2.key)
                return true;
            else
                return false;
        }

        public static bool operator <(TreeItem item1, TreeItem item2)
        {
            if (item1.key < item2.key)
                return true;
            else
                return false;
        }
    }
}
