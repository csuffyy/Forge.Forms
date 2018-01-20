﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Forge.Forms.Controls;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.Win32;

namespace Forge.Forms
{
    public static class HotReloadManager
    {
        private static bool HasAlreadyBeenInitialized { get; set; }

        public static void Init(string directory)
        {
            if (HasAlreadyBeenInitialized)
                return;

            var watcher = new FileSystemWatcher
            {
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                                        | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.cs",
                Path = directory,
                IncludeSubdirectories = true
            };

            watcher.Changed += OnChanged;
            watcher.Error += (sender, eventArgs) => { };
            watcher.EnableRaisingEvents = true;
            HasAlreadyBeenInitialized = true;
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    var types = GetTypesFromFile(e.FullPath).ToList();
                    var dynamicForms =
                        DynamicForm.ActiveForms.Where(i =>
                            types.Select(o => o.FullName).Contains(i.Model?.GetType().FullName));

                    foreach (var dynamicForm in dynamicForms)
                    {
                        dynamicForm.Model =
                            Activator.CreateInstance(
                                types.Last(i => i.FullName == dynamicForm.Model?.GetType().FullName));
                    }
                });
            }
            catch
            {
            }
        }

        internal static string GetVisualStudioInstalledPath()
        {
            var visualStudioInstalledPath = string.Empty;
            var visualStudioRegistryPath =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0");
            if (visualStudioRegistryPath != null)
            {
                visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
            }

            if (string.IsNullOrEmpty(visualStudioInstalledPath) || !Directory.Exists(visualStudioInstalledPath))
            {
                visualStudioRegistryPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0");
                if (visualStudioRegistryPath != null)
                {
                    visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
                }
            }

            if (string.IsNullOrEmpty(visualStudioInstalledPath) || !Directory.Exists(visualStudioInstalledPath))
            {
                visualStudioRegistryPath =
                    Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\12.0");
                if (visualStudioRegistryPath != null)
                {
                    visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
                }
            }

            if (string.IsNullOrEmpty(visualStudioInstalledPath) || !Directory.Exists(visualStudioInstalledPath))
            {
                visualStudioRegistryPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\12.0");
                if (visualStudioRegistryPath != null)
                {
                    visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
                }
            }

            if (string.IsNullOrEmpty(visualStudioInstalledPath) || !Directory.Exists(visualStudioInstalledPath))
            {
                visualStudioRegistryPath =
                    Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7");
                if (visualStudioRegistryPath != null)
                {
                    visualStudioInstalledPath = visualStudioRegistryPath.GetValue("15.0", string.Empty) as string;
                }
            }

            return visualStudioInstalledPath;
        }

        private static CSharpCodeProvider CSharpCodeProvider()
        {
            var csc = new CSharpCodeProvider();
            var settings = csc
                .GetType()
                .GetField("_compilerSettings", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(csc);

            var path = settings?.GetType()
                .GetField("_compilerFullPath", BindingFlags.Instance | BindingFlags.NonPublic);

            path?.SetValue(settings,
                Path.Combine(
                    $"{GetVisualStudioInstalledPath()}MSBuild\\15.0\\Bin\\Roslyn",
                    "csc.exe"));

            return csc;
        }

        private static IEnumerable<Type> GetTypesFromFile(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var readToEnd = reader.ReadToEnd();
                    return CompileFile(readToEnd);
                }
            }
        }

        private static IEnumerable<Type> CompileFile(string code)
        {
            var provider = CSharpCodeProvider();

            var parameters = new CompilerParameters();

            foreach (var assemblyName in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    parameters.ReferencedAssemblies.Add(assemblyName.CodeBase.Replace("file:///", ""));
                }
                catch
                {
                }
            }


            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;
            var results = provider.CompileAssemblyFromSource(parameters, code);
            if (results.Errors.HasErrors)
            {
                var sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine($"Error ({error.ErrorNumber}): {error.ErrorText}");
                }

                throw new InvalidOperationException(sb.ToString());
            }

            var assembly = results.CompiledAssembly;
            return assembly.GetTypes();
        }
    }
}