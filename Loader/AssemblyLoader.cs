using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TaiwuModLoader
{
    class AssemblyLoader
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MonoImage
        {
            public int refCount;
            public IntPtr rawDataHandle;
            public IntPtr rawData;
            public int rawDataLen;
        }

        [DllImport("mono.dll", EntryPoint = "mono_image_open_from_data_with_name", CharSet = CharSet.Ansi)]
        internal static extern IntPtr MonoOpenImage(
            IntPtr data,
            uint dataLen,
            bool needCopy,
            IntPtr status,
            bool refOnly,
            string name
        );

        [DllImport("mono.dll", EntryPoint = "mono_images_init")]
        internal static extern void MonoInitImages();

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllPath);

        public static byte[] LoadImage(string monoPath, string assemblyPath)
        {
            IntPtr pData = IntPtr.Zero;

            try
            {
                // prepare raw assembly data for native method call
                byte[] assemblyData = File.ReadAllBytes(assemblyPath);
                pData = Marshal.AllocHGlobal(assemblyData.Length);
                Marshal.Copy(assemblyData, 0, pData, assemblyData.Length);

                // load mono library and open the assembly
                LoadLibrary(monoPath);
                MonoInitImages();
                var pImage = MonoOpenImage(pData, (uint)assemblyData.Length, false, IntPtr.Zero, false, assemblyPath);
                MonoImage loadedImage = (MonoImage)Marshal.PtrToStructure(pImage, typeof(MonoImage));

                // dump the decoded assembly
                byte[] dumpedData = new byte[loadedImage.rawDataLen];
                Marshal.Copy(loadedImage.rawData, dumpedData, 0, loadedImage.rawDataLen);
                return dumpedData;
            }
            finally
            {
                if (pData != IntPtr.Zero) Marshal.FreeHGlobal(pData);
            }
        }
    }
}
