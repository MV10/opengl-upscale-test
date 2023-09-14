# opengl-upscale-test

Some shaders work well at lower resolutions, but can't sustain smooth framerates at higher resolutions.

This is an attempt to determine whether rendering to a lower-resolution framebuffer then upscaling
via blitter is an acceptable fix, both from a performance standpoint (I'm unsure of the overhead of
blitting from, say, 960x720 to 3840x2160) and an image-quality standpoint.

Obviously this will also be heavily GPU (and probably CPU/DRAM) dependent, but hey, you work with the
tools you have, right?

The shader is the _very_ heavyweight (but very cool) [Protean Tunnel](https://www.shadertoy.com/view/3l23Rh)
created by user nimitz on Shadertoy.

Uses OpenTK 4.8.0 for convenient .NET OpenGL bindings.
