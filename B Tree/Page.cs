using System;
using System.Collections.Generic;
using System.Text;

namespace B_Tree
{
    class Page
    {
        public int parentPageNumber;
        public int currentPageNumber;
        int order;
        public int numberOfItems;
        public List<TreeItem> itemList;

        public Page(int currentNumber, int parentNumber, int d)
        {
            currentPageNumber = currentNumber;
            parentPageNumber = parentNumber;
            itemList = new List<TreeItem>();
            order = d;
            numberOfItems = 0;
        }

        public void SetChildrensParentPageNumber(DiskFunctionality df)
        {
            TreeItem treeItem1;
            TreeItem treeItem2;
            List<TreeItem>.Enumerator enumerator = itemList.GetEnumerator();
            enumerator.MoveNext();
            treeItem1 = enumerator.Current;
            if (treeItem1.leftChildNumber == -1)
                return;
            Page child = df.ReadPage(treeItem1.leftChildNumber);
            child.parentPageNumber = currentPageNumber;
            df.SavePage(child);
            while (enumerator.MoveNext())
            {
                treeItem2 = enumerator.Current;
                child = df.ReadPage(treeItem2.leftChildNumber);
                child.parentPageNumber = currentPageNumber;
                df.SavePage(child);
                treeItem1 = treeItem2;
            }
            child = df.ReadPage(treeItem1.rightChildNumber);
            child.parentPageNumber = currentPageNumber;
            df.SavePage(child);
        }

        public Page(byte[] buffer, int d)
        {
            int i = 0;
            int itemEnumerator = 0;
            itemList = new List<TreeItem>();
            TreeItem treeItem;
            int key, valueOffset, leftChild, rightChild;
            order = d;
            currentPageNumber = BitConverter.ToInt32(buffer, 0);
            parentPageNumber = BitConverter.ToInt32(buffer, 4);
            numberOfItems = BitConverter.ToInt32(buffer, 8);
            i = 12;
            while (itemEnumerator < numberOfItems)
            {
                leftChild = BitConverter.ToInt32(buffer, i);
                i += 4;
                key = BitConverter.ToInt32(buffer, i);
                i += 4;
                valueOffset = BitConverter.ToInt32(buffer, i);
                i += 4;
                rightChild = BitConverter.ToInt32(buffer, i);
                treeItem = new TreeItem(key, valueOffset, leftChild, rightChild);
                itemList.Add(treeItem);
                itemEnumerator++;
            }
        }

        public Tuple<int, bool> Find(int key)    //returns number of page the key should be looked for or number of page the key was found with information whether it was found
        {
            TreeItem currentItem, nextItem;
            List<TreeItem>.Enumerator en = itemList.GetEnumerator();
            en.MoveNext();
            currentItem = en.Current;
            if (key < currentItem.key)
                return new Tuple<int, bool>(currentItem.leftChildNumber, false);
            nextItem = currentItem;
            while (true)
            {
                currentItem = nextItem;
                if (key == currentItem.key)
                {
                    //Console.WriteLine("Key found on page " + currentPageNumber + ". Address is: " + currentItem.valueOffset);
                    return new Tuple<int, bool>(currentPageNumber, true);
                }
                bool flag = en.MoveNext();
                if (flag == false)
                {
                    return new Tuple<int, bool>(currentItem.rightChildNumber, false);
                }
                else
                {
                    nextItem = en.Current;
                    if (currentItem.key < key && key < nextItem.key)
                        return new Tuple<int, bool>(currentItem.rightChildNumber, false);
                }
            }
        }

        public void Insert(TreeItem treeItem)
        {
            TreeItem iterator;
            if (numberOfItems == 0)    //if list is empty
            {
                itemList.Add(treeItem);
                numberOfItems++;
                return;  //      1 = successfull insert
            }
            for (int i = 0; i < numberOfItems; i++)
            {
                iterator = itemList[i];
                if (iterator.key > treeItem.key)
                {
                    itemList.Insert(i, treeItem);   //insert in middle
                    if (i > 0)
                    {
                        if (treeItem.leftChildNumber != -1)
                            itemList[i - 1].rightChildNumber = treeItem.leftChildNumber;
                        else
                            treeItem.leftChildNumber = itemList[i - 1].rightChildNumber;
                    }
                    if (i < itemList.Count - 1)
                    {
                        if (treeItem.rightChildNumber != -1)
                            itemList[i + 1].leftChildNumber = treeItem.rightChildNumber;
                        else
                            treeItem.rightChildNumber = itemList[i + 1].leftChildNumber;
                    }
                    break;
                }
                if (i == numberOfItems - 1)
                {
                    itemList.Add(treeItem);     //insert at the end
                    if (treeItem.leftChildNumber != -1)
                        itemList[itemList.Count - 2].rightChildNumber = treeItem.leftChildNumber;
                    else
                        treeItem.leftChildNumber = itemList[itemList.Count - 2].rightChildNumber;
                    break;
                }
            }
            numberOfItems++;
        }

        public byte[] ToByte(int pageSize)
        {
            int itemEnumerator = 0;
            TreeItem treeItem;
            int i = 0;
            byte[] buffer = new byte[pageSize];
            BitConverter.GetBytes(currentPageNumber).CopyTo(buffer, i);
            i += 4;
            BitConverter.GetBytes(parentPageNumber).CopyTo(buffer, i);
            i += 4;
            BitConverter.GetBytes(numberOfItems).CopyTo(buffer, i);
            i += 4;
            while (itemEnumerator < numberOfItems)
            {
                treeItem = itemList[itemEnumerator];
                BitConverter.GetBytes(treeItem.leftChildNumber).CopyTo(buffer, i);
                i += 4;
                BitConverter.GetBytes(treeItem.key).CopyTo(buffer, i);
                i += 4;
                BitConverter.GetBytes(treeItem.valueOffset).CopyTo(buffer, i);
                i += 4;
                BitConverter.GetBytes(treeItem.rightChildNumber).CopyTo(buffer, i);
                itemEnumerator++;
            }
            return buffer;
        }

        public void Show()   //Display 
        {
            List<TreeItem>.Enumerator en = itemList.GetEnumerator();
            TreeItem item;
            if (itemList.Count == 0)
            {
                Console.WriteLine("No items on this page");
                return;
            }
            Console.WriteLine("Parent page: " + parentPageNumber);
            if (itemList[0].leftChildNumber != -1)
            {
                Console.CursorTop = Console.CursorTop + 1;
                Console.Write(itemList[0].leftChildNumber + "   ");
                Console.CursorTop = Console.CursorTop - 1;
            }
            while (en.MoveNext())
            {
                item = en.Current;
                Console.Write(item.key + "    ");
                if (item.rightChildNumber != -1)
                {
                    Console.CursorTop = Console.CursorTop + 1;
                    Console.Write(item.rightChildNumber + "   ");
                    Console.CursorTop = Console.CursorTop - 1;
                }
            }
            Console.CursorTop = Console.CursorTop + 2;
            Console.CursorLeft = 0;
        }

        public TreeItem GetLeftAncestor(Page ancestor)    //number of left sibling
        {
            TreeItem iterator;
            List<TreeItem>.Enumerator en = ancestor.itemList.GetEnumerator();
            while (en.MoveNext())
            {
                iterator = en.Current;
                if (iterator.rightChildNumber == currentPageNumber)
                    return iterator;
            }
            return null;
        }

        public TreeItem GetItem(int key)
        {
            List<TreeItem>.Enumerator enumerator = itemList.GetEnumerator();
            TreeItem item;
            while (enumerator.MoveNext())
            {
                item = enumerator.Current;
                if (item.key == key)
                    return item;
            }
            return null;
        }

        public TreeItem GetRightAncestor(Page ancestor)
        {
            TreeItem iterator;
            List<TreeItem>.Enumerator en = ancestor.itemList.GetEnumerator();
            while (en.MoveNext())
            {
                iterator = en.Current;
                if (iterator.leftChildNumber == currentPageNumber)
                    return iterator;
            }
            return null;
        }

        public TreeItem GetSmallestItem()
        {
            TreeItem item = itemList[0];
            itemList.RemoveAt(0);
            numberOfItems--;
            return item;
        }

        public TreeItem GetBiggestItem()
        {
            TreeItem item = itemList[itemList.Count - 1];
            itemList.RemoveAt(itemList.Count - 1);
            numberOfItems--;
            return item;
        }

        public void RemoveElementWithKey(int key)
        {
            List<TreeItem>.Enumerator enumerator = itemList.GetEnumerator();
            TreeItem item;
            while (enumerator.MoveNext())
            {
                item = enumerator.Current;
                if (item.key == key)
                {
                    itemList.Remove(item);
                    numberOfItems--;
                    return;
                }
            }
        }

        public void SetParentsChildrenPageNumber(int lastPageNumber, DiskFunctionality df)
        {
            if (parentPageNumber == -1)
                return;
            Page ancestorPage = df.ReadPage(parentPageNumber);
            List<TreeItem>.Enumerator e = ancestorPage.itemList.GetEnumerator();
            TreeItem iterator;
            while (e.MoveNext())
            {
                iterator = e.Current;
                if (iterator.leftChildNumber == lastPageNumber)
                    iterator.leftChildNumber = currentPageNumber;
                if (iterator.rightChildNumber == lastPageNumber)
                    iterator.rightChildNumber = currentPageNumber;
            }
            df.SavePage(ancestorPage);
        }
    }
}
