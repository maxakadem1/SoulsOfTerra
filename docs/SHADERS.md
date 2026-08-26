# Shader workflow

Shader source files live under `Effects/` as `.fx` files. tModLoader loads the
matching compiled `.fxc` assets; Build & Reload does not compile HLSL source.

## Prerequisite

Install the Windows SDK with the DirectX shader compiler (`fxc.exe`). The build
script discovers the newest installed Windows Kits compiler automatically. A
specific compiler can also be supplied with `-CompilerPath`.

## Compile shaders

```powershell
pwsh -File tools/CompileShaders.ps1
```

The script recursively finds `.fx` files and only rebuilds missing or stale
`.fxc` assets. Other useful modes are:

```powershell
# Rebuild every shader.
pwsh -File tools/CompileShaders.ps1 -Force

# Recompile in memory and fail unless every tracked asset matches byte-for-byte.
pwsh -File tools/CompileShaders.ps1 -Check
```

Commit both the editable `.fx` and generated `.fxc` files. Run the compiler
before Build & Reload whenever shader source changes.
