using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Borealis;

public static class MathValidator
{
    /// <summary>
    /// Represents a mathematical operation, such as addition or cosine
    /// </summary>
    private class MathOp
    {
        public int CharIndex = -1;
        public string Word;
        public List<MathOp> Arguments = new List<MathOp>();

        public MathOp(string word, int index)
        {
            Word = word;
            CharIndex = index;
        }

        public MathOp(string word, int index, MathOp argument)
        {
            Word = word;
            CharIndex = index;
            Arguments.Add(argument);
        }

        public MathOp(string word, int wordIndex, string argument, int argIndex)
        {
            Word = word;
            CharIndex = wordIndex;
            Arguments.Add(new MathOp(argument, argIndex));
        }
    }

    private class Token
    {
        /// <summary>
        /// indicates it can be discarded when traversed over
        /// </summary>
        public bool Discard = false;

        /// <summary>
        /// indicates the text is already in validated form
        /// </summary>
        public bool IsProcessed = false;
        public string Text;
        public int CharOffset;

        public Token(string text, int charOffset)
        {
            Text = text;
            CharOffset = charOffset;
        }
    }

    private class MathWord {
        public string Word, Formula, Description;
    }

    private static readonly Dictionary<string, MathWord> _ValidMathWords = buildWordLookup((JsonArray) JsonArray.Parse(File.ReadAllText("GpuCode/Reference/MathFunctions.json")));
    private static readonly Regex _StartNumberRegex = new Regex("[\\d\\.-]");
    private static readonly Regex _FullNumberRegex = new Regex("-?(\\d[_\\d]*|[_\\d]*\\.[\\d_]+)");
    private static readonly Regex _OperatorRegEx = new Regex("[*/^%+-]");
    private static readonly Regex _SpaceRegEx = new Regex("\\s+");
    private static readonly Regex _TokenRegEx = new Regex("[^\\s,\\(\\)]+");
    private static readonly Regex _StartWordRegEx = new Regex("[A-Za-z0-9-]");
    private static readonly Regex _FullWordRegEx = new Regex("-?[A-Za-z0-9]+");


    public static string CleanMathExpression(string mathExpression)
    {
        mathExpression = mathExpression.Replace("_", "");
        mathExpression = Regex.Replace(mathExpression, "\\s+", " ");

        return mathExpression;
    }

    /// <summary>
    /// Validates a user-input math expression and cleans it up.
    /// </summary>
    /// <param name="mathExpression"></param>
    /// <returns>The C-compliant equivalent of the given math expression</returns>
    /// <exception cref="ApplicationException"></exception>
    public static string ValidateMathExpression(string mathExpression) //, string[] specialWords = Array.Empty<string>())
    {
        // ***** validate characters *****

        var validCharsRegEx = new Regex("[^a-zA-Z0-9. _()*/%+-]");

        var invalidCharsFound = validCharsRegEx.Matches(mathExpression);

        if (invalidCharsFound.Count > 0)
        {
            var positions = invalidCharsFound.Select((m) => m.Index);
            var invalidChars = invalidCharsFound.Select((m) => m.Value[0])
                .Distinct().Select((c) => {
                    if (char.IsSymbol(c) || char.IsPunctuation(c))
                    {
                        return c.ToString();
                    }
                    else
                    {
                        return "0x" + char.GetNumericValue(c).ToString("X2");
                    }
                });
            
            string exampleDescription = "";
            if (positions.Count() <= 5)
            {
                exampleDescription = $" ({String.Join(",", invalidChars)})";
            }

            string positionDescription = $"from positions {positions.Min()} to {positions.Max()}";
            if (positions.Count() == 1)
            {
                positionDescription = $"at position {positions.FirstOrDefault()}";
            }
            else if (positions.Count() == 2)
            {
                positionDescription = $"at positions {positions.Min()} and {positions.Max()}";
            }

            throw new ApplicationException($"Found invalid characters{exampleDescription} in math expression {positionDescription}.");
        }


        // ***** validate words and arguments *****

        var cleanEpression = validateSyntax(mathExpression);

        return cleanEpression;
    }

    public static bool IsValidMathWord(string word)
    {
        return _ValidMathWords.ContainsKey(word);
    }




    private static Dictionary<string, MathWord> buildWordLookup(JsonArray jsonArray)
    {
        var nodeList = jsonArray.Select((n) => n.AsObject());
        var dictionary = new Dictionary<string, MathWord>();

        foreach(var node in nodeList)
        {
            var mathWord = new MathWord();
            mathWord.Word = node["word"].AsValue().ToString();
            mathWord.Formula = node["formula"].AsValue().ToString();
            mathWord.Description = node["description"].AsValue().ToString();

            dictionary.Add(mathWord.Word, mathWord);
        }

        return dictionary;
    }

    /// <summary>
    /// Traverses a list of tokens and performs an action on each one that has IsProcessed = false. Note that adjacent tokens may or may not be processed. You can set tokens as IsProcessed or Discard.
    /// </summary>
    /// <param name="tokenList"></param>
    /// <param name="action">a function to call. Takes 3 arguments: the token, index in list, and the list.</param>
    /// <returns>A new list of all tokens not set to Discard = true. Some may still have IsProcessed = false</returns>
    private static List<Token> processTokenList(List<Token> tokenList, Action<Token, int, List<Token>> action)
    {
        for(int t = 0; t < tokenList.Count; t++)
        {
            Token token = tokenList[t];

            // ignore already processed items
            if (token.Discard || token.IsProcessed)
            {
                continue;
            }

            action(token, t, tokenList);
        }

        return tokenList.Where((o) => !o.Discard).ToList();
    }

    /// <summary>
    /// Scans a list of tokens for the given math operators. When found, tokens are processed into a new list of tokens.
    /// </summary>
    /// <param name="inputList"></param>
    /// <param name="mathOperators"></param>
    /// <returns>The new list of tokens, some of which might still need to be processed by other operators.</returns>
    private static List<Token> scanForOperators(List<Token> inputList, string mathOperators)
    {
        return processTokenList(inputList, (token, t, tokenList) =>
        {
            string text = token.Text;
            if (text.Length > 1 || !mathOperators.Contains(text))
            {
                return;
            }

            if (t == 0)
            {
                throw new ApplicationException($"Operator '{text}' found at position {token.CharOffset} without a number to the left.");
            }

            if (t + 1 == tokenList.Count)
            {
                throw new ApplicationException($"Operator '{text}' found at position {token.CharOffset} without a number to the right.");
            }

            var operand1 = tokenList[t-1];
            if (!operand1.IsProcessed)
            {
                throw new ApplicationException($"Non-numerical operand found to the left of '{text}' at position {operand1.CharOffset}.");
            }

            var operand2 = tokenList[t+1];
            if (!operand2.IsProcessed)
            {
                throw new ApplicationException($"Non-numerical operand found to the right of '{text}' at position {operand2.CharOffset}.");
            }            

            token.Text = $"{operand1.Text} {text} {operand2.Text}";
            token.IsProcessed = true;
            operand1.Discard = true;
            operand2.Discard = true;
        });
    }

    /// <summary>
    /// finds the index of a closing parenthesis, or the comma for the next argument in a list of arguments
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="startIndex"></param>
    /// <param name="stopOnComma"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    private static int findClosure(string expression, int startIndex, bool stopOnComma = false)
    {
        int i = startIndex;

        if (i == expression.Length)
        {
            throw new ApplicationException("Cannot end expression with open parenthesis.");
        }
        int level = 0;
        char c = expression[i];
        while(c != ')' || level > 0)
        {
            if (c == '(')
                level++;
            else if (c == ')')
                level--;
            else if (stopOnComma && c == ',' && level == 0)
                break;

            c = expression[i++];
            
            if (i == expression.Length)
                break;
        }

        return i;
    }

    /// <summary>
    /// Scans a string and separates it into "tokens" that represent unprocessed numbers, operators, functions, or parentheses.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="charOffset">the offset from the original expression, so that error messages and tokens use correct positiion values</param>
    /// <param name="stopOnCommas">set to true to tokenize function parameters</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    private static List<Token> tokenizeExpression(string expression, int charOffset = 0, bool stopOnCommas = false)
    {
        Match numberMatch, operatorMatch, spaceMatch, wordMatch;
        List<Token> tokens = new List<Token>();

        // break into tokens around parentheses and white space

        int i = 0, a, b;
        char c;
        string subExpression;
        bool previousNegative = false; // keeps track of whether the last "-" is a negative sign
        while(i < expression.Length)
        {
            // skip white space 
            c = expression[i];
            if (_SpaceRegEx.Match(c.ToString(), i).Success)
            {
                spaceMatch = _SpaceRegEx.Match(expression, i);
                i += spaceMatch.Length;
            }
            if (i == expression.Length)
            {
                break;
            }
            
            c = expression[i];
            switch(c)
            {
                case '(': // open paren -- look for closed paren
                    
                    if (stopOnCommas)
                    {
                        while(c != ')')
                        {
                            a = ++i; // ignore open parenthesis or comma
                            if (a == expression.Length)
                            {
                                throw new ApplicationException($"Unclosed parenthesis at end of expression.");
                            }
                            i = findClosure(expression, i, true);
                            c = expression[i];
                            if (c != ')' && c != ',')
                            {
                                throw new ApplicationException($"Unexpected end of argument at end of expression.");
                            }
                            
                            b = i++;
                            if (a == b)
                            {
                                throw new ApplicationException($"Empty argument provided at position {b}.");
                            }
                            subExpression = expression.Substring(a, b - a);
                            
                            tokens.Add(new Token(subExpression, a));
                        }
                    }
                    else
                    {
                        a = i++;
                        i = findClosure(expression, i, false);
                        c = expression[i];
                        if (c != ')')
                        {
                            throw new ApplicationException($"Unclosed parenthesis at end of expression.");
                        }
                        
                        b = i++;
                        if (a == b)
                        {
                            throw new ApplicationException($"Empty expression provided at position {b}.");
                        }
                        subExpression = expression.Substring(a, b - a);
                        if (previousNegative)
                        {
                            tokens.Add(new Token($"-{subExpression})", a-1));
                        }
                        else
                        {
                            tokens.Add(new Token(subExpression, a));
                        }
                    }

                    break;

                case ')':
                    throw new ApplicationException($"Unexpected ')' found at position {charOffset + i}.");

                case ',':
                    throw new ApplicationException($"Unexpected comma found at position {charOffset + i}.");

                default:
                    // check for number
                    if (_StartNumberRegex.IsMatch(c.ToString()))
                    {
                        numberMatch = _FullNumberRegex.Match(expression, i);

                        // add token if valid
                        if (numberMatch.Success)
                        {
                            tokens.Add(new Token(numberMatch.Value.Replace("_", ""), i));
                            i += numberMatch.Length;
                        }

                        break;
                    }
                        // check for math functions and other key words
                    else if (_StartWordRegEx. IsMatch(c.ToString()))
                    {
                        wordMatch = _FullWordRegEx.Match(expression, i);

                        // add token if valid
                        if (wordMatch.Success)
                        {
                            tokens.Add(new Token(wordMatch.Value, i));

                            i += wordMatch.Length;
                        }

                        break;
                    }
                        // check for operator
                    else if (_OperatorRegEx.IsMatch(c.ToString()))
                    {
                        // handle minus signs carefully
                        if (c == '-')
                        {
                            a = (i == 0 ? ' ' : expression[i-1]);
                            b = (i+1 == expression.Length ? '\0' : expression[i+1]);

                            // if white-space directly before and no white-space directly after
                            if (_SpaceRegEx.IsMatch(a.ToString()) && !_SpaceRegEx.IsMatch(b.ToString()))
                            {
                                // treat as negative sign
                                previousNegative = true;

                                i++;
                            }
                            else
                            {
                                // treat as substraction operator
                                tokens.Add(new Token("-", i++));
                            }

                            break;
                        }
                            // handle other operators
                        else
                        {
                            tokens.Add(new Token(c.ToString(), i++));
                            break;
                        }
                    }
                    else
                    {
                        string printableChar;
                        if (char.IsSymbol(c) || char.IsPunctuation(c))
                        {
                            printableChar = c.ToString();
                        }
                        else
                        {
                            printableChar = "0x" + char.GetNumericValue(c).ToString("X2");
                        }

                        throw new ApplicationException($"Unexpected character '{printableChar}' found at position {charOffset + i}.");
                    }
            } // end switch
        } // end while

        return tokens;
    }

    /// <summary>
    /// validates math operations in the given expression starting at the given charIndex. Throws an error if invalid.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="charOffset">optional parameter used in recursion, representing offset from original expression</param>
    /// <returns>Returns the C-compliant version of the expression</returns>
    private static string validateSyntax(string expression, int charOffset = 0)
    {
        // tokenize to handle white space and group parentheses

        List<Token> tokenList = tokenizeExpression(expression);

        if (tokenList.Count == 0)
        {
            throw new ApplicationException($"Empty expression found at position {charOffset}.");
        }

        // process numbers, constants, parentheses, and functions

        tokenList = processTokenList(tokenList, (token, t, tokenList) =>
        {
            // handle numbers
            if (_FullNumberRegex.IsMatch(token.Text))
            {
                token.IsProcessed = true;
                return;
            }

            var negative = false;
            if (token.Text.StartsWith("-"))
            {
                negative = true;
                token.Text = token.Text.Substring(1);
            }

            // handle parentheses
            if (token.Text.StartsWith("("))
            {
                token.Text = "(" + validateSyntax(token.Text.Substring(1, token.Text.Length - 2), token.CharOffset+1) + ")";
                token.IsProcessed = true;
            }
            else if (IsValidMathWord(token.Text))
            {
                var mathWord = _ValidMathWords[token.Text];

                // handle constants and variables
                if (mathWord.Formula == token.Text)
                {
                    token.IsProcessed = true;
                }
                    // handle math functions
                else if (mathWord.Formula.Contains("("))
                {
                    // get parameter count
                    var paramCount = 1 + mathWord.Formula.Count((c) => (c == ','));

                    if (t + 1 == tokenList.Count || !tokenList[t+1].Text.StartsWith("("))
                    {
                        throw new ApplicationException($"Function {mathWord}() requires {paramCount} arguments but found none at position {token.CharOffset + token.Text.Length}.");
                    }

                    // tokenize parameters
                    var paramToken = tokenList[t+1];
                    var paramTokens = tokenizeExpression(paramToken.Text, paramToken.CharOffset, true);

                    if (paramTokens.Count != paramCount)
                    {
                        throw new ApplicationException($"Function {mathWord}() requires {paramCount} arguments but found {paramTokens.Count} at position {token.CharOffset + token.Text.Length}.");
                    }

                    foreach(var param in paramTokens)
                    {
                        param.Text = validateSyntax(param.Text, param.CharOffset);
                        param.IsProcessed = true;
                    }

                    string paramString = String.Join(",", paramTokens.Select((p) => p.Text));
                    token.Text = $"{token.Text}({paramString})";

                    token.IsProcessed = true;
                    paramToken.Discard = true;
                }
            }
            else
            {
                throw new ApplicationException($"Unrecognized word '{token.Text}' found at position {token.CharOffset}");
            }

            if (!token.Discard && negative)
            {
                token.Text = $"(-1 * {token.Text})";
            }
        });

        // scan for operators

        tokenList = scanForOperators(tokenList, "^");
        tokenList = scanForOperators(tokenList, "*/");


        // - handle implied multiplication
        for(int t = 0; t < tokenList.Count - 1; t++)
        {
            Token operand1 = tokenList[t];
            Token operand2 = tokenList[t+1];

            if (operand1.IsProcessed && operand2.IsProcessed)
            {
                operand1.Text = $"{operand1.Text} * {operand2.Text}";
                tokenList.Remove(operand2);
            }
        }


        tokenList = scanForOperators(tokenList, "+-");


        // final sanity checks

        var unprocessed = tokenList.Where((o) => !o.IsProcessed);
        if (unprocessed.Count() > 0)
        {
            if (unprocessed.Count() == 1)
            {
                throw new ApplicationException($"Unprocessed token found at position {unprocessed.FirstOrDefault().CharOffset}.");
            }
            else
            {
                int min = unprocessed.Min((o) => o.CharOffset);
                int max = unprocessed.Max((o) => o.CharOffset);
                throw new ApplicationException($"Unprocessed tokens found from positions {min} to {max}.");
            }
        }

        if (tokenList.Count > 1)
        {
            int max = unprocessed.Max((o) => o.CharOffset);
            throw new ApplicationException($"Second expression found at position {max}.");
        }

        return tokenList[0].Text;
    }
}
