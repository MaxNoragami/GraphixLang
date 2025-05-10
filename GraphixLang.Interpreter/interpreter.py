from PIL import Image, ImageEnhance, ImageFilter, ImageOps, ImageDraw, ImageFont
import os
import json
from enum import Enum
import sys
import glob
import io

# Import piexif - a library for handling EXIF data
try:
    import piexif
    PIEXIF_AVAILABLE = True
except ImportError:
    print("Warning: piexif library not found. Metadata operations will have limited functionality.")
    PIEXIF_AVAILABLE = False

class TokenType(Enum):
    # Token types from C# implementation
    TYPE_INT = "TYPE_INT"
    TYPE_DBL = "TYPE_DBL"
    TYPE_STR = "TYPE_STR"
    TYPE_BOOL = "TYPE_BOOL"
    TYPE_IMG = "TYPE_IMG"
    TYPE_BATCH = "TYPE_BATCH"
    TYPE_PXLS = "TYPE_PXLS"
    
    # Image formats
    PNG = "PNG"
    JPG = "JPG"
    JPEG = "JPEG"
    WEBP = "WEBP"
    TIFF = "TIFF"
    BMP = "BMP"
    
    # Filter types
    SHARPEN = "SHARPEN"
    NEGATIVE = "NEGATIVE"
    BW = "BW"
    SEPIA = "SEPIA"
    
    # Rotation directions
    RIGHT = "RIGHT"
    LEFT = "LEFT"
    
    # Metadata types
    FWIDTH = "FWIDTH"
    FHEIGHT = "FHEIGHT"
    FNAME = "FNAME"
    FSIZE = "FSIZE"
    
    # Orientation types
    LANDSCAPE = "LANDSCAPE"
    PORTRAIT = "PORTRAIT"

class GraphixInterpreter:
    def __init__(self):
        self.environment = {}  # Variable storage
        self.image_cache = {}  # Store loaded images
        self.operation_count = 0  # Track number of successful operations
        
    def interpret(self, ast_json):
        """
        Main interpretation method - processes AST from JSON
        """
        ast = json.loads(ast_json) if isinstance(ast_json, str) else ast_json
        
        # Debug: Print AST structure
        print("Received AST structure:")
        print(json.dumps(ast, indent=2)[:500] + "..." if len(json.dumps(ast)) > 500 else json.dumps(ast, indent=2))
        
        # Handle different AST structures more flexibly
        if isinstance(ast, dict):
            # Case 1: Standard ProgramNode
            if "Type" in ast and ast["Type"] == "ProgramNode":
                return self.visit_program(ast)
            # Case 2: Object with Blocks
            elif "Blocks" in ast:
                return self.visit_program_blocks(ast)
            # Case 3: Check if it has a list of statements directly
            elif "Statements" in ast:
                return self.visit_program(ast)
            # Case 4: Process a single node
            elif "Type" in ast:
                return [self.visit(ast)]
        # Case 5: Process a list of nodes
        elif isinstance(ast, list):
            results = []
            for node in ast:
                result = self.visit(node) if isinstance(node, dict) else None
                if result is not None:
                    results.append(result)
            return results
            
        # If we've reached here, print the AST structure for debugging
        print(f"Unrecognized AST structure: {type(ast)}")
        if isinstance(ast, dict):
            print(f"Keys: {list(ast.keys())}")
        
        # Still try to process it if it's a dictionary
        if isinstance(ast, dict):
            try:
                return [self.visit(ast)]
            except Exception as e:
                print(f"Failed to process as single node: {str(e)}")
        
        raise ValueError("Invalid AST structure: could not determine how to process")
    
    def visit_program(self, node):
        """Process the Program node which contains a list of statements"""
        results = []
        for statement in node.get("Statements", []):
            result = self.visit(statement)
            if result is not None:
                results.append(result)
        return results
    
    def visit_program_blocks(self, node):
        """Process program with blocks structure"""
        results = []
        # Process blocks if present
        for block in node.get("Blocks", []):
            if isinstance(block, dict) and "Statements" in block:
                for statement in block["Statements"]:
                    result = self.visit(statement)
                    if result is not None:
                        results.append(result)
        return results
    
    def visit(self, node):
        """Visit a node in the AST and dispatch to the appropriate handler"""
        # Check for both lowercase "type" (new format) and uppercase "Type" (old format)
        node_type = node.get("type", node.get("Type", ""))
        
        # Dispatch to the appropriate visit method
        method_name = f"visit_{node_type.lower()}"
        method = getattr(self, method_name, self.visit_unknown)
        
        result = method(node)
        if method != self.visit_unknown and node_type.lower() != "binaryexpression":
            self.operation_count += 1
        return result
    
    def visit_unknown(self, node):
        """Handle unknown node types"""
        print(f"Unknown node type: {node.get('type', node.get('Type', 'undefined'))}")
        return None

    def visit_literal(self, node):
        """Handle literal values"""
        value_type = node.get("valueType", node.get("Type", ""))
        value = node.get("Value")
        
        # Convert the value based on its type
        if value_type == "INT_VALUE":
            return int(value)
        elif value_type == "DBL_VALUE":
            return float(value)
        elif value_type == "STR_VALUE":
            return str(value)
        elif value_type == "BOOL_VALUE":
            return value.lower() == "true"
        else:
            return value

    def visit_batchdeclaration(self, node):
        """Handle batch declarations"""
        identifier = node.get("Identifier")
        expression = node.get("Expression")
        
        # Handle the expression based on its type
        if expression.get("type", "").lower() == "binaryexpression" and expression.get("Operator") == "PLUS":
            # For binary expressions with + in batch context, use visit_batchexpression
            # Tag the expression so we know it's part of a batch declaration
            expression["parent_type"] = "batchexpression"
            paths = self.visit_batchexpression(expression)
        else:
            # For simple literals or references
            path = self.visit(expression)
            paths = path if isinstance(path, list) else [path]
        
        # Normalize paths for cross-platform compatibility
        normalized_paths = [os.path.normpath(p) for p in paths]
        
        # Store in environment - we store a list of paths
        self.environment[identifier] = {
            "type": TokenType.TYPE_BATCH,
            "value": normalized_paths
        }
        
        print(f"Declared batch '{identifier}' with paths: {normalized_paths}")
        return identifier
    
    def visit_batchexpression(self, node):
        """Handle batch expressions (combining multiple locations with +)"""
        # Get the left and right operands directly from the node
        left = node.get("Left")
        right = node.get("Right")
        
        # For batch expressions, we want to keep the paths as separate items
        paths = []
        
        # Process left operand - could be a string path or another batch expression
        if left.get("type", "").lower() == "batchexpression":
            # If it's another batch expression, recurse
            left_paths = self.visit_batchexpression(left)
            paths.extend(left_paths)
        else:
            # Otherwise, evaluate and add to our paths list
            left_val = self.visit(left)
            if isinstance(left_val, list):
                paths.extend(left_val)
            else:
                paths.append(left_val)
        
        # Process right operand - similar logic
        if right.get("type", "").lower() == "batchexpression":
            right_paths = self.visit_batchexpression(right)
            paths.extend(right_paths)
        else:
            right_val = self.visit(right)
            if isinstance(right_val, list):
                paths.extend(right_val)
            else:
                paths.append(right_val)
        
        return paths

    def visit_foreach(self, node):
        """Handle ForEach node that processes operations on a batch of images"""
        var_id = node.get("VarIdentifier")
        batch_id = node.get("BatchIdentifier")
        export_path = node.get("ExportPath")
        body = node.get("Body")
        # Default to OGKEEP behavior since FOREACH doesn't specify keep/delete
        keep_original = node.get("KeepOriginal", True)  
        
        if batch_id not in self.environment:
            raise ValueError(f"Unknown batch identifier: {batch_id}")
            
        batch_paths = self.environment[batch_id]["value"]
        if not isinstance(batch_paths, list):
            batch_paths = [batch_paths]  # Convert to list if it's a single path
        
        # Normalize export path for cross-platform compatibility
        export_path = os.path.normpath(export_path)
        
        # Ensure export directory exists
        os.makedirs(export_path, exist_ok=True)
        
        # Find all images across all batch paths
        image_files = []
        for batch_path in batch_paths:
            if os.path.isdir(batch_path):
                # Find all image files in the directory
                for ext in ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.tiff", "*.bmp"]:
                    image_files.extend(glob.glob(os.path.join(batch_path, ext)))
        
        print(f"Processing {len(image_files)} images from batch {batch_id}")
        
        results = []
        # Process each image in the batch
        for img_path in image_files:
            # Load image and store in environment
            try:
                img = Image.open(img_path)
                self.environment[var_id] = {
                    "type": TokenType.TYPE_IMG,
                    "value": img,
                    "path": img_path,
                    "filename": os.path.basename(img_path)
                }
                
                # Process the body of the ForEach
                if body and "Statements" in body:
                    for statement in body["Statements"]:
                        result = self.visit(statement)
                        if result is not None:
                            results.append(result)
                
                # After processing all operations, export the image using our export functionality
                # Create an export node to use with visit_export
                export_node = {
                    "ImageIdentifier": var_id,
                    "DestinationPath": export_path,
                    "KeepOriginal": keep_original
                }
                
                # Use the export handler to ensure metadata is preserved
                export_result = self.visit_export(export_node)
                if export_result:
                    results.append(export_result)
                
            except Exception as e:
                print(f"Error processing {img_path}: {str(e)}")
        
        return results

    def visit_variabledeclaration(self, node):
        """Handle variable declarations"""
        var_type = node.get("Type")
        identifier = node.get("Identifier")
        initializer = node.get("Initializer")
        
        # Evaluate the initializer if present
        value = self.visit(initializer) if initializer else None
        
        # Store in environment based on type
        self.environment[identifier] = {
            "type": var_type,
            "value": value
        }
        
        return identifier

    def visit_assignment(self, node):
        """Handle variable assignments"""
        identifier = node.get("Identifier")
        value_expr = node.get("Value")
        
        # Evaluate the value expression
        value = self.visit(value_expr)
        
        # Update the variable in the environment
        if identifier in self.environment:
            self.environment[identifier]["value"] = value
        else:
            # If variable doesn't exist, create it (type inference)
            self.environment[identifier] = {
                "type": "INFERRED",
                "value": value
            }
        
        return value

    def visit_metadata(self, node):
        """Handle metadata expressions"""
        img_id = node.get("ImageIdentifier")
        metadata_type = node.get("MetadataType")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        
        # Return the requested metadata
        if metadata_type == "FWIDTH":
            return img_data["value"].width
        elif metadata_type == "FHEIGHT":
            return img_data["value"].height
        elif metadata_type == "FNAME":
            return img_data.get("filename", "")
        elif metadata_type == "FSIZE":
            img_path = img_data.get("path", "")
            return os.path.getsize(img_path) if img_path and os.path.exists(img_path) else 0
        else:
            return None

    def visit_if(self, node):
        """Handle if statements"""
        condition = node.get("Condition")
        then_branch = node.get("ThenBranch")
        elif_branches = node.get("ElifBranches", [])
        else_branch = node.get("ElseBranch")
        
        # Evaluate the condition
        if self.evaluate_condition(condition):
            # Execute the 'then' branch
            return self.visit_block(then_branch)
            
        # Check elif branches
        for elif_branch in elif_branches:
            if self.evaluate_condition(elif_branch.get("Condition")):
                return self.visit_block(elif_branch.get("Body"))
                
        # If no conditions matched, execute else branch if present
        if else_branch:
            return self.visit_block(else_branch)
            
        return None

    def visit_block(self, node):
        """Process a block of statements"""
        if not node or "Statements" not in node:
            return None
            
        results = []
        for statement in node["Statements"]:
            result = self.visit(statement)
            if result is not None:
                results.append(result)
                
        return results if results else None

    def evaluate_condition(self, condition):
        """Evaluate a conditional expression"""
        if not condition:
            return False
            
        # Check if it's a binary expression
        if condition.get("type", "").lower() == "binaryexpression":
            return self.visit_binaryexpression(condition)
        
        # Handle direct values (should be boolean)
        return bool(self.visit(condition))

    def visit_setfilter(self, node):
        """Handle filter application to images"""
        img_id = node.get("ImageIdentifier")
        filter_type = node.get("FilterType")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Apply the appropriate filter
        if filter_type == "SEPIA":
            # Sepia filter implementation
            sepia_img = img.convert("RGB")
            pixels = sepia_img.load()
            width, height = sepia_img.size
            
            for x in range(width):  # Fixed: Use range(width) instead of width
                for y in range(height):  # Fixed: Use range(height) instead of height
                    r, g, b = pixels[x, y]
                    # Sepia formula
                    tr = int(0.393 * r + 0.769 * g + 0.189 * b)
                    tg = int(0.349 * r + 0.686 * g + 0.168 * b)
                    tb = int(0.272 * r + 0.534 * g + 0.131 * b)
                    pixels[x, y] = (min(tr, 255), min(tg, 255), min(tb, 255))
                    
            img_data["value"] = sepia_img
            print(f"Applied SEPIA filter to {img_id}")
            
        elif filter_type == "BW":
            # Black and white filter
            bw_img = img.convert("L").convert("RGB")
            img_data["value"] = bw_img
            print(f"Applied BW filter to {img_id}")
            
        elif filter_type == "NEGATIVE":
            # Negative filter
            negative_img = ImageOps.invert(img.convert("RGB"))
            img_data["value"] = negative_img
            print(f"Applied NEGATIVE filter to {img_id}")
            
        elif filter_type == "SHARPEN":
            # Sharpen filter
            sharpen_img = img.filter(ImageFilter.SHARPEN)
            img_data["value"] = sharpen_img
            print(f"Applied SHARPEN filter to {img_id}")
            
        return img_id

    def visit_variablereference(self, node):
        """Handle variable references - retrieve values from the environment"""
        identifier = node.get("Identifier")
        
        if identifier not in self.environment:
            print(f"Warning: Variable {identifier} not found in environment")
            return None
        
        var_data = self.environment[identifier]
        return var_data["value"]

    def visit_binaryexpression(self, node):
        """Handle binary expressions (math and comparisons)"""
        left_expr = node.get("Left")
        right_expr = node.get("Right")
        operator = node.get("Operator")
        
        # Special case for batch expressions - handled separately
        if operator == "PLUS" and node.get("parent_type") == "batchexpression":
            # This should never be called directly for batch expressions
            # We handle batch expressions through visit_batchexpression
            raise ValueError("Binary expression within batch context should be handled by visit_batchexpression")
        
        left_val = self.visit(left_expr)
        right_val = self.visit(right_expr)
        
        # Handle different operators
        if operator == "PLUS":
            return left_val + right_val
        elif operator == "MINUS":
            return left_val - right_val
        elif operator == "MULTIPLY":
            return left_val * right_val
        elif operator == "DIVIDE":
            return left_val / right_val
        elif operator == "EQUAL":
            return left_val == right_val
        elif operator == "NOT_EQUAL":
            return left_val != right_val
        elif operator == "GREATER":
            return left_val > right_val
        elif operator == "GREATER_EQUAL":
            return left_val >= right_val
        elif operator == "SMALLER":
            return left_val < right_val
        elif operator == "SMALLER_EQUAL":
            return left_val <= right_val
        else:
            raise ValueError(f"Unknown operator: {operator}")

    def visit_imagedeclaration(self, node):
        """Handle image declaration/loading"""
        identifier = node.get("Identifier")
        path = node.get("Path")
        
        # Normalize path for cross-platform compatibility
        path = os.path.normpath(path)
        
        # Check if the file exists and is readable
        if not os.path.isfile(path):
            raise FileNotFoundError(f"Image file not found: {path}")
            
        try:
            # Load the image using PIL
            img = Image.open(path)
            
            # Store in environment
            self.environment[identifier] = {
                "type": TokenType.TYPE_IMG,
                "value": img,
                "path": path,
                "filename": os.path.basename(path)
            }
            
            print(f"Loaded image '{identifier}' from {path}")
            return identifier
        except Exception as e:
            raise RuntimeError(f"Error loading image '{path}': {str(e)}")

    def visit_brightness(self, node):
        """Handle brightness adjustment"""
        img_id = node.get("ImageIdentifier")
        value = node.get("Value")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # PIL brightness factor is between 0.0 and 2.0 where 1.0 is original
        # Convert from 0-200 scale to 0.0-2.0 scale
        factor = value / 100.0
        
        # Apply brightness adjustment
        enhancer = ImageEnhance.Brightness(img)
        enhanced_img = enhancer.enhance(factor)
        
        img_data["value"] = enhanced_img
        print(f"Adjusted brightness of {img_id} to {value}")
        
        return img_id

    def visit_contrast(self, node):
        """Handle contrast adjustment"""
        img_id = node.get("ImageIdentifier")
        value = node.get("Value")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # PIL contrast factor is between 0.0 and 2.0 where 1.0 is original
        # Convert from 0-200 scale to 0.0-2.0 scale
        factor = value / 100.0
        
        # Apply contrast adjustment
        enhancer = ImageEnhance.Contrast(img)
        enhanced_img = enhancer.enhance(factor)
        
        img_data["value"] = enhanced_img
        print(f"Adjusted contrast of {img_id} to {value}")
        
        return img_id

    def visit_opacity(self, node):
        """Handle opacity adjustment"""
        img_id = node.get("ImageIdentifier")
        value = node.get("Value")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Ensure image has alpha channel
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
            
        # Create a new image with adjusted alpha
        pixels = img.load()
        width, height = img.size
        
        # Apply opacity
        for y in range(height):
            for x in range(width):
                r, g, b, a = pixels[x, y]
                # Adjust alpha (opacity)
                new_alpha = int(a * (value / 100.0))
                pixels[x, y] = (r, g, b, new_alpha)
                
        img_data["value"] = img
        print(f"Adjusted opacity of {img_id} to {value}")
        
        return img_id

    def visit_noise(self, node):
        """Handle adding noise to an image"""
        img_id = node.get("ImageIdentifier")
        value = node.get("Value")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Convert to RGB mode if not already
        if img.mode != 'RGB':
            img = img.convert('RGB')
            
        # Create a new image with noise
        pixels = img.load()
        width, height = img.size
        
        import random
        # Convert to integer for random.randint which requires integers
        noise_level = int(value * 2.55)  # Convert 0-100 to 0-255 as integer
        
        # Apply noise
        for y in range(height):
            for x in range(width):
                r, g, b = pixels[x, y]
                # Add random noise
                noise = random.randint(-noise_level, noise_level)
                pixels[x, y] = (
                    max(0, min(255, r + noise)),
                    max(0, min(255, g + noise)),
                    max(0, min(255, b + noise))
                )
                
        img_data["value"] = img
        print(f"Added noise to {img_id} with level {value}")
        
        return img_id

    def visit_blur(self, node):
        """Handle blurring an image"""
        img_id = node.get("ImageIdentifier")
        value = node.get("Value")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Apply Gaussian blur (radius is proportional to the blur amount)
        radius = value / 10.0  # Convert 0-100 to 0-10 radius
        blurred_img = img.filter(ImageFilter.GaussianBlur(radius=radius))
        
        img_data["value"] = blurred_img
        print(f"Applied blur to {img_id} with radius {radius}")
        
        return img_id

    def visit_pixelate(self, node):
        """Handle pixelate effect"""
        img_id = node.get("ImageIdentifier")
        value = node.get("Value")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Calculate pixelation factor
        width, height = img.size
        pixel_size = max(1, int(min(width, height) / (100 - value + 1)))
        
        # Pixelate by downscaling and upscaling
        small_img = img.resize((width // pixel_size, height // pixel_size), Image.NEAREST)
        pixelated_img = small_img.resize((width, height), Image.NEAREST)
        
        img_data["value"] = pixelated_img
        print(f"Applied pixelate effect to {img_id} with size {pixel_size}")
        
        return img_id

    def visit_quantize(self, node):
        """Handle color quantization (reducing colors)"""
        img_id = node.get("ImageIdentifier")
        colors = node.get("Colors")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Convert to RGB mode if not already
        if img.mode not in ['RGB', 'RGBA']:
            img = img.convert('RGBA' if img.mode == 'RGBA' else 'RGB')
        
        # Quantize colors
        quantized_img = img.quantize(colors=colors)
        
        # Convert back to original mode
        if img.mode == 'RGBA':
            quantized_img = quantized_img.convert('RGBA')
        else:
            quantized_img = quantized_img.convert('RGB')
            
        img_data["value"] = quantized_img
        print(f"Reduced {img_id} to {colors} colors")
        
        return img_id

    def visit_export(self, node):
        """Handle export operation"""
        img_id = node.get("ImageIdentifier")
        destination_path = node.get("DestinationPath")
        keep_original = node.get("KeepOriginal", True)
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        original_path = img_data.get("path")
        
        # Get the renamed filename (important: this might have been changed by rename operations)
        filename = img_data.get("filename", os.path.basename(original_path))
        
        # Normalize the destination path for cross-platform compatibility
        destination_path = os.path.normpath(destination_path)
        
        # Create destination directory if it doesn't exist
        if os.path.isdir(destination_path) or destination_path.endswith(os.sep):
            # If destination is a directory, ensure it exists
            os.makedirs(destination_path, exist_ok=True)
            # Append the renamed filename to the directory path
            full_destination_path = os.path.join(destination_path, filename)
        else:
            # Ensure the parent directory exists
            parent_dir = os.path.dirname(destination_path)
            if parent_dir:  # Check if there's a parent directory
                os.makedirs(parent_dir, exist_ok=True)
            full_destination_path = destination_path
        
        # Export the image
        try:
            # Determine format from extension or use original format
            format_from_path = os.path.splitext(full_destination_path)[1].upper().lstrip('.')
            
            # Normalize the format name (PIL uses 'JPEG' for .jpg files)
            if format_from_path == 'JPG':
                format_from_path = 'JPEG'
                
            img_format = format_from_path if format_from_path else (img.format or "PNG")
            
            # Save the image with metadata if available
            save_kwargs = {'format': img_format}
            
            # Include EXIF metadata if it exists
            if PIEXIF_AVAILABLE and 'exif' in img.info and img.info['exif']:
                save_kwargs['exif'] = img.info['exif']
            
            # Save the image with all appropriate parameters
            img.save(full_destination_path, **save_kwargs)
            print(f"Exported {img_id} to {full_destination_path}")
            
            # Delete original if specified and exists
            if not keep_original and original_path and os.path.exists(original_path):
                os.remove(original_path)
                print(f"Deleted original file: {original_path}")
                
            return full_destination_path
        except Exception as e:
            raise RuntimeError(f"Error exporting image to {full_destination_path}: {str(e)}")

    def visit_rotate(self, node):
        """Handle image rotation"""
        img_id = node.get("ImageIdentifier")
        direction = node.get("Direction")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Apply rotation based on direction
        if direction == "LEFT":
            # Rotate 90 degrees counter-clockwise
            rotated_img = img.transpose(Image.ROTATE_90)
            img_data["value"] = rotated_img
            print(f"Rotated {img_id} LEFT (90° counter-clockwise)")
        elif direction == "RIGHT":
            # Rotate 90 degrees clockwise
            rotated_img = img.transpose(Image.ROTATE_270)
            img_data["value"] = rotated_img
            print(f"Rotated {img_id} RIGHT (90° clockwise)")
        else:
            raise ValueError(f"Unknown rotation direction: {direction}")
        
        return img_id

    def visit_crop(self, node):
        """Handle image cropping"""
        img_id = node.get("ImageIdentifier")
        width_expr = node.get("Width")
        height_expr = node.get("Height")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Evaluate width and height expressions
        width = self.visit(width_expr)
        height = self.visit(height_expr)
        
        # Ensure width and height are integers
        width = int(width)
        height = int(height)
        
        # Get current image dimensions
        current_width, current_height = img.size
        
        # Calculate crop box (centered crop)
        left = (current_width - width) / 2 if width < current_width else 0
        top = (current_height - height) / 2 if height < current_height else 0
        right = left + min(width, current_width)
        bottom = top + min(height, current_height)
        
        # Apply crop
        cropped_img = img.crop((int(left), int(top), int(right), int(bottom)))
        img_data["value"] = cropped_img
        print(f"Cropped {img_id} to {width}x{height}")
        
        return img_id

    def visit_compress(self, node):
        """Handle image compression"""
        img_id = node.get("ImageIdentifier")
        quality = node.get("Quality")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Create a temporary file for compression - use a cross-platform approach
        import tempfile
        import shutil
        
        # Create a temp directory that will be automatically cleaned up
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_file_path = os.path.join(temp_dir, 'temp_compressed.jpg')
            
            # Save with compression
            img.save(temp_file_path, format='JPEG', quality=quality)
            
            # Close the original file handle before opening the new one
            # to avoid file locking issues on Windows
            compressed_img = Image.open(temp_file_path)
            compressed_img.load()  # Load image data into memory
            
            # Convert back to original mode if needed
            if img.mode != compressed_img.mode:
                compressed_img = compressed_img.convert(img.mode)
            
            img_data["value"] = compressed_img
            print(f"Compressed {img_id} with quality {quality}")
        
        return img_id

    def visit_orientation(self, node):
        """Handle image orientation setting"""
        img_id = node.get("ImageIdentifier")
        orientation_type = node.get("OrientationType")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Get current dimensions
        width, height = img.size
        
        # Check if we need to rotate the image to match the desired orientation
        if orientation_type == "LANDSCAPE" and height > width:
            # Rotate to landscape if currently portrait
            rotated_img = img.transpose(Image.ROTATE_90)
            img_data["value"] = rotated_img
            print(f"Rotated {img_id} to LANDSCAPE orientation")
        elif orientation_type == "PORTRAIT" and width > height:
            # Rotate to portrait if currently landscape
            rotated_img = img.transpose(Image.ROTATE_270)
            img_data["value"] = rotated_img
            print(f"Rotated {img_id} to PORTRAIT orientation")
        else:
            print(f"Image {img_id} already in {orientation_type} orientation")
        
        return img_id
    
    def visit_hue(self, node):
        """Handle hue adjustment for an image"""
        img_id = node.get("ImageIdentifier")
        hue_value = node.get("HueValue")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Convert to HSV color space to adjust hue
        from colorsys import rgb_to_hsv, hsv_to_rgb
        
        # Ensure image is in RGB mode
        if img.mode != 'RGB':
            img = img.convert('RGB')
            
        # Create a new image for the hue-shifted result
        width, height = img.size
        result = Image.new('RGB', (width, height))
        
        # Get pixel data
        pixels = img.load()
        result_pixels = result.load()
        
        # Calculate the hue shift value (map 0-360 to 0-1)
        hue_shift = (hue_value % 360) / 360.0
        
        # Process each pixel
        for y in range(height):
            for x in range(width):
                r, g, b = pixels[x, y]
                
                # Convert RGB to HSV
                h, s, v = rgb_to_hsv(r/255.0, g/255.0, b/255.0)
                
                # Apply hue adjustment (add hue_shift and wrap around)
                h = (h + hue_shift) % 1.0
                
                # Convert back to RGB
                r, g, b = hsv_to_rgb(h, s, v)
                
                # Scale back to 0-255 range
                r = int(r * 255)
                g = int(g * 255)
                b = int(b * 255)
                
                result_pixels[x, y] = (r, g, b)
                
        img_data["value"] = result
        print(f"Applied HUE adjustment to {img_id} with value {hue_value}")
        
        return img_id
    
    def visit_stripmetadata(self, node):
        """Handle stripping metadata from an image"""
        img_id = node.get("ImageIdentifier")
        strip_all = node.get("StripAll", False)
        metadata_types = node.get("MetadataTypes", [])
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        if PIEXIF_AVAILABLE:
            # More robust metadata handling with piexif
            try:
                # Extract existing EXIF data if present
                exif_dict = None
                if 'exif' in img.info and img.info['exif']:
                    exif_dict = piexif.load(img.info['exif'])
                
                if strip_all or exif_dict is None:
                    # Clear all metadata
                    img_bytes = io.BytesIO()
                    img.save(img_bytes, format=img.format or 'JPEG')
                    new_img = Image.open(img_bytes)
                    new_img.load()  # Force loading of image data
                    img_data["value"] = new_img
                else:
                    # Selectively remove metadata types
                    modified = False
                    for metadata_type in metadata_types:
                        # Map GraphixLang metadata types to EXIF IFD sections
                        if metadata_type == "GPS" and "GPS" in exif_dict:
                            del exif_dict["GPS"]
                            modified = True
                        elif metadata_type == "CAMERA" and "0th" in exif_dict:
                            # Remove camera-related tags
                            for tag in [piexif.ImageIFD.Make, piexif.ImageIFD.Model]:
                                if tag in exif_dict["0th"]:
                                    del exif_dict["0th"][tag]
                                    modified = True
                        # Handle other metadata types similarly
                    
                    if modified:
                        # Create new EXIF bytes and update the image
                        new_exif = piexif.dump(exif_dict)
                        img.info['exif'] = new_exif
            except Exception as e:
                print(f"Warning: Error while stripping metadata: {str(e)}")
        else:
            # Fallback method if piexif is not available
            # Create a new image without metadata
            img_bytes = io.BytesIO()
            img.save(img_bytes, format=img.format or 'JPEG')
            img_bytes.seek(0)
            stripped_img = Image.open(img_bytes)
            stripped_img.load()  # Force loading of image data
            img_data["value"] = stripped_img
        
        # Store the metadata state in our environment
        if "metadata" not in img_data:
            img_data["metadata"] = {}
        
        if strip_all:
            img_data["metadata"]["stripped_all"] = True
            print(f"Stripped ALL metadata from {img_id}")
        else:
            for metadata_type in metadata_types:
                img_data["metadata"][f"stripped_{metadata_type}"] = True
            print(f"Stripped specific metadata from {img_id}: {', '.join(str(m) for m in metadata_types)}")
        
        return img_id
    
    def visit_addmetadata(self, node):
        """Handle adding metadata to an image"""
        img_id = node.get("ImageIdentifier")
        metadata_type = node.get("MetadataType")
        value = node.get("Value")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        if PIEXIF_AVAILABLE:
            # More robust metadata handling with piexif
            try:
                # Extract existing EXIF data or create new
                exif_dict = {"0th": {}, "Exif": {}, "GPS": {}, "1st": {}, "thumbnail": None}
                if 'exif' in img.info and img.info['exif']:
                    try:
                        exif_dict = piexif.load(img.info['exif'])
                    except:
                        pass
                
                # Add metadata based on type
                if metadata_type == "TAGS":
                    # Store as keywords in IPTC
                    if "0th" not in exif_dict:
                        exif_dict["0th"] = {}
                    exif_dict["0th"][piexif.ImageIFD.XPKeywords] = value.encode('utf-16le')
                elif metadata_type == "TITLE":
                    if "0th" not in exif_dict:
                        exif_dict["0th"] = {}
                    exif_dict["0th"][piexif.ImageIFD.XPTitle] = value.encode('utf-16le')
                elif metadata_type == "COPYRIGHT":
                    if "0th" not in exif_dict:
                        exif_dict["0th"] = {}
                    exif_dict["0th"][piexif.ImageIFD.Copyright] = value.encode('utf-8')
                elif metadata_type == "DESCRIPTION":
                    if "0th" not in exif_dict:
                        exif_dict["0th"] = {}
                    exif_dict["0th"][piexif.ImageIFD.XPComment] = value.encode('utf-16le')
                
                # Create new EXIF bytes and update the image
                new_exif = piexif.dump(exif_dict)
                img.info['exif'] = new_exif
                
                # Need to save and reload to ensure metadata is applied
                img_bytes = io.BytesIO()
                img.save(img_bytes, format=img.format or 'JPEG', exif=new_exif)
                img_bytes.seek(0)
                updated_img = Image.open(img_bytes)
                updated_img.load()  # Force loading of image data
                img_data["value"] = updated_img
            except Exception as e:
                print(f"Warning: Error while adding metadata: {str(e)}")
        
        # Store metadata in the environment for reference
        if "metadata" not in img_data:
            img_data["metadata"] = {}
        
        # Add the metadata to our internal tracking
        img_data["metadata"][metadata_type] = value
        
        print(f"Added {metadata_type} metadata to {img_id}: {value}")
        return img_id
    
    def visit_rename(self, node):
        """Handle renaming image files"""
        img_id = node.get("ImageIdentifier")
        terms = node.get("Terms", [])
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        old_filename = img_data.get("filename", "")
        base_name, ext = os.path.splitext(old_filename) if old_filename else ("image", ".png")
        
        # Build the new filename from the rename terms
        new_name = ""
        
        # Initialize counter if it doesn't exist
        if not hasattr(self, 'counter_value'):
            self.counter_value = 0
        
        # Process each term in the rename expression
        for i, term in enumerate(terms):
            # Get term type consistently regardless of case
            term_type = None
            if "Type" in term:
                term_type = term["Type"]
            elif "type" in term:
                term_type = term["type"]
                
            # If no type is specified but has StringValue, assume STRING type
            if not term_type and "StringValue" in term:
                term_type = "STRING"
            # If no type is specified but has no other fields, assume COUNTER type
            elif not term_type:
                term_type = "COUNTER"
            
            if term_type == "STRING" or term_type == "RenameTerm" and "StringValue" in term:
                # String literal term
                string_value = term.get("StringValue", "")
                # Remove quotes if present
                string_value = string_value.strip('"')
                new_name += string_value
            
            elif term_type == "COUNTER" or term_type == "RenameTerm" and not "StringValue" in term and not "MetadataValue" in term:
                # Counter term - add current counter value and increment
                new_name += str(self.counter_value)
                self.counter_value += 1
            
            elif term_type == "METADATA" or term_type == "RenameTerm" and "MetadataValue" in term:
                # Metadata term - extract from the image's metadata
                metadata_value = term.get("MetadataValue")
                if metadata_value:
                    # Extract details from the metadata node
                    metadata_id = metadata_value.get("ImageIdentifier")
                    metadata_type = metadata_value.get("MetadataType")
                    
                    if metadata_id and metadata_type:
                        temp_node = {
                            "ImageIdentifier": metadata_id,
                            "MetadataType": metadata_type
                        }
                        value = self.visit_metadata(temp_node)
                        if value is not None:
                            new_name += str(value)
        
        # Ensure we have a valid filename with proper extension
        if not new_name:
            new_name = base_name
        
        # Add extension if it's not already included
        if not new_name.endswith(ext):
            new_name += ext
        
        # Make sure the filename is valid for the filesystem
        new_name = self._sanitize_filename(new_name)
        
        # Store new filename in environment
        img_data["filename"] = new_name
        
        print(f"Renamed {img_id} to: {new_name}")
        return img_id
    
    def _sanitize_filename(self, filename):
        """Ensure filename is valid for most filesystems"""
        # Replace invalid characters with underscores
        invalid_chars = '<>:"/\\|?*'
        for char in invalid_chars:
            filename = filename.replace(char, '_')
        
        # Ensure filename isn't empty
        if not filename or filename.strip() == '':
            return "image.png"
            
        return filename

    def visit_watermark(self, node):
        """Handle text watermarking"""
        img_id = node.get("ImageIdentifier")
        text = node.get("Text")
        color_value = node.get("ColorValue")
        is_hex_color = node.get("IsHexColor", False)
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Parse color value (both hex and RGB formats)
        if is_hex_color:
            # Remove any ~ characters that might surround hex color
            color_str = color_value.strip('~H').strip('~')
            r = int(color_str[0:2], 16)
            g = int(color_str[2:4], 16)
            b = int(color_str[4:6], 16)
            color = (r, g, b)
        else:
            # Parse RGB format (assumed to be comma-separated or 3-digit blocks)
            color_str = color_value.strip('~R').strip('~')
            if ',' in color_str:
                r, g, b = map(int, color_str.split(','))
                color = (r, g, b)
            else:
                r = int(color_str[0:3])
                g = int(color_str[3:6])
                b = int(color_str[6:9])
                color = (r, g, b)
        
        # Create a new image for the watermark text
        width, height = img.size
        txt_img = Image.new('RGBA', img.size, (255, 255, 255, 0))
        
        # Load a default font (or allow custom font path in the future)
        try:
            # Try to use a common system font
            font_size = min(width, height) // 20  # Reasonable size based on the image dimensions
            font = ImageFont.truetype("Arial", font_size)
        except IOError:
            # Fallback to default font
            font = ImageFont.load_default()
        
        # Create draw object
        draw = ImageDraw.Draw(txt_img)
        
        # Calculate text size and position (centered) with compatibility for different Pillow versions
        try:
            # For newer Pillow versions
            bbox = draw.textbbox((0, 0), text, font=font)
            text_width = bbox[2] - bbox[0]
            text_height = bbox[3] - bbox[1]
        except AttributeError:
            try:
                # For older Pillow versions
                text_width, text_height = draw.textsize(text, font=font)
            except AttributeError:
                try:
                    # Alternative approach
                    text_width, text_height = font.getsize(text)
                except AttributeError:
                    # Last resort - estimate based on font size
                    text_width = len(text) * font_size * 0.6
                    text_height = font_size * 1.2
        
        text_x = (width - text_width) // 2
        text_y = (height - text_height) // 2
        
        # Draw the text with some shadow for better visibility
        # Draw shadow first (offset slightly)
        draw.text((text_x+2, text_y+2), text, font=font, fill=(0, 0, 0, 128))
        # Draw main text
        draw.text((text_x, text_y), text, font=font, fill=color + (255,))
        
        # Composite the text onto the original image
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
        watermarked = Image.alpha_composite(img, txt_img)
        
        # Convert back to original mode if needed
        if img_data["value"].mode != 'RGBA':
            watermarked = watermarked.convert(img_data["value"].mode)
            
        img_data["value"] = watermarked
        print(f"Applied text watermark to {img_id}")
        
        return img_id

    def visit_imagewatermark(self, node):
        """Handle image watermarking"""
        img_id = node.get("ImageIdentifier")
        watermark_img_id = node.get("WatermarkImageIdentifier")
        transparency = node.get("Transparency", 128)  # Default to half transparency if not specified
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
        
        if watermark_img_id not in self.environment:
            raise ValueError(f"Unknown watermark image variable: {watermark_img_id}")
            
        img_data = self.environment[img_id]
        watermark_data = self.environment[watermark_img_id]
        
        img = img_data["value"]
        watermark = watermark_data["value"]
        
        # Resize watermark to a reasonable size relative to the main image
        # (typically watermarks shouldn't cover the entire image)
        base_width, base_height = img.size
        wm_width, wm_height = watermark.size
        
        # Calculate new watermark size (max 1/4 of the original image's width)
        new_wm_width = min(base_width // 4, wm_width)
        # Keep aspect ratio
        new_wm_height = int(wm_height * (new_wm_width / wm_width))
        
        # Resize watermark
        watermark = watermark.resize((new_wm_width, new_wm_height), Image.LANCZOS)
        
        # Ensure both images are in RGBA mode
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
        
        if watermark.mode != 'RGBA':
            watermark = watermark.convert('RGBA')
        
        # Apply transparency to watermark
        watermark_with_transparency = Image.new('RGBA', watermark.size)
        for x in range(watermark.width):
            for y in range(watermark.height):
                r, g, b, a = watermark.getpixel((x, y))
                # Adjust alpha channel according to transparency value (0-255)
                # Higher transparency value means more opaque
                new_a = min(a, transparency)
                watermark_with_transparency.putpixel((x, y), (r, g, b, new_a))
        
        # Determine watermark position (bottom right corner with some margin)
        x_pos = base_width - new_wm_width - 10
        y_pos = base_height - new_wm_height - 10
        
        # Create a new transparent image the size of the base image
        watermark_layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
        # Paste the watermark into position
        watermark_layer.paste(watermark_with_transparency, (x_pos, y_pos))
        
        # Composite the watermarked image
        watermarked = Image.alpha_composite(img, watermark_layer)
        
        # Convert back to original mode if needed
        if img_data["value"].mode != 'RGBA':
            watermarked = watermarked.convert(img_data["value"].mode)
            
        img_data["value"] = watermarked
        print(f"Applied image watermark to {img_id}")
        
        return img_id
        
    def visit_resize(self, node):
        """Handle image resizing"""
        img_id = node.get("ImageIdentifier")
        width_expr = node.get("Width")
        height_expr = node.get("Height")
        maintain_ratio = not node.get("IgnoreAspectRatio", False)
        aspect_ratio = node.get("AspectRatio")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Get current dimensions
        current_width, current_height = img.size
        
        # Case 1: AspectRatio specified (e.g., 16:9)
        if aspect_ratio:
            parts = aspect_ratio.split(':')
            if len(parts) == 2:
                ratio_width = int(parts[0])
                ratio_height = int(parts[1])
                
                # Determine new dimensions based on the aspect ratio
                # We'll maintain the current width and adjust height
                new_width = current_width
                new_height = int(current_width * ratio_height / ratio_width)
                
                # If new height would be larger than current, use current height instead
                if new_height > current_height:
                    new_height = current_height
                    new_width = int(current_height * ratio_width / ratio_height)
                
                # Resize the image
                resized = img.resize((new_width, new_height), Image.LANCZOS)
                img_data["value"] = resized
                print(f"Resized {img_id} to aspect ratio {aspect_ratio} ({new_width}x{new_height})")
                
        # Case 2: Width and height specified
        elif width_expr is not None and height_expr is not None:
            # Evaluate width and height expressions
            new_width = self.visit(width_expr)
            new_height = self.visit(height_expr)
            
            # Ensure integer values
            new_width = int(new_width)
            new_height = int(new_height)
            
            if maintain_ratio:
                # Calculate the scaling factors for width and height
                width_ratio = new_width / current_width
                height_ratio = new_height / current_height
                
                # Use the smaller ratio to maintain aspect ratio
                ratio = min(width_ratio, height_ratio)
                
                # Calculate new dimensions
                new_width = int(current_width * ratio)
                new_height = int(current_height * ratio)
            
            # Resize the image
            resized = img.resize((new_width, new_height), Image.LANCZOS)
            img_data["value"] = resized
            
            ratio_info = "maintaining aspect ratio" if maintain_ratio else "ignoring aspect ratio"
            print(f"Resized {img_id} to {new_width}x{new_height} ({ratio_info})")
        
        else:
            raise ValueError(f"Insufficient resize parameters for {img_id}")
            
        return img_id
    
    def visit_weboptimize(self, node):
        """Handle web optimization for images"""
        img_id = node.get("ImageIdentifier")
        quality = node.get("Quality", 85)  # Default quality for lossy compression
        
        # Check for either Mode string or IsLossless boolean
        mode = None
        if "Mode" in node:
            mode = node.get("Mode")
        elif "IsLossless" in node:
            # Convert boolean IsLossless to string Mode
            is_lossless = node.get("IsLossless")
            mode = "LOSSLESS" if is_lossless else "LOSSY"
        
        # Default to LOSSLESS if no mode is specified
        if mode is None or not isinstance(mode, str):
            mode = "LOSSLESS"
            
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Create a BytesIO buffer to hold the optimized image
        buffer = io.BytesIO()
        
        if mode.upper() == "LOSSLESS":
            # For lossless optimization, we'll use PNG format with highest compression
            img.save(buffer, format="PNG", optimize=True, compress_level=9)
            print(f"Applied lossless web optimization to {img_id}")
            
        elif mode.upper() == "LOSSY":
            # For lossy optimization, we'll use JPEG with specified quality
            # Convert to RGB if needed (JPEG doesn't support alpha)
            if img.mode == 'RGBA':
                # Create a white background and paste the image on it
                bg = Image.new('RGB', img.size, (255, 255, 255))
                bg.paste(img, mask=img.split()[3])  # 3 is the alpha channel
                img = bg
            elif img.mode != 'RGB':
                img = img.convert('RGB')
                
            # Save with specified quality
            img.save(buffer, format="JPEG", quality=quality, optimize=True)
            print(f"Applied lossy web optimization to {img_id} with quality {quality}")
            
        else:
            raise ValueError(f"Unknown web optimization mode: {mode}")
        
        # Load the optimized image back from the buffer
        buffer.seek(0)
        optimized_img = Image.open(buffer)
        optimized_img.load()  # Force loading the image data
        
        # Update the image in environment
        img_data["value"] = optimized_img
        
        return img_id

    def visit_convert(self, node):
        """Handle converting image format"""
        img_id = node.get("ImageIdentifier")
        target_format = node.get("TargetFormat")
        
        if img_id not in self.environment:
            raise ValueError(f"Unknown image variable: {img_id}")
            
        img_data = self.environment[img_id]
        img = img_data["value"]
        
        # Get the old filename and extension
        old_filename = img_data.get("filename", "image.png")
        base_name, _ = os.path.splitext(old_filename)
        
        # Prepare the new format extension
        new_ext = "." + target_format.lower()
        
        # Create a new filename
        new_filename = base_name + new_ext
        
        # Convert the image (this doesn't actually change the file format
        # until we save it, but we'll update the filename in our environment)
        img_data["filename"] = new_filename
        
        print(f"Converted {img_id} format to {target_format}")
        return img_id

def process_ast(ast_data):
    """Process the AST data and execute image operations"""
    try:
        interpreter = GraphixInterpreter()
        results = interpreter.interpret(ast_data)
        return f"GraphixLang interpreter executed successfully with {interpreter.operation_count} operations"
    except Exception as e:
        print(f"Error processing AST: {str(e)}")
        import traceback
        traceback.print_exc()
        return f"Error: {str(e)}"

def main():
    if len(sys.argv) < 2:
        print("Usage: python interpreter.py <ast_json_file>")
        sys.exit(1)
    
    ast_file = sys.argv[1]
    
    # Handle file paths consistently on all platforms
    ast_file = os.path.normpath(ast_file)
    
    # Load the AST from the JSON file
    with open(ast_file, 'r', encoding='utf-8') as f:  # Adding encoding for Windows
        ast_data = json.load(f)
    
    # Process the AST
    result = process_ast(ast_data)
    
    # Print the result
    print(result)

if __name__ == "__main__":
    main()
