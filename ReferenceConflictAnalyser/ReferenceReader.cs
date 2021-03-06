﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using ReferenceConflictAnalyser.DataStructures;
using System.Configuration;

namespace ReferenceConflictAnalyser
{

    public class ReferenceReader
    {
        public ReferenceList Read(string entryAssemblyFilePath, bool skipSystemAssemblies = true)
        {
            if (!File.Exists(entryAssemblyFilePath))
                throw new ArgumentException(string.Format("File does not exist: {0}", entryAssemblyFilePath));

            _skipSystemAssemblies = skipSystemAssemblies;
            _result = new ReferenceList();
            _cache = new Dictionary<string, ReferencedAssembly>();

            _workingDirectory = Path.GetDirectoryName(entryAssemblyFilePath);

            AssemblyName[] entryPointReferences;
            var entryPoint = LoadEntryPoint(entryAssemblyFilePath, out entryPointReferences);
            _result.AddEntryPoint(entryPoint);

            ReadReferencesRecursively(entryPoint, entryPointReferences);

            return _result;
        }

        #region private members

        private bool _skipSystemAssemblies;
        private ReferenceList _result;
        private string _workingDirectory;
        private Dictionary<string, ReferencedAssembly> _cache;

        private void ReadReferencesRecursively(ReferencedAssembly assembly, AssemblyName[] references)
        {
            foreach (var reference in references)
            {
                if (_skipSystemAssemblies
                    &&
                        (reference.Name == "mscorlib"
                        || reference.Name == "System"
                        || reference.Name.StartsWith("System."))
                    )
                    continue;

                if (_cache.ContainsKey(reference.FullName))
                {
                    _result.AddReference(assembly, _cache[reference.FullName]);
                    continue;
                }

                AssemblyName[] referencedAssemblyReferences;
                var referencedAssembly = LoadReferencedAssembly(reference, out referencedAssemblyReferences);
                if (referencedAssembly.Category != Category.Missed)
                {
                    var isNewReference = _result.AddReference(assembly, referencedAssembly);
                    if (isNewReference)
                        ReadReferencesRecursively(referencedAssembly, referencedAssemblyReferences);
                }
                else
                {
                    _result.AddReference(assembly, referencedAssembly);
                }
            }

        }

        private ReferencedAssembly LoadEntryPoint(string filePath, out AssemblyName[] assemblyReferences)
        {
            assemblyReferences = Assembly.ReflectionOnlyLoadFrom(filePath).GetReferencedAssemblies();
            var assembly = AssemblyName.GetAssemblyName(filePath);

            var referencedAssembly = new ReferencedAssembly(assembly)
            {
                Category = Category.EntryPoint,
            };

            _cache.Add(assembly.FullName, referencedAssembly);
            return referencedAssembly;
        }


        private ReferencedAssembly LoadReferencedAssembly(AssemblyName reference, out AssemblyName[] referencedAssemblyReferences)
        {
            referencedAssemblyReferences = null;
            ReferencedAssembly referencedAssembly;
            try
            {
                var files = Directory.GetFiles(_workingDirectory, reference.Name + ".???", SearchOption.TopDirectoryOnly);
                var file = files.FirstOrDefault(x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                if (file != null)
                {
                    referencedAssemblyReferences = Assembly.ReflectionOnlyLoadFrom(file).GetReferencedAssemblies();
                    //reload the assembly name from the file (reason: the assembly name taken from the assembly loaded by the reflection only load is not complete)
                    reference = AssemblyName.GetAssemblyName(file);
                }
                else
                {
                    referencedAssemblyReferences = Assembly.ReflectionOnlyLoad(reference.FullName).GetReferencedAssemblies();
                }
                referencedAssembly = new ReferencedAssembly(reference);
            }
            catch (Exception e)
            {
                referencedAssembly = new ReferencedAssembly(reference, e);
            }

            if (!_cache.ContainsKey(reference.FullName))
                _cache.Add(reference.FullName, referencedAssembly);

            return referencedAssembly;
        }

        #endregion
    }
}
