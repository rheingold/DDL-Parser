using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Linq;

using System.Diagnostics;

public class DDL {

  private List<Statement> statements = new List<Statement>  {};

  public int Length {
    get { return this.statements.Count; }
  }

  public Statement this[int key] {
      get { return this.statements[key]; }
  }

  public DDL() {}
  
  /*

    A DDL basically consists of two types of "statements": comments and actual
    statements. The former is identified by a double leading dash (--), the
    latter are separated using semi-colons (;).

    This parser starts of by checking of its current input starts with a double
    dash, if so, the remainder of the line, up to a new-line character
    (optional carriage returns are discarted anyway), is wrapped in a Comment
    object. If it is a statement, everything up to the next semi-colon is
    passed to a Statement.

  */
    
  private Parsable ddl;
      
  public bool parse(String ddl) {
    this.ddl = new Parsable(ddl);

    while( this.ddl.Length > 0 ) {
      this.showParsingInfo();

      if(! ( this.parseComment() || this.parseStatement() ) ) {
        return this.notifyParseFailure();
      }

      this.ddl.SkipLeadingWhitespace();
    }

    return true;
  }

  private bool parseComment() {
    if( this.ddl.Consume("--") ) {
      Comment stmt = new Comment() { Body = this.ddl.ConsumeUpTo("\n") };
      this.ddl.Consume("\n");
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool parseStatement() {
    return this.parseCreateStatement()
        || this.parseAlterStatement()
        || this.parseSetStatement()

        || this.notifyParseStatementFailure();
  }

  private bool parseCreateStatement() {
    if( this.ddl.Consume("CREATE ") || this.ddl.Consume("CREATE\n") ) {
      return this.parseCreateDatabaseStatement()
          || this.parseCreateTablespaceStatement()
          || this.parseCreateTableStatement()
          || this.parseCreateIndexStatement()
          || this.parseCreateViewStatement()
          || this.notifyParseCreateStatementFailure();
    }
    return false;
  }

  private bool parseCreateDatabaseStatement() {
    if( this.ddl.Consume("DATABASE ") || this.ddl.Consume("DATABASE\n") ) {
      string name = this.ddl.ConsumeId();
      if( name.Length > 0 ) {
        Dictionary<string,string> parameters = this.ddl.ConsumeDictionary();
        CreateDatabaseStatement stmt = new CreateDatabaseStatement() {
          Name       = name,
          Parameters = parameters
        };
        this.statements.Add(stmt);
        return true;
      }
    }
    return false;
  }

  private bool parseCreateTablespaceStatement() {
    if( this.ddl.Consume("TABLESPACE ") || this.ddl.Consume("TABLESPACE\n") ) {
      string name = this.ddl.ConsumeId();
      if( ! (name.Length > 0)                  ) { return false; }
      if( ! this.ddl.Consume("IN")             ) { return false; }
      string database = this.ddl.ConsumeId();
      if( ! (database.Length > 0)              ) { return false; }
      if( ! this.ddl.Consume("USING STOGROUP") ) { return false; }
      string storageGroup = this.ddl.ConsumeId();
      if( ! (storageGroup.Length > 0)          ) { return false; }
      this.showParsingInfo();
      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary();
      CreateTablespaceStatement stmt = 
        new CreateTablespaceStatement() {
          Name         = name,
          Database     = database,
          StorageGroup = storageGroup,
          Parameters   = parameters
        };
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool parseCreateTableStatement() {
    string statement = this.ddl.ConsumeUpTo(";");
    this.ddl.Consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool parseCreateIndexStatement() {
    string statement = this.ddl.ConsumeUpTo(";");
    this.ddl.Consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool parseCreateViewStatement() {
    string statement = this.ddl.ConsumeUpTo(";");
    this.ddl.Consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool parseAlterStatement() {
    if( this.ddl.Consume("ALTER ") || this.ddl.Consume("ALTER\n") ) {
      string statement = this.ddl.ConsumeUpTo(";");
      this.ddl.Consume(";");
      this.statements.Add(new AlterStatement() { Body = statement });
      return true;
    }
    return false;
  }

  private bool parseSetStatement() {
    if( this.ddl.Consume("SET ") || this.ddl.Consume("SET\n") ) {
      string statement = this.ddl.ConsumeUpTo(";");
      this.ddl.Consume(";");
      this.statements.Add(new SetStatement() { Body = statement });
      return true;
    }
    return false;
  }
  
  private bool notifyParseCreateStatementFailure() {
    Console.WriteLine("Failed to parse Create Statement!");
    return false;
  }

  private bool notifyParseStatementFailure() {
    Console.WriteLine("Failed to parse Statement!");
    return false;
  }
  
  private bool notifyParseFailure() {
    Console.WriteLine("Failed to parse DDL! Aborting..");
    Console.WriteLine(this.ddl.Peek(75));
    return false;
  }

  public void dump() {
    var statements = from statement in this.statements
                     where !(statement is Comment)
                     select statement;
                       
    foreach(var statement in statements) {
      Console.WriteLine(statement);
    }
  }

  [ConditionalAttribute("DEBUG")]
  private void showParsingInfo() {
    Console.WriteLine(new String('-', 75));
    Console.WriteLine(this.ddl.Length + " bytes remaining:");
    Console.WriteLine(this.ddl.Peek(50) + " [...]");
    Console.WriteLine(new String('-', 75));
  }

  [ConditionalAttribute("DEBUG")]
  private void log(string msg) {
    Console.WriteLine("!!! " + msg);
  }
}

public class Program {
  public static void Main(string[] args) {

    if(args.Length != 1) {
      Console.WriteLine("USAGE: main.exe <filename>");
      return;
    }
    
    if(! File.Exists(args[0])) {
      Console.WriteLine("Unknown file");
      return;
    }
    
    string input = System.IO.File.ReadAllText(args[0]);
    
    DDL ddl = new DDL();
    ddl.parse(input);
    ddl.dump();
  }
}
