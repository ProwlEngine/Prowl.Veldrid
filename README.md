# Veldrid

Veldrid is a cross-platform, graphics API-agnostic rendering and compute library for .NET. It provides a powerful, unified interface to a system's GPU and includes more advanced features than any other .NET library. Unlike other platform- or vendor-specific technologies, Veldrid can be used to create high-performance 3D applications that are truly portable.<br>

Prowl.Veldrid is a fork of Veldrid designed to serve as the graphics backend of the [Prowl](https://github.com/ProwlEngine/Prowl) game engine.<br>

Prowl.Veldrid does not attempt to provide Veldrid as a standalone package, and is designed around integrating directly into Prowl as a submodule. Therefore, this repository omits support for .NET versions older than 8.0 and does not contains several parts of the source which are not used or already integrated in Prowl. These removals include the **ImGUI** integration, the **ImageSharp** loader, and the **NeoDemo**.
