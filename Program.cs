
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SetLastAccess2
{
    class Program
    {
        static Program()
        {
            DefaultTuple = new Tuple<DateTime, DateTime>(default(DateTime), default(DateTime));
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: SetLastAccess.exe <path-to-folder> [/P]" + Environment.NewLine +
                    "if /P present - user parallel processing" + Environment.NewLine +
                    "Press any key to exit");
                Console.ReadKey();
                return;
            }

            var path = args[0];
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar.ToString();
            }

            bool bParalell = false;
            if (args.Length == 2 && args[1].ToUpper() == "/P")
            {
                bParalell = true;
            }

            if (bParalell)
            {
                ProcessFolderParalell(new DirectoryInfo(path));
            }
            else
            {
                ProcessFolder(new DirectoryInfo(path));
            }
            Console.WriteLine("All Done. Press any key to exit");
            Console.ReadKey();
        }


        private static Tuple<DateTime, DateTime> ProcessFolder(DirectoryInfo directoryInfo)
        {
            Console.WriteLine(directoryInfo.FullName);
            try
            {
                var items = directoryInfo.EnumerateFileSystemInfos();
                var max = items.Select(
                    info =>
                    {
                        if (info is DirectoryInfo)
                        {
                            return ProcessFolder(info as DirectoryInfo);
                        }
                        return new Tuple<DateTime, DateTime>(info.LastAccessTime, info.LastWriteTime);
                    })
                    .OrderBy(tuple => tuple.Item2)
                    .LastOrDefault();

                if (max != null)
                {
                    try
                    {
                        directoryInfo.LastAccessTime = max.Item1;
                        directoryInfo.LastWriteTime = max.Item2;
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                    }

                    return max;
                }
                else
                {
                    return new Tuple<DateTime, DateTime>(directoryInfo.LastAccessTime, directoryInfo.LastWriteTime);

                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            return DefaultTuple;
        }

        private static Tuple<DateTime, DateTime> ProcessFolderParalell(DirectoryInfo directoryInfo)
        {
            Console.WriteLine(directoryInfo.FullName);
            var dates = new BlockingCollection<Tuple<DateTime, DateTime>>(new ConcurrentBag<Tuple<DateTime, DateTime>>());
            try
            {

                var items = directoryInfo.EnumerateFileSystemInfos();
                Parallel.ForEach(items, info =>
                                            {
                                                if (info is DirectoryInfo)
                                                {
                                                    dates.Add(ProcessFolderParalell(info as DirectoryInfo));
                                                }
                                                else
                                                {
                                                    dates.Add(new Tuple<DateTime, DateTime>(info.LastAccessTime,
                                                                                            info.LastWriteTime));
                                                }
                                            });

                var max = dates.GetConsumingEnumerable()
                    .OrderBy(tuple => tuple.Item2)
                    .Last();

                try
                {
                    directoryInfo.LastAccessTime = max.Item1;
                    directoryInfo.LastWriteTime = max.Item2;
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                }

                return max;
            }

            catch (DirectoryNotFoundException ex)
            {

                Console.WriteLine("Error: {0}", ex.Message);
            }
            return DefaultTuple;
        }


        public static Tuple<DateTime, DateTime> DefaultTuple { get; set; }
    }
}
