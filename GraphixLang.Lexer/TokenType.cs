namespace GraphixLang.Lexer;

public enum TokenType
{
    // Reserved Keywords
    IN,
    FOREACH,
    FSIZE,
    FNAME,
    FHEIGHT,
    FWIDTH,
    METADATA,
    RIGHT,
    LEFT,
    SET,
    ELIF,
    ELSE,
    IF,
    SHARPEN,
    NEGATIVE,
    BW,
    SEPIA,
    CROP,
    ORIENTATION,
    ROTATE,
    LANDSCAPE,
    PORTRAIT,
    HUE,


    // Types
    TYPE_BATCH,
    TYPE_IMG,
    TYPE_STR,
    TYPE_BOOL,
    TYPE_DBL,
    TYPE_INT,
    TYPE_PXLS,


    // Values
    PXLS_VALUE,
    STR_VALUE,
    BOOL_VALUE,
    DBL_VALUE,
    INT_VALUE,
    WATERMARK,
    HEX_COLOR,
    RGB_COLOR,
    
    // Identifier
    VAR_IDENTIFIER, // Either Batch '#' or Var '$'
    BATCH_IDENTIFIER,
    
    // Symbols
    CLOSE_P,
    OPEN_P,
    COMMA,
    NOT_EQUAL,
    EQUAL,
    SMALLER,
    GREATER,
    SMALLER_EQUAL,
    GREATER_EQUAL,
    ASSIGN,
    CLOSE_BLOCK,
    OPEN_BLOCK,
    DIVIDE,
    MULTIPLY,
    MINUS,
    PLUS,
    

    // SPECIAL
    EOL,
    EOF
}
