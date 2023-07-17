/// Summarize disk usage
/// Author: Chen Lin

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace du
{

    class Program
    {

        static void Main(string[] args)
        {

            if (args.Length != 2)
            {
                helpMessage();
            }
            else
            {

                string comm = args[0];
                string path = args[1];

                if (comm == "-s")
                {
                    sequentialMode(path);
                }
                else if (comm == "-d")
                {
                    parallelMode(path);
                }
                else if (comm == "-b")
                {
                    parallelMode(path);
                    sequentialMode(path);
                }
                else
                {
                    helpMessage();
                }

            }

        }


        static void helpMessage()
        {
            Console.WriteLine(
                "Usage: du [-s] [-d] [-b] <path>\n" +
                "Summarize disk usage of the set of FILES, recursively for directories.\n" +
                "You MUST specify one of the parameters, -s, -d, or -b\n" +
                "-s          Run in single threaded mode\n" +
                "-d          Run in parallel mode (use all available processors\n" +
                "-b          Run in both parallel and single threaded mode.\n" +
                "            Runs parallel followed by sequential mode\n");
        }


        static void parallelMode(string path)
        {

            double parallelTime = 0;
            int numFolders = 0;
            int numFiles = 0;
            long totalSize = 0;

            try
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();

                Parallel.ForEach(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories),
                    filePath =>
                    {

                        Interlocked.Increment(ref numFiles);
                        var f = new FileInfo(filePath).Length;
                        Interlocked.Add(ref totalSize, f);

                    });


                Parallel.ForEach(Directory.EnumerateDirectories(path, "*",
                    SearchOption.AllDirectories), filePath =>
                    {
                        Interlocked.Increment(ref numFolders);

                    });

                sw.Stop();

                parallelTime += sw.Elapsed.TotalSeconds;

                Console.WriteLine($"Directory '{path}':\n");
                Console.WriteLine($"Parallel Calculated in: {parallelTime}s");
                Console.WriteLine($"{numFolders} Folders, " + $"{numFiles} Files, " + $"{totalSize} bytes.\n");

            }

            catch (DirectoryNotFoundException d)
            {

                Console.WriteLine($"{d.Message}\n");
                helpMessage();
            }

        }


        static void sequentialMode(string path)
        {
            double sequentialTime = 0;
            int numFolders = 0;
            int numFiles = 0;
            long totalBytes = 0;

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                foreach (string filePath in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories))
                {

                    if (File.Exists(filePath))
                    {
                        numFiles++;

                        totalBytes += new FileInfo(filePath).Length;
                    }
                    else
                    {
                        numFolders++;

                    }

                }

                sw.Stop();

                sequentialTime += sw.Elapsed.TotalSeconds;

                Console.WriteLine($"Directory '{path}':\n");
                Console.WriteLine($"Sequential Calculated in: {sequentialTime}s");
                Console.WriteLine($"{numFolders} Folders, " + $"{numFiles} Files, " + $"{totalBytes} bytes.");

            }

            catch (DirectoryNotFoundException d)
            {
                Console.WriteLine($"{d.Message}\n");
                helpMessage();
            }

        }

    }
}


