using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using SharpMetal;
using SharpMetal.Foundation;
using SharpMetal.Metal;

namespace Borealis;

    [SupportedOSPlatform("macos")]
    public class MetalGpuShader
    {
        public class Moon
        {
            public Vector2 Position = Vector2.NaN;
            public Color Color = Color.Transparent;
        }

        private MetalGpuDrawer _Drawer;
        private MTLLibrary _Dev_Library;
        private MTLComputePipelineState _Dev_PipelineState;
        private MTLFunction _Dev_Function;
        private MTLBuffer _Dev_MoonBuffer; // only used if Shader uses a moon
        private float[] _MoonValue;
        private bool _UsesMoon = false;


        public MetalGpuShader(MetalGpuDrawer drawer, string fileName, bool usesMoon = false)
        {
            NSError error = new NSError(IntPtr.Zero);

            try
            {
                _Drawer = drawer;
                _UsesMoon = usesMoon;

                if (_UsesMoon)
                {
                    _MoonValue = new float[5];
                    _Dev_MoonBuffer = _Drawer.Device.NewBuffer((ulong)_MoonValue.Length * sizeof(float), MTLResourceOptions.ResourceStorageModeShared);
                }

                // 4. Compile the Metal Shader Source Code
                string shaderSource = System.IO.File.ReadAllText($"GpuCode/MetalShaders/{fileName}");
                shaderSource = shaderSource.Replace("{{WIDTH}}", drawer.Width.ToString());
                shaderSource = shaderSource.Replace("{{HEIGHT}}", drawer.Height.ToString());
                var options = new MTLCompileOptions();
                _Dev_Library = drawer.Device.NewLibrary(shaderSource, options, ref error);
                
                if (error.NativePtr != 0)
                {
                    Console.WriteLine($"Shader Compilation Error: {error.LocalizedDescription}");
                    return;
                }

                // 5. Build the Compute Pipeline State
                _Dev_Function = _Dev_Library.NewFunction("generate_image");
                _Dev_PipelineState = drawer.Device.NewComputePipelineState(_Dev_Function, ref error);

                if (error.NativePtr != 0)
                {
                    Console.WriteLine($"New Compute Pipeline State Error: {error.LocalizedDescription}");
                    return;
                }
            }
            finally
            {
                if (error.NativePtr != 0)
                {
                    throw new ApplicationException("GPU threw an error!");
                }
            }
        }

        ~MetalGpuShader()
        {
            _Dev_Function.Dispose();
            _Dev_PipelineState.Dispose();
            _Dev_Library.Dispose();
        }

        public void Draw(Moon? moon = null)
        {
            lock(_Drawer)
            {

            Marshal.Copy(_Drawer.Host_BitmapData, 0, _Drawer.Dev_InputBuffer.Contents, _Drawer.Host_BitmapData.Length);
            if (moon is not null)
            {
                _MoonValue[0] = moon.Position.X;
                _MoonValue[1] = moon.Position.Y;
                _MoonValue[2] = (float)(moon.Color.R / 255.0);
                _MoonValue[3] = (float)(moon.Color.G / 255.0);
                _MoonValue[4] = (float)(moon.Color.B / 255.0);

                Marshal.Copy(_MoonValue, 0, _Dev_MoonBuffer.Contents, _MoonValue.Length);
            }


            // 6. Encode and Dispatch the Compute Commands
            var commandBuffer = _Drawer.CommandQueue.CommandBuffer();
            var computeEncoder = commandBuffer.ComputeCommandEncoder();
            computeEncoder.SetComputePipelineState(_Dev_PipelineState);
            computeEncoder.SetBuffer(_Drawer.Dev_InputBuffer, 0, 0);
            computeEncoder.SetBuffer(_Drawer.Dev_OutputBuffer, 0, 1);
            if (_UsesMoon)
            {
                computeEncoder.SetBuffer(_Dev_MoonBuffer, 0, 2);
            }

            // 7. Calculate Grid Thread Layout
            // Query the max threads allowed per threadgroup by the hardware architecture
            ulong maxThreads = _Dev_PipelineState.MaxTotalThreadsPerThreadgroup;
            MTLSize threadGroupSize = new MTLSize{
                width = Math.Min(maxThreads, (ulong)_Drawer.Width * (ulong)_Drawer.Height),
                height = 1,
                depth = 1
            };
            MTLSize gridCount = new MTLSize{
                width = (ulong) _Drawer.Width,
                height = (ulong) _Drawer.Height,
                depth = 1
            };

            computeEncoder.DispatchThreads(gridCount, threadGroupSize);
            computeEncoder.EndEncoding();

            // 8. Commit and Block CPU until GPU Completes Work
            commandBuffer.Commit();
            commandBuffer.WaitUntilCompleted();

            // 9. Read the Output Back from GPU Memory
            nint resultPtr = _Drawer.Dev_OutputBuffer.Contents;
            Marshal.Copy(resultPtr, _Drawer.Host_BitmapData, 0, _Drawer.Host_BitmapData.Length);
            // using(var writeBuffer = bitmap.Lock())
            // {
            //     Marshal.Copy(resultPtr, h_bitmapData, 0, h_bitmapData.Length);
            // }

            computeEncoder.Dispose();
            commandBuffer.Dispose();

            } // end lock
        }
    }
