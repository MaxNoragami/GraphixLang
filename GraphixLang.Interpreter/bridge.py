import sys
import os
import json
from interpreter import GraphixInterpreter

def execute_ast(ast_json_path, output_path=None):
    """
    Execute an AST from a JSON file and optionally save the output
    
    Args:
        ast_json_path (str): Path to the JSON file containing the AST
        output_path (str, optional): Path to save output. Defaults to None.
    """
    try:
        # Load AST from JSON file
        with open(ast_json_path, 'r') as f:
            ast_json = f.read()
        
        # Initialize the interpreter
        interpreter = GraphixInterpreter()
        
        # Execute the AST
        result = interpreter.interpret(ast_json)
        
        # If output path is provided, save results
        if output_path:
            output_dir = os.path.dirname(output_path)
            if not os.path.exists(output_dir):
                os.makedirs(output_dir)
            
            with open(output_path, 'w') as f:
                json.dump({"result": str(result)}, f)
        
        return True, "Execution completed successfully"
        
    except Exception as e:
        error_message = str(e)
        return False, error_message

if __name__ == "__main__":
    # Command-line usage
    if len(sys.argv) < 2:
        print("Usage: python bridge.py <ast_json_path> [output_path]")
        sys.exit(1)
    
    ast_json_path = sys.argv[1]
    output_path = sys.argv[2] if len(sys.argv) > 2 else None
    
    success, message = execute_ast(ast_json_path, output_path)
    
    if not success:
        print(f"Error: {message}")
        sys.exit(1)
    else:
        print(message)
