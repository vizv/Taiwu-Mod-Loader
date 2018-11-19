using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace TaiwuModLoader
{
    /// <summary>
    /// The EasyHook injection entrypoint.
    /// </summary>
    public class InjectionEntryPoint : EasyHook.IEntryPoint
    {
        /// <summary>
        /// Reference to the server interface within TaiwuFragmentsHook.
        /// </summary>
        ServerInterface _server = null;

        /// <summary>
        /// Message queue of all files accessed.
        /// </summary>
        Queue<string> _messageQueue = new Queue<string>();

        /// <summary>
        /// Constructor for the EasyHook entrypoint.
        /// </summary>
        /// <param name="context">The RemoteHooking context</param>
        /// <param name="channelName">The name of the IPC channel</param>
        public InjectionEntryPoint(EasyHook.RemoteHooking.IContext context, string channelName)
        {
            // Connect to server object using provided channel name
            _server = EasyHook.RemoteHooking.IpcConnectClient<ServerInterface>(channelName);

            // If Ping fails then the Run method will be not be called
            _server.Ping();
        }

        /// <summary>
        /// Logic of the entrypoint.
        /// </summary>
        /// <param name="context">The RemoteHooking context</param>
        /// <param name="channelName">The name of the IPC channel</param>
        public void Run(EasyHook.RemoteHooking.IContext context, string channelName)
        {
            EasyHook.LocalHook monoOpenImageHook = null;
            var filename = @"Mono\EmbedRuntime\mono.dll";
            LoadLibraryW(filename);

            // Install the hook
            IntPtr pTargetProc = EasyHook.LocalHook.GetProcAddress(filename, "mono_image_open_from_data_with_name");
            monoOpenImageHook = EasyHook.LocalHook.Create(pTargetProc, new MonoOpenImage_Delegate(MonoOpenImage_Hook), this);
            monoOpenImageHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
            
            // Wake up the process
            EasyHook.RemoteHooking.WakeUpProcess();

            while (true)
            {
                Thread.Sleep(500);

                string[] queued = null;

                lock (_messageQueue)
                {
                    queued = _messageQueue.ToArray();
                    _messageQueue.Clear();
                }

                // Send newly received message back to Loader.exe
                if (queued != null && queued.Length > 0)
                {
                    _server.OutputMessages(queued);
                }
                else
                {
                    _server.Ping();
                }
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibraryW(string lpFileName);

        #region ImageOpen Hook
        delegate IntPtr MonoOpenImage_Delegate(IntPtr data, uint dataLen, bool needCopy, IntPtr status, bool refOnly, string name);

        [DllImport("mono.dll", EntryPoint = "mono_image_open_from_data_with_name", CharSet = CharSet.Ansi)]
        internal static extern IntPtr MonoOpenImage(IntPtr data, uint dataLen, bool needCopy, IntPtr status, bool refOnly, string name);

        IntPtr MonoOpenImage_Hook(IntPtr data, uint dataLen, bool needCopy, IntPtr status, bool refOnly, string name)
        {
            const string originalFilename = @"Assembly-CSharp.dll";
            const string overrideFilename = @"Assembly-CSharp-Override.dll";

            if (name.EndsWith(originalFilename))
            {
                string dumpedFilename = @"Assembly-CSharp-Patched.dll";

                var originalPath = name;
                string overridePath = originalPath.Replace(originalFilename, overrideFilename);
                if (File.Exists(overridePath)) dumpedFilename = overrideFilename;

                var patchedPath = originalPath.Replace(originalFilename, dumpedFilename);

                _server.OutputMessage(string.Format("Detour {0} to: {1}", originalFilename, dumpedFilename));
                
                byte[] assemblyData = File.ReadAllBytes(patchedPath);
                data = Marshal.AllocHGlobal(assemblyData.Length);
                dataLen = (uint)assemblyData.Length;
                name = patchedPath;

                Marshal.Copy(assemblyData, 0, data, (int)dataLen);
            }

            return MonoOpenImage(data, dataLen, needCopy, status, refOnly, name);
        }
        #endregion
    }
}
