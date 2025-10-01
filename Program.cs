using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TableDetection
{
    public enum TableType
    {
        SAP,
        WEB,
        UNKNOWN
    }

    public class LineFeatures
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

    public class TablePrototype
    {
        private Mat originalImage;
        private Mat grayImage = new Mat();
        private Mat thresholdImage = new Mat();
        
        public string Name { get; set; }
        public LineFeatures LineFeatures { get; private set; } = new LineFeatures();
        public TableType TableType { get; private set; } = TableType.UNKNOWN;
        public Rect? Boundaries { get; private set; } = null; // (x, y, width, height)

        public TablePrototype(Mat image, string name = "")
        {
            originalImage = image.Clone();
            Name = name;
        }

        public Mat Preprocess()
        {
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

            // Draw horizontal lines in RED
            if (LineFeatures?.HorizontalPositions != null)
            {
                foreach (var y in LineFeatures.HorizontalPositions)
                {
                    Cv2.Line(visImage, new Point(0, y), 
                        new Point(visImage.Width, y), 
                        new Scalar(0, 0, 255), 2);
                }
            }

            // Draw vertical lines in BLUE
            if (LineFeatures?.VerticalPositions != null)
            {
                foreach (var x in LineFeatures.VerticalPositions)
                {
                    Cv2.Line(visImage, new Point(x, 0), 
                        new Point(x, visImage.Height), 
                        new Scalar(255, 0, 0), 2);
                }
            }

            // Add text annotation
            string text = $"{Name} | Type: {TableType} | " +
                         $"Rows: {LineFeatures?.HorizontalPositions?.Count ?? 0} | " +
                         $"Cols: {LineFeatures?.VerticalPositions?.Count ?? 0}";
            Cv2.PutText(visImage, text, new Point(10, 30),
                HersheyFonts.HersheySimplex, 0.6, 
                new Scalar(0, 255, 0), 2);

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

            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = TableType.ToString(),
                ["dimensions"] = $"{originalImage.Width}x{originalImage.Height}",
                ["num_rows"] = LineFeatures?.HorizontalPositions?.Count ?? 0,
                ["num_cols"] = LineFeatures?.VerticalPositions?.Count ?? 0,
                ["avg_row_height"] = LineFeatures?.AverageRowHeight ?? 0.0,
                ["avg_col_width"] = LineFeatures?.AverageColumnWidth ?? 0.0,
                ["header_height"] = LineFeatures?.HeaderHeight ?? 0
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
            Console.WriteLine("Table Detection Program - Advanced Prototype Analysis");
            Console.WriteLine("Based on Python notebook implementation\n");
            
            try
            {
                // Test OpenCV initialization first
                Console.WriteLine("Testing OpenCV initialization...");
                string version = Cv2.GetVersionString() ?? "Unknown";
                Console.WriteLine($"OpenCV Version: {version}");
                
                // Test with sample images if they exist
                ProcessTableImage("media/SAP.png", "SAP_Table");
                ProcessTableImage("media/WEB.png", "WEB_Table");
                
                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("Programa ejecutado correctamente.");
                Console.WriteLine("Si las imágenes SAP.png y WEB.png estuvieran disponibles,");
                Console.WriteLine("se habría realizado el análisis completo de prototipos de tabla.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    if (ex.InnerException.InnerException != null)
                    {
                        Console.WriteLine($"Inner Inner Exception: {ex.InnerException.InnerException.Message}");
                    }
                }
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        static void ProcessTableImage(string imagePath, string tableName)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                Console.WriteLine($"Imagen no encontrada: {imagePath}");
                return;
            }

            using var image = Cv2.ImRead(imagePath);
            if (image.Empty())
            {
                Console.WriteLine($"No se pudo cargar la imagen: {imagePath}");
                return;
            }

            // Create table prototype
            var tablePrototype = new TablePrototype(image, tableName);
            
            // Extract features automatically
            var features = tablePrototype.ExtractLineFeatures();
            
            // Print results
            Console.WriteLine($"\n=== {tableName} Analysis ===");
            Console.WriteLine($"Características detectadas: {features}");
            Console.WriteLine($"Tipo de tabla: {tablePrototype.TableType}");
            
            // Get detailed summary
            var summary = tablePrototype.GetSummary();
            Console.WriteLine("\nResumen detallado:");
            foreach (var kvp in summary)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            
            // Visualize and save result
            string outputPath = $"output_{tableName.ToLower()}.png";
            using var result = tablePrototype.VisualizeFeatures(outputPath);
            Console.WriteLine($"Resultado guardado en: {outputPath}");
            
            tablePrototype.Dispose();
        }
    }
}