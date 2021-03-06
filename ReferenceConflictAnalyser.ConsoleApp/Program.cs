﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ReferenceConflictAnalyser.DataStructures;

namespace ReferenceConflictAnalyser.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new ReferenceReader();
            var analyser = new ReferenceAnalyser();
            var entryAssemblyPath = args[0];

            while (string.IsNullOrWhiteSpace(entryAssemblyPath))
            {
                Console.WriteLine("Enter path to assembly to analyze:");
                entryAssemblyPath = Console.ReadLine();
            }

            string configFilePath;
            ConfigurationHelper.TrySuggestConfigFile(entryAssemblyPath, out configFilePath);

            var result = reader.Read(entryAssemblyPath);

            result = analyser.AnalyzeReferences(result, ConfigurationHelper.GetBindingRedirects(configFilePath));



            Console.WriteLine("Select outout mode: C - outout to console, D - output to dgml file");
            var mode = Console.ReadKey();
            Console.WriteLine();

            switch (mode.Key)
            {
                case ConsoleKey.C:
                    WriteReferencesToConsole(result);
                    break;

                case ConsoleKey.D:
                    WriteReferencesToDgmlFile(result);
                    break;

            }
        }

        private static void WriteReferencesToDgmlFile(ReferenceList result)
        {
            var builder = new GraphBuilder();
            var doc = builder.BuildDgml(result);

            var path = Path.Combine(Path.GetTempFileName() + ".dgml");
            doc.Save(path);

            Process.Start(path);
        }

        private static void WriteReferencesToConsole(ReferenceList result)
        {
            Console.WriteLine("References:");
            foreach (var item in result.References)
                Console.WriteLine($"{item.Assembly.Name} {item.Assembly.Version} -> {item.ReferencedAssembly.Name} {item.ReferencedAssembly.Version}");

            Console.WriteLine();
            Console.WriteLine("Assemblies:");
            foreach (var item in result.Assemblies)
                Console.WriteLine($"{item.Name} {item.Version}: {item.Category}");

            Console.ReadKey();
        }

 
    }
}
