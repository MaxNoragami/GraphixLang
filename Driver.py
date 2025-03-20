import sys
from antlr4 import *
from GrammarLexer import GrammarLexer
from GrammarParser import GrammarParser
#from VisitorInterp import VisitorInterp

def main(argv):
    input_stream = FileStream(argv[1])
    lexer = GrammarLexer(input_stream)
    stream = CommonTokenStream(lexer)
    parser = GrammarParser(stream)
    tree = parser.start()
    print("Parse tree:", tree.toStringTree(recog=parser))

if __name__ == '__main__':
    main(sys.argv)