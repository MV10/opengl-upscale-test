# opengl-upscale-test

Some shaders work well at lower resolutions, but can't sustain smooth framerates at higher resolutions.

This is an attempt to determine whether rendering to a lower-resolution framebuffer then upscaling
via blitter is an acceptable fix, both from a performance standpoint (I'm unsure of the overhead of
blitting from, say, 960x540 to 3840x2160) and an image-quality standpoint.

Obviously this will also be heavily GPU (and probably CPU/DRAM) dependent, but hey, you work with the
tools you have, right?

The shader is the _very_ heavyweight (but very cool) [Protean Clouds](https://www.shadertoy.com/view/3l23Rh)
created by user nimitz on Shadertoy.

It uses OpenTK for convenient .NET OpenGL bindings and my eyecandy library to load/compile the shader files.

Results from my machine:

* With simple output directly to the OpenGL-managed backbuffer, windowed (960x540) averages around 186 FPS, and full-screen (3840x2160) tanks that to just 12 FPS.
* With full-sized buffering those numbers are 183 FPS windowed, and still 12 FPS full-screen.
* However, with a 1024x576 buffer upscaled to full-screen, the program reports 158 FPS.

The 1024x576 buffer dimensions were determined by setting the maximum width 1024 and calculating the height based on the viewport resolution. Note the code won't allocate a buffer that is _larger_ than the current viewport resolution. (The current code would have to be changed to also set `GL.Viewport` for this to work, but it doesn't make sense to do _more_ work than is required.)

Best of all, the quality was _very_ acceptable, at least for this particular shader.
