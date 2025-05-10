# GraphixLang Interpreter

This Python-based interpreter processes the Abstract Syntax Tree (AST) produced by the GraphixLang parser and executes the image processing operations.

## Requirements

- Python 3.7 or higher
- Pillow (PIL) library for image processing
- NumPy for advanced operations (optional, but recommended)

## Installation

1. Ensure Python 3.7+ is installed on your system
2. Install dependencies:
   ```
   pip install -r requirements.txt
   ```

## Usage

### From Command Line

Execute an AST directly:

```bash
python interpreter.py path/to/ast.json
```

Using the bridge for C# integration:

```bash
python bridge.py path/to/ast.json [path/to/output.json]
```

### From C# Code

This interpreter is designed to be called from C# code. Typically, the C# component will:
1. Parse the GraphixLang code into an AST
2. Serialize the AST to JSON
3. Call the Python interpreter via process or IPC
4. Process the results

## Supported Operations

The interpreter supports all operations defined in the GraphixLang specification:

- Image loading and declaration
- Format conversion
- Resize operations with aspect ratio control
- Image rotation (left/right)
- Cropping
- Filters (Sharpen, Black & White, Negative, Sepia)
- Watermarking (text and image)
- Brightness/contrast adjustments
- Opacity control
- Noise generation
- Blur effects
- Pixelation
- Hue adjustment
- Compression
- Metadata operations
- Image export

## Error Handling

The interpreter provides detailed error messages for common issues:
- File not found
- Invalid operations
- Type mismatches
- Out-of-range values

## Extending the Interpreter

To add new operations:
1. Add a new visit_* method in the GraphixInterpreter class
2. Implement the image processing logic using PIL
3. Update the TokenType enum if needed
