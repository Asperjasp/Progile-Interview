# CS_progile - OpenCV Image Display Project

Este proyecto demuestra cómo cargar y mostrar imágenes usando OpenCvSharp en .NET 9.0 con Ubuntu 24.04.

## Estructura del Proyecto

```
CS_progile/
├── CS_progile.csproj          # Archivo de proyecto .NET
├── Program.cs                 # Código principal
├── Dockerfile.simple          # Dockerfile optimizado para el proyecto
├── build_and_run.sh          # Script para construir y ejecutar
├── media/                     # Carpeta con imágenes
│   ├── SAP.png               # Imagen SAP
│   └── WEB.png               # Imagen WEB
└── README.md                 # Este archivo
```

## Características del Código

El programa realiza las siguientes operaciones:

1. **Carga de Imágenes**: Lee las imágenes SAP.png y WEB.png desde la carpeta `media/`
2. **Validación**: Verifica que los archivos existan y se carguen correctamente
3. **Visualización**: Muestra las imágenes en ventanas separadas usando OpenCV
4. **Combinación**: Crea una imagen combinada (lado a lado) y la guarda
5. **Información**: Muestra las dimensiones de las imágenes cargadas

## Dependencias

- **.NET 9.0**: Framework de desarrollo
- **OpenCvSharp4**: Wrapper de .NET para OpenCV
- **OpenCV**: Librerías nativas de procesamiento de imágenes

## Opciones de Ejecución

### Opción 1: Ejecución Local (Requiere dependencias instaladas)

```bash
# Instalar dependencias del sistema
sudo apt update
sudo apt install -y libopencv-dev libgtk-3-dev

# Compilar y ejecutar
dotnet restore
dotnet build
dotnet run
```

### Opción 2: Ejecución con Docker (Recomendado)

```bash
# Dar permisos al script
chmod +x build_and_run.sh

# Ejecutar
./build_and_run.sh
```

### Opción 3: Docker Manual

```bash
# Construir la imagen
docker build -f Dockerfile.simple -t cs_progile:latest .

# Ejecutar con soporte X11 (Linux)
docker run --rm -it \
    -e DISPLAY=$DISPLAY \
    -v /tmp/.X11-unix:/tmp/.X11-unix:rw \
    cs_progile:latest

# Ejecutar en modo headless
docker run --rm -it cs_progile:latest
```

## Solución de Problemas

### Error: "Unable to load shared library 'OpenCvSharpExtern'"

Este es el error más común. Las soluciones incluyen:

1. **Usar Docker**: La forma más confiable de ejecutar el proyecto
2. **Instalar dependencias del sistema**: `sudo apt install libopencv-dev`
3. **Verificar versiones**: Asegurar compatibilidad entre OpenCvSharp y OpenCV nativo

### Error: "No se encontró la imagen"

- Verificar que las imágenes estén en la carpeta `media/`
- Verificar que los nombres sean exactamente `SAP.png` y `WEB.png`

### Error: "Cannot connect to X server"

Para entornos sin GUI:
- El programa creará un archivo `combined_output.png` en lugar de mostrar ventanas
- Usar el Dockerfile que incluye Xvfb para simulación de display

## Configuración del Proyecto

### CS_progile.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="OpenCvSharp4" Version="4.6.0.20220608" />
    <PackageReference Include="OpenCvSharp4.Extensions" Version="4.6.0.20220608" />
    <PackageReference Include="OpenCvSharp4.runtime.ubuntu.18.04-x64" Version="4.6.0.20220608" />
  </ItemGroup>

  <ItemGroup>
    <None Include="media/**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

## Resultado Esperado

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