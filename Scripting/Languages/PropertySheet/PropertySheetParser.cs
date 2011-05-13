//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using Spec;
    using Utility;

    public class PropertySheetParser {
        private readonly string PropertySheetText;
        private readonly string Filename;

        protected PropertySheetParser(string text, string originalFilename) {
            PropertySheetText = text;
            Filename = originalFilename;
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
                                rule.Name = token.Data;
                                continue;

                            case TokenType.Dot:
                                state = ParseState.SelectorDot;
                                rule.Name = "*";
                                // take next identifier as the classname
                                continue;

                            case TokenType.Pound:
                                state = ParseState.SelectorPound;
                                rule.Name = "*";
                                // take next identifier as the id
                                continue;

                            case TokenType.Semicolon:
                                // tolerate extra semicolons.
                                continue;
                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 100", "Expected one of '.' , '#' or identifier");
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
                                state = ParseState.InRule;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 101", "Expected one of '.' , '#' , '[' or '{{' ." );
                        }

                    case ParseState.SelectorDot:
                        switch (token.Type) {
                            case TokenType.Identifier:
                                rule.Class = token.Data;
                                state = ParseState.Selector;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 102", "Expected identifier");
                        }

                    case ParseState.SelectorPound:
                        switch (token.Type) {
                            case TokenType.Identifier:
                                rule.Id = token.Data;
                                state = ParseState.Selector;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 103", "Expected identifier");
                        }

                    case ParseState.InRule:
                        switch (token.Type) {
                            case TokenType.Semicolon: // extra semicolons are tolerated.
                                continue;

                            case TokenType.StringLiteral:
                            case TokenType.Identifier:
                                property.Name = token.Data;
                                state = ParseState.HavePropertyName;
                                continue;

                            case TokenType.CloseBrace: // extra semicolons are tolerated.
                                propertySheet._rules.Add(rule.FullSelector, rule);
                                rule = new Rule();
                                state = ParseState.Global;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 104", "In rule, expected semi-colon ';', close-brace '}}' or value." );
                        }

                    case ParseState.HavePropertyName:
                        switch (token.Type) {
                            case TokenType.Colon:
                                state = ParseState.HavePropertySeparator;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 105", "Found rule property name, expected colon ':'." );
                        }

                    case ParseState.HavePropertySeparator:
                        switch (token.Type) {
                            case TokenType.StringLiteral:
                            case TokenType.NumericLiteral:
                                state = ParseState.HavePropertyValue;
                                if ("@Literal" == token.RawData) {
                                    property.LValue = token.Data;
                                }
                                else {
                                    property.RawValue = token.Data;
                                }
                                continue;

                            case TokenType.Identifier:
                                state = ParseState.HavePropertyValue;
                                property.LValue = token.Data;
                                continue;

                            case TokenType.OpenBrace:
                                valueCollection = new List<string>();
                                state = ParseState.InPropertyCollection;
                                continue;

                            case TokenType.OpenParenthesis:
                                depth = 1;
                                property.Expression = string.Empty;
                                state = ParseState.InPropertyExpression;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 106", "After rule property name, expected value, open-brace '{{' or open-parenthesis '('." );
                        }
                    case ParseState.InPropertyExpression:
                        switch (token.Type) {
                            case TokenType.CloseParenthesis:
                                depth--;
                                if (depth == 0) {
                                    state = ParseState.HavePropertyCompleted;
                                    continue;
                                }
                                break;
                            case TokenType.OpenParenthesis:
                                depth++;
                                break;
                        }
                        property.Expression += token.Data;
                        continue;
                        

                    case ParseState.InPropertyCollection:
                        switch (token.Type) {
                            case TokenType.StringLiteral:
                            case TokenType.NumericLiteral:
                            case TokenType.Identifier:
                                valueCollection.Add(token.Data);
                                state = ParseState.HaveCollectionValue;
                                continue;

                            case TokenType.CloseBrace:
                                property.Values = valueCollection;
                                state = ParseState.HavePropertyCompleted;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 107", "In property collection, expected value or close brace '}}'" );
                        }

                    case ParseState.HaveCollectionValue: 
                        switch (token.Type) {
                            case TokenType.Comma:
                                state = ParseState.InPropertyCollection;
                                continue;
                            case TokenType.CloseBrace:
                                property.Values = valueCollection;
                                state = ParseState.HavePropertyCompleted;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 108", "With property collection value, expected comma ',' or close-brace '}}'.");
                        }

                    case ParseState.HavePropertyValue:
                        switch (token.Type) {
                            case TokenType.Equal:
                                state = ParseState.HavePropertyEquals;
                                continue;

                            case TokenType.Dot:
                                var t = SkipToNext(ref enumerator);

                                if( !t.HasValue ) {
                                    throw new PropertySheetParseException(token, Filename, "PSP 109", "Unexpected end of Token stream [HavePropertyValue]");
                                }
                                token = t.Value;

                                if (token.Type == TokenType.Identifier || token.Type == TokenType.NumericLiteral) {
                                    property.RawValue = property.RawValue + "." + token.Data;
                                }
                                else 
                                    throw new PropertySheetParseException(token, Filename, "PSP 110", "Expected identifier or numeric literal after Dot '.'.");
                                continue;
                           
                            case TokenType.Semicolon:
                                state = ParseState.InRule;
                                rule.Properties.Add(property);
                                property = new RuleProperty();
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 111", "After property value, expected semi-colon ';' or equals '='." );
                        }

                    case ParseState.HavePropertyEquals:
                        switch (token.Type) {
                            case TokenType.Identifier:
                            case TokenType.StringLiteral:
                            case TokenType.NumericLiteral:
                                state = ParseState.HavePropertyCompleted;
                                property.RValue = token.Data;
                                continue;

                            case TokenType.OpenBrace:
                                valueCollection = new List<string>();
                                state = ParseState.InPropertyCollection;
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 112", "After equals in property, expected value or close-brace '{B{'." );
                        }

                    case ParseState.HavePropertyCompleted:
                        switch (token.Type) {
                            case TokenType.Semicolon:
                                state = ParseState.InRule;
                                rule.Properties.Add(property);
                                property = new RuleProperty();
                                continue;

                            default:
                                throw new PropertySheetParseException(token, Filename, "PSP 113", "After rule completed, expected semi-colon ';'.");
                        }
                }
            } while (enumerator.MoveNext());
            return propertySheet;
        }

        public static PropertySheet Parse(string propertySheetText, string originalFilename) {
            var p = new PropertySheetParser(propertySheetText,originalFilename);
            return p.Parse();
        }

        #region Nested type: ParseState

        private enum ParseState {
            Global,
            Selector,
            SelectorDot,
            SelectorPound,
            InRule,
            HavePropertyName,
            HavePropertySeparator,
            HavePropertyValue,
            HavePropertyCompleted,
            HavePropertyEquals,
            InPropertyCollection,
            HaveCollectionValue,
            InPropertyExpression,

        }

        #endregion
    }
}