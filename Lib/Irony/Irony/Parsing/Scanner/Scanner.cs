#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace sones.Lib.Frameworks.Irony.Parsing {

  //Scanner class. The Scanner's function is to transform a stream of characters into aggregates/words or lexemes, 
  // like identifier, number, literal, etc. 

  public class Scanner  {
    #region Properties and Fields: Data, _source, _context, _caseSensitive, _currentToken
    ScannerData _data;
    Grammar _grammar;
    CompilerContext _context;
    //buffered tokens can come from expanding a multi-token, when Terminal.TryMatch() returns several tokens packed into one token
    TokenList _bufferedTokens = new TokenList();
    public IEnumerable<Token> UnfilteredStream;
    public IEnumerable<Token> FilteredStream;
    public IEnumerator<Token> FilteredTokenEnumerator;

    public ISourceStream Source {
      get { return _source; }
    } ISourceStream _source;

    #endregion

    public Scanner(ScannerData data) {
      _data = data;
      _grammar = _data.Language.Grammar;
    }

    public void BeginScan(CompilerContext context) {
      _context = context;
      _bufferedTokens.Clear();
      //create streams
      FilteredStream = UnfilteredStream = CreateUnfilteredTokenStream();
      //chain all token filters
      foreach (TokenFilter filter in _data.TokenFilters) {
        FilteredStream = filter.BeginFiltering(context, FilteredStream);
      }
      FilteredTokenEnumerator = FilteredStream.GetEnumerator(); 
    }

    public void SetSource(String text) {
      _source = new SourceStream(text, 0);
      _nextNewLinePosition = text.IndexOfAny(_data.LineTerminators);
    }
    public void SetPartialSource(String text, int offset) {
      if (_source == null)
        _source = new SourceStream(text, offset);
      else {
        _source.Text = text;
        _source.Position = offset;
      }
      _nextNewLinePosition = _source.Text.IndexOf('\n', offset);
    }

    public Token GetToken() {
      //not in preview; check if there are previewed tokens
      if (!_inPreview && _previewTokens.Count > 0) {
        var result = _previewTokens[0];
        _previewTokens.RemoveAt(0);
        return result;
      }
      //get new token from pipeline
      if (!FilteredTokenEnumerator.MoveNext()) return null;
      var token = FilteredTokenEnumerator.Current;
      if (_inPreview)
        _previewTokens.Add(token);
      else 
        _context.CurrentParseTree.Tokens.Add(token);
      return token;
    }

    //This is iterator method, so it returns immediately when called directly
    // returns unfiltered, "raw" token stream
    private Token _currentToken; //it is used only in BeginScan iterator, but we define it as a field to avoid creating local state in iterator
    private IEnumerable<Token> CreateUnfilteredTokenStream() {
      //We don't do "while(!_source.EOF())... because on EOF() we need to continue and produce EOF token 
      //  and then do "yield break" - see below
      while (true) {  
        _currentToken = ReadToken();
        _context.OnTokenCreated(_currentToken);
        yield return _currentToken;
        //Don't yield break, continue returning EOF
       // if (_currentToken != null && _currentToken.Terminal == _grammar.Eof)
         // yield break;
      }//while
    }// method

    #region VS Integration methods
    //Use this method for VS integration; VS language package requires scanner that returns tokens one-by-one. 
    // Start and End positions required by this scanner may be derived from Token : 
    //   start=token.Location.Position; end=start + token.Length;
    public Token VsReadToken(ref int state) {
      _context.ScannerState.Value = state;
      if (_source.EOF()) return null;
      
      Token result;
      if (state == 0)
        result = ReadToken();
      else {
        Terminal term = _data.MultilineTerminals[_context.ScannerState.TerminalIndex - 1];
        result = term.TryMatch(_context, _source); 
      }
      //set state value from context
      state = _context.ScannerState.Value;
      if (result != null && result.Terminal == _grammar.Eof)
        result = null; 
      return result;
    }
    public void VsSetSource(String text, int offset) {
      SetPartialSource(text, offset); 
    }
    #endregion

    #region Reading token
    private Token ReadToken() {
      if (_bufferedTokens.Count > 0) 
        return ReadBufferedToken(); 
      //1. Skip whitespace. We don't need to check for EOF: at EOF we start getting 0-char, so we'll get out automatically
      while (_grammar.WhitespaceChars.IndexOf(_source.CurrentChar) >= 0)
        _source.Position++;
      //That's the token start, calc location (line and column)
      ComputeNewTokenLocation();
      //Check for EOF
      if (_source.EOF())
        return CreateFinalToken(); 
      //Find matching terminal
      // First, try terminals with explicit "first-char" prefixes, selected by current char in source
      var terms = SelectTerminals(_source.CurrentChar);
      var token = MatchTerminals(terms);
      //If no token, try FallbackTerminals
      if (token == null && terms != _data.FallbackTerminals && _data.FallbackTerminals.Count > 0)
        token = MatchTerminals(_data.FallbackTerminals); 
      //If we don't have a token from registered terminals, try Grammar's method
      if (token == null) 
        token = _grammar.TryMatch(_context, _source);
      if (token is MultiToken)
        token = UnpackMultiToken(token); 
      //If we have normal token then return it
      if (token != null && !token.IsError()) {
        //set position to point after the result token
        _source.Position = _source.TokenStart.Position + token.Length;
        return token;
      } 
      //we have an error: either error token or no token at all
      if (token == null)  //if no token then create error token
        token = _context.CreateErrorTokenAndReportError(_source.TokenStart, _source.CurrentChar.ToString(), "Invalid character: '{0}'", _source.CurrentChar);
      Recover();
      return token;
    }//method

    private Token ReadBufferedToken() {
      Token tkn = _bufferedTokens[0];
      _bufferedTokens.RemoveAt(0);
      return tkn;
    }

    //If token is MultiToken then push all its child tokens into _bufferdTokens and return first token in buffer
    private Token UnpackMultiToken(Token token) {
      var mtoken = token as MultiToken;
      if (mtoken == null) return null; 
      for (int i = mtoken.ChildTokens.Count-1; i >= 0; i--)
        _bufferedTokens.Insert(0, mtoken.ChildTokens[i]);
      return ReadBufferedToken(); 
    }
    
    //returns EOF token or NewLine token if flag NewLineBeforeEOF set
    private Token CreateFinalToken() {
      var result = new Token(_grammar.Eof, _source.TokenStart, string.Empty, _grammar.Eof.Name);
      //check if we need extra newline before EOF
      bool currentIsNewLine = _currentToken != null && _currentToken.Terminal == _grammar.NewLine;
      if (_grammar.FlagIsSet(LanguageFlags.NewLineBeforeEOF) && !currentIsNewLine) {
        _bufferedTokens.Insert(0, result); //put it into buffer
        result = new Token(_grammar.NewLine, _currentToken.Location, "\n", null);
      }//if AutoNewLine
      return result;
    }

    private Token MatchTerminals(TerminalList terminals) {
      Token result = null;
      foreach (Terminal term in terminals) {
        // Check if the term has lower priority that result token we already have; 
        //  if term.Priority is lower then we don't need to check anymore, higher priority wins
        // Note that terminals in the list are sorted in descending priority order
        if (result != null && result.Terminal.Priority > term.Priority)
          break;
        //Reset source position and try to match
        _source.Position = _source.TokenStart.Position;
        Token token = term.TryMatch(_context, _source);
        //Take this token as result only if we don't have anything yet, or if it is longer token than previous
        if (token != null && (token.IsError() || result == null || token.Length > result.Length))
          result = token;
        if (result != null && result.IsError()) break;
      }
      return result; 
    }

    //list for filterered terminals
    private TerminalList _filteredTerminals = new TerminalList();
    //reuse single instance to avoid garbage generation
    private SelectTerminalArgs _selectedTerminalArgs = new SelectTerminalArgs(); 
    
    private TerminalList SelectTerminals(char current) {
      TerminalList termList;
      if (!_grammar.CaseSensitive)
        current = char.ToLower(current);
      if (!_data.TerminalsLookup.TryGetValue(current, out termList))
        termList = _data.FallbackTerminals;
      if (termList.Count <= 1)  return termList;

      //We have more than one candidate
      //First try calling grammar method
      _selectedTerminalArgs.SetData(_context, current, termList); 
      _grammar.OnScannerSelectTerminal(_selectedTerminalArgs);
      if (_selectedTerminalArgs.SelectedTerminal != null) {
        _filteredTerminals.Clear();
        _filteredTerminals.Add(_selectedTerminalArgs.SelectedTerminal);
        return _filteredTerminals;
      }
      // Now try filter them by checking with parser which terms it expects but do it only if we're not recovering or previewing
      if (_context.ParserIsRecovering || _inPreview)
        return termList;
      var parserState = _context.GetCurrentParserState();
      if (parserState == null) 
        return termList;
      //we cannot modify termList - it will corrupt the list in TerminalsLookup table; we make a copy
      _filteredTerminals.Clear();
      foreach(var term in termList) {
        if (parserState.ExpectedTerms.Contains(term) || _grammar.NonGrammarTerminals.Contains(term))
          _filteredTerminals.Add(term);
      }
      //Now, if filtered list is empty then ran into error. Don't report it as scanner error - scanner still has options, 
      // let parser report it
      if (_filteredTerminals.Count == 0)
        return termList;
      else 
        return _filteredTerminals;
    }//Select
    #endregion

    #region Error recovery
    private bool Recover() {
      try {
        _context.ScannerIsRecovering = true;
        _source.Position++;
        while (!_source.EOF()) {
          if (_data.ScannerRecoverySymbols.IndexOf(_source.CurrentChar) >= 0) return true;
          _source.Position++;
        }
        return false; 
      } finally {
        _context.ScannerIsRecovering = false; 
      }
    }
    #endregion 


    #region TokenPreview
    //Preview mode allows custom code in grammar to help parser decide on appropriate action in case of conflict
    // In preview mode, tokens returned by ReadToken are collected in _previewTokens list; after finishing preview
    //  the scanner "rolls back" to original position - either by directly restoring the position, or moving the preview
    //  tokens into _bufferedTokens list, so that they will read again by parser in normal mode.
    // See c# grammar sample for an example of using preview methods
    TokenList _previewTokens = new TokenList();
    SourceLocation _savedTokenStart;
    int _savedPosition;

    public bool InPreview {
      get { return _inPreview; }
    } bool _inPreview;

    //Switches Scanner into preview mode
    public void BeginPreview() {
      _inPreview = true;
      _savedTokenStart = _source.TokenStart;
      _savedPosition = _source.Position;
      _previewTokens.Clear();
    }

    //Ends preview mode
    public void EndPreview(bool keepPreviewTokens) {
      if (keepPreviewTokens)
        _bufferedTokens.InsertRange(0, _previewTokens); //insert previewed tokens into buffered list, so we don't recreate them again
      else 
        SetSourceLocation(_savedTokenStart, _savedPosition);
      _previewTokens.Clear();
      _inPreview = false;
    }
    #endregion

    //TODO: this is messed up, need to fix all code related to TokenStart and position and resetting it
    // problem: tokenStart is the position of "last" (previous) token, while position parameter is a position where next token will start
    public void SetSourceLocation(SourceLocation tokenStart, int position) {
      foreach (var filter in _data.TokenFilters)
        filter.OnSetSourceLocation(tokenStart); 
      _source.TokenStart = tokenStart;
      _source.Position = position; 
    }

    #region TokenStart calculations
    private int _nextNewLinePosition = -1; //private field to cache position of next \n character
    //Calculates the _source.TokenStart values (row/column) for the token which starts at the current position.
    // We just skipped the whitespace and about to start scanning the next token.
    internal void ComputeNewTokenLocation() {
      //cache values in local variables
      SourceLocation tokenStart = _source.TokenStart;
      int newPosition = _source.Position;
      string text = _source.Text;
      if (newPosition > text.Length - 1) 
        newPosition = text.Length - 1; 

      // Currently TokenStart field contains location (pos/line/col) of the last created token. 
      // First, check if new position is in the same line; if so, just adjust column and return
      //  Note that this case is not line start, so we do not need to check tab chars (and adjust column) 
      if (newPosition <= _nextNewLinePosition || _nextNewLinePosition < 0) {
        tokenStart.Column += newPosition - tokenStart.Position;
        tokenStart.Position = newPosition;
        _source.TokenStart = tokenStart;
        return;
      }
      //So new position is on new line (beyond _nextNewLinePosition)
      //First count \n chars in the string fragment
      int lineStart = _nextNewLinePosition;
      int nlCount = 1; //we start after old _nextNewLinePosition, so we count one NewLine char
      CountCharsInText(text, _data.LineTerminators, lineStart + 1, newPosition - 1, ref nlCount, ref lineStart);
      tokenStart.Line += nlCount;
      //at this moment lineStart is at start of line where newPosition is located 
      //Calc # of tab chars from lineStart to newPosition to adjust column#
      int tabCount = 0;
      int dummy = 0;
      char[] tab_arr = { '\t' };
      if (_source.TabWidth > 1)
        CountCharsInText(text, tab_arr, lineStart, newPosition - 1, ref tabCount, ref dummy);

      //adjust TokenStart with calculated information
      tokenStart.Position = newPosition;
      tokenStart.Column = newPosition - lineStart - 1;
      if (tabCount > 0)
        tokenStart.Column += (_source.TabWidth - 1) * tabCount; // "-1" to count for tab char itself

      //finally cache new line and assign TokenStart
      _nextNewLinePosition = text.IndexOfAny(_data.LineTerminators, newPosition);
      _source.TokenStart = tokenStart;
    }

    private void CountCharsInText(String text, char[] chars, int from, int until, ref int count, ref int lastPosition) {
      if (from >= until) return;
      if (until >= text.Length) until = text.Length - 1;
      while (true) {
        int next = text.IndexOfAny(chars, from, until - from + 1);
        if (next < 0) return;
        //CR followed by LF is one line terminator, not two; we put it here, just to cover for special case; it wouldn't break
        // the case when this function is called to count tabs
        bool isCRLF = (text[next] == '\n' && next > 0 && text[next - 1] == '\r');
        if (!isCRLF)
          count++; //count
        lastPosition = next;
        from = next + 1;
      }

    }
    #endregion


  }//class

}//namespace
