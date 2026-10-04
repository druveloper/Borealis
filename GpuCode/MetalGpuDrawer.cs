using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Borealis.GpuCode;
using SharpMetal;
using SharpMetal.Foundation;
using SharpMetal.Metal;

namespace Borealis;

// The library uses the SupportedOSPlatform attribute to stay true to macOS targets
[SupportedOSPlatform("macos")] 
public class MetalGpuDrawer : GpuDrawer
{
    public MTLDevice Device = MTLDevice.CreateSystemDefaultDevice();
    public MTLCommandQueue CommandQueue;
    public MTLBuffer Dev_InputBuffer;
    public MTLBuffer Dev_OutputBuffer;

    private MetalGpuShader _OverlayShader;
    private MetalGpuShader _TransformShader;
    private BlockingCollection<Task> _ShaderTasks = new BlockingCollection<Task>();
    private Task _ShaderThread;

    
    public MetalGpuDrawer(byte[] bitmapData, int width, int height) : base(bitmapData, width, height)
    {
        var error = new NSError(IntPtr.Zero);
        try
        {
            _Width = width;
            _Height = height;
            Host_BitmapData = bitmapData;

            // 1. Initialize GPU Device and Command Queue
            Device = MTLDevice.CreateSystemDefaultDevice();
            if (Device.NativePtr == IntPtr.Zero)
            {
                throw new NotSupportedException("Metal is not supported or no Metal GPU device was found.");

            }
            CommandQueue = Device.NewCommandQueue();

            // 3. Allocate GPU memory Buffers
            uint bufferSize = (uint)bitmapData.Length;
            Dev_InputBuffer = Device.NewBuffer(bufferSize, MTLResourceOptions.ResourceStorageModeShared);
            Dev_OutputBuffer = Device.NewBuffer(bufferSize, MTLResourceOptions.ResourceStorageModeShared);

            // set Shaders
            _OverlayShader = new MetalGpuShader(this, "Overlay.metal", true);
            _TransformShader = new MetalGpuShader(this, "Transform.metal");

            // start shader task queue
            _ShaderThread = new Task(shaderTaskQueue);
            _ShaderThread.Start();
        }
        finally
        {
            if (error.NativePtr != 0)
            {
                throw new ApplicationException("GPU threw an error!");
            }
        }
    }

    ~MetalGpuDrawer()
    {
        Dev_InputBuffer.Dispose();
        Dev_OutputBuffer.Dispose();
        CommandQueue.Dispose();
        Device.Dispose();

        _ShaderThread.Dispose();
    }

    public override void DrawOverlay(GpuDrawer.IOverlay overlay, GpuDrawer.Moon? moon = null)
    {
        _ShaderTasks.Add(new Task(() => {
            _OverlayShader.Draw(moon);
        }));
    }
    
    public override void DrawTransform(GpuDrawer.ITransform transform)
    {
        _ShaderTasks.Add(new Task(() => {
            _TransformShader.Draw();
        }));
    }

    protected override IOverlay newOverlay(string R_Function, string G_Function, string B_Function)
    {
        return _OverlayShader;
    }

    protected override ITransform newTransform(string X_Function, string Y_Function)
    {
        return _TransformShader;
    }

    private void shaderTaskQueue()
    {
        foreach(Task shaderTask in _ShaderTasks.GetConsumingEnumerable())
        {
            shaderTask.RunSynchronously();
        }
    }

}

