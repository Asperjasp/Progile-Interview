using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TableDetection
{
    public class CommandLineArgs
    {
        public string? ScreenshotPath { get; set; }
        public string? PartialImagePath { get; set; }
        public bool ShowHelp { get; set; }
        public bool IsValid => !string.IsNullOrEmpty(ScreenshotPath) && !string.IsNullOrEmpty(PartialImagePath);

        public static CommandLineArgs Parse(string[] args)
        {
            var result = new CommandLineArgs();
            
            if (args.Length == 0)
            {
                result.ShowHelp = true;
                return result;
            }

            // Support for named arguments (-src, -par, --screenshot, --partial)
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                
                switch (arg)
                {
                    case "-src":
                    case "--screenshot":
                    case "-s":
                        if (i + 1 < args.Length)
                        {
                            result.ScreenshotPath = args[++i];
                        }
                        break;
                        
                    case "-par":
                    case "--partial":
                    case "-p":
                        if (i + 1 < args.Length)
                        {
                            result.PartialImagePath = args[++i];
                        }
                        break;
                        
                    case "-h":
                    case "--help":
                    case "/?":
                        result.ShowHelp = true;
                        return result;
                        
                    default:
                        // Support for positional arguments (backward compatibility)
                        if (!arg.StartsWith("-") && !arg.StartsWith("--"))
                        {
                            if (string.IsNullOrEmpty(result.ScreenshotPath))
                            {
                                result.ScreenshotPath = args[i];
                            }
                            else if (string.IsNullOrEmpty(result.PartialImagePath))
                            {
                                result.PartialImagePath = args[i];
                            }
                        }
                        break;
                }
            }
            
            return result;
        }
        
        public static void ShowUsage()
        {
            Console.WriteLine("Table Detection Program - Command Line Usage");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  dotnet run [options]");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  -src, --screenshot <path>    Path to the complete screenshot image");
            Console.WriteLine("  -par, --partial <path>       Path to the partial reference image");
            Console.WriteLine("  -h, --help                   Show this help message");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  dotnet run -src media/SAP.png -par media/SAP_partial.png");
            Console.WriteLine("  dotnet run --screenshot screenshot.png --partial reference.png");
            Console.WriteLine("  dotnet run media/SAP.png media/SAP_partial.png  (positional)");
            Console.WriteLine();
            Console.WriteLine("OUTPUT:");
            Console.WriteLine("  1. Boundaries of the complete table (StdOut)");
            Console.WriteLine("  2. Boundaries of header (StdOut)");
            Console.WriteLine("  3. Annotated image with colored highlights:");
            Console.WriteLine("     - Red rectangle: Complete table");
            Console.WriteLine("     - Green rectangle: Header");
            Console.WriteLine("     - Yellow rectangles: Rows");
            Console.WriteLine("     - Black lines: Columns");
        }
    }

    public enum TableType
    /* 
    Create a Template for the clients and different types of tables allowing reusability
    */
    {
        SAP,
        WEB,
        UNKNOWN
    }

    public class LineFeatures
 /* Define the features we are going to use to characterize the table */
    {
        public List<int> HorizontalPositions { get; set; } = new();
        public List<int> VerticalPositions { get; set; } = new();
        public double AverageRowHeight { get; set; }
        public double AverageColumnWidth { get; set; }
        public int HeaderHeight { get; set; }

        public override string ToString()
        {
            return $"LineFeatures(rows={HorizontalPositions.Count}, " +
                   $"cols={VerticalPositions.Count}, " +
                   $"avg_row_h={AverageRowHeight:F1}, " +
                   $"avg_col_w={AverageColumnWidth:F1})";
        }
    }

// Class to handle the data processing pipeline alongside all the process that should be done to the table
    public class TablePrototype
    {
        private Mat originalImage;
        private Mat grayImage = new Mat();
        private Mat thresholdImage = new Mat();

        public string Name { get; set; }
        public LineFeatures LineFeatures { get; private set; } = new LineFeatures();
        public TableType TableType { get; private set; } = TableType.UNKNOWN;
        public Rect? Boundaries { get; private set; } = null; // (x, y, width, height)
        // Rect is used for defining the bounding box of the detected table

        // Constructor
        public TablePrototype(Mat image, string name = "")
        {
            originalImage = image.Clone();
            Name = name;
        }

        // Data Preprocessing helper functions
        public Mat Preprocess()
        {
            // INPUT: campo de clase, ya establecido en constructor )
            // OUTPUT: TRESHOLGimage ( imagen binarizada para análisis )

            // Convert to grayscale
            if (originalImage.Channels() == 3)
            {
                grayImage = new Mat();
                Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                grayImage = originalImage.Clone();
            }

            // Adaptive thresholding
            thresholdImage = new Mat();
            Cv2.AdaptiveThreshold(
                grayImage,
                thresholdImage,
                255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.Binary,
                blockSize: 11,
                c: 2
            );

            // Invert for white lines on black background
            Cv2.BitwiseNot(thresholdImage, thresholdImage);

            return thresholdImage;
        }

        public LineFeatures ExtractLineFeatures()
        {
            if (thresholdImage.Empty())
            {
                Preprocess();
            }

            // Morphological operations to detect lines
            var (hLines, vLines) = DetectLinesMorphology();

            // Get line positions using projection
            var hPositions = DetectHorizontalProjections(hLines);
            var vPositions = DetectVerticalProjections(vLines);

            // If few lines detected, try text-based approach (for WEB)
            if (hPositions.Count < 3)
            {
                (hPositions, vPositions) = DetectTextBasedStructure();
                TableType = TableType.WEB;
            }
            else
            {
                TableType = TableType.SAP;
            }

            // Calculate statistics
            double avgRowHeight = CalculateAverageSpacing(hPositions);
            double avgColWidth = CalculateAverageSpacing(vPositions);
            int headerHeight = hPositions.Count >= 2 ?
                hPositions[1] - hPositions[0] : 0;

            LineFeatures = new LineFeatures
            {
                HorizontalPositions = hPositions,
                VerticalPositions = vPositions,
                AverageRowHeight = avgRowHeight,
                AverageColumnWidth = avgColWidth,
                HeaderHeight = headerHeight
            };

            return LineFeatures;
        }

        private (Mat horizontal, Mat vertical) DetectLinesMorphology()
        {
            int height = thresholdImage.Height;
            int width = thresholdImage.Width;

            // Horizontal lines: wide horizontal kernel
            var hKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new Size(width / 4, 1)
            );
            var horizontalLines = new Mat();
            Cv2.MorphologyEx(thresholdImage, horizontalLines,
                MorphTypes.Open, hKernel);

            // Vertical lines: tall vertical kernel
            var vKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new Size(1, height / 4)
            );
            var verticalLines = new Mat();
            Cv2.MorphologyEx(thresholdImage, verticalLines,
                MorphTypes.Open, vKernel);

            return (horizontalLines, verticalLines);
        }

        private List<int> DetectHorizontalProjections(Mat lineImage)
        {
            var positions = new List<int>();
            int height = lineImage.Height;
            int width = lineImage.Width;
            double threshold = width * 255 * 0.3; // 30% white pixels

            for (int y = 0; y < height; y++)
            {
                double rowSum = 0;
                for (int x = 0; x < width; x++)
                {
                    rowSum += lineImage.At<byte>(y, x);
                }

                if (rowSum > threshold)
                {
                    positions.Add(y);
                }
            }

            return MergeSimilarPositions(positions, 5);
        }

        private List<int> DetectVerticalProjections(Mat lineImage)
        {
            var positions = new List<int>();
            int height = lineImage.Height;
            int width = lineImage.Width;
            double threshold = height * 255 * 0.3;

            for (int x = 0; x < width; x++)
            {
                double colSum = 0;
                for (int y = 0; y < height; y++)
                {
                    colSum += lineImage.At<byte>(y, x);
                }

                if (colSum > threshold)
                {
                    positions.Add(x);
                }
            }

            return MergeSimilarPositions(positions, 5);
        }

        private List<int> MergeSimilarPositions(List<int> positions, int threshold)
        {
            if (positions.Count == 0) return positions;

            positions = positions.OrderBy(p => p).ToList();
            var merged = new List<int> { positions[0] };

            foreach (var pos in positions.Skip(1))
            {
                if (pos - merged.Last() > threshold)
                {
                    merged.Add(pos);
                }
            }

            return merged;
        }

        private double CalculateAverageSpacing(List<int> positions)
        {
            if (positions.Count < 2) return 0.0;

            var spacings = new List<double>();
            for (int i = 0; i < positions.Count - 1; i++)
            {
                spacings.Add(positions[i + 1] - positions[i]);
            }

            return spacings.Average();
        }

        private (List<int> horizontal, List<int> vertical) DetectTextBasedStructure()
        {
            // Apply binary threshold
            var binary = new Mat();
            Cv2.Threshold(grayImage, binary, 0, 255,
                ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // Find contours (text regions)
            Cv2.FindContours(binary, out Point[][] contours,
                out HierarchyIndex[] hierarchy,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            // Get bounding boxes and filter noise
            var textBoxes = contours
                .Select(c => Cv2.BoundingRect(c))
                .Where(r => r.Width > 10 && r.Height > 5)
                .ToList();

            // Group by Y-coordinate for rows
            var yCoords = textBoxes.Select(r => r.Y).ToList();
            var hPositions = ClusterCoordinates(yCoords, 10);

            // Group by X-coordinate for columns
            var xCoords = textBoxes.Select(r => r.X).ToList();
            var vPositions = ClusterCoordinates(xCoords, 20);

            return (hPositions, vPositions);
        }

        private List<int> ClusterCoordinates(List<int> coords, int tolerance)
        {
            if (coords.Count == 0) return new List<int>();

            coords = coords.OrderBy(c => c).ToList();
            var clusters = new List<List<int>> { new List<int> { coords[0] } };

            foreach (var coord in coords.Skip(1))
            {
                if (coord - clusters.Last().Last() <= tolerance)
                {
                    clusters.Last().Add(coord);
                }
                else
                {
                    clusters.Add(new List<int> { coord });
                }
            }

            // Return centroid of each cluster
            return clusters.Select(c => (int)c.Average()).ToList();
        }

        public Mat VisualizeFeatures(string? savePath = null)
        {
            if (LineFeatures?.HorizontalPositions == null || LineFeatures?.VerticalPositions == null)
            {
                ExtractLineFeatures();
            }

            Mat visImage = originalImage.Clone();
            if (visImage.Channels() == 1)
            {
                var temp = new Mat();
                Cv2.CvtColor(visImage, temp, ColorConversionCodes.GRAY2BGR);
                visImage = temp;
            }

            // a. Complete table in red (rectangle)
            var tableBounds = CalculateTableBoundaries();
            if (tableBounds.ContainsKey("topleft") && tableBounds.ContainsKey("bottomright"))
            {
                Cv2.Rectangle(visImage, tableBounds["topleft"], tableBounds["bottomright"],
                    new Scalar(0, 0, 255), 3); // Red
            }

            // b. Header in green (rectangle)
            var headerBounds = CalculateHeaderBoundaries();
            if (headerBounds.ContainsKey("topleft") && headerBounds.ContainsKey("bottomright"))
            {
                Cv2.Rectangle(visImage, headerBounds["topleft"], headerBounds["bottomright"],
                    new Scalar(0, 255, 0), 3); // Green
            }

            // c. Rows in yellow (rectangle)
            if (LineFeatures?.HorizontalPositions != null && LineFeatures.HorizontalPositions.Count > 1)
            {
                int minX = LineFeatures.VerticalPositions?.Count > 0 ? LineFeatures.VerticalPositions.Min() : 0;
                int maxX = LineFeatures.VerticalPositions?.Count > 0 ? LineFeatures.VerticalPositions.Max() : visImage.Width;

                for (int i = 1; i < LineFeatures.HorizontalPositions.Count - 1; i++)
                {
                    int rowTop = LineFeatures.HorizontalPositions[i];
                    int rowBottom = LineFeatures.HorizontalPositions[i + 1];
                    Cv2.Rectangle(visImage, new Point(minX, rowTop), new Point(maxX, rowBottom),
                        new Scalar(0, 255, 255), 2); // Yellow
                }
            }

            // d. Columns with straight strokes in black
            if (LineFeatures?.VerticalPositions != null)
            {
                foreach (var x in LineFeatures.VerticalPositions)
                {
                    Cv2.Line(visImage, new Point(x, 0),
                        new Point(x, visImage.Height),
                        new Scalar(0, 0, 0), 2); // Black
                }
            }

            // Add text annotation
            string text = $"{Name} | Type: {TableType} | " +
                         $"Rows: {LineFeatures?.HorizontalPositions?.Count ?? 0} | " +
                         $"Cols: {LineFeatures?.VerticalPositions?.Count ?? 0}";
            Cv2.PutText(visImage, text, new Point(10, 30),
                HersheyFonts.HersheySimplex, 0.6,
                new Scalar(255, 255, 255), 2); // White text for visibility

            if (!string.IsNullOrEmpty(savePath))
            {
                Cv2.ImWrite(savePath, visImage);
            }

            return visImage;
        }

        public Dictionary<string, object> GetSummary()
        {
            if (LineFeatures == null)
            {
                ExtractLineFeatures();
            }

            // Calculate table boundaries
            var tableBoundaries = CalculateTableBoundaries();
            var headerBoundaries = CalculateHeaderBoundaries();

            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = TableType.ToString(),
                ["dimensions"] = $"{originalImage.Width}x{originalImage.Height}",
                ["num_rows"] = LineFeatures?.HorizontalPositions?.Count ?? 0,
                ["num_cols"] = LineFeatures?.VerticalPositions?.Count ?? 0,
                ["avg_row_height"] = LineFeatures?.AverageRowHeight ?? 0.0,
                ["avg_col_width"] = LineFeatures?.AverageColumnWidth ?? 0.0,
                ["header_height"] = LineFeatures?.HeaderHeight ?? 0,
                ["table_boundaries"] = tableBoundaries,
                ["header_boundaries"] = headerBoundaries
            };
        }

        private Dictionary<string, Point> CalculateTableBoundaries()
        {
            if (LineFeatures?.HorizontalPositions == null || LineFeatures?.VerticalPositions == null)
                return new Dictionary<string, Point>();

            int minX = LineFeatures.VerticalPositions.Count > 0 ? LineFeatures.VerticalPositions.Min() : 0;
            int maxX = LineFeatures.VerticalPositions.Count > 0 ? LineFeatures.VerticalPositions.Max() : originalImage.Width;
            int minY = LineFeatures.HorizontalPositions.Count > 0 ? LineFeatures.HorizontalPositions.Min() : 0;
            int maxY = LineFeatures.HorizontalPositions.Count > 0 ? LineFeatures.HorizontalPositions.Max() : originalImage.Height;

            return new Dictionary<string, Point>
            {
                ["topleft"] = new Point(minX, minY),
                ["topright"] = new Point(maxX, minY),
                ["bottomleft"] = new Point(minX, maxY),
                ["bottomright"] = new Point(maxX, maxY)
            };
        }

        private Dictionary<string, Point> CalculateHeaderBoundaries()
        {
            if (LineFeatures?.HorizontalPositions == null || LineFeatures?.VerticalPositions == null || LineFeatures.HorizontalPositions.Count < 2)
                return new Dictionary<string, Point>();

            int minX = LineFeatures.VerticalPositions.Count > 0 ? LineFeatures.VerticalPositions.Min() : 0;
            int maxX = LineFeatures.VerticalPositions.Count > 0 ? LineFeatures.VerticalPositions.Max() : originalImage.Width;
            int headerTop = LineFeatures.HorizontalPositions[0];
            int headerBottom = LineFeatures.HorizontalPositions[1];

            return new Dictionary<string, Point>
            {
                ["topleft"] = new Point(minX, headerTop),
                ["topright"] = new Point(maxX, headerTop),
                ["bottomleft"] = new Point(minX, headerBottom),
                ["bottomright"] = new Point(maxX, headerBottom)
            };
        }

        public void Dispose()
        {
            originalImage?.Dispose();
            grayImage?.Dispose();
            thresholdImage?.Dispose();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var cliArgs = CommandLineArgs.Parse(args);
            
            if (cliArgs.ShowHelp)
            {
                CommandLineArgs.ShowUsage();
                return;
            }
            
            if (!cliArgs.IsValid)
            {
                Console.Error.WriteLine("Error: Both screenshot and partial image paths are required.");
                Console.Error.WriteLine();
                CommandLineArgs.ShowUsage();
                Environment.Exit(1);
            }
            
            // Validate that files exist
            if (!File.Exists(cliArgs.ScreenshotPath))
            {
                Console.Error.WriteLine($"Error: Screenshot file not found: {cliArgs.ScreenshotPath}");
                Environment.Exit(1);
            }
            
            if (!File.Exists(cliArgs.PartialImagePath))
            {
                Console.Error.WriteLine($"Error: Partial image file not found: {cliArgs.PartialImagePath}");
                Environment.Exit(1);
            }

            Console.WriteLine($"Processing screenshot: {cliArgs.ScreenshotPath}");
            Console.WriteLine($"Using partial reference: {cliArgs.PartialImagePath}");
            Console.WriteLine("=====================================");
            
            try
            {
                // Test OpenCV initialization
                string version = Cv2.GetVersionString() ?? "Unknown";
                Console.WriteLine($"OpenCV Version: {version}");
                
                // Process the images
                ProcessTableImages(cliArgs.ScreenshotPath, cliArgs.PartialImagePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing images: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
        
        private static string GenerateOutputPath(string screenshotPath)
        {
            string directory = Path.GetDirectoryName(screenshotPath) ?? "";
            string filename = Path.GetFileNameWithoutExtension(screenshotPath);
            string extension = Path.GetExtension(screenshotPath);
            
            return Path.Combine(directory, $"{filename}_annotated{extension}");
        }

        static void ProcessTableImages(string screenshotPath, string partialImagePath)
        {
            using var screenshot = Cv2.ImRead(screenshotPath);
            if (screenshot.Empty())
            {
                Console.Error.WriteLine($"Could not load screenshot: {screenshotPath}");
                return;
            }

            using var partialImage = Cv2.ImRead(partialImagePath);
            if (partialImage.Empty())
            {
                Console.Error.WriteLine($"Could not load partial image: {partialImagePath}");
                return;
            }

            // Create table prototype using the screenshot for detection
            var tablePrototype = new TablePrototype(screenshot, Path.GetFileNameWithoutExtension(screenshotPath));
            
            // Extract features from the screenshot
            var features = tablePrototype.ExtractLineFeatures();
            
            // Get detailed summary
            var summary = tablePrototype.GetSummary();
            
            Console.WriteLine("\n=== Table Detection Results ===");
            
            // 1. StdOut: Boundaries of the complete table in the screenshot
            if (summary.ContainsKey("table_boundaries"))
            {
                var tableBounds = (Dictionary<string, Point>)summary["table_boundaries"];
                Console.WriteLine("Table Boundaries:");
                if (tableBounds.ContainsKey("topleft"))
                    Console.WriteLine($"  topleft: ({tableBounds["topleft"].X}, {tableBounds["topleft"].Y})");
                if (tableBounds.ContainsKey("topright"))
                    Console.WriteLine($"  topright: ({tableBounds["topright"].X}, {tableBounds["topright"].Y})");
                if (tableBounds.ContainsKey("bottomleft"))
                    Console.WriteLine($"  bottomleft: ({tableBounds["bottomleft"].X}, {tableBounds["bottomleft"].Y})");
                if (tableBounds.ContainsKey("bottomright"))
                    Console.WriteLine($"  bottomright: ({tableBounds["bottomright"].X}, {tableBounds["bottomright"].Y})");
                Console.WriteLine();
            }
            
            // 2. StdOut: Boundaries of header
            if (summary.ContainsKey("header_boundaries"))
            {
                var headerBounds = (Dictionary<string, Point>)summary["header_boundaries"];
                Console.WriteLine("Header Boundaries:");
                if (headerBounds.ContainsKey("topleft"))
                    Console.WriteLine($"  topleft: ({headerBounds["topleft"].X}, {headerBounds["topleft"].Y})");
                if (headerBounds.ContainsKey("topright"))
                    Console.WriteLine($"  topright: ({headerBounds["topright"].X}, {headerBounds["topright"].Y})");
                if (headerBounds.ContainsKey("bottomleft"))
                    Console.WriteLine($"  bottomleft: ({headerBounds["bottomleft"].X}, {headerBounds["bottomleft"].Y})");
                if (headerBounds.ContainsKey("bottomright"))
                    Console.WriteLine($"  bottomright: ({headerBounds["bottomright"].X}, {headerBounds["bottomright"].Y})");
                Console.WriteLine();
            }
            
            Console.WriteLine($"Table type detected: {tablePrototype.TableType}");
            Console.WriteLine($"Features detected: {features}");
            
            // 3. Visualize and save result with color coding
            string outputPath = GenerateOutputPath(screenshotPath);
            using var result = tablePrototype.VisualizeFeatures(outputPath);
            Console.WriteLine($"Annotated image saved to: {outputPath}");
            
            tablePrototype.Dispose();
        }
    }
}

