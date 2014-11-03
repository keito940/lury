//
// Lexer.cs
//
// Author:
//       Tomona Nanase <nanase@users.noreply.github.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014 Tomona Nanase
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lury.Compiling.Logger;

namespace Lury.Compiling
{
    class Lexer : yyParser.yyInput
    {
        #region -- Private Fields --

        private TokenizedToken[] tokens;
        private int position = -1;
        private Stack<int> indentStack;
        private OutputLogger logger;
        private string sourceCode;

        #endregion

        #region -- Constructor --

        public Lexer(OutputLogger logger, string sourceCode)
        {
            this.logger = logger;
            this.indentStack = new Stack<int>();
            this.indentStack.Push(0);

            this.sourceCode = sourceCode;

            this.tokens = this.Tokenize().ToArray();
        }

        #endregion

        #region -- Public Methods --

        public bool Advance()
        {
            if (this.tokens.Length <= this.position + 1)
            {
                return false;
            }
            else
            {
                this.position++;
                return true;
            }
        }

        public yyParser.IToken GetToken()
        {
            return this.tokens[this.position];
        }

        public object GetValue()
        {
            return 0;
        }

        #endregion

        #region -- Private Methods --

        private IEnumerable<TokenizedToken> Tokenize()
        {
            int index = 0;
            int inputLength = this.sourceCode.Length;
            bool newLine = false;
            TokenizedToken token = null;

            while (index < inputLength)
            {
                int length;

                if (!this.SkipComments(this.sourceCode, index, out length))
                {
                    if (newLine)
                    {
                        Regex space = new Regex(@"[\t\f\v\x85\p{Z}]*");
                        Match match = space.Match(this.sourceCode, index);

                        int indentSize = match.Length;

                        if (this.indentStack.Peek() != indentSize)
                        {
                            if (this.indentStack.Peek() < indentSize)
                            {
                                this.indentStack.Push(indentSize);
                                yield return new TokenizedToken(this.sourceCode.Substring(index, indentSize), Token.Indent, this.indentStack.Count - 1, index);
                            }
                            else
                            {
                                do
                                {
                                    this.indentStack.Pop();
                                    yield return new TokenizedToken("", Token.Dedent, this.indentStack.Count - 1, index);
                                } while (this.indentStack.Count > 0 &&
                                         this.indentStack.Peek() != indentSize);

                                if (this.indentStack.Count == 0)
                                {
                                    this.ReportError(ErrorCategory.Lexer_CannotTokenize, index);
                                    yield break;
                                }
                            }
                        }

                        index += indentSize;
                        newLine = false;
                    }

                    if ((token = this.TokenizeFor(TokenPairs.Operators, this.sourceCode, index, out length)) != null)
                        yield return token;
                    else if ((token = this.TokenizeFor(TokenPairs.Delimiters, this.sourceCode, index, out length)) != null)
                        yield return token;
                    else if ((token = this.TokenizeFor(TokenPairs.Keywords, this.sourceCode, index, out length)) != null)
                        yield return token;
                    else if ((token = this.TokenizeFor(TokenPairs.Operands, this.sourceCode, index, out length)) != null)
                        yield return token;
                    else
                    {
                        this.ReportError(ErrorCategory.Lexer_CannotTokenize, index);
                        yield break;
                    }
                }

                if (token != null)
                    index += length;

                if (index > inputLength)
                    throw new ArgumentOutOfRangeException();

                if (token.TokenNumber == Token.NewLine)
                    newLine = true;
            }

            if (this.indentStack.Count == 0)
            {
                this.ReportError(ErrorCategory.Lexer_IllegalIndent, index);
                yield break;
            }
            else if (this.indentStack.Peek() != 0)
                do
                {
                    this.indentStack.Pop();
                    yield return new TokenizedToken("", Token.Dedent, this.indentStack.Count - 1, index);
                } while (this.indentStack.Peek() != 0);
        }

        private bool SkipComments(string input, int index, out int length)
        {
            length = 0;

            foreach (var comment in TokenPairs.Comments)
            {
                var match = comment.MatchBeforeSpace(input, index);

                if (match.Success && match.Groups[1].Index == index)
                {
                    length = match.Length;
                    return true;
                }
            }

            return false;
        }

        private TokenizedToken TokenizeFor(IEnumerable<TokenPair> tokenPair, string input, int index, out int length)
        {
            length = 0;

            foreach (var pair in tokenPair)
            {
                var match = pair.MatchBeforeSpace(input, index);

                if (match.Success && match.Groups[1].Index == index)
                {
                    length = match.Length;
                    return new TokenizedToken(match.Groups[1].Value, pair.TokenNumber, this.indentStack.Count - 1, index);
                }
            }

            return null;
        }

        private void ReportError(ErrorCategory error, int index, string code = null, string appendix = null)
        {
            this.logger.Error(error,
                              sourceCode: this.sourceCode,
                              code: code,
                              position: this.sourceCode.GetPositionByIndex(index),
                              appendix: appendix);
        }

        #endregion
    }
}

