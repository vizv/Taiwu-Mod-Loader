using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using UnityModManagerNet;

namespace TaiwuModLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            const string originalFilename = @"Assembly-CSharp.dll";
            const string patchedFilename = @"Assembly-CSharp-Patched.dll";

            // find the assembly
            string originalPath;
            string[] matches = Directory.GetFiles(
                Directory.GetCurrentDirectory(),
                originalFilename,
                SearchOption.AllDirectories
            );
            if (matches.Length != 1)
            {
                Console.Error.WriteLine(string.Format("Error: Failed to find \"{0}\"", originalFilename));
                return;
            }
            originalPath = matches[0];
            Console.WriteLine(string.Format("Found assembly: {0}", originalPath));

            // load the assembly image
            byte[] assemblyData = AssemblyLoader.LoadImage(@"Mono\EmbedRuntime\mono.dll", originalPath);

            // prepare the assembly image
            ModuleDefMD assembly = ModuleDefMD.Load(assemblyData);

            TypeDef targetClass = assembly.Types.FirstOrDefault((TypeDef x) => x.FullName == "DateFile");
            MethodDef targetMethod = targetClass.Methods.FirstOrDefault((MethodDef x) => x.Name == "Awake");

            Type modManagerType = typeof(UnityModManager);
            string patchedPath = originalPath.Replace(originalFilename, patchedFilename);

            TypeDef modManagerInjected = assembly.Types.FirstOrDefault((TypeDef x) => x.Name == modManagerType.Name);
            if (modManagerInjected == null)
            {
                // patch the assembly image if the image has not been injected with UMM
                ModuleDefMD moduleDefMD = ModuleDefMD.Load(modManagerType.Module);
                TypeDef modManager = moduleDefMD.Types.First((TypeDef x) => x.Name == modManagerType.Name);
                modManager.Fields.First((FieldDef x) => x.Name == "modsDirname").Constant.Value = "Mods";
                modManager.Fields.First((FieldDef x) => x.Name == "infoFilename").Constant.Value = "Info.json";
                moduleDefMD.Types.Remove(modManager);
                assembly.Types.Add(modManager);
                Instruction instr = OpCodes.Call.ToInstruction(modManager.Methods.First((MethodDef x) => x.Name == "Start"));
                targetMethod.Body.Instructions.Insert(targetMethod.Body.Instructions.Count - 1, instr);

                // write the patched assembly image
                assembly.Write(patchedPath);
                Console.WriteLine(string.Format("Write patched assembly: {0}", patchedPath));
            }
            else
            {
                // write the loaded assembly image if the image has already been patched
                File.WriteAllBytes(patchedPath, assemblyData);
                Console.WriteLine(string.Format("Assembly has already been patched: {0}", patchedPath));
            }

            // copy library
            string libraryPath = originalPath.Replace(originalFilename, "");
            File.Copy(Path.Combine(Directory. GetCurrentDirectory(), "0Harmony12.dll"), Path.Combine(libraryPath, "0Harmony12.dll"), true);

            int targetPID = 0;
            const string targetExe = "The Scroll Of Taiwu Alpha V1.0.exe";

            string channelName = null;

            EasyHook.RemoteHooking
                .IpcCreateServer<ServerInterface>(
                ref channelName,
                System.Runtime.Remoting.WellKnownObjectMode.Singleton);
            
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location),
                "Hook.dll");

            try
            {
                EasyHook.RemoteHooking.CreateAndInject(
                    targetExe,
                    "",
                    0,
                    EasyHook.InjectionOptions.DoNotRequireStrongName,
                    injectionLibrary,
                    injectionLibrary,
                    out targetPID,
                    channelName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            var p = Process.GetProcessById(targetPID);
            p.WaitForExit();

            Console.WriteLine("Game process exited, press any key to exit...");
            Console.ReadKey();
        }
    }
}
