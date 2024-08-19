# Veldrid

Veldrid is a cross-platform, graphics API-agnostic rendering and compute library for .NET. It provides a powerful, unified interface to a system's GPU and includes more advanced features than any other .NET library. Unlike other platform- or vendor-specific technologies, Veldrid can be used to create high-performance 3D applications that are truly portable.<br>

Prowl.Veldrid is a fork of Veldrid designed to serve as the graphics backend of the [Prowl](https://github.com/ProwlEngine/Prowl) game engine.<br>

Prowl.Veldrid does not attempt to provide Veldrid as a standalone package, and is designed around integrating directly into Prowl as a submodule. Therefore, this repository omits support for .NET versions older than 8.0 and several modules of the source which are not used or already integrated in Prowl have been gutted.

The following modules have been removed:
- **NeoDemo** (Unused by Prowl)
- **Veldrid.ImGUI** (Unused by Prowl)
- **Veldrid.Utilities** (Unused by Prowl)
- **Veldrid.ImageSharp** (Prowl contains an image loader)
- **Veldrid.VirtualReality** (Prowl will aim to use OpenXR instead for better cross-platform compatibility)
- **Veldrid.VirtualReality.Sample** (Unused by Prowl)
- **Veldrid.RenderDoc** (Unused by Prowl)
- **Veldrid.Tests** (Unused by Prowl - ideally the Prowl editor/player serves as the test itself)
- **Veldrid.Tests.Android** (Unused by Prowl - ideally the Prowl editor/player serves as the test itself)

