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
    EXPORT,
    TO,
    OGKEEP,
    OGDELETE,
    SHARPEN,
    NEGATIVE,
    BW,
    WEBOPTIMIZE,
    LOSSLESS,
    LOSSY,
    SEPIA,
    CROP,
    ORIENTATION,
    ROTATE,
    LANDSCAPE,
    CONVERT,
    PORTRAIT,
    HUE,
    RESIZE,
    RATIOFALSE,
    COMPRESS,
    BRIGHTNESS,
    CONTRAST,
    OPACITY,
    NOISE,
    BLUR,
    PIXELATE,


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
    STRIP,
    ADD,
    ALL,
    TAGS,
    TITLE,
    COPYRIGHT,
    GPS,
    RENAME,
    COUNTER,
    CAMERA,
    ADVANCE,
    ORIGIN,
    DESCRIPTION,
    PNG,
    JPG,
    JPEG,
    WEBP,
    TIFF,
    BMP,

    // Aspect Ratios
    RATIO_16_9,    // 16:9
    RATIO_9_16,    // 9:16
    RATIO_4_3,     // 4:3
    RATIO_3_4,     // 3:4
    RATIO_1_1,     // 1:1
    RATIO_2_3,     // 2:3
    RATIO_3_2,     // 3:2
    RATIO_2_1,     // 2:1
    RATIO_1_2,     // 1:2
    RATIO_16_10,   // 16:10
    RATIO_10_16,   // 10:16
    RATIO_21_9,    // 21:9
    RATIO_9_21,    // 9:21
    
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
