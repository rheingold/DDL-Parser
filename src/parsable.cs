// Parsable - a text wrapping class with helper functions and behaviour for
//            easier construction of (manually) crafted parsers.
// author: Christophe VG <contact@christophe.vg>

// implicit behaviour:
// - ignores whitespace
// - ignores carriage returns
// - trims consumed tokens

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Parsable {

  // the parsable text
  private string text = "";

  // helper Regular Expressions for general clean-up
  private static Regex discardedCharacters    = new Regex( "\r"              );
  private static Regex repeatedWhitespace     = new Regex( "[ \t][ \t]*"     );

  private static Regex leadingWhitespace      = new Regex( "^\\s+"           );
  private static Regex trailingWhitespace     = new Regex( "\\s+$"           );
  private static Regex newlinesWithWhitespace = new Regex( "\n[ \t]*"        );

  private static Regex nextId                 = new Regex( "^([A-Z0-9]+)\\s" );

  public Parsable(string text) {
    this.text = text;
    this.text = Parsable.discardedCharacters  .Replace(this.text, ""  );
    this.text = Parsable.repeatedWhitespace   .Replace(this.text, " " );
  }

  public int  Length { get { return this.text.Length; } }

  // skips any whitespace at the start of the current text buffer
  public void SkipLeadingWhitespace() {
    this.text = Parsable.leadingWhitespace.Replace(this.text, "");
  }
  
  // tries to consume a give string, returns success
  public bool Consume(string text) {
    this.text = Parsable.leadingWhitespace.Replace(this.text, "");
    if(this.text.StartsWith(text)) {
      return this.Consume(text.Length) == this.Trim(text);
    }
    return false;
  }

  // consumes all characters up to a given string
  public string ConsumeUpTo(string text) {
    return this.Consume(this.text.IndexOf(text));
  }

  // consumes an ID, returning it or null in case of failure
  public string ConsumeId() {
    this.SkipLeadingWhitespace();
    Match m = Parsable.nextId.Match(this.text);
    if(m.Success) {
      int length = m.Groups[0].Captures[0].ToString().Length;
      return this.Consume(length);
    }
    return null;
  }

  // consumes key value pairs into a dictionary
  public Dictionary<string,string> ConsumeDictionary(string upTo = ";",
                                                     char   separator = ' ' )
  {
    string[] mappings = this.Trim(this.ConsumeUpTo(upTo)).Split(separator);
    this.Consume(upTo);
    Dictionary<string,string> dict = new Dictionary<string,string>();
    int pairs = mappings.Length - (mappings.Length % 2);
    for(int i=0; i<pairs; i+=2) {
      dict[this.Trim(mappings[i])] = this.Trim(mappings[i+1]);
    }
    return dict;
  }

  // returns an amount of characters, without consuming, not trimming!
  public string Peek(int amount) {
    return this.text.Substring(0, Math.Min(amount, this.Length));
  }
  
  // consumes an amount of characters
  private string Consume(int amount) {
    amount = Math.Min(amount, this.Length);
    // extract
    string consumed = this.text.Substring(0, amount);
    // drop
    this.text = this.text.Substring(amount);
    return this.Trim(consumed);
  }

  // helper function to trim leading and trailing whitespace
  // also replace new-line characters and optionally addition whitespace
  private string Trim(string text) {
    text = Parsable.leadingWhitespace    .Replace(text, "");
    text = Parsable.trailingWhitespace   .Replace(text, "");
    text = Parsable.newlinesWithWhitespace.Replace(text, " ");
    return text;
  }
}