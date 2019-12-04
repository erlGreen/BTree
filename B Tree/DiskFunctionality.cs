﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace B_Tree
{
    class DiskFunctionality
    {
        byte[] filler;
        byte[] pageBuffer;
        int order;
        int pageSize;
        private int childrenLevel;
        private int maxPageNumber;
        private List<int> checkedPages;
        public FileStream stream;
        List<Page> pageList;
        List<int> pagesToDelete;

        public DiskFunctionality(int d)
        {
            stream = new FileStream("B-Tree", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            order = d;
            pageSize = 4 *(6 * order + 4);   //in bytes ; currentPageOffset, parentPageOffset, numberOfRecords, array of ints... (3 * 2d + 1)
            filler = new byte[pageSize];
            pageBuffer = new byte[pageSize];
            for (int i = 0; i < pageSize; i++)
                filler[i] = 0;
        }


        public void ResetCheckupParams()
        {
            checkedPages = new List<int>();
            childrenLevel = -1;
            maxPageNumber = -1;
        }


        public Page ReadPage(int number)
        {
            int offset = number * pageSize;
            if (stream.Length <= offset)
            {
                Console.WriteLine("Attempt to read page outside the file");
                return null;
            }
            else
            {
                stream.Position = offset;
                stream.Read(pageBuffer, 0, pageSize);
                return new Page(pageBuffer, order);
            }
        }

        public Page FindInTree(int key, bool findOrInsert)   //returns page which element is found on or where it should be inserted
        {
            pageList = new List<Page>();
            Page page = ReadPage(0);
            pageList.Add(page);
            Tuple<int, bool> tuple;
            while (true)
            {
                tuple = page.Find(key);
                if (tuple.Item2 == true)
                {
                    if (findOrInsert == Constants.FIND)
                        return page;
                    else
                        return null;
                }
                else if (tuple.Item1 == -1)
                {
                    if (findOrInsert == Constants.FIND)
                        return null;
                    else
                        return page;
                }
                page = ReadPage(tuple.Item1);
                pageList.Add(page);
            }
        }

        public void Update(int key, int value)
        {
            if (stream.Length == 0)
            {
                Console.WriteLine("Tree is empty");
                return;
            }
            Page page = FindInTree(key, Constants.FIND);
            if (page is null)
                Console.WriteLine("Key doesnt exist");
            else
            {
                TreeItem item = page.GetItem(key);
                item.valueOffset = value;
                Console.WriteLine("Updated");
                SavePage(page);
            }
        }

        public void InsertIntoPage(Page page, TreeItem item)
        {
            TreeItem ancestorItem;
            if (page.numberOfItems == 2 * order)
            {
                if (pageList.Count > 1)     //if it has siblings
                {
                    Page ancestorPage = pageList[pageList.Count - 2];
                    ancestorItem = page.GetLeftAncestor(ancestorPage);  //from parent page
                    if (!(ancestorItem is null))
                    {
                        Page leftSibling = ReadPage(ancestorItem.leftChildNumber);
                        if (leftSibling.numberOfItems < 2 * order)
                        {
                            page.Insert(item);
                            item = page.GetSmallestItem();
                            leftSibling.Insert(new TreeItem(ancestorItem.key, ancestorItem.valueOffset, -1, item.leftChildNumber));
                            ancestorItem.key = item.key;
                            ancestorItem.valueOffset = item.valueOffset;
                            //Console.WriteLine("Successfull compensation with left sibling");
                            //Set parent page nr
                            if (item.rightChildNumber != -1)
                            {
                                Page child = ReadPage(item.leftChildNumber);
                                child.parentPageNumber = leftSibling.currentPageNumber;
                                SavePage(child);
                            }
                            SavePage(page);
                            SavePage(leftSibling);
                            SavePage(ancestorPage);
                            //Console.WriteLine("Successfully inserted element");
                            return;
                        }
                        //Console.WriteLine("Left sibling is full");
                    }
                    else
                        //Console.WriteLine("No left sibling");
                    //Console.WriteLine("Trying right sibling");
                    ancestorItem = page.GetRightAncestor(ancestorPage);
                    if (!(ancestorItem is null))
                    {
                        Page rigthSibling = ReadPage(ancestorItem.rightChildNumber);
                        if (rigthSibling.numberOfItems < 2 * order)
                        {
                            page.Insert(item);
                            item = page.GetBiggestItem();
                            rigthSibling.Insert(new TreeItem(ancestorItem.key, ancestorItem.valueOffset, item.rightChildNumber, -1));
                            ancestorItem.key = item.key;
                            ancestorItem.valueOffset = item.valueOffset;
                            //Console.WriteLine("Successfull compensation with right sibling");
                            //Set parent page nr
                            if (item.leftChildNumber != -1)
                            {
                                Page child = ReadPage(item.rightChildNumber);
                                child.parentPageNumber = rigthSibling.currentPageNumber;
                                SavePage(child);
                            }
                            SavePage(page);
                            SavePage(rigthSibling);
                            SavePage(ancestorPage);
                            //Console.WriteLine("Successfully inserted element");
                            return;
                        }
                        //Console.WriteLine("Right sibling is full");
                    }
                    //else
                        //Console.WriteLine("No right sibling");
                }
                //split
                Page newPage;
                List<TreeItem> listToSplit;
                int middle;
                TreeItem middleItem;
                if (page.parentPageNumber == -1)
                {
                    //jezeli jest rootem
                    newPage = new Page(GetFreePageNumber(), 0, order);
                    Page newPage2 = new Page(GetFreePageNumber() + 1, 0, order);
                    page.Insert(item);
                    listToSplit = page.itemList;
                    middle = listToSplit.Count / 2;
                    middleItem = listToSplit[middle];
                    middleItem.leftChildNumber = newPage.currentPageNumber;
                    middleItem.rightChildNumber = newPage2.currentPageNumber;
                    for (int i = listToSplit.Count - 1; i > middle; i--)
                        newPage2.Insert(listToSplit[i]);
                    for (int i = 0; i < middle; i++)
                        newPage.Insert(listToSplit[i]);
                    page.itemList = new List<TreeItem>();
                    page.numberOfItems = 0;
                    page.Insert(middleItem);
                    newPage.SetChildrensParentPageNumber(this);
                    newPage2.SetChildrensParentPageNumber(this);
                    SavePage(page);
                    SavePage(newPage);
                    SavePage(newPage2);
                    //Console.WriteLine("Successfully inserted element with split");
                    return;
                }       
                newPage = new Page(GetFreePageNumber(), page.parentPageNumber, order);
                page.Insert(item);
                listToSplit = page.itemList;
                middle = listToSplit.Count / 2;
                middleItem = listToSplit[middle];
                for (int i = middle + 1; i < listToSplit.Count; i++)
                    newPage.Insert(listToSplit[i]);
                for (int i = listToSplit.Count - 1; i >= middle; i--)
                {
                    listToSplit.RemoveAt(i);
                    page.numberOfItems--;
                }
                middleItem.leftChildNumber = page.currentPageNumber;
                middleItem.rightChildNumber = newPage.currentPageNumber;        //jezeli nad stroną nikogo nie ma to nie ma komu podać elementu
                newPage.SetChildrensParentPageNumber(this);
                SavePage(newPage);
                SavePage(page);
                pageList.RemoveAt(pageList.Count - 1);
                //Console.WriteLine("Page split");
                InsertIntoPage(pageList[pageList.Count - 1], middleItem);
                //zapisać strony i wywyłać dla parenta;
            }

            else
            {
                page.Insert(item);
                SavePage(page);
                //Console.WriteLine("Successfully inserted element");
            }
        }

        public bool InsertIntoTree(int key, int valueOffset)
        {
            Page page;
            TreeItem treeItem = new TreeItem(key, valueOffset, -1, -1);
            if (stream.Length == 0)
            {
                page = new Page(0, -1, order);
                page.Insert(treeItem);
                SavePage(page);
                Console.WriteLine("Inserted element");
                return true;
            }
            page = FindInTree(key, Constants.INSERT);
            if (!(page is null))   //if key doesnt exist
            {
                InsertIntoPage(page, treeItem);
                Console.WriteLine("Inserted element");
                return true;
            }
            Console.WriteLine("Key already exists");
            return false;
        }

        public void CreateFromFile(string path)
        {
            if (!File.Exists(path))
                return;
            DestroyTree();
            byte[] bnumber = new byte[4];
            int key;
            int offset;
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            while (fs.Read(bnumber, 0, 4) == 4)
            {
                key = BitConverter.ToInt32(bnumber, 0);
                offset = (int)fs.Position - 4;
                Console.WriteLine("Inserting " + key + " at " + offset);
                InsertIntoTree(key, offset);
            }
            Console.WriteLine("Finished reading file");
        }

        public void SavePage(Page page)
        {
            stream.Position = page.currentPageNumber * pageSize;
            stream.Write(page.ToByte(pageSize), 0, pageSize);
        }

        public int GetFreePageNumber()
        {
            return (int) stream.Length / pageSize;
        }

        public void ShowPage(int pageNumber)
        {
            Page page = ReadPage(pageNumber);
            if (!(page is null))
            {
                page.Show();
            }
        }

        public void CheckUp()
        {
            ResetCheckupParams();
            Check(0, order, -1, -1, Int32.MaxValue, 0);
            if (checkedPages.Count * pageSize != stream.Length)
                throw new Exception("Wrong file length");
        }

        public void DestroyTree()
        {
            stream.SetLength(0);
            Console.WriteLine("Tree destroyed");
        }

        private void Check(int pageNumber, int order, int parentPageNumber, int smallest, int biggest, int currentLevel)
        {
            if (pageNumber == -1 || stream.Length == 0)
                return;
            Page page = ReadPage(pageNumber);
            if (page.itemList.Count > 2 * order)
                throw new Exception("Error on page nr " + page.currentPageNumber + ". Too many items\n");
            if (page.itemList.Count < order && page.currentPageNumber != 0)
                throw new Exception("Error on page nr " + page.currentPageNumber + ". Too few items\n");
            if (parentPageNumber != page.parentPageNumber)
                throw new Exception("Error. Wrong parent page number on page " + page.currentPageNumber);
            if (maxPageNumber < page.currentPageNumber)
            {
                maxPageNumber = page.currentPageNumber;
            }
            if (!checkedPages.Contains(page.currentPageNumber))
                checkedPages.Add(page.currentPageNumber);
            int numberOfKeys = 0;
            List<TreeItem>.Enumerator enumerator = page.itemList.GetEnumerator();
            TreeItem item;
            while (enumerator.MoveNext())
            {
                item = enumerator.Current;
                if (item.key < smallest)
                    throw new Exception("Wrong place if item on page " + page.currentPageNumber);
                if (item.key > biggest)
                    throw new Exception("Wrong place if item on page " + page.currentPageNumber);
                if (item.leftChildNumber != -1)
                    numberOfKeys++;
                if (item.rightChildNumber != -1)
                    numberOfKeys++;
            }
            if (childrenLevel == -1 && numberOfKeys == 0)
                childrenLevel = currentLevel;
            if (childrenLevel != -1 && numberOfKeys == 0 && childrenLevel != currentLevel)
                throw new Exception("Wrong level of children on page " + page.currentPageNumber +". Saved level: " + childrenLevel + ". Current level: " + currentLevel);
            if (numberOfKeys != 0 && numberOfKeys != 2 * page.itemList.Count)
                throw new Exception("Wrong number of childern on page " + page.currentPageNumber);
            if (page.itemList.Count > 1)
            {
                enumerator = page.itemList.GetEnumerator();
                enumerator.MoveNext();
                TreeItem item1 = enumerator.Current;
                TreeItem item2;
                while (enumerator.MoveNext())
                {
                    item2 = enumerator.Current;
                    if (item2.leftChildNumber != item1.rightChildNumber)
                        throw new Exception("Childern dont match on page " + page.currentPageNumber);
                    if (item1.key > item2.key)
                        throw new Exception("Wrong placement of items on page " + page.currentPageNumber);
                    item1 = item2;
                }
            }
            enumerator = page.itemList.GetEnumerator();
            TreeItem treeItem;
            while (enumerator.MoveNext())
            {
                treeItem = enumerator.Current;
                Check(treeItem.leftChildNumber, order, page.currentPageNumber, smallest, treeItem.key, currentLevel + 1);
                Check(treeItem.rightChildNumber, order, page.currentPageNumber, treeItem.key, biggest, currentLevel + 1);
            }
        }

        public bool Delete(int key)
        {
            pagesToDelete = new List<int>();
            Page page = FindInTree(key, Constants.FIND);
            if (page is null)
            {
                Console.WriteLine("Key doesnt exist");
                return false;
            }
            TreeItem item = page.GetItem(key);
            Page leftGrandSon = GetLeftGrandSon(item.leftChildNumber);
            if (leftGrandSon != null)   //jeżeli ma lewego wnuka
            {
                TreeItem leftGrandSonItem = leftGrandSon.itemList[leftGrandSon.itemList.Count - 1];
                SwapItems(ref item, ref leftGrandSonItem);
                leftGrandSon.RemoveElementWithKey(key);
                Console.WriteLine("Deleted item");
                if (leftGrandSon.numberOfItems < order)
                {
                    SavePage(page);
                    CompensateUnderflow(leftGrandSon);
                }
                else
                {
                    SavePage(page);
                    SavePage(leftGrandSon);
                }
            }
            else
            {
                page.RemoveElementWithKey(key);
                Console.WriteLine("Deleted item");
                if (page.numberOfItems < order)
                    CompensateUnderflow(page);
                else
                    SavePage(page);
            }
            DeletePages();
            return true;
        }

        public void DeletePages()
        {
            int numberOfPage;
            int lastPageNumber;
            Page lastPageInFile;
            bool continueFlag;
            for (int i = 0; i < pagesToDelete.Count; i++)
            {
                numberOfPage = pagesToDelete[i];
                if ((numberOfPage + 1) * pageSize == stream.Length)
                {
                    stream.SetLength(stream.Length - pageSize);
                    continue;
                }
                lastPageInFile = ReadPage((int)stream.Length / pageSize - 1);
                lastPageNumber = lastPageInFile.currentPageNumber;
                continueFlag = false;
                for (int j = i + 1; j < pagesToDelete.Count; j++)
                {
                    if (pagesToDelete[j] == lastPageNumber)
                    {
                        pagesToDelete.RemoveAt(j);
                        stream.SetLength(stream.Length - pageSize);
                        i--;
                        continueFlag = true;
                        break;
                    }
                }
                if (continueFlag)
                    continue;
                lastPageInFile.currentPageNumber = numberOfPage;
                lastPageInFile.SetChildrensParentPageNumber(this);
                lastPageInFile.SetParentsChildrenPageNumber(lastPageNumber, this);
                stream.SetLength(stream.Length - pageSize);
                SavePage(lastPageInFile);
            }
        }

        public void CompensateUnderflow(Page page)
        {
            if (page.currentPageNumber == 0)
            {
                if (page.numberOfItems == 0)
                {
                    pagesToDelete.Add(0);
                    return;
                }
                SavePage(page);
                return;
            }
            Page ancestorPage = ReadPage(page.parentPageNumber);
            TreeItem ancestorItem = page.GetLeftAncestor(ancestorPage); //spróbuj lewego, jak nie - prawego, jak nie to merge
            if (ancestorItem != null && ancestorItem.leftChildNumber != -1) //if it has left sibling
            {
                Page leftSibling = ReadPage(ancestorItem.leftChildNumber);
                if (leftSibling.numberOfItems > order)
                {
                    TreeItem item = leftSibling.GetBiggestItem();
                    page.Insert(new TreeItem(ancestorItem.key, ancestorItem.valueOffset, item.rightChildNumber, -1));
                    if (item.rightChildNumber != -1)
                    {
                        Page child = ReadPage(item.rightChildNumber);
                        child.parentPageNumber = page.currentPageNumber;
                        SavePage(child);
                    }
                    ancestorItem.key = item.key;
                    ancestorItem.valueOffset = item.valueOffset;
                    SavePage(page);
                    SavePage(leftSibling);
                    SavePage(ancestorPage);
                    return;
                }
            }
            ancestorItem = page.GetRightAncestor(ancestorPage);
            if (ancestorItem != null && ancestorItem.rightChildNumber != -1)    //if it has right sibling
            {
                Page rightSibling = ReadPage(ancestorItem.rightChildNumber);
                if (rightSibling.numberOfItems > order)
                {
                    TreeItem item = rightSibling.GetSmallestItem();
                    page.Insert(new TreeItem(ancestorItem.key, ancestorItem.valueOffset, -1, item.leftChildNumber));
                    if (item.leftChildNumber != -1)
                    {
                        Page child = ReadPage(item.leftChildNumber);
                        child.parentPageNumber = page.currentPageNumber;
                        SavePage(child);
                    }
                    ancestorItem.key = item.key;
                    ancestorItem.valueOffset = item.valueOffset;
                    SavePage(page);
                    SavePage(rightSibling);
                    SavePage(ancestorPage);
                    return;
                }
            }

            //merge with left
            ancestorItem = page.GetLeftAncestor(ancestorPage);
            if (ancestorItem != null && ancestorItem.leftChildNumber != -1)
            {
                Page leftSibling = ReadPage(ancestorItem.leftChildNumber);
                leftSibling.Insert(new TreeItem(ancestorItem.key, ancestorItem.valueOffset, -1, -1));
                while (page.numberOfItems != 0)
                    leftSibling.Insert(page.GetSmallestItem());
                ancestorPage.RemoveElementWithKey(ancestorItem.key);
                ancestorItem = page.GetRightAncestor(ancestorPage);
                if (ancestorItem != null)
                    ancestorItem.leftChildNumber = leftSibling.currentPageNumber;
                pagesToDelete.Add(page.currentPageNumber);
                if (ancestorPage.numberOfItems == 0)
                {
                    pagesToDelete.Add(leftSibling.currentPageNumber);
                    leftSibling.currentPageNumber = 0;
                    leftSibling.parentPageNumber = -1;
                }
                leftSibling.SetChildrensParentPageNumber(this);
                SavePage(leftSibling);
                pageList.RemoveAt(pageList.Count - 1);
                if (ancestorPage.numberOfItems == 0)
                    return;
                else if (ancestorPage.numberOfItems < order)
                    CompensateUnderflow(ancestorPage);
                else
                    SavePage(ancestorPage);
            }
            //merge with right

            ancestorItem = page.GetRightAncestor(ancestorPage);
            if (ancestorItem != null && ancestorItem.rightChildNumber != -1)
            {
                Page rightSibling = ReadPage(ancestorItem.rightChildNumber);
                rightSibling.Insert(new TreeItem(ancestorItem.key, ancestorItem.valueOffset, -1, -1));
                while (page.numberOfItems != 0)
                    rightSibling.Insert(page.GetBiggestItem());
                ancestorPage.RemoveElementWithKey(ancestorItem.key);
                ancestorItem = page.GetLeftAncestor(ancestorPage);
                if (ancestorItem != null)
                    ancestorItem.rightChildNumber = rightSibling.currentPageNumber;
                pagesToDelete.Add(page.currentPageNumber);
                if (ancestorPage.numberOfItems == 0)
                {
                    pagesToDelete.Add(rightSibling.currentPageNumber);
                    rightSibling.currentPageNumber = 0;
                    rightSibling.parentPageNumber = -1;
                }
                rightSibling.SetChildrensParentPageNumber(this);
                SavePage(rightSibling);
                pageList.RemoveAt(pageList.Count - 1);
                if (ancestorPage.numberOfItems == 0)
                    return;
                else if (ancestorPage.numberOfItems < order)
                    CompensateUnderflow(ancestorPage);
                else
                    SavePage(ancestorPage);
            }
        }

        public Tuple<int, int> RemovePage(int numberOfPage) //returns the number last page was swaped with
        {
            if ((numberOfPage + 1) * pageSize == stream.Length)
            {
                stream.SetLength(stream.Length - pageSize);
                return null;
            }
            Page lastPageInFile = ReadPage((int)stream.Length / pageSize - 1);
            int lastPageNumber = lastPageInFile.currentPageNumber;
            lastPageInFile.currentPageNumber = numberOfPage;
            lastPageInFile.SetChildrensParentPageNumber(this);
            lastPageInFile.SetParentsChildrenPageNumber(lastPageNumber, this);
            stream.SetLength(stream.Length - pageSize);
            SavePage(lastPageInFile);
            return new Tuple<int, int>(lastPageNumber, numberOfPage);
        }

        public Page GetLeftGrandSon(int pageNumber)
        {
            Page page;
            TreeItem item;
            if (pageNumber == -1)
                return null;
            while (true)
            {
                page = ReadPage(pageNumber);
                pageList.Add(page);
                item = page.itemList[page.itemList.Count - 1];
                if (item.rightChildNumber == -1)
                    return page;
                pageNumber = item.rightChildNumber;
            }
        }

        public Page GetRightGrandSon(int pageNumber)
        {
            Page page;
            TreeItem item;
            if (pageNumber == -1)
                return null;
            while (true)
            {
                page = ReadPage(pageNumber);
                item = page.itemList[0];
                if (item.leftChildNumber == -1)
                    return page;
                pageNumber = item.leftChildNumber;
            }
        }

        public void SwapItems(ref TreeItem item1, ref TreeItem item2)
        {
            int key = item1.key, offset = item1.valueOffset;
            item1.key = item2.key;
            item1.valueOffset = item2.valueOffset;
            item2.key = key;
            item2.valueOffset = offset;
        }
    }
}