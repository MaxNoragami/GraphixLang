<div align="center">
  <img src="banner.png" alt="logo">

<hr>
  <a href="https://marketplace.visualstudio.com/items?itemName=GraphixLang.graphixlang">
    <img src="https://img.shields.io/visual-studio-marketplace/v/GraphixLang.graphixlang?label=Extension">
  </a>
</div>

GraphixLang is a domain-specific language designed for batch image processing and manipulation operations. It provides an intuitive syntax for applying filters, transformations, metadata operations, and exporting images across entire directories with powerful conditional logic and automation features.

## Features

- **Batch Processing**: Process entire directories of images with `FOREACH` loops
- **Image Operations**: Filters (sepia, black & white, sharpen, negative), resizing, cropping, rotation, compression
- **Advanced Effects**: Brightness/contrast adjustment, opacity, noise, blur, pixelation, hue shifts
- **Watermarking**: Both text and image watermarks with customizable transparency and colors
- **Metadata Management**: Read, strip, and add image metadata (EXIF, GPS, camera info)
- **Format Conversion**: Convert between PNG, JPG, JPEG, WEBP, TIFF, BMP formats
- **Web Optimization**: Lossless and lossy compression for web deployment
- **Conditional Logic**: IF/ELIF/ELSE statements for dynamic processing based on image properties
- **File Renaming**: Advanced renaming with counters, metadata, and custom strings

## Prerequisites

### .NET Requirements
- .NET 9.0 SDK or later
- Compatible with Windows, macOS, and Linux

## Installation

### 1. Clone the Repository
```bash
git clone https://github.com/MaxNoragami/GraphixLang
cd GraphixLang
```

### 2. Build the C# Solution
```bash
dotnet build GraphixLang.sln
```


## Running GraphixLang

### Using the Presentation Layer
The easiest way to run GraphixLang is using the provided console application:

```bash
cd GraphixLang.Presentation
dotnet run
```

This will:
1. Process all `.pixil` files in the `TestInputs` directory
2. Show tokenization, parsing, and execution results

You can also specify a specific pixil file to process using command-line arguments:

```bash
dotnet run path/to/your/program.pixil
```

**Important**: 
- The application processes files with the `.pixil` extension
- When specifying a pixil file via command line, all relative file paths in your program will be relative to the location of that pixil file
- Example files are provided with `.1pixil` extensions to prevent them from running by default (since they use placeholder paths)

### Managing Test Files
- **To test existing examples**: Rename files from `.1pixil` to `.pixil` (e.g., `filters.1pixil` → `filters.pixil`)
- **To skip certain tests**: Rename files from `.pixil` to `.1pixil` 
- **To add your own tests**: Create new `.pixil` files in the `TestInputs` directory

**Before running examples**, update the file paths in the `.pixil` files to match your system:
- Change `/Users/mcittkmims/Documents/images/` to your actual image directory
- Change `/Users/mcittkmims/Documents/processed_photos/` to your desired output directory
- Ensure the input directories contain actual image files (PNG, JPG, JPEG, WEBP, TIFF, BMP)

### Running Individual Programs
1. Create a `.pixil` file with your GraphixLang code
2. Place it in the `TestInputs` directory
3. Update any file paths to match your system
4. Run the application

## Example Programs

The `TestInputs` directory contains several example programs (with `.1pixil` extensions). Rename them to `.pixil` and update the file paths to test them on your system.

### Basic Image Processing
```graphixlang
{
    IMG $photo = "/path/to/image.jpg";
    SET $photo SEPIA;
    RESIZE $photo (800, 600);
    EXPORT $photo TO "/path/to/output/" OGKEEP;
}
```

### Batch Processing with Conditions
```graphixlang
{
    BATCH #photos = "/path/to/images/";
    FOREACH IMG $img IN #photos EXPORT TO "/path/to/processed/" {
        INT $width = METADATA $img FWIDTH;
        
        IF $width > 1000 {
            SET $img SEPIA;
            RESIZE $img (800, 600);
        }
        ELSE {
            SET $img BW;
            CROP $img (400, 300);
        }
        
        WATERMARK $img "© 2025" ~HFF0000~;
    }
}
```

### Advanced Metadata and Effects
```graphixlang
{
    BATCH #rawImages = "/path/to/raw/";
    FOREACH IMG $img IN #rawImages EXPORT TO "/path/to/final/" {
        // Apply creative effects
        SET $img BRIGHTNESS 120;
        SET $img CONTRAST 80;
        SET $img HUE 45;
        
        // Metadata management
        STRIP METADATA $img GPS, CAMERA;
        ADD METADATA $img TITLE "Processed Image";
        ADD METADATA $img COPYRIGHT "© 2025 Your Name";
        
        // Smart renaming
        RENAME $img "IMG_" + COUNTER + "_" + METADATA $img FNAME;
        
        // Web optimization
        WEBOPTIMIZE $img LOSSY 85;
    }
}
```

## Language Syntax

### Variable Types
- `IMG`: Image variables
- `BATCH`: Batch/directory variables  
- `INT`: Integer values
- `DOUBLE`: Floating-point numbers
- `STRING`: Text strings
- `BOOL`: Boolean values

### Key Operations
- `SET $img FILTER`: Apply filters (SEPIA, BW, NEGATIVE, SHARPEN)
- `RESIZE $img (width, height)`: Resize with optional aspect ratio preservation
- `CROP $img (width, height)`: Crop from center
- `ROTATE $img LEFT/RIGHT`: 90-degree rotations
- `WATERMARK $img "text" ~HexColor~`: Add text watermarks
- `COMPRESS $img quality`: JPEG compression (0-100)
- `CONVERT $img TO format`: Format conversion

### File Paths
- Use forward slashes `/` for cross-platform compatibility
- Enclose paths in double quotes: `"/path/to/images/"`
- Supports both absolute and relative paths

## Project Structure

```
GraphixLang/
├── GraphixLang.Lexer/          # Tokenization and lexical analysis
├── GraphixLang.Parser/         # Syntax parsing and AST generation  
├── GraphixLang.Interpreter/    # Execution engine
├── GraphixLang.Presentation/   # Console application and examples
└── TestImgs/                   # Example images to process
```

## Troubleshooting

### File Extension Issues
- **No files being processed**: Ensure your test files have `.pixil` extension, not `.1pixil`
- **Examples not working**: Update file paths in the `.pixil` files to match your actual directories
- **Missing input images**: Ensure your specified input directories contain valid image files

### File Permission Errors
Ensure the application has read/write permissions for:
- Input image directories
- Output/export directories
- Temporary file locations

### Path Configuration
- Ensure input directories exist and contain image files before running
- Create output directories or ensure they're writable
### Path Configuration
- Ensure input directories exist and contain image files before running
- Create output directories or ensure they're writable
