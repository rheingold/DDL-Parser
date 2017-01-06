// DDL - parsing DDL class representation, Linq queryable using Statements
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Linq;

using System.Diagnostics;

public class DDL {
  
  // internal representation of a parsable DDL-text
  private Parsable ddl;
  
  // resulting statements from parsing
  private List<Statement> statements = new List<Statement>  {};

  // length of DDL is the number of statements that were parsed
  public int Length {
    get { return this.statements.Count; }
  }

  // Indexer to retrieve a given (parsed) statement
  public Statement this[int key] {
      get { return this.statements[key]; }
  }

  // initiates parsing of DLL into statements
  public bool Parse(String ddl) {
    this.ddl = new Parsable(ddl);

    while( this.ddl.Length > 0 ) {
      this.ShowParsingInfo();

      if(                         this.ParseComment()   ) { continue; }
      if( this.ddl.Length == 0 || this.ParseStatement() ) { continue; }
      if( this.ddl.Length  > 0) { return this.NotifyParseFailure();   }
    }

    return true;
  }
  
  // TODO expose differently
  public void Dump() {
    var statements = from statement in this.statements
                     where !(statement is Comment)
                     select statement;
                       
    foreach(var statement in statements) {
      Console.WriteLine(statement);
    }
  }

  // internal parsing steps

  private bool ParseComment() {
    if( this.ddl.Consume("--") ) {
      Comment stmt = new Comment() { Body = this.ddl.ConsumeUpTo("\n") };
      this.ddl.Consume("\n");
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool ParseStatement() {
    return this.ParseCreateStatement()
        || this.ParseAlterStatement()
        || this.ParseSetStatement()

        || this.NotifyParseStatementFailure();
  }

  private bool ParseCreateStatement() {
    if( this.ddl.Consume("CREATE ") || this.ddl.Consume("CREATE\n") ) {
      return this.ParseCreateDatabaseStatement()
          || this.ParseCreateTablespaceStatement()
          || this.ParseCreateTableStatement()
          || this.ParseCreateIndexStatement()
          || this.ParseCreateViewStatement()

          || this.NotifyParseCreateStatementFailure();
    }
    return false;
  }

  private bool ParseCreateDatabaseStatement() {
    if( this.ddl.Consume("DATABASE ") || this.ddl.Consume("DATABASE\n") ) {
      string name = this.ddl.ConsumeId();
      if( name == null ) { return false; }
      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary();
      CreateDatabaseStatement stmt = new CreateDatabaseStatement() {
        Name       = name,
        Parameters = parameters
      };
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool ParseCreateTablespaceStatement() {
    if( this.ddl.Consume("TABLESPACE ") || this.ddl.Consume("TABLESPACE\n") ) {
      string name = this.ddl.ConsumeId();
      if( name == null                         ) { return false; }
      if( ! this.ddl.Consume("IN")             ) { return false; }
      string database = this.ddl.ConsumeId();
      if( database == null                     ) { return false; }
      if( ! this.ddl.Consume("USING STOGROUP") ) { return false; }
      string storageGroup = this.ddl.ConsumeId();
      if( storageGroup == null                 ) { return false; }
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

  private bool ParseCreateTableStatement() {
    string statement = this.ddl.ConsumeUpTo(";");
    this.ddl.Consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool ParseCreateIndexStatement() {
    string statement = this.ddl.ConsumeUpTo(";");
    this.ddl.Consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool ParseCreateViewStatement() {
    string statement = this.ddl.ConsumeUpTo(";");
    this.ddl.Consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool ParseAlterStatement() {
    if( this.ddl.Consume("ALTER ") || this.ddl.Consume("ALTER\n") ) {
      string statement = this.ddl.ConsumeUpTo(";");
      this.ddl.Consume(";");
      this.statements.Add(new AlterStatement() { Body = statement });
      return true;
    }
    return false;
  }

  private bool ParseSetStatement() {
    if( this.ddl.Consume("SET ") || this.ddl.Consume("SET\n") ) {
      string statement = this.ddl.ConsumeUpTo(";");
      this.ddl.Consume(";");
      this.statements.Add(new SetStatement() { Body = statement });
      return true;
    }
    return false;
  }
  
  private bool NotifyParseCreateStatementFailure() {
    Console.WriteLine("Failed to parse Create Statement!");
    return false;
  }

  private bool NotifyParseStatementFailure() {
    Console.WriteLine("Failed to parse Statement!");
    return false;
  }
  
  private bool NotifyParseFailure() {
    Console.WriteLine("Failed to parse DDL! Aborting..");
    Console.WriteLine(this.ddl.Peek(75));
    return false;
  }

  [ConditionalAttribute("DEBUG")]
  private void ShowParsingInfo() {
    Console.WriteLine(new String('-', 75));
    Console.WriteLine(this.ddl.Length + " bytes remaining:");
    Console.WriteLine(this.ddl.Peek(50) + " [...]");
    Console.WriteLine(new String('-', 75));
  }

  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.WriteLine("!!! " + msg);
  }
}