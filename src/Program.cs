using SqlParser;

var printer = new TreePrinter();

var tokens1 = new Lexer("Select * from table join table2 on id1 = id1 OR id2 = id2 AND id3 = id3 OR id4 = id4").GetAllTokens();

var parser1 = new Parser(tokens1);
// parser1.PrintTokens();
var sqlNode1 = parser1.Parse();
sqlNode1.Accept(printer);
printer.PrintAndClear();