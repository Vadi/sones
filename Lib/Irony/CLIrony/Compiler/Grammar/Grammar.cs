#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for CLIrony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace sones.Lib.Frameworks.CLIrony.Compiler {

  [Flags]
  public enum LanguageFlags {
    None = 0,

    //Capabilities
    SupportsInterpreter = 0x01,
    SupportsConsole = 0x02,
    //Tail-recursive language - Scheme is one example
    TailRecursive = 0x04,

    //Parsing specifics
    //Bubble nodes in AST tree - see Parser.CreateNode method
    BubbleNodes = 0x0100,
    //Be careful - use this flag ONLY if you use NewLine terminal in grammar explicitly!
    // - it happens only in line-based languages like Basic.
    NewLineBeforeEOF = 0x0200,

    //Default value
    Default = BubbleNodes,
  }


  public class Grammar {

    #region properties: CaseSensitive, WhitespaceChars, Delimiters, ExtraTerminals, Root, TokenFilters
    public bool CaseSensitive = true;
    //List of chars that unambigously identify the start of new token. 
    //used in scanner error recovery, and in quick parse path in Number literals 
    public string Delimiters = ",;[](){}";

    public string WhitespaceChars = " \t\r\n\v";
    
    //Used for line counting in source file
    public string LineTerminators = "\n\r\v";

    //The following list must include only words that are reserved and are not general identifiers (variables)
    public readonly StringSet Keywords = new StringSet();

    //Language options
    public LanguageFlags LanguageFlags = LanguageFlags.Default;
    public bool FlagIsSet(LanguageFlags flag) {
      return (LanguageFlags & flag) != 0;
    }

    //Terminals not present in grammar expressions and not reachable from the Root
    // (Comment terminal is usually one of them)
    // Tokens produced by these terminals will be ignored by parser input. 
    public readonly TerminalList NonGrammarTerminals = new TerminalList();

    //Terminals that either don't have explicitly declared Firsts symbols, or can start with chars not covered by these Firsts 
    // For ex., identifier in c# can start with a Unicode char in one of several Unicode classes, not necessarily latin letter.
    //  Whenever terminals with explicit Firsts() cannot produce a token, the Scanner would call terminals from this fallback 
    // collection to see if they can produce it. 
    // Note that IdentifierTerminal automatically add itself to this collection if its StartCharCategories list is not empty, 
    // so programmer does not need to do this explicitly
    public readonly TerminalList FallbackTerminals = new TerminalList();

    //Default node type; if null then GenericNode type is used. 
    public Type DefaultNodeType = typeof(AstNode);


    public ABnfTerm Root;
    public readonly TokenFilterList TokenFilters = new TokenFilterList();

    //derived lists
    public readonly BnfTermList AllTerms = new BnfTermList();
    public readonly TerminalList Terminals = new TerminalList();
    public readonly NonTerminalList NonTerminals = new NonTerminalList();

    public readonly StringSet Errors = new StringSet();
    #endregion 

    #region Keywords handling
    public void AddKeywords(params string[] keywords) {
      Keywords.AddRange(keywords);
    }
    public void AddKeywordList(string keywordList) {
      string[] arr = keywordList.Split(' ', ',', ';', '\n', '\r', '\t');
      foreach (string kw in arr) {
        string trimmed = kw.Trim();
        if (!string.IsNullOrEmpty(trimmed))
          Keywords.Add(trimmed);
      }
    }
    #endregion 

    #region Register methods
    public void RegisterPunctuation(params string[] symbols) {
      foreach (string symbol in symbols) {
        SymbolTerminal term = SymbolTerminal.GetSymbol(symbol);
        term.SetOption(TermOptions.IsPunctuation);
      }
    }
    
    public void RegisterPunctuation(params ABnfTerm[] elements) {
      foreach (ABnfTerm term in elements) 
        term.SetOption(TermOptions.IsPunctuation);
    }

    public void RegisterOperators(int precedence, params string[] opSymbols) {
      RegisterOperators(precedence, Associativity.Left, opSymbols);
    }

    public void RegisterOperators(int precedence, Associativity associativity, params string[] opSymbols) {
      foreach (string op in opSymbols) {
        string opCased = this.CaseSensitive ? op : op.ToLower(); 
        SymbolTerminal opSymbol = SymbolTerminal.GetSymbol(opCased);
        opSymbol.SetOption(TermOptions.IsOperator, true);
        opSymbol.Precedence = precedence;
        opSymbol.Associativity = associativity;
      }
    }//method

    public void RegisterBracePair(string openBrace, string closeBrace) {
      SymbolTerminal openS = SymbolTerminal.GetSymbol(openBrace);
      SymbolTerminal closeS = SymbolTerminal.GetSymbol(closeBrace);
      openS.SetOption(TermOptions.IsOpenBrace);
      openS.IsPairFor = closeS;
      closeS.SetOption(TermOptions.IsCloseBrace);
      closeS.IsPairFor = openS;
    }
    public void MarkTransient(params NonTerminal[] nonTerminals) {
      foreach (NonTerminal nt in nonTerminals)
        nt.Options |= TermOptions.IsTransient;
    }
    #endregion

    #region virtual methods: TryMatch, CreateNode, GetSyntaxErrorMessage, CreateRuntime
    //This method is called if Scanner failed to produce token
    public virtual Token TryMatch(CompilerContext context, ISourceStream source) {
      return null;
    }
    // Override this method in language grammar if you want a custom node creation mechanism.
    public virtual AstNode CreateNode(CompilerContext context, object reduceAction, 
                                      SourceSpan sourceSpan, AstNodeList childNodes) {
      return null;      
    }
    public virtual string GetSyntaxErrorMessage(CompilerContext context, StringSet expectedSymbolSet) {
      return null; //CLIrony then would construct default message
    }
    public virtual void OnActionSelected(IParser parser, Token input, object action) {
    }
    public virtual object OnActionConflict(IParser parser, Token input, object action) {
      return action;
    }
    public virtual sones.Lib.Frameworks.CLIrony.Runtime.LanguageRuntime CreateRuntime() {
      return new sones.Lib.Frameworks.CLIrony.Runtime.LanguageRuntime();
    }

    #endregion

    #region Static utility methods used in custom grammars: Symbol()
    protected static SymbolTerminal Symbol(string symbol) {
      return SymbolTerminal.GetSymbol(symbol);
    }
    protected static SymbolTerminal Symbol(string symbol, string name) {
      return SymbolTerminal.GetSymbol(symbol, name);
    }
    #endregion


    #region MakePlusRule, MakeStarRule methods
    public BnfExpression MakePlusRule(NonTerminal listNonTerminal, ABnfTerm listMember) {
      return MakePlusRule(listNonTerminal, null, listMember);
    }
    public BnfExpression MakePlusRule(NonTerminal listNonTerminal, ABnfTerm delimiter, ABnfTerm listMember) {
      listNonTerminal.SetOption(TermOptions.IsList);
      if (delimiter == null)
        listNonTerminal.Rule = listMember | listNonTerminal + listMember;
      else
        listNonTerminal.Rule = listMember | listNonTerminal + delimiter + listMember;
      return listNonTerminal.Rule;
    }
    public BnfExpression MakeStarRule(NonTerminal listNonTerminal, ABnfTerm listMember) {
      return MakeStarRule(listNonTerminal, null, listMember);
    }
    public BnfExpression MakeStarRule(NonTerminal listNonTerminal, ABnfTerm delimiter, ABnfTerm listMember) {
      if (delimiter == null) {
        //it is much simpler case
        listNonTerminal.SetOption(TermOptions.IsList);
        listNonTerminal.Rule = Empty | listNonTerminal + listMember;
        return listNonTerminal.Rule;
      }
      NonTerminal tmp = new NonTerminal(listMember.Name + "+");
      tmp.SetOption(TermOptions.IsTransient); //important - mark it as Transient so it will be eliminated from AST tree
      MakePlusRule(tmp, delimiter, listMember);
      listNonTerminal.Rule = Empty | tmp;
      listNonTerminal.SetOption(TermOptions.IsStarList);
      return listNonTerminal.Rule;
    }
    #endregion

    #region Hint utilities
    protected GrammarHint PreferShiftHere() {
      return new GrammarHint(HintType.PreferShift);
    }
    protected GrammarHint ReduceThis() {
      return new GrammarHint(HintType.ReduceThis);
    }
    #endregion

    #region Standard terminals: EOF, Empty, NewLine, Indent, Dedent
    // Empty object is used to identify optional element: 
    //    term.Rule = term1 | Empty;
    public readonly static Terminal Empty = new Terminal("EMPTY");
    // The following terminals are used in indent-sensitive languages like Python;
    // they are not produced by scanner but are produced by CodeOutlineFilter after scanning
    public readonly static NewLineTerminal NewLine = new NewLineTerminal("LF");
    public readonly static Terminal Indent = new Terminal("INDENT", TokenCategory.Outline);
    public readonly static Terminal Dedent = new Terminal("DEDENT", TokenCategory.Outline);
    public static NonTerminal NewLinePlus = CreateNewLinePlus();
    
    private static NonTerminal CreateNewLinePlus() {
      var result = new NonTerminal("LF+");
      result.SetOption(TermOptions.IsList);
      result.Rule = NewLine | result + NewLine;
      return result; 
    }

    // Identifies end of file
    // Note: using Eof in grammar rules is optional. Parser automatically adds this symbol 
    // as a lookahead to Root non-terminal
    public readonly static Terminal Eof = new Terminal("EOF", TokenCategory.Outline);

    //End-of-Statement terminal
    public readonly static Terminal Eos = new Terminal("EOS", TokenCategory.Outline);

    public readonly static Terminal SyntaxError = new Terminal("SYNTAX_ERROR", TokenCategory.Error);


    #endregion


    #region Preparing for processing
    public bool Initialized {
      get { return _initialized; }
    } bool _initialized;

    public void Init() {
      CollectAllTerms();
      //Init filters
      foreach (TokenFilter filter in TokenFilters)
        filter.Init(this);

      InitKeywords();
      _initialized = true; 
    }

    private void InitKeywords() {
      //If grammar is case insensitive, we need to refill the keywords set with lowercase versions
      if (!CaseSensitive && Keywords.Count > 0) {
        string[] buff = new string[Keywords.Count];
        Keywords.CopyTo(buff);
        Keywords.Clear();
        foreach (string kw in buff) {
          string adjkw = kw.ToLower();
          Keywords.Add(adjkw);
        }
      }//if
    }

    int _unnamedCount; //internal counter for generating names for unnamed non-terminals
    public void CollectAllTerms() {
      _unnamedCount = 0;
      AllTerms.Clear();
      //set IsNonGrammar flag in all NonGrammarTerminals and add them to Terminals collection
      foreach (Terminal t in NonGrammarTerminals) {
        t.SetOption(TermOptions.IsNonGrammar);
        AllTerms.Add(t);
      }
      _unnamedCount = 0;
      CollectAllRecursive(Root);

      //Init all terms
      foreach (ABnfTerm term in this.AllTerms)
        term.Init(this);

      //Collect terminals and NonTerminals
      NonTerminals.Clear();
      foreach (ABnfTerm t in AllTerms) {
        NonTerminal nt = t as NonTerminal;
        if (nt != null)
          NonTerminals.Add(nt);
        Terminal terminal = t as Terminal;
        if (terminal != null)
          Terminals.Add(terminal);
      }
      //Adjust case for Symbols for case-insensitive grammar (change keys to lowercase)
      if (!CaseSensitive) {
        foreach (Terminal term in Terminals)
          if (term is SymbolTerminal)
            term.Key = term.Key.ToLower();
      }
      Terminals.Sort(Terminal.ByName);
    }

    private void CollectAllRecursive(ABnfTerm element) {
      //Terminal
      Terminal term = element as Terminal;
      // Do not add pseudo terminals defined as static singletons in Grammar class (Empty, Eof, etc)
      //  We will never see these terminals in the input stream.
      //   Filter them by type - their type is exactly "Terminal", not derived class. 
      if (term != null && !AllTerms.Contains(term) && term.GetType() != typeof(Terminal)) {
        AllTerms.Add(term);
        return;
      }
      //NonTerminal
      NonTerminal nt = element as NonTerminal;
      if (nt == null || AllTerms.Contains(nt))
        return;

      if (nt.Name == null) {
        if (nt.Rule != null && !string.IsNullOrEmpty(nt.Rule.Name))
          nt.Name = nt.Rule.Name;
        else
          nt.Name = "NT" + (_unnamedCount++);
      }
      AllTerms.Add(nt);
      if (nt.Rule == null) {
        ThrowError("Non-terminal {0} has uninitialized Rule property.", nt.Name);
        return;
      }
      //check all child elements
      foreach (BnfTermList elemList in nt.Rule.Data)
        for (int i = 0; i < elemList.Count; i++) {
          ABnfTerm child = elemList[i];
          if (child == null){ 
            ThrowError("Rule for NonTerminal {0} contains null as an operand in position {1} in one of productions.", nt, i);
            continue; //for i loop 
          }
          //Check for nested expression - convert to non-terminal
          BnfExpression expr = child as BnfExpression;
          if (expr != null) {
            child = new NonTerminal(null, expr);
            elemList[i] = child;
          }
          CollectAllRecursive(child);
        }
    }//method
    

    private void ThrowError(string message, params object[] values) {
      if (values != null && values.Length > 0)
        message = string.Format(message, values);
      throw new ApplicationException(message);
    }

    #endregion

        
  }//class

}//namespace
