using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CleanupDotnet
{
    class Program
    {
        private static String programFilesx64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).ToString();
        private static String programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ToString();
        private const String coreclrDLL = "coreclr.dll";
        private const String systemRuntimeDLL = "System.Runtime.dll";
        private const String runtimesDir = "dotnet\\shared\\Microsoft.NETCore.App";
        private static List<String> directoriesToDelete = new List<string>() {
            runtimesDir,
            "dotnet\\shared\\Microsoft.AspNetCore.All",
            "dotnet\\shared\\Microsoft.AspNetCore.App",
            "dotnet\\host\\fxr"};

        static void Main(string[] args)
        {
            Console.WriteLine("Found dotnet runtimes!");
            Console.WriteLine("x86");
            foreach (String version in GetRuntimes(programFilesx86))
            {
                Console.WriteLine(version);
            }
            Console.WriteLine("x64");
            foreach (String version in GetRuntimes(programFilesx64))
            {
                Console.WriteLine(version);
            }

            Console.WriteLine("old dotnet runtimes");
            Console.WriteLine("x86");
            foreach (String version in GetOldRuntimes(programFilesx86))
            {
                Console.WriteLine(version);
            }
            Console.WriteLine("x64");
            foreach (String version in GetOldRuntimes(programFilesx64))
            {
                Console.WriteLine(version);
            }

            Console.WriteLine("x86");
            foreach (String version in GetRuntimes(programFilesx86))
            {
                if (IsRuntimeInUse(programFilesx86, version)) 
                {
                    Console.WriteLine(String.Format("{0} in use", version));
                }
                else
                {
                    Console.WriteLine(String.Format("{0} not in use", version));
                    DeleteRuntimes(programFilesx86, version);
                }
            }
            Console.WriteLine("x64");
            foreach (String version in GetRuntimes(programFilesx64))
            {
                if (IsRuntimeInUse(programFilesx64, version))
                {
                    Console.WriteLine(String.Format("{0} in use", version));
                }
                else
                {
                    Console.WriteLine(String.Format("{0} not in use", version));
                    DeleteRuntimes(programFilesx64, version);
                }
            }
        }

        static void DeleteRuntimes(string programFiles, string version)
        {
            try
            {
                foreach(String directory in directoriesToDelete)
                {
                    string directoryPath = String.Format("{0}\\{1}\\{2}", programFiles, directory, version);
                    if (Directory.Exists(directoryPath))
                    {
                        //Directory.Delete(directoryPath, true);
                        Console.WriteLine(String.Format("Deleted {0}", directoryPath));

                        string installedMarkerFile = String.Format("{0}\\{1}\\{2}.installed", programFiles, directory, version);
                        if (File.Exists(installedMarkerFile))
                        {
                            File.Delete(installedMarkerFile);
                        }
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Could not find directory {0}", directoryPath));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Failed to delete runtime {0} {1}", programFiles, version));
                Console.WriteLine(ex.ToString());
            }
        }

        static List<string> GetRuntimes(string programfiles)
        {
            try
            {
                List<string> runtimes = new List<String>();

                String directory = String.Format("{0}\\{1}", programfiles, runtimesDir);
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine("no runtimes found");
                    return new List<String> { };
                }
                foreach (string path in Directory.GetDirectories(directory))
                {
                    runtimes.Add(path.Split("\\").Last());
                }

                return runtimes;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new List<String> { };
            }
        }

        static List<String> GetOldRuntimes(string programfiles)
        {
            try
            {
                Dictionary<String, List<String>> runtimesDictionary = new Dictionary<String, List<String>>();
                List<String> runtimesList = GetRuntimes(programfiles);

                // group runtimes by major version
                foreach (String version in runtimesList)
                {
                    string major = version.Substring(0, version.LastIndexOf("."));
                    string minor = version.Substring(version.LastIndexOf(".") + 1);

                    if (runtimesDictionary.ContainsKey(major))
                    {
                        runtimesDictionary[major].Add(minor);
                    }
                    else
                    {
                        runtimesDictionary.Add(major, new List<String> { minor });
                    }
                }

                // for each major version, remove latest version
                foreach (String key in runtimesDictionary.Keys)
                {
                    runtimesList.Remove(String.Format("{0}.{1}", key, runtimesDictionary[key].Max()));
                }

                return runtimesList;
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return new List<String> { };
            }
        }

        static bool IsRuntimeInUse(string programFiles, string version)
        {
            string coreclrDLLPath = string.Format("{0}\\{1}\\{2}\\{3}", programFiles, runtimesDir, version, coreclrDLL);
            string systemRuntimeDLLPath = string.Format("{0}\\{1}\\{2}\\{3}", programFiles, runtimesDir, version, systemRuntimeDLL);
            return IsFileLocked(coreclrDLLPath, FileAccess.Write) || IsFileLocked(systemRuntimeDLLPath, FileAccess.Write);
        }
        
        static bool IsFileLocked(string fileName, FileAccess fileAccess)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Open, fileAccess))
                {
                    fs.Close();
                }

                return false;
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("it is being used by another process"))
                {
                    Console.WriteLine(ex);
                }
                return true;
            }
        }

    }
}
