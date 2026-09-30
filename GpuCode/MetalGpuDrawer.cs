using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpMetal.Metal;
using SharpMetal.Foundation;
using SharpMetal;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Borealis;

// The library uses the SupportedOSPlatform attribute to stay true to macOS targets
[SupportedOSPlatform("macos")] 
public class MetalGpuDrawer
{
    public int Width { get {return _Width;} }
    public int Height { get {return _Height;} }
    public byte[] Host_BitmapData;
    public MTLDevice Device = MTLDevice.CreateSystemDefaultDevice();
    public MTLCommandQueue CommandQueue;
    public MTLBuffer Dev_InputBuffer;
    public MTLBuffer Dev_OutputBuffer;

    private int _Width, _Height;
    private MetalGpuShader _OverlayShader;
    private MetalGpuShader _TransformShader;

    
    public MetalGpuDrawer(byte[] bitmapData, int width, int height)
    {
        var error = new NSError(IntPtr.Zero);
        try
        {
            _Width = width;
            _Height = height;
            Host_BitmapData = bitmapData;

            // 1. Initialize GPU Device and Command Queue
            Device = MTLDevice.CreateSystemDefaultDevice();
            CommandQueue = Device.NewCommandQueue();

            // 3. Allocate GPU memory Buffers
            uint bufferSize = (uint)bitmapData.Length;
            Dev_InputBuffer = Device.NewBuffer(bufferSize, MTLResourceOptions.ResourceStorageModeShared);
            Dev_OutputBuffer = Device.NewBuffer(bufferSize, MTLResourceOptions.ResourceStorageModeShared);

            // set Shaders
            _OverlayShader = new MetalGpuShader(this, "Overlay.metal", true);
            _TransformShader = new MetalGpuShader(this, "Transform.metal");
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
    }

    public void DrawOverlay(MetalGpuShader.Moon? moon = null)
    {
        _OverlayShader.Draw(moon);
    }
    
    public void DrawTransform() //(WriteableBitmap bitmap)
    {
        _TransformShader.Draw();
    }
}

