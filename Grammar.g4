grammar Grammar;
start: statements EOF;

statements: statement statements | statement;

statement: simple_statement | compound_statement | NEWLINE;

simple_statement: assignment
                | import_statement
                | export_statement
                | operation
                | COMMENT;

compound_statement: conditional | foreach_loop;

assignment: IDENTIFIER '=' expression
          | IDENTIFIER '+=' expression
          | IDENTIFIER '-=' expression
          | IDENTIFIER '*=' expression
          | IDENTIFIER '/=' expression
          | IDENTIFIER '%=' expression;

import_statement: 'import' STRING
                | 'import' STRING 'as' IDENTIFIER
                | 'import' 'folder' STRING
                | 'import' 'folder' STRING 'as' IDENTIFIER
                | 'import' 'recursive' 'folder' STRING
                | 'import' 'recursive' 'folder' STRING 'as' IDENTIFIER;

export_statement: 'export' expression 'to' STRING
                | 'export' expression 'to' STRING 'with' options;

conditional: 'if' expression ':' '{' statements '}'
           | 'if' expression ':' '{' statements '}' 'else' ':' '{' statements '}'
           | 'if' expression ':' '{' statements '}' 'elif' expression ':' '{' statements '}';

foreach_loop: 'foreach' IDENTIFIER 'in' expression ':' '{' statements '}';

expression: term ((BIN_OP term)*) ;

term: factor ((comparison)*) ;

factor: literal
      | IDENTIFIER
      | '(' expression ')'
      | operation;

literal: NUMBER | STRING | batch_literal | color_literal | BOOL;

batch_literal: '[' batch_items ']' | '[' ']';

batch_items: expression | expression ',' batch_items;

color_literal: 'rgb' '(' NUMBER ',' NUMBER ',' NUMBER ')'
             | 'rgba' '(' NUMBER ',' NUMBER ',' NUMBER ',' NUMBER ')'
             | 'hex' '(' STRING ')';

comparison: expression COMP_OP expression;

binary_operation: expression BIN_OP expression;

operation: resize_op
         | crop_op
         | filter_op
         | format_op
         | enhancement_op
         | metadata_op
         | batch_op
         | watermark_op
         | quantize_op;

watermark_op: image_watermark_op | text_watermark_op;

image_watermark_op: 'add' 'watermark' expression 'to' expression
                  | 'add' 'watermark' expression 'to' expression 'with' watermark_options;

quantize_op: 'quantize' expression 'to' NUMBER 'colors';

text_watermark_op: 'add' 'text' 'watermark' STRING 'to' expression
                 | 'add' 'text' 'watermark' STRING 'to' expression 'with' watermark_options
                 | 'add' 'text' 'watermark' STRING 'to' expression watermark_text_params;

watermark_options: watermark_option | watermark_option ',' watermark_options;

watermark_option: 'size' '=' NUMBER
                | 'position' '=' watermark_position
                | 'opacity' '=' NUMBER '%'
                | 'rotation' '=' NUMBER
                | 'shadow' '=' BOOL
                | 'shadow_color' '=' color_literal
                | 'blend_mode' '=' STRING;

watermark_text_params: watermark_text_param | watermark_text_param watermark_text_params;

watermark_text_param: 'font' '=' STRING
                    | 'size' '=' NUMBER
                    | 'color' '=' color_literal
                    | 'position' '=' watermark_position
                    | 'opacity' '=' NUMBER '%'
                    | 'rotation' '=' NUMBER
                    | 'padding' '=' NUMBER
                    | 'shadow' '=' BOOL
                    | 'shadow_color' '=' color_literal
                    | 'blend_mode' '=' STRING;

watermark_position: 'top-left' | 'top-center' | 'top-right'
                  | 'center-left' | 'center' | 'center-right'
                  | 'bottom-left' | 'bottom-center' | 'bottom-right'
                  | '(' NUMBER ',' NUMBER ')';

resize_op: 'resize' expression resize_params;

resize_params: 'width' '=' NUMBER ',' 'height' '=' NUMBER
             | 'width' '=' NUMBER ',' 'height' '=' NUMBER ',' 'preserve_aspect_ratio' '=' BOOL
             | 'scale' '=' NUMBER '%'
             | 'to' 'fit' dimension_params
             | 'to' 'cover' dimension_params
             | 'to' aspect_ratio;

dimension_params: 'width' '=' NUMBER
                | 'height' '=' NUMBER
                | 'width' '=' NUMBER ',' 'height' '=' NUMBER;

aspect_ratio: 'aspect_ratio' '=' STRING;

crop_op: 'crop' expression crop_params;

crop_params: 'from' 'x' '=' NUMBER ',' 'y' '=' NUMBER ',' 'width' '=' NUMBER ',' 'height' '=' NUMBER
           | 'to' aspect_ratio
           | 'center' 'width' '=' NUMBER ',' 'height' '=' NUMBER;

filter_op: 'apply' filter_type 'to' expression
         | 'apply' filter_type 'to' expression 'with' options;

filter_type: 'grayscale'
           | 'sepia'
           | 'blur'
           | 'blur' '(' NUMBER ')'
           | 'sharpen'
           | 'sharpen' '(' NUMBER ')'
           | 'invert'
           | 'custom' '(' STRING ')';

format_op: 'convert' expression 'to' format_type
         | 'convert' expression 'to' format_type 'with' options;

format_type: 'jpg' | 'jpeg' | 'png' | 'webp' | 'gif' | 'bmp' | 'tiff';

enhancement_op: 'adjust' enhancement_type 'of' expression 'by' NUMBER
              | 'set' enhancement_type 'of' expression 'to' NUMBER;

enhancement_type: 'brightness' | 'contrast' | 'saturation' | 'hue' | 'sharpness' | 'gamma';

metadata_op: 'get' metadata_field 'from' expression
           | 'set' metadata_field 'of' expression 'to' value
           | 'strip' 'metadata' 'from' expression
           | 'rename' expression 'to' pattern
           | 'organize' expression 'by' metadata_field 'into' STRING;

metadata_field: 'exif' | 'date' | 'location' | 'width' | 'height'
              | 'file_size' | 'format' | 'area' | 'author' | 'copyright' | STRING;

pattern: STRING;

batch_op: 'merge' expression
        | 'merge' expression ',' expression_list
        | 'filter' expression 'where' comparison
        | 'limit' expression 'to' NUMBER
        | 'sort' expression 'by' metadata_field
        | 'sort' expression 'by' metadata_field 'asc'
        | 'sort' expression 'by' metadata_field 'desc'
        | 'optimize' expression
        | 'optimize' expression 'for' 'web'
        | 'count' expression;

expression_list: expression | expression ',' expression_list;

options: option | option ',' options;

option: IDENTIFIER '=' value;

value: NUMBER | STRING | BOOL | IDENTIFIER;

COMMENT: '//' .*? '\n' -> skip;

NEWLINE: '\r'? '\n' -> skip;

BOOL: 'true' | 'false';
IDENTIFIER: [a-zA-Z_] [a-zA-Z0-9_]*;
STRING: '"' (~["\r\n])* '"' | '\'' (~['\r\n])* '\'';
NUMBER: [0-9]+ ('.' [0-9]+)?;
COMP_OP: '==' | '!=' | '<' | '>' | '<=' | '>=';

BIN_OP: '+' | '-' | '*' | '/' | '%' | 'and' | 'or';

WS: [ \t\r\n]+ -> skip;