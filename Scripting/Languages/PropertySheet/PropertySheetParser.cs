//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System;
    using System.Collections.Generic;
    using Spec;
    using Utility;

    public class PropertySheetParser {
        private readonly string PropertySheetText;

        protected PropertySheetParser(string text) {
            PropertySheetText = text;
        }

        protected static Token? SkipToNext(ref List<Token>.Enumerator enumerator) {
            Token? token = null;

            do {
                if (!enumerator.MoveNext())
                    return null;

                token = enumerator.Current;

                switch (token.Value.Type) {
                        // regardless where we are, skip over whitespace, etc.
                    case TokenType.WhiteSpace:
                    case TokenType.LineComment:
                    case TokenType.MultilineComment:
                        continue;
                }
                break;
            } while (true);
            return token;
        }

        protected PropertySheet Parse() {
            var tokenStream = PropertySheetTokenizer.Tokenize(PropertySheetText);
            var state = ParseState.Global;
            var enumerator = tokenStream.GetEnumerator();
            Token token;

            var propertySheet = new PropertySheet();

            var rule = new Rule();
            var property = new RuleProperty();
            List<string> valueCollection= null;
            var depth = 0;

            enumerator.MoveNext();

            do {
                token = enumerator.Current;

                switch (token.Type) {
                        // regardless where we are, skip over whitespace, etc.
                    case TokenType.WhiteSpace:
                    case TokenType.LineComment:
                    case TokenType.MultilineComment:
                        continue;
                }

                switch (state) {
                    case ParseState.Global:
                        switch (token.Type) {
                            case TokenType.Identifier: // look for identifier as the start of a selector
                                state = ParseState.Selector;
                                rule.Selector = token.Data;
                                continue;

                            case TokenType.Dot:
                                state = ParseState.SelectorDot;
                                rule.Selector = "*";
                                // take next identifier as the classname
                                continue;

                            case TokenType.Pound:
                                state = ParseState.SelectorPound;
                                rule.Selector = "*";
                                // take next identifier as the id
                                continue;

                            case TokenType.Semicolon:
                                // tolerate extra semicolons.
                                continue;
                            default:
                                throw new Exception("Unexpected Token in stream [Global]:" + token.Data);
                        }

                    case ParseState.Selector:
                        switch (token.Type) {
                            case TokenType.Dot:
                                state = ParseState.SelectorDot;
                                continue;

                            case TokenType.Pound:
                                state = ParseState.SelectorPound;
                                continue;

                            case TokenType.SelectorParameter:
                                rule.Parameter = token.Data;
                                continue;

                            case TokenType.OpenBrace:
                                state = ParseState.InRules;
                                continue;

                            default:
                                Console.WriteLine("RULE {0}", rule.FullSelector);
                                throw new Exception("Unexpected Token in stream [Selector]:" + token.Data);
                        }

                    case ParseState.SelectorDot:
                        switch (token.Type) {
                            case TokenType.Identifier:
                                rule.Class = token.Data;
                                state = ParseState.Selector;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [SelectorDot]:" + token.Data);
                        }

                    case ParseState.SelectorPound:
                        switch (token.Type) {
                            case TokenType.Identifier:
                                rule.Id = token.Data;
                                state = ParseState.Selector;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [SelectorPound]:" + token.Data);
                        }

                    case ParseState.InRules:
                        switch (token.Type) {
                            case TokenType.Semicolon: // extra semicolons are tolerated.
                                continue;

                            case TokenType.StringLiteral:
                            case TokenType.Identifier:
                                property.Name = token.Data;
                                state = ParseState.HaveRuleName;
                                continue;

                            case TokenType.CloseBrace: // extra semicolons are tolerated.
                                propertySheet._rules.Add(rule.FullSelector, rule);
                                rule = new Rule();
                                state = ParseState.Global;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [InRules]:" + token.Data);
                        }

                    case ParseState.HaveRuleName:
                        switch (token.Type) {
                            case TokenType.Colon:
                                state = ParseState.HaveRuleSeparator;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleName]:" + token.Data);
                        }

                    case ParseState.HaveRuleSeparator:
                        switch (token.Type) {
                            case TokenType.StringLiteral:
                            case TokenType.NumericLiteral:
                                state = ParseState.HaveRuleValue;
                                if ("@Literal" == token.RawData) {
                                    property.LValue = token.Data;
                                }
                                else {
                                    
                                    property.Value = token.Data;
                                }
                                continue;

                            case TokenType.Identifier:
                                state = ParseState.HaveRuleValue;
                                property.LValue = token.Data;
                                continue;

                            case TokenType.OpenBrace:
                                valueCollection = new List<string>();
                                state = ParseState.InRuleCollection;
                                continue;

                            case TokenType.OpenParenthesis:
                                depth = 1;
                                property.Expression = string.Empty;
                                state = ParseState.InRuleExpression;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleSeparator]:" + token.Data);
                        }
                    case ParseState.InRuleExpression:
                        switch (token.Type) {
                            case TokenType.CloseParenthesis:
                                depth--;
                                if (depth == 0) {
                                    state = ParseState.HaveRuleCompleted;
                                    continue;
                                }
                                break;
                            case TokenType.OpenParenthesis:
                                depth++;
                                break;
                        }
                        property.Expression += token.Data;
                        continue;
                        

                    case ParseState.InRuleCollection:
                        switch (token.Type) {
                            case TokenType.StringLiteral:
                            case TokenType.NumericLiteral:
                            case TokenType.Identifier:
                                valueCollection.Add(token.Data);
                                state = ParseState.HaveCollectionValue;
                                continue;

                            case TokenType.CloseBrace:
                                property.Values = valueCollection;
                                state = ParseState.HaveRuleCompleted;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [InRuleCollection]:" + token.Data);
                        }

                    case ParseState.HaveCollectionValue: 
                        switch (token.Type) {
                            case TokenType.Comma:
                                state = ParseState.InRuleCollection;
                                continue;
                            case TokenType.CloseBrace:
                                property.Values = valueCollection;
                                state = ParseState.HaveRuleCompleted;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveCollectionValue]:" + token.Data);
                        }

                    case ParseState.HaveRuleValue:
                        switch (token.Type) {
                            case TokenType.Equal:
                                state = ParseState.HaveRuleEquals;
                                continue;

                            case TokenType.Dot:
                                var t = SkipToNext(ref enumerator);

                                if( !t.HasValue ) {
                                    throw new Exception("Unexpected end of Token stream [HaveRuleValue]");
                                }
                                token = t.Value;

                                if (token.Type == TokenType.Identifier || token.Type == TokenType.NumericLiteral) {
                                    property.Value = property.Value + "." + token.Data;
                                }
                                else 
                                    throw new Exception("Expected Identifier or NumericLiteral after Dot [HaveRuleValue]");
                                continue;
                           
                            case TokenType.Semicolon:
                                state = ParseState.InRules;
                                rule.Properties.Add(property);
                                property = new RuleProperty();
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleValue]:" + token.Data);
                        }

                    case ParseState.HaveRuleEquals:
                        switch (token.Type) {
                            case TokenType.Identifier:
                            case TokenType.StringLiteral:
                            case TokenType.NumericLiteral:
                                state = ParseState.HaveRuleCompleted;
                                property.RValue = token.Data;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleEquals]:" + token.Data);
                        }

                    case ParseState.HaveRuleCompleted:
                        switch (token.Type) {
                            case TokenType.Semicolon:
                                state = ParseState.InRules;
                                rule.Properties.Add(property);
                                property = new RuleProperty();
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleCompleted]:" + token.Data);
                        }
                }
            } while (enumerator.MoveNext());
            return propertySheet;
        }

        public static PropertySheet Parse(string propertySheetText) {
            var p = new PropertySheetParser(propertySheetText);
            return p.Parse();
        }

        #region Nested type: ParseState

        private enum ParseState {
            Global,
            Selector,
            SelectorDot,
            SelectorPound,
            InRules,
            HaveRuleName,
            HaveRuleSeparator,
            HaveRuleValue,
            HaveRuleCompleted,
            HaveRuleEquals,
            InRuleCollection,
            HaveCollectionValue,
            InRuleExpression,

        }

        #endregion
    }
}