# CS_progile - Computer Vision Challenge
## Overview


This system automatically detects and analyzes table structures in screenshots using computer vision techniques. It uses a **prototype-based approach** where a partial table image (header + 2 rows) serves as a template to extract structural features, which are then used to detect and parse complete tables in full screenshots.
This project is a pure Computer Vision task based on the tasks that Progile company does.




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

## Installation

First Make sure to download all the dependencies

- [DotNet 8.0](https://dotnet.microsoft.com/es-es/download)

- [Docker desktop with support for WSL enabled](https://docs.docker.com/desktop/features/wsl/)

Clone this repository with

```sh
git clone https://github.com/Asperjasp/Progile-Interview/
```

Or initiate a new console project on your own with the command, and create the files needed

```sh
dotnet new console -n Progile
touch Dockerfile .dockerignore .gitignore 

# Copy the content from the Dockerfile in the repo for the Docker building
```

Then we are going to build the Docker image
```sh
docker buildx build -t progile:latest .
docker build -t progile:sdk --target final-sdk .
```

Next We are going to  mount our files and code to the docker container which already has the specifications of .Net 8.0 and the installation of OpenCvSharp 

Build the container with the new form by docker

Mount your local files to the Docker Container which already has installed OpenCV  

```sh
docker run -it --rm \
  -v ~/asperjasp/Job/Progile/CS_progile:/app \ 
 # THE PATH TO THE PROJECT, CHANGE IF YOURS IS DIFFERENT
  -w /app \
	progile:sdk bash
	
# Equivalent to 
	
docker run -it --rm -v ~/asperjasp/Job/Progile/CS_progile:/app -w /app   progile:sdk bash
```


## Expected Result - Use Cases


<!-- Add a Video Documentation -->

1. Mensajes de confirmación en la consola
2. Dos ventanas mostrando las imágenes SAP.png y WEB.png
3. Una tercera ventana con la imagen combinada
4. Un archivo `combined_output.png` guardado en la carpeta `media/`


## Explanation - How it works

### Phase 1: Prototype Analysis (Template Learning)

The system learns table structure from a small prototype image:
#### 1.1 Preprocessing Pipeline

**Purpose**: Convert the image into a format optimal for line detection.
```csharp
public Mat Preprocess()
{
    // Step 1: Grayscale conversion
    Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);
    
    // Step 2: Adaptive thresholding
    Cv2.AdaptiveThreshold(grayImage, thresholdImage, 255,
        AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
    
    // Step 3: Invert (lines become white on black)
    Cv2.BitwiseNot(thresholdImage, thresholdImage);
    
    return thresholdImage;
}
```
**Why Adaptive Thresholding?**

Global thresholding fails with varying lighting conditions
Adaptive thresholding calculates threshold locally for each pixel region
blockSize: 11 defines the neighborhood size (11x11 pixels)
C: 2 is a constant subtracted from the weighted mean
Result: Robust binarization regardless of shadows or highlights

**Why Invert?**

Morphological operations work better on white foreground, black background
Original tables have dark lines on white background
Inversion makes lines the "objects of interest"


#### 1.2 Line Detection Using Morphological Operations
The Core Technique: Use specialized kernels to isolate horizontal and vertical structures.

```csharp

private (Mat horizontal, Mat vertical) DetectLinesMorphology()
{
    // Horizontal line kernel: wide and short
    var hKernel = Cv2.GetStructuringElement(
        MorphShapes.Rect, 
        new Size(width / 4, 1)  // 25% of image width, 1 pixel tall
    );
    Cv2.MorphologyEx(thresholdImage, horizontalLines, MorphTypes.Open, hKernel);
    
    // Vertical line kernel: narrow and tall
    var vKernel = Cv2.GetStructuringElement(
        MorphShapes.Rect, 
        new Size(1, height / 4)  // 1 pixel wide, 25% of image height
    );
    Cv2.MorphologyEx(thresholdImage, verticalLines, MorphTypes.Open, vKernel);
    
    return (horizontalLines, verticalLines);
}

```

**Why This Works:**

1. Opening Operation = Erosion followed by Dilation

Erosion: Removes pixels that don't fit the kernel shape
Dilation: Expands remaining structures back to original size
Net effect: Only structures matching the kernel shape survive


2. Horizontal Kernel (width/4 × 1):

Matches long horizontal lines
Removes text, noise, and vertical elements
Result: Pure horizontal grid lines


3. Vertical Kernel (1 × height/4):

Matches long vertical lines
Removes text, noise, and horizontal elements
Result: Pure vertical grid lines

#### 1.3 Projection-Based Position Detection
The Key Innovation: Instead of searching pixel-by-pixel, use 1D projections.

```csharp

private List<int> DetectHorizontalProjections(Mat lineImage)
{
    var positions = new List<int>();
    double threshold = width * 255 * 0.3; // 30% of row must be white
    
    for (int y = 0; y < height; y++)
    {
        double rowSum = 0;
        for (int x = 0; x < width; x++)
        {
            rowSum += lineImage.At<byte>(y, x);
        }
        
        if (rowSum > threshold)
            positions.Add(y);
    }
    
    return MergeSimilarPositions(positions, threshold: 5);
}

```

**How Projection Works:**

Sum all pixel values in each row (horizontal projection)
Rows with high sums → horizontal lines present
Threshold: 30% of pixels must be white
Result: Y-coordinates of all horizontal lines

**Why 30% Threshold?**

Too low (e.g., 10%): Noise gets detected as lines
Too high (e.g., 70%): Broken or faint lines get missed
30%: Balanced detection of continuous and partially visible lines

#### 1.4 Fallback: Text-Based Detection (for WEB tables)
Problem: WEB table has minimal visual separators.
Solution: Detect table structure from text alignment patterns.

```csharp

private (List<int>, List<int>) DetectTextBasedStructure()
{
    // 1. Find text regions using contour detection
    Cv2.FindContours(binary, out Point[][] contours, ...);
    
    // 2. Get bounding boxes of text
    var textBoxes = contours
        .Select(c => Cv2.BoundingRect(c))
        .Where(r => r.Width > 10 && r.Height > 5)  // Filter small noise
        .ToList();
    
    // 3. Cluster by Y-coordinate → rows
    var yCoords = textBoxes.Select(r => r.Y).ToList();
    var hPositions = ClusterCoordinates(yCoords, tolerance: 10);
    
    // 4. Cluster by X-coordinate → columns
    var xCoords = textBoxes.Select(r => r.X).ToList();
    var vPositions = ClusterCoordinates(xCoords, tolerance: 20);
    
    return (hPositions, vPositions);
}

```

**Why This Works:**

- Text in tables aligns in rows and columns
- Clustering Y-coordinates reveals row - positions
- Clustering X-coordinates reveals column - positions
- No grid lines needed!

#### 1.5 Statistical Feature Extraction
From detected line positions, calculate:

```csharp

// Average row height (for extending rows in full screenshot)
AverageRowHeight = (sum of consecutive row spacings) / (number of gaps)

// Average column width (for validating column detection)
AverageColumnWidth = (sum of consecutive column spacings) / (number of gaps)

// Header height (first row is typically header)
HeaderHeight = HorizontalPositions[1] - HorizontalPositions[0]


```

## Technical Notes
> [!NOTE]
A python like solution was provided and build before hand, since I am more confortable with the  Python OpenCV environment so the C# solution is based in the `Progile.ipynb` notebook

- **OpenCvSharp4 Version**: We use the version 4.11.0.20250507 for compatibility with Ubuntu 24.04

- **Docker**:  Multi-stage building is used to optimize the size of the final image and because that was the format the original Docker image was




>[!WARNING]
There Have been a lot of trouble dating back to 2018 installing OpenCvSharp so we followed the advise of [installing via Docker Image **Ubuntu 24.04 ( Noble ) WSL**](https://github.com/shimat/opencvsharp/blob/main/docker/ubuntu24-dotnet8-opencv4.12.0/Dockerfile) supporting **.NET 8.0** given by the autor of the OpenCvSharp Library [Shimat](https://www.notion.so/OpenCvSharp4-in-NET-and-C-Troubleshooting-27f1075a5e82805b83b1d4750a537419?source=copy_link) in that issue

- [Unable to load shared library 'OpenCvSharpExtern' or one of its dependencies. #701](https://github.com/shimat/opencvsharp/issues/701)

- [Ubuntu 24.04 support #1776
](https://github.com/shimat/opencvsharp/issues/1776)

- [Running error reporting under Linux arm32 system #667
](https://github.com/shimat/opencvsharp/issues/667)

- [The type initializer for 'OpenCvSharp.NativeMethods' threw an exception #983]()
