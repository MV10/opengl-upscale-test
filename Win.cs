
using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace opengl_upscale_test;

internal partial class Win : GameWindow, IDisposable
{
    static DebugProcKhr DebugMessageDelegate = OnDebugMessage;

    // buffered height is calculated using viewport aspect ratio
    int maxBufferWidth = 1024;

    bool isFullscreen;
    bool isBuffered;
    bool isScaled;
    Vector2 renderSize;

    int instantFPS;
    int averageFPS;
    int frameCounter;
    int statsSecond;
    int averageFPSTotal;
    int fpsBufferIndex;
    int[] fpsBuffer = new int[10];

    Color4 backgroundColor = new(0, 0, 0, 1);
    Shader shader;
    FrameData frameData = new();
    int framebufferHandle = -1;
    int textureHandle = -1;
    
    Stopwatch Clock = new();
    double nextReport;

    public Win(GameWindowSettings gameWindow, NativeWindowSettings nativeWindow)
        : base(gameWindow, nativeWindow)
    {
        GL.Khr.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);

        shader = new("passthrough.vert", "protean-clouds.frag");
        if(!shader.IsValid) Crash("Shader load/compile failed.");

        isFullscreen = false;
        isBuffered = false;
        isScaled = false;
        PrepareToRender();

        Clock.Start();
        Reset();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(backgroundColor);
        
        // Unnecessary, OnResize fires at startup
        // frameData.ViewportChanged(shader);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        shader.SetUniform("iResolution", renderSize);
        shader.SetUniform("iTime", (float)Clock.Elapsed.TotalSeconds);

        if(isBuffered)
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, framebufferHandle);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            frameData.Draw();

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebufferHandle);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BlitFramebuffer(
                0, 0, (int)renderSize.X, (int)renderSize.Y,
                0, 0, ClientSize.X, ClientSize.Y,
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        else
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            frameData.Draw();
        }

        SwapBuffers();
        CalculateStatistics();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // IMPORTANT: As of OpenTK 4.8, WindowState is NOT reliable in
        // this event when the state change was initiated through code.
        // https://github.com/opentk/opentk/issues/1640

        Console.WriteLine($"OnResize event: ({e.Width},{e.Height})");
        if (WindowState == WindowState.Minimized || e.Width == 0 || e.Height == 0) return;

        GL.Viewport(0, 0, e.Width, e.Height);
        frameData.ViewportChanged(shader);
        PrepareToRender();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (Clock.Elapsed.TotalSeconds >= nextReport) Report();

        var input = KeyboardState;

        if (input.IsKeyReleased(Keys.Escape))
        {
            Close();
            return;
        }

        // fullscreen toggle
        if (input.IsKeyReleased(Keys.Space))
        {
            if(WindowState != WindowState.Normal && WindowState != WindowState.Fullscreen)
            {
                Console.WriteLine("\nCan't toggle window state, current state isn't Normal or Fullscreen.");
                return;
            }
            if(WindowState == WindowState.Normal)
            {
                Console.WriteLine($"\nChanging WindowState to Fullscreen.");
                isFullscreen = true;
                WindowState = WindowState.Fullscreen;
            }
            else
            {
                Console.WriteLine($"\nChanging WindowState to Normal.");
                isFullscreen = false;
                WindowState = WindowState.Normal;
            }
            Clock.Restart();
            Reset();
            return;
        }

        // buffering toggle
        if (input.IsKeyReleased(Keys.Enter))
        {
            isBuffered = !isBuffered;
            Console.WriteLine($"\nBuffering {(isBuffered ? "en" : "dis")}abled.");
            PrepareToRender();
            Reset();
            return;
        }

        // buffer size toggle
        if (input.IsKeyReleased(Keys.Z))
        {
            if(!isBuffered)
            {
                Console.WriteLine("\nNot curently buffered, can't toggle buffer size.");
                return;
            }
            isScaled = !isScaled;
            frameData.ViewportChanged(shader);
            PrepareToRender();
            Console.WriteLine($"\nBuffer scaling {(isScaled ? "en" : "dis")}abled, ({renderSize.X},{renderSize.Y}).");
            Reset();
            return;
        }
    }

    void CalculateStatistics()
    {
        frameCounter++;
        if (DateTime.Now.Second != statsSecond)
        {
            statsSecond = DateTime.Now.Second;
            instantFPS = frameCounter;
            frameCounter = 0;

            averageFPSTotal = averageFPSTotal - fpsBuffer[fpsBufferIndex] + instantFPS;
            averageFPS = averageFPSTotal / 10;
            fpsBuffer[fpsBufferIndex] = instantFPS;
            fpsBufferIndex++;
            if (fpsBufferIndex == 10) fpsBufferIndex = 0;
        }
    }

    void Reset()
    {
        Console.WriteLine($"\nResetting statistics, next report in 15 sec.");
        nextReport = Clock.Elapsed.TotalSeconds + 15;
    }

    void Report()
    {
        var msg = $"{(isBuffered ? "" : "not ")}buffered, {(isFullscreen ? "" : "not ")}fullscreen, ";
        if (isBuffered) msg += $"{(isScaled ? "" : "not ")}scaled ({renderSize.X},{renderSize.Y}), ";
        msg += $"\n  {instantFPS} instant FPS, {averageFPS} avg FPS";
        Console.WriteLine($"\n{DateTime.Now.ToString("hh:mm:ss.ff")}:\n  {msg}");
        nextReport = Clock.Elapsed.TotalSeconds + 10;
    }

    void PrepareToRender()
    {
        DestroyBuffers();

        if(!isBuffered)
        {
            renderSize = new(ClientSize.X, ClientSize.Y);
            return;
        }

        int w = ClientSize.X;
        int h = ClientSize.Y;
        if (isScaled && ClientSize.X >= maxBufferWidth)
        {
            double aspect = (double)h / (double)w;
            w = maxBufferWidth;
            h = (int)(maxBufferWidth * aspect);
        }
        renderSize = new(w, h);
        Console.WriteLine($"Allocating buffer with dimensions ({renderSize.X},{renderSize.Y}).");

        framebufferHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferHandle);

        textureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureHandle, 0);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

        if(!status.Equals(FramebufferErrorCode.FramebufferComplete) 
            && !status.Equals(FramebufferErrorCode.FramebufferCompleteExt)) 
            Crash($"Error creating framebuffer: {status}");

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    void DestroyBuffers()
    {
        if (framebufferHandle > -1) GL.DeleteFramebuffer(framebufferHandle);
        if (textureHandle > -1) GL.DeleteBuffer(textureHandle);
        framebufferHandle = -1;
        textureHandle = -1;
    }

    void Crash(string message)
    {
        Console.WriteLine(message);
        Thread.Sleep(250);
        Environment.Exit(-1);
    }

    private static void OnDebugMessage(
        DebugSource source,     // Source of the debugging message.
        DebugType type,         // Type of the debugging message.
        int id,                 // ID associated with the message.
        DebugSeverity severity, // Severity of the message.
        int length,             // Length of the string in pMessage.
        IntPtr pMessage,        // Pointer to message string.
        IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
    {
        // ignore the noise
        if (id == 131185) return;

        // In order to access the string pointed to by pMessage, you can use Marshal
        // class to copy its contents to a C# string without unsafe code. You can
        // also use the new function Marshal.PtrToStringUTF8 since .NET Core 1.1.
        string message = Marshal.PtrToStringAnsi(pMessage, length);

        // The rest of the function is up to you to implement, however a debug output
        // is always useful.
        Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);

        // Potentially, you may want to throw from the function for certain severity
        // messages.
        //if (type == DebugType.DebugTypeError)
        //{
        //    throw new Exception(message);
        //}
    }

    protected new void Dispose()
    {
        base.Dispose();
        DestroyBuffers();
        shader.Dispose();
    }
}
