using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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
        private static string RapidUpdateXMLPath = "rapidupdate.xml";

        static void Main(string[] args)
        {
            RapidUpdateXMLPath = GetRapidUpdateFeedXMLPath();
            Console.WriteLine(String.Format("RapidUpdateFeed XML file path: {0}", RapidUpdateXMLPath));

            bool isX86 = true;
            Console.WriteLine(String.Format("Dotnet x86 runtimes found on RapidUpdateFeed: {0}", String.Join(", ", getRapidUpdateRuntimes(RapidUpdateXMLPath, isX86))));
            Console.WriteLine(String.Format("Dotnet x64 runtimes found on RapidUpdateFeed: {0}", String.Join(", ", getRapidUpdateRuntimes(RapidUpdateXMLPath, !isX86))));

            Console.WriteLine(String.Format("Dotnet x86 runtimes found locally: {0}", String.Join(", ", GetRuntimes(programFilesx86))));
            Console.WriteLine(String.Format("Dotnet x64 runtimes found locally: {0}", String.Join(", ", GetRuntimes(programFilesx64))));
            
            Console.WriteLine(String.Format("Outdated Dotnet x86 runtimes: {0}", String.Join(", ", GetOldRuntimes(programFilesx86))));
            Console.WriteLine(String.Format("Outdated Dotnet x64 runtimes: {0}", String.Join(", ", GetOldRuntimes(programFilesx64))));

            DeleteOldRuntimes();
        }

        static string GetRapidUpdateFeedXMLPath()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Length >= 2)
            {
                RapidUpdateXMLPath = arguments[1];
            }
            else
            {
                if (Directory.Exists("C:\\Resources\\Directory"))
                {
                    String feedDirectory = Directory.GetDirectories("C:\\Resources\\Directory")
                                            .Where(name => name.ToLower().Contains("offlinefeed"))
                                            .ToList<String>()
                                            .FirstOrDefault<String>();
                    RapidUpdateXMLPath = String.Format("{0}\\feed-rapidupdate\\main\\feeds\\latest\\rapidupdate.xml", feedDirectory);
                }
            }
            return RapidUpdateXMLPath;
        }

        static void DeleteOldRuntimes()
        {
            Console.WriteLine("Deleting old runtimes");
            foreach (String version in GetOldRuntimes(programFilesx86))
            {
                if (IsRuntimeInUse(programFilesx86, version))
                {
                    Console.WriteLine(String.Format("{0} x86 in use", version));
                }
                else
                {
                    Console.WriteLine(String.Format("{0} x86 not in use", version));
                    DeleteRuntimes(programFilesx86, version);
                }
            }

            foreach (String version in GetOldRuntimes(programFilesx64))
            {
                if (IsRuntimeInUse(programFilesx64, version))
                {
                    Console.WriteLine(String.Format("{0} x64 in use", version));
                }
                else
                {
                    Console.WriteLine(String.Format("{0} x64 not in use", version));
                    DeleteRuntimes(programFilesx64, version);
                }
            }
        }

        static string CoreclrDLLPath(string programFiles, string version)
        {
            return string.Format("{0}\\{1}\\{2}\\{3}", programFiles, runtimesDir, version, coreclrDLL);
        }

        static string SystemRuntimeDLLPath(string programFiles, string version)
        {
            return string.Format("{0}\\{1}\\{2}\\{3}", programFiles, runtimesDir, version, systemRuntimeDLL);
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
                        // delete dll locks first to prevent race condition
                        string coreclrDLLPath = CoreclrDLLPath(programFiles, version);
                        string systemRuntimeDLLPath = SystemRuntimeDLLPath(programFiles, version);
                        if (File.Exists(coreclrDLLPath))
                        {
                            //File.Delete(coreclrDLLPath);
                        }
                        if (File.Exists(systemRuntimeDLLPath))
                        {
                            //File.Delete(systemRuntimeDLLPath));
                        }
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
                List<String> runtimesList = GetRuntimes(programfiles);
                List<String> feedRuntimes = getRapidUpdateRuntimes(RapidUpdateXMLPath, programfiles.ToLowerInvariant().Contains("x86"));
                
                foreach (String runtime in feedRuntimes)
                {
                    runtimesList.Remove(runtime);
                }

                return runtimesList;
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return new List<String> { };
            }
        }

        static List<String> getRapidUpdateRuntimes(string path, bool x86)
        {
            XElement feed = XElement.Load(path);
            IEnumerable<String> runtimes;
            if (x86)
            {
                runtimes = from entry in feed.Descendants("{http://www.w3.org/2005/Atom}entry")
                           where ((String)entry.Element("{http://www.w3.org/2005/Atom}title")).Equals("dotnet-runtime-win-x86")
                           select entry.Element("{http://www.w3.org/2005/Atom}version").Value;
            }
            else
            {
                runtimes = from entry in feed.Descendants("{http://www.w3.org/2005/Atom}entry")
                           where ((String)entry.Element("{http://www.w3.org/2005/Atom}title")).Equals("dotnet-runtime-win-x64")
                           select entry.Element("{http://www.w3.org/2005/Atom}version").Value;
            }

            return runtimes.ToList();
        }

        // check if coreclr and system.runtime dll are in use
        static bool IsRuntimeInUse(string programFiles, string version)
        {
            string coreclrDLLPath = CoreclrDLLPath(programFiles, version);
            string systemRuntimeDLLPath = SystemRuntimeDLLPath(programFiles, version);
            return IsFileLocked(coreclrDLLPath, FileAccess.Write) || IsFileLocked(systemRuntimeDLLPath, FileAccess.Write);
        }
        
        static bool IsFileLocked(string fileName, FileAccess fileAccess)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    return false;
                }
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
