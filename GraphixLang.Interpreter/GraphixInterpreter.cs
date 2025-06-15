using GraphixLang.Parser;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SixLabors.Fonts;
using System.Numerics;
using System.Text;

namespace GraphixLang.Interpreter
{
    public class GraphixInterpreter : IASTVisitor
    {
        private readonly Dictionary<string, object> _environment = new();
        private readonly Dictionary<string, Type> _variableTypes = new();
        private int _counterValue = 0;
        private int _operationCount = 0;
        private readonly string _baseDir;

        
        private class ImageData
        {
            public Image Image { get; set; }
            public string Path { get; set; }
            public string Filename { get; set; }
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        public GraphixInterpreter(string baseDir = null)
        {
            _baseDir = baseDir ?? System.IO.Directory.GetCurrentDirectory();
        }

        public int OperationCount => _operationCount;

        public string ExecuteAst(ProgramNode ast)
        {
            try
            {
                Visit(ast);
                return $"Executed successfully with {_operationCount} operations";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        
        private string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);
                
            
            return Path.GetFullPath(Path.Combine(_baseDir, path));
        }

        
        private IEnumerable<string> GetImageFilesInDirectory(string path)
        {
            string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.tiff", "*.bmp" };
            return extensions.SelectMany(ext => System.IO.Directory.GetFiles(path, ext));
        }

        
        private IImageEncoder GetEncoderFromExtension(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" => new PngEncoder(),
                ".jpg" or ".jpeg" => new JpegEncoder { Quality = 90 },
                ".webp" => new WebpEncoder(),
                ".tiff" => new TiffEncoder(),
                ".bmp" => new BmpEncoder(),
                _ => new PngEncoder()
            };
        }

        
        private string SanitizeFilename(string filename)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", filename.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        #region IASTVisitor Implementation

        public void Visit(ProgramNode node)
        {
            foreach (var block in node.Blocks)
            {
                Visit(block);
            }
        }

        public void Visit(BlockNode node)
        {
            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
                _operationCount++;
            }
        }

        public void Visit(BatchDeclarationNode node)
        {
            
            var result = EvaluateExpression(node.Expression, true);
            
            
            List<string> paths;
            if (result is List<string> pathList)
            {
                paths = pathList;
            }
            else if (result is string singlePath)
            {
                paths = new List<string> { singlePath };
            }
            else
            {
                paths = new List<string>();
                Console.WriteLine($"Warning: Batch expression for {node.Identifier} did not evaluate to a valid path or paths");
            }

            
            var resolvedPaths = paths.Select(p => ResolvePath(p)).ToList();
            
            _environment[node.Identifier] = resolvedPaths;
            _variableTypes[node.Identifier] = typeof(List<string>);
            
            Console.WriteLine($"Created batch {node.Identifier} with {resolvedPaths.Count} paths: {string.Join(", ", resolvedPaths)}");
        }

        public void Visit(ForEachNode node)
        {
            if (!_environment.TryGetValue(node.BatchIdentifier, out var batchObj))
                throw new Exception($"Unknown batch identifier: {node.BatchIdentifier}");
                
            if (!(batchObj is List<string> batch))
                throw new Exception($"Batch {node.BatchIdentifier} is not a valid batch (got {batchObj?.GetType()?.Name})");

            
            string exportPath = ResolvePath(node.ExportPath);
            System.IO.Directory.CreateDirectory(exportPath);

            Console.WriteLine($"Processing batch {node.BatchIdentifier} with {batch.Count} directories");
            foreach (var path in batch)
            {
                Console.WriteLine($"  - {path}");
            }

            
            List<string> imageFiles = new List<string>();
            foreach (string batchPath in batch)
            {
                if (System.IO.Directory.Exists(batchPath))
                {
                    var batchFiles = GetImageFilesInDirectory(batchPath).ToList();
                    Console.WriteLine($"Found {batchFiles.Count} images in {batchPath}");
                    imageFiles.AddRange(batchFiles);
                }
                else
                {
                    Console.WriteLine($"Warning: Batch directory not found: {batchPath}");
                }
            }

            Console.WriteLine($"Total images to process: {imageFiles.Count}");
            
            
            foreach (string imgPath in imageFiles)
            {
                try
                {
                    
                    using (var img = Image.Load(imgPath))
                    {
                        var imgData = new ImageData
                        {
                            Image = img,
                            Path = imgPath,
                            Filename = Path.GetFileName(imgPath)
                        };

                        _environment[node.VarIdentifier] = imgData;
                        _variableTypes[node.VarIdentifier] = typeof(ImageData);

                        
                        Visit(node.Body);

                        
                        ExportImage(node.VarIdentifier, exportPath, true);
                    }
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine($"Error processing {imgPath}: {ex.Message}");
                }
            }
        }

        public void Visit(VariableDeclarationNode node)
        {
            
            if (node.Initializer != null)
            {
                var value = EvaluateExpression(node.Initializer);
                _environment[node.Identifier] = value;
                _variableTypes[node.Identifier] = value?.GetType() ?? typeof(object);
            }
            else
            {
                
                switch (node.Type)
                {
                    case GraphixLang.Lexer.TokenType.TYPE_INT:
                        _environment[node.Identifier] = 0;
                        _variableTypes[node.Identifier] = typeof(int);
                        break;
                    case GraphixLang.Lexer.TokenType.TYPE_DBL:
                        _environment[node.Identifier] = 0.0;
                        _variableTypes[node.Identifier] = typeof(double);
                        break;
                    case GraphixLang.Lexer.TokenType.TYPE_STR:
                        _environment[node.Identifier] = string.Empty;
                        _variableTypes[node.Identifier] = typeof(string);
                        break;
                    case GraphixLang.Lexer.TokenType.TYPE_BOOL:
                        _environment[node.Identifier] = false;
                        _variableTypes[node.Identifier] = typeof(bool);
                        break;
                }
            }
        }

        public void Visit(ImageDeclarationNode node)
        {
            
            string resolvedPath = ResolvePath(node.Path);
            
            if (!File.Exists(resolvedPath))
                throw new FileNotFoundException($"Image file not found: {resolvedPath}");

            try
            {
                var img = Image.Load(resolvedPath);
                var imgData = new ImageData
                {
                    Image = img,
                    Path = resolvedPath,
                    Filename = Path.GetFileName(resolvedPath)
                };

                _environment[node.Identifier] = imgData;
                _variableTypes[node.Identifier] = typeof(ImageData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading image '{resolvedPath}': {ex.Message}");
            }
        }

        public void Visit(AssignmentNode node)
        {
            var value = EvaluateExpression(node.Value);
            _environment[node.Identifier] = value;
        }

        public void Visit(IfNode node)
        {
            bool condition = EvaluateCondition(node.Condition);
            
            if (condition)
            {
                Visit(node.ThenBranch);
            }
            else
            {
                
                bool elifMatched = false;
                foreach (var elifBranch in node.ElifBranches)
                {
                    if (EvaluateCondition(elifBranch.Condition))
                    {
                        Visit(elifBranch.Body);
                        elifMatched = true;
                        break;
                    }
                }

                
                if (!elifMatched && node.ElseBranch != null)
                {
                    Visit(node.ElseBranch);
                }
            }
        }

        public void Visit(ExportNode node)
        {
            ExportImage(node.ImageIdentifier, node.DestinationPath, node.KeepOriginal);
        }

        public void Visit(SetFilterNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            
            switch (node.FilterType)
            {
                case GraphixLang.Lexer.TokenType.SEPIA:
                    imgData.Image.Mutate(x => x.Sepia());
                    break;
                case GraphixLang.Lexer.TokenType.BW:
                    imgData.Image.Mutate(x => x.Grayscale());
                    break;
                case GraphixLang.Lexer.TokenType.NEGATIVE:
                    imgData.Image.Mutate(x => x.Invert());
                    break;
                case GraphixLang.Lexer.TokenType.SHARPEN:
                    imgData.Image.Mutate(x => x.GaussianSharpen(3.5f));
                    break;
            }
        }

        public void Visit(RotateNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            switch (node.Direction)
            {
                case GraphixLang.Lexer.TokenType.LEFT:
                    imgData.Image.Mutate(x => x.Rotate(-90));
                    break;
                case GraphixLang.Lexer.TokenType.RIGHT:
                    imgData.Image.Mutate(x => x.Rotate(90));
                    break;
            }
        }

        public void Visit(CropNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            int width = Convert.ToInt32(EvaluateExpression(node.Width));
            int height = Convert.ToInt32(EvaluateExpression(node.Height));
            
            
            int x = Math.Max(0, (imgData.Image.Width - width) / 2);
            int y = Math.Max(0, (imgData.Image.Height - height) / 2);
            
            
            width = Math.Min(width, imgData.Image.Width - x);
            height = Math.Min(height, imgData.Image.Height - y);

            imgData.Image.Mutate(i => i.Crop(new Rectangle(x, y, width, height)));
        }

        public void Visit(OrientationNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            bool isLandscape = imgData.Image.Width > imgData.Image.Height;

            if (node.OrientationType == GraphixLang.Lexer.TokenType.LANDSCAPE && !isLandscape)
            {
                imgData.Image.Mutate(x => x.Rotate(90));
            }
            else if (node.OrientationType == GraphixLang.Lexer.TokenType.PORTRAIT && isLandscape)
            {
                imgData.Image.Mutate(x => x.Rotate(90));
            }
        }

        public void Visit(HueNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            
            float hue = node.HueValue / 360.0f;
            imgData.Image.Mutate(x => x.Hue(hue));
        }

        public void Visit(BrightnessNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            
            float factor = node.Value / 100.0f;
            imgData.Image.Mutate(x => x.Brightness(factor));
        }

        public void Visit(ContrastNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            float factor = node.Value / 100.0f;
            imgData.Image.Mutate(x => x.Contrast(factor));
        }
        public void Visit(QuantizeNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            
            var quantizer = new WuQuantizer(new QuantizerOptions
            {
                MaxColors = node.Colors
            });
            
            
            imgData.Image.Mutate(x => x.Quantize(quantizer));
        }
        

        public void Visit(CompressNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            
            
            imgData.Metadata["CompressionQuality"] = node.Quality;
        }

        public void Visit(ResizeNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            int currentWidth = imgData.Image.Width;
            int currentHeight = imgData.Image.Height;
            int newWidth, newHeight;
            
            ResizeOptions options = new ResizeOptions
            {
                Mode = node.MaintainAspectRatio ? ResizeMode.Max : ResizeMode.Stretch
            };

            if (node.IsAspectRatioMode)
            {
                
                string aspectRatioStr = node.AspectRatio.ToString().Replace("RATIO_", "").Replace("_", ":");
                string[] parts = aspectRatioStr.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int ratioWidth) && int.TryParse(parts[1], out int ratioHeight))
                {
                    
                    newWidth = currentWidth;
                    newHeight = (currentWidth * ratioHeight) / ratioWidth;
                    
                    if (newHeight > currentHeight)
                    {
                        newHeight = currentHeight;
                        newWidth = (currentHeight * ratioWidth) / ratioHeight;
                    }
                    
                    options.Size = new Size(newWidth, newHeight);
                    imgData.Image.Mutate(x => x.Resize(options));
                }
            }
            else
            {
                
                newWidth = Convert.ToInt32(EvaluateExpression(node.Width));
                newHeight = Convert.ToInt32(EvaluateExpression(node.Height));
                options.Size = new Size(newWidth, newHeight);
                imgData.Image.Mutate(x => x.Resize(options));
            }
        }

        public void Visit(ConvertNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            
            
            string baseFilename = Path.GetFileNameWithoutExtension(imgData.Filename);
            string newExtension = "." + node.TargetFormat.ToString().ToLower();
            imgData.Filename = baseFilename + newExtension;
        }

        public void Visit(WatermarkNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            try
            {
                
                Color color;
                if (node.IsHexColor)
                {
                    string hexColor = node.ColorValue.Replace("~H", "").Replace("~", "");
                    color = Color.ParseHex("#" + hexColor);
                }
                else
                {
                    string rgbColor = node.ColorValue.Replace("~R", "").Replace("~", "");
                    if (rgbColor.Contains(','))
                    {
                        var parts = rgbColor.Split(',').Select(int.Parse).ToArray();
                        color = Color.FromRgb((byte)parts[0], (byte)parts[1], (byte)parts[2]);
                    }
                    else
                    {
                        int r = int.Parse(rgbColor.Substring(0, 3));
                        int g = int.Parse(rgbColor.Substring(3, 3));
                        int b = int.Parse(rgbColor.Substring(6, 3));
                        color = Color.FromRgb((byte)r, (byte)g, (byte)b);
                    }
                }

                
                Font font;
                try
                {
                    var fontCollection = new FontCollection();
                    var fontFamily = fontCollection.Add("Arial.ttf");
                    font = fontFamily.CreateFont(Math.Min(imgData.Image.Width, imgData.Image.Height) / 20);
                }
                catch
                {
                    var fontCollection = new FontCollection();
                    var fontFamily = fontCollection.Add("C:/Windows/Fonts/Arial.ttf");
                    font = fontFamily.CreateFont(Math.Min(imgData.Image.Width, imgData.Image.Height) / 20);
                }

                
                imgData.Image.Mutate(x => x.DrawText(node.Text, font, color, new PointF(10, 10)));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to apply watermark: {ex.Message}");
            }
        }

        public void Visit(ImageWatermarkNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var targetImgData))
                throw new Exception($"Unknown target image variable: {node.ImageIdentifier}");
                
            if (!TryGetImage(node.WatermarkImageIdentifier, out var watermarkImgData))
                throw new Exception($"Unknown watermark image variable: {node.WatermarkImageIdentifier}");

            
            int watermarkWidth = targetImgData.Image.Width / 4;
            int watermarkHeight = (int)(watermarkImgData.Image.Height * ((float)watermarkWidth / watermarkImgData.Image.Width));
            
            
            int x = targetImgData.Image.Width - watermarkWidth - 10;
            int y = targetImgData.Image.Height - watermarkHeight - 10;
            
            
            using var resizedWatermark = watermarkImgData.Image.Clone(i => i.Resize(watermarkWidth, watermarkHeight));
            
            
            float opacity = node.Transparency / 255.0f;
            
            
            targetImgData.Image.Mutate(i => i.DrawImage(resizedWatermark, new Point(x, y), opacity));
        }

        public void Visit(StripMetadataNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            if (node.StripAll)
            {
                
                imgData.Image.Metadata.ExifProfile = null;
                imgData.Image.Metadata.IptcProfile = null;
                imgData.Image.Metadata.XmpProfile = null;
            }
            else
            {
                
                if (imgData.Image.Metadata.ExifProfile != null)
                {
                    foreach (var metadataType in node.MetadataTypes)
                    {
                        
                        switch (metadataType)
                        {
                            case GraphixLang.Lexer.TokenType.GPS:
                                RemoveExifTags(imgData.Image.Metadata.ExifProfile, ExifTag.GPSLatitude, ExifTag.GPSLongitude);
                                break;
                            case GraphixLang.Lexer.TokenType.CAMERA:
                                RemoveExifTags(imgData.Image.Metadata.ExifProfile, ExifTag.Make, ExifTag.Model);
                                break;
                            case GraphixLang.Lexer.TokenType.COPYRIGHT:
                                RemoveExifTags(imgData.Image.Metadata.ExifProfile, ExifTag.Copyright);
                                break;
                        }
                    }
                }
            }
        }

        public void Visit(AddMetadataNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            if (imgData.Image.Metadata.ExifProfile == null)
                imgData.Image.Metadata.ExifProfile = new ExifProfile();

            switch (node.MetadataType)
            {
                case GraphixLang.Lexer.TokenType.TITLE:
                    imgData.Image.Metadata.ExifProfile.SetValue(ExifTag.ImageDescription, node.Value);
                    break;
                case GraphixLang.Lexer.TokenType.COPYRIGHT:
                    imgData.Image.Metadata.ExifProfile.SetValue(ExifTag.Copyright, node.Value);
                    break;
                case GraphixLang.Lexer.TokenType.DESCRIPTION:
                    imgData.Image.Metadata.ExifProfile.SetValue(ExifTag.UserComment, node.Value);
                    break;
                case GraphixLang.Lexer.TokenType.TAGS:
                    
                    imgData.Image.Metadata.ExifProfile.SetValue(ExifTag.XPKeywords, node.Value);
                    
                    
                    if (imgData.Image.Metadata.IptcProfile == null)
                        imgData.Image.Metadata.IptcProfile = new SixLabors.ImageSharp.Metadata.Profiles.Iptc.IptcProfile();
                        
                    
                    var keywords = node.Value.Split(',').Select(t => t.Trim()).ToArray();
                    foreach (var keyword in keywords)
                    {
                        imgData.Image.Metadata.IptcProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Iptc.IptcTag.Keywords, keyword);
                    }
                    
                    
                    Console.WriteLine($"Added tags to {imgData.Filename}: {node.Value}");
                    break;
            }
        }

        public void Visit(RenameNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            string oldFilename = imgData.Filename;
            string baseFilename = Path.GetFileNameWithoutExtension(oldFilename);
            string extension = Path.GetExtension(oldFilename);
            
            StringBuilder newName = new StringBuilder();
            
            foreach (var term in node.Terms)
            {
                term.Accept(this);
                
                switch (term.Type)
                {
                    case RenameTermType.STRING:
                        newName.Append(term.StringValue);
                        break;
                    case RenameTermType.COUNTER:
                        newName.Append(_counterValue++);
                        break;
                    case RenameTermType.METADATA:
                        if (term.MetadataValue.MetadataType == GraphixLang.Lexer.TokenType.FNAME)
                        {
                            newName.Append(baseFilename);
                        }
                        break;
                }
            }
            
            if (newName.Length == 0)
                newName.Append(baseFilename);
                
            if (!newName.ToString().EndsWith(extension))
                newName.Append(extension);
                
            imgData.Filename = SanitizeFilename(newName.ToString());
        }

        public void Visit(WebOptimizeNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            
            imgData.Metadata["WebOptimizeMode"] = node.IsLossless ? "LOSSLESS" : "LOSSY";
            imgData.Metadata["WebOptimizeQuality"] = node.IsLossless ? 100 : node.Quality;
            
            
            string baseFilename = Path.GetFileNameWithoutExtension(imgData.Filename);
            string extension = node.IsLossless ? ".png" : ".jpg";
            imgData.Filename = baseFilename + extension;
        }

        public void Visit(OpacityNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            float opacity = node.Value / 100.0f;
            
            
            if (imgData.Image.PixelType.AlphaRepresentation == PixelAlphaRepresentation.None)
            {
                var temp = imgData.Image.CloneAs<Rgba32>();
                imgData.Image.Dispose();
                imgData.Image = temp;
            }
            
            
            imgData.Image.Mutate(x => x.Opacity(opacity));
        }

        public void Visit(NoiseNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            float noiseLevel = node.Value / 100.0f;
            
            Random random = new Random();
            imgData.Image.Mutate(x => x.ProcessPixelRowsAsVector4((span, point) => 
            {
                for (int i = 0; i < span.Length; i++)
                {
                    ref Vector4 pixel = ref span[i];
                    float noise = (random.NextSingle() - 0.5f) * noiseLevel;
                    
                    pixel.X = Math.Clamp(pixel.X + noise, 0, 1);
                    pixel.Y = Math.Clamp(pixel.Y + noise, 0, 1);
                    pixel.Z = Math.Clamp(pixel.Z + noise, 0, 1);
                }
            }));
        }

        public void Visit(BlurNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            float blurAmount = node.Value / 10.0f;
            imgData.Image.Mutate(x => x.GaussianBlur(blurAmount));
        }

        public void Visit(PixelateNode node)
        {
            if (!TryGetImage(node.ImageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {node.ImageIdentifier}");

            int pixelSize = (101 - node.Value) / 2; 
            if (pixelSize <= 0) pixelSize = 1;
            
            imgData.Image.Mutate(x => x.Pixelate(pixelSize));
        }

        public void Visit(RenameTermNode node)
        {
            
        }

        public void Visit(ElifBranchNode node)
        {
            
        }

        public void Visit(BinaryExpressionNode node)
        {
            
        }

        public void Visit(LiteralNode node)
        {
            
        }

        public void Visit(VariableReferenceNode node)
        {
            
        }

        public void Visit(MetadataNode node)
        {
            
        }

        public void Visit(BatchExpressionNode node)
        {
            
        }

        #endregion

        #region Helper Methods

        private bool TryGetImage(string identifier, out ImageData imageData)
        {
            if (_environment.TryGetValue(identifier, out var value) && value is ImageData imgData)
            {
                imageData = imgData;
                return true;
            }
            
            imageData = null;
            return false;
        }

        private void RemoveExifTags(ExifProfile profile, params ExifTag[] tags)
        {
            foreach (var tag in tags)
            {
                
                profile.RemoveValue(tag);
            }
        }

        private object EvaluateExpression(ExpressionNode node, bool isBatchExpression = false)
        {
            if (node is LiteralNode literal)
            {
                return literal.Value;
            }
            else if (node is VariableReferenceNode varRef)
            {
                if (!_environment.TryGetValue(varRef.Identifier, out var value))
                    throw new Exception($"Unknown variable: {varRef.Identifier}");
                return value;
            }
            else if (node is MetadataNode metadata)
            {
                if (!TryGetImage(metadata.ImageIdentifier, out var imgData))
                    throw new Exception($"Unknown image variable: {metadata.ImageIdentifier}");
                    
                switch (metadata.MetadataType)
                {
                    case GraphixLang.Lexer.TokenType.FWIDTH:
                        return imgData.Image.Width;
                    case GraphixLang.Lexer.TokenType.FHEIGHT:
                        return imgData.Image.Height;
                    case GraphixLang.Lexer.TokenType.FNAME:
                        return Path.GetFileNameWithoutExtension(imgData.Filename);
                    case GraphixLang.Lexer.TokenType.FSIZE:
                        return new FileInfo(imgData.Path).Length / 1024.0; 
                }
            }
            else if (node is BinaryExpressionNode binary)
            {
                
                if (binary.Operator == GraphixLang.Lexer.TokenType.PLUS && isBatchExpression)
                {
                    var left = EvaluateExpression(binary.Left, true);
                    var right = EvaluateExpression(binary.Right, true);
                    
                    List<string> result = new List<string>();
                    
                    
                    if (left is List<string> leftList)
                        result.AddRange(leftList);
                    else if (left is string leftStr)
                        result.Add(leftStr);
                    
                    
                    if (right is List<string> rightList)
                        result.AddRange(rightList);
                    else if (right is string rightStr)
                        result.Add(rightStr);
                    
                    return result;
                }
                
                
                var leftValue = EvaluateExpression(binary.Left);
                var rightValue = EvaluateExpression(binary.Right);

                
                if (binary.Operator == GraphixLang.Lexer.TokenType.PLUS && 
                    (leftValue is string || rightValue is string))
                {
                    return leftValue.ToString() + rightValue.ToString();
                }

                
                if (TryConvertToNumeric(leftValue, out dynamic leftNum) && 
                    TryConvertToNumeric(rightValue, out dynamic rightNum))
                {
                    switch (binary.Operator)
                    {
                        case GraphixLang.Lexer.TokenType.PLUS:
                            return leftNum + rightNum;
                        case GraphixLang.Lexer.TokenType.MINUS:
                            return leftNum - rightNum;
                        case GraphixLang.Lexer.TokenType.MULTIPLY:
                            return leftNum * rightNum;
                        case GraphixLang.Lexer.TokenType.DIVIDE:
                            return leftNum / rightNum;
                        
                        case GraphixLang.Lexer.TokenType.EQUAL:
                            return leftNum == rightNum;
                        case GraphixLang.Lexer.TokenType.NOT_EQUAL:
                            return leftNum != rightNum;
                        case GraphixLang.Lexer.TokenType.GREATER:
                            return leftNum > rightNum;
                        case GraphixLang.Lexer.TokenType.GREATER_EQUAL:
                            return leftNum >= rightNum;
                        case GraphixLang.Lexer.TokenType.SMALLER:
                            return leftNum < rightNum;
                        case GraphixLang.Lexer.TokenType.SMALLER_EQUAL:
                            return leftNum <= rightNum;
                    }
                }
                
                
                switch (binary.Operator)
                {
                    case GraphixLang.Lexer.TokenType.EQUAL:
                        return Equals(leftValue, rightValue);
                    case GraphixLang.Lexer.TokenType.NOT_EQUAL:
                        return !Equals(leftValue, rightValue);
                }
                
                throw new Exception($"Cannot perform operation {binary.Operator} on types {leftValue?.GetType()} and {rightValue?.GetType()}");
            }
            else if (node is BatchExpressionNode batchExpr)
            {
                
                return EvaluateExpression(batchExpr, true);
            }
            
            return null;
        }

        private bool EvaluateCondition(ExpressionNode condition)
        {
            var result = EvaluateExpression(condition, false);
            if (result is bool boolResult)
                return boolResult;
                
            
            if (result is int intResult)
                return intResult != 0;
            if (result is double doubleResult)
                return doubleResult != 0;
            if (result is string strResult)
                return !string.IsNullOrEmpty(strResult);
                
            return result != null;
        }

        private bool TryConvertToNumeric(object value, out dynamic result)
        {
            if (value is int || value is double || value is float || value is long)
            {
                result = value;
                return true;
            }
            
            if (value is string str && double.TryParse(str, out double parsed))
            {
                result = parsed;
                return true;
            }
            
            result = null;
            return false;
        }

        private void ExportImage(string imageIdentifier, string destinationPath, bool keepOriginal)
        {
            if (!TryGetImage(imageIdentifier, out var imgData))
                throw new Exception($"Unknown image variable: {imageIdentifier}");
                
            string resolvedPath = ResolvePath(destinationPath);
            
            
            if (System.IO.Directory.Exists(resolvedPath) || resolvedPath.EndsWith(Path.DirectorySeparatorChar))
            {
                
                System.IO.Directory.CreateDirectory(resolvedPath);
                
                resolvedPath = Path.Combine(resolvedPath, imgData.Filename);
            }
            else
            {
                
                string parentDir = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(parentDir))
                    System.IO.Directory.CreateDirectory(parentDir);
            }
            
            
            string extension = Path.GetExtension(resolvedPath).ToLower();
            IImageEncoder encoder;
            
            
            if (extension == ".png")
            {
                PngCompressionLevel compressionLevel = PngCompressionLevel.DefaultCompression;
                
                
                if (imgData.Metadata.TryGetValue("WebOptimizeMode", out var modeObj) && 
                    modeObj is string mode && mode == "LOSSLESS")
                {
                    compressionLevel = PngCompressionLevel.BestCompression;
                }
                
                encoder = new PngEncoder {
                    CompressionLevel = compressionLevel,
                    
                    ChunkFilter = PngChunkFilter.None 
                };
            }
            else if (extension == ".jpg" || extension == ".jpeg")
            {
                
                int jpegQuality = 90; 
                
                if (imgData.Metadata.TryGetValue("CompressionQuality", out var qualityObj) && qualityObj is int quality)
                    jpegQuality = quality;
                else if (imgData.Metadata.TryGetValue("WebOptimizeQuality", out var webQualityObj) && webQualityObj is int webQuality)
                    jpegQuality = webQuality;
                
                
                encoder = new JpegEncoder { Quality = jpegQuality };
            }
            else
            {
                encoder = GetEncoderFromExtension(extension);
            }
            
            
            
            
            if (imgData.Image.Metadata.ExifProfile != null)
            {
                Console.WriteLine($"Saving image with {imgData.Image.Metadata.ExifProfile.Values.Count()} EXIF tags");
                var exifTags = imgData.Image.Metadata.ExifProfile.Values.Select(v => v.Tag.ToString()).Take(3);
                Console.WriteLine($"Sample EXIF tags: {string.Join(", ", exifTags)}...");
            }
            
            if (imgData.Image.Metadata.IptcProfile != null)
            {
                Console.WriteLine($"Image has IPTC profile with data");
            }
            
            
            imgData.Image.Save(resolvedPath, encoder);
            
            
            if (!keepOriginal && File.Exists(imgData.Path))
            {
                try
                {
                    File.Delete(imgData.Path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not delete original file {imgData.Path}: {ex.Message}");
                }
            }
        }

        #endregion
    }
    
    
    public static class ImageProcessingExtensions
    {
        
        public static IImageProcessingContext Hue(this IImageProcessingContext context, float hue)
        {
            return context.ProcessPixelRowsAsVector4((span, point) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    ref Vector4 pixel = ref span[i];
                    RgbToHsv(pixel.X, pixel.Y, pixel.Z, out float h, out float s, out float v);
                    h = (h + hue) % 1.0f;
                    HsvToRgb(h, s, v, out float r, out float g, out float b);
                    pixel.X = r;
                    pixel.Y = g;
                    pixel.Z = b;
                }
            });
        }
        

private static void RgbToHsv(float r, float g, float b, out float h, out float s, out float v)
{
    float max = Math.Max(r, Math.Max(g, b));
    float min = Math.Min(r, Math.Min(g, b));
    float delta = max - min;
    
    
    v = max;
    
    
    s = max == 0 ? 0 : delta / max;
    
    
    if (delta == 0)
        h = 0;
    else if (max == r)
        h = ((g - b) / delta) % 6;
    else if (max == g)
        h = (b - r) / delta + 2;
    else
        h = (r - g) / delta + 4;
        
    h = h / 6.0f;
    
    if (h < 0)
        h += 1.0f;
}


private static void HsvToRgb(float h, float s, float v, out float r, out float g, out float b)
{
    if (s == 0)
    {
        r = g = b = v;
        return;
    }
    
    float c = v * s;
    float x = c * (1 - Math.Abs((h * 6) % 2 - 1));
    float m = v - c;
    
    int hi = (int)(h * 6);
    switch (hi)
    {
        case 0: r = c; g = x; b = 0; break;
        case 1: r = x; g = c; b = 0; break;
        case 2: r = 0; g = c; b = x; break;
        case 3: r = 0; g = x; b = c; break;
        case 4: r = x; g = 0; b = c; break;
        default: r = c; g = 0; b = x; break;
    }
    
    r += m;
    g += m;
    b += m;
}
    }
}
