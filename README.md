# CS_progile - Computer Vision Challenge


## Project Structure

```
.
├── CS_progile.csproj
├── CS_progile.sln
├── Dockerfile
├── Progile.ipynb
├── Program.cs
├── README.md
├── bin
│   └── Debug
│       ├── net8.0
│       │   ├── CS_progile
│       │   ├── CS_progile.deps.json
│       │   ├── CS_progile.dll
│       │   ├── CS_progile.pdb
│       │   ├── CS_progile.runtimeconfig.json
│       │   ├── Microsoft.Win32.SystemEvents.dll
│       │   ├── OpenCvSharp.Extensions.dll
│       │   ├── OpenCvSharp.dll
│       │   ├── System.Drawing.Common.dll
│       │   ├── media
│       │   │   ├── SAP.png
│       │   │   └── WEB.png
│       │   └── runtimes
│       │       └── win
│       │           └── lib
│       │               └── net8.0
│       │                   └── Microsoft.Win32.SystemEvents.dll
│       └── net9.0
│           ├── CS_progile
│           ├── CS_progile.deps.json
│           ├── CS_progile.dll
│           ├── CS_progile.pdb
│           ├── CS_progile.runtimeconfig.json
│           ├── Microsoft.Win32.SystemEvents.dll
│           ├── OpenCvSharp.Extensions.dll
│           ├── OpenCvSharp.dll
│           ├── System.Drawing.Common.dll
│           ├── media
│           │   ├── SAP.png
│           │   └── WEB.png
│           └── runtimes
│               ├── linux-x64
│               │   └── native
│               │       └── libOpenCvSharpExtern.so
│               ├── ubuntu.18.04-x64
│               │   └── native
│               ├── ubuntu.20.04-x64
│               │   └── native
│               ├── unix
│               │   └── lib
│               │       └── netcoreapp3.0
│               └── win
│                   └── lib
│                       ├── net8.0
│                       │   └── Microsoft.Win32.SystemEvents.dll
│                       └── netcoreapp3.0
├── media
│   ├── SAP.png
│   └── WEB.png
├── obj
│   ├── CS_progile.csproj.nuget.dgspec.json
│   ├── CS_progile.csproj.nuget.g.props
│   ├── CS_progile.csproj.nuget.g.targets
│   ├── Debug
│   │   ├── net8.0
│   │   │   ├── CS_progile.AssemblyInfo.cs
│   │   │   ├── CS_progile.AssemblyInfoInputs.cache
│   │   │   ├── CS_progile.GeneratedMSBuildEditorConfig.editorconfig
│   │   │   ├── CS_progile.GlobalUsings.g.cs
│   │   │   ├── CS_progile.assets.cache
│   │   │   ├── CS_progile.csproj.AssemblyReference.cache
│   │   │   ├── CS_progile.csproj.CoreCompileInputs.cache
│   │   │   ├── CS_progile.csproj.FileListAbsolute.txt
│   │   │   ├── CS_progile.csproj.Up2Date
│   │   │   ├── CS_progile.dll
│   │   │   ├── CS_progile.genruntimeconfig.cache
│   │   │   ├── CS_progile.pdb
│   │   │   ├── apphost
│   │   │   ├── ref
│   │   │   │   └── CS_progile.dll
│   │   │   └── refint
│   │   │       └── CS_progile.dll
│   │   └── net9.0
│   │       ├── CS_progile.AssemblyInfo.cs
│   │       ├── CS_progile.AssemblyInfoInputs.cache
│   │       ├── CS_progile.GeneratedMSBuildEditorConfig.editorconfig
│   │       ├── CS_progile.GlobalUsings.g.cs
│   │       ├── CS_progile.assets.cache
│   │       ├── CS_progile.csproj.AssemblyReference.cache
│   │       ├── CS_progile.csproj.CoreCompileInputs.cache
│   │       ├── CS_progile.csproj.FileListAbsolute.txt
│   │       ├── CS_progile.csproj.Up2Date
│   │       ├── CS_progile.dll
│   │       ├── CS_progile.genruntimeconfig.cache
│   │       ├── CS_progile.pdb
│   │       ├── apphost
│   │       ├── ref
│   │       │   └── CS_progile.dll
│   │       └── refint
│   │           └── CS_progile.dll
│   ├── project.assets.json
│   └── project.nuget.cache
├── output_sap_table.png
└── output_web_table.png 
```


The following dependencies come included in the Docker Image 


```sh
 apt-get -y install --no-install-recommends \
      apt-transport-https \
      software-properties-common \
      wget \
      unzip \
      ca-certificates \
      build-essential \
      cmake \
      git \
      ccache \
      libtbb-dev \
      libatlas-base-dev \
      libgtk-3-dev \
      libavcodec-dev \
      libavformat-dev \
      libswscale-dev \
      libdc1394-dev \
      libxine2-dev \
      libv4l-dev \
      libtheora-dev \
      libvorbis-dev \
      libxvidcore-dev \
      libopencore-amrnb-dev \
      libopencore-amrwb-dev \
      x264 \
      libtesseract-dev \
      libgdiplus \
      pkg-config \
      libavutil-dev; \

```





## Expected Result - User Documentation

Al ejecutar exitosamente, verás:

1. Mensajes de confirmación en la consola
2. Dos ventanas mostrando las imágenes SAP.png y WEB.png
3. Una tercera ventana con la imagen combinada
4. Un archivo `combined_output.png` guardado en la carpeta `media/`

## Notas Técnicas

- **Versión de OpenCvSharp**: Se usa la 4.6.0.20220608 por compatibilidad
- **Runtime**: Se incluye el runtime específico para Ubuntu x64
- **Docker**: Se usa multi-stage build para optimizar el tamaño de la imagen final
- **X11**: Se incluye soporte para display virtual en entornos headless