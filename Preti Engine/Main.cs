using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;


namespace VoxelEditor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Current Directory: " + Environment.CurrentDirectory);
            var nativeWindowSettings = new NativeWindowSettings
            {
                Size = new Vector2i(1280, 720), 
                Title = "OpenTK Voxel Editor",
                WindowState = WindowState.Normal
            };
            var gameWindowSettings = GameWindowSettings.Default;
            using var window = new Window(gameWindowSettings, nativeWindowSettings);
            window.Run();
        }
    }
}