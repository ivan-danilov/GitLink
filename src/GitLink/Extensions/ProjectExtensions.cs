﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectExtensions.cs" company="CatenaLogic">
//   Copyright (c) 2014 - 2014 CatenaLogic. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace GitLink
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Catel;
    using Catel.Logging;
    using Microsoft.Build.Evaluation;
    using Pdb;

    public static class ProjectExtensions
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public static string GetProjectName(this Project project)
        {
            Argument.IsNotNull(() => project);

            var projectName = project.GetPropertyValue("MSBuildProjectName");
            return projectName ?? Path.GetFileName(project.FullPath);
        }

        public static void CreateSrcSrv(this Project project, string rawUrl, string revision, Dictionary<string, string> paths)
        {
            Argument.IsNotNull(() => project);
            Argument.IsNotNullOrWhitespace(() => rawUrl);
            Argument.IsNotNullOrWhitespace(() => revision);

            var srcsrvFile = GetOutputSrcSrvFile(project);

            File.WriteAllBytes(srcsrvFile, SrcSrv.Create(rawUrl, revision, paths.Select(x => new Tuple<string, string>(x.Key, x.Value))));
        }

        public static IEnumerable<ProjectItem> GetCompilableItems(this Project project)
        {
            Argument.IsNotNull(() => project);

            return project.Items.Where(x => string.Equals(x.ItemType, "Compile") || string.Equals(x.ItemType, "ClCompile") || string.Equals(x.ItemType, "ClInclude"));
        }

        public static Dictionary<string, string> VerifyPdbFiles(this Project project, IEnumerable<string> files)
        {
            Argument.IsNotNull(() => project);

            var pdbFile = GetOutputPdbFile(project);

            using (var pdb = new PdbFile(pdbFile))
            {
                return pdb.VerifyPdbFiles(files);
            }
        }

        public static string GetOutputSrcSrvFile(this Project project)
        {
            Argument.IsNotNull(() => project);

            var pdbFile = GetOutputPdbFile(project);
            return string.Format("{0}.srcsrv", pdbFile);
        }

        public static string GetOutputPdbFile(this Project project)
        {
            Argument.IsNotNull(() => project);

            var outputFile = project.GetOutputFile();
            var pdbFile = Path.ChangeExtension(outputFile, ".pdb");

            return pdbFile;
        }

        public static string GetOutputFile(this Project project)
        {
            Argument.IsNotNull(() => project);

            var targetDirectory = GetTargetDirectory(project);
            var targetFileName = GetTargetFileName(project);

            var outputFile = Path.Combine(targetDirectory, targetFileName);
            outputFile = Path.GetFullPath(outputFile);

            return outputFile;
        }

        public static string GetTargetDirectory(this Project project)
        {
            Argument.IsNotNull(() => project);

            var targetDirectory = project.GetPropertyValue("TargetDir");
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                var relativeTargetDirectory = GetRelativeTargetDirectory(project);
                targetDirectory = Path.Combine(project.DirectoryPath, relativeTargetDirectory);
            }

            return targetDirectory;
        }

        public static string GetRelativeTargetDirectory(this Project project)
        {
            Argument.IsNotNull(() => project);

            var projectOutputDirectory = project.GetPropertyValue("OutputPath");
            if (string.IsNullOrWhiteSpace(projectOutputDirectory))
            {
                projectOutputDirectory = project.GetPropertyValue("OutDir");
            }

            return projectOutputDirectory;
        }

        public static string GetTargetFileName(this Project project)
        {
            Argument.IsNotNull(() => project);

            var targetFileName = project.GetPropertyValue("TargetFileName");
            if (string.IsNullOrWhiteSpace(targetFileName))
            {
                var assemblyName = project.GetPropertyValue("AssemblyName");
                var extension = GetTargetExtension(project);

                targetFileName = string.Format("{0}{1}", assemblyName, extension);
            }

            return targetFileName;
        }

        public static string GetTargetExtension(this Project project)
        {
            Argument.IsNotNull(() => project);

            var extension = project.GetPropertyValue("TargetExt");
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".dll";

                var outputType = project.GetPropertyValue("OutputType").ToLower();
                if (outputType.Contains("exe") || outputType.Contains("winexe"))
                {
                    extension = ".exe";
                }
            }

            return extension;
        }
    }
}