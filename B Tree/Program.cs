using System;
using System.Linq;
using System.Diagnostics;

namespace B_Tree
{
    class Program
    {
        static void Main(string[] args)
        {
            int order = 2;
            int key;
            int offset;
            int max;
            int amount;
            string answear;
            string[] parts;
            Random random = new Random();
            DiskFunctionality df = new DiskFunctionality(order);
            Console.WriteLine("i key value    insert value");
            Console.WriteLine("ii amount max  insert random keys");
            Console.WriteLine("d key          delete key");
            Console.WriteLine("dd amount max  remove random keys");
            Console.WriteLine("u key value    update value");
            Console.WriteLine("v              view data file"); 
            Console.WriteLine("x              destroy tree");
            Console.WriteLine("f key          find key");
            Console.WriteLine("c              check");
            Console.WriteLine("q              quit");
            Console.WriteLine("o f            operations from file");
            Console.WriteLine("rw             display number of operations");
            Console.WriteLine("z              zero rw data");
            while (true)
            {
                answear = Console.ReadLine();
                parts = answear.Split(' ');
                if (parts[0] == "i")
                {
                    try
                    {
                        key = Int32.Parse(parts[1]);
                        if (parts.Length == 2)
                            offset = Int32.Parse(parts[1]);
                        else
                            offset = Int32.Parse(parts[2]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    df.InsertIntoTree(key);
                }
                else if (parts[0] == "rw")
                    df.DisplayDiskIOinfo();
                else if (parts[0] == "z")
                    df.ZeroIOdata();
                else if (parts[0] == "ii")
                {
                    try
                    {
                        amount = Int32.Parse(parts[1]);
                        max = Int32.Parse(parts[2]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    for (int i = 0, tryNr = 0; i < amount; i++, tryNr++)
                    {
                        if (tryNr == 5 * amount)
                            break;
                        key = random.Next(0, max);
                        //Console.WriteLine("Inserting " + key);
                        if (!df.InsertIntoTree(key))
                            i--;
                    }
                }
                else if (parts[0] == "v")
                {
                    df.ViewDataFile();
                }
                else if (parts[0] == "dd")
                {
                    try
                    {
                        amount = Int32.Parse(parts[1]);
                        max = Int32.Parse(parts[2]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    for (int i = 0, tryNr = 0; i < amount; i++, tryNr++)
                    {
                        if (tryNr == 5 * amount)
                            break;
                        key = random.Next(0, max);
                        Console.WriteLine("Deleting " + key);
                        if (!df.Delete(key))
                            i--;
                    }
                }
                else if (parts[0] == "x")
                {
                    Console.WriteLine("Are you sure? y/n");
                    answear = Console.ReadLine();
                    if (answear == "y")
                        df.DestroyTree();
                }
                else if (parts[0] == "o")
                {
                    try
                    {
                        df.ExecuteOperations(parts[1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else if (parts[0] == "u")
                {
                    try
                    {
                        key = Int32.Parse(parts[1]);
                        offset = Int32.Parse(parts[2]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    df.Update(key, offset);
                }
                else if (parts[0] == "f")
                {
                    try
                    {
                        key = Int32.Parse(parts[1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    Page page = df.FindInTree(key, Constants.FIND);
                    if (page is null)
                        Console.WriteLine("Key doesnt exist");
                    else
                    {
                        TreeItem item = page.GetItem(key);
                        df.dataStream.Position = item.valueOffset;
                        byte[] data = new byte[4];
                        df.dataStream.Read(data, 0, 4);
                        Console.WriteLine("Key " + key + " found. Offset is " + item.valueOffset + ". Value is " + BitConverter.ToInt32(data));
                    }
                }
                else if (parts[0] == "q")
                    break;
                else if (parts[0] == "s")
                {
                    try
                    {
                        key = Int32.Parse(parts[1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    df.ShowPage(key);
                }
                else if (parts[0] == "c")
                {
                    try
                    {
                        df.CheckUp();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    Console.WriteLine("OK");
                }
                else if (parts[0] == "d")
                {
                    try
                    {
                        key = Int32.Parse(parts[1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    df.Delete(key);
                }
            }
        }

    }
}
