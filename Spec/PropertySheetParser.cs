//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using CoApp.Toolkit.Scripting;
using CoApp.Toolkit.Scripting.Utility;

namespace CoApp.Toolkit.Spec {
    public class PropertySheetParser {
    
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

        }

        private string PropertySheetText;

        protected PropertySheetParser(string text) {
            PropertySheetText = text;

        }

        protected PropertySheet Parse() {
            var tokenStream = PropertySheetTokenizer.Tokenize(PropertySheetText);
            var state = ParseState.Global;
            var enumerator = tokenStream.GetEnumerator();
            Token token;

            PropertySheet propertySheet = new PropertySheet();
            
            Rule rule = new Rule();
            RuleProperty property = new RuleProperty();
            enumerator.MoveNext();

            do {
                token = enumerator.Current;
                
                switch(token.Type) { // regardless where we are, skip over whitespace, etc.
                    case TokenType.WhiteSpace:
                    case TokenType.LineComment:
                    case TokenType.MultilineComment:
                        continue;
                }

                switch(state) {
                    case ParseState.Global:
                        switch(token.Type) {
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

                            default:
                                throw new Exception("Unexpected Token in stream [Global]:" + token.Data);

                        }

                    case ParseState.Selector:
                        switch(token.Type) {
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
                                throw new Exception("Unexpected Token in stream [Selector]:" + token.Data);
                        }

                    case ParseState.SelectorDot:
                        switch(token.Type) {
                            case TokenType.Identifier:
                                rule.Class = token.Data;
                                state = ParseState.Selector;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [SelectorDot]:" + token.Data);
                        }

                    case ParseState.SelectorPound:
                        switch(token.Type) {
                            case TokenType.Identifier:
                                rule.Id = token.Data;
                                state = ParseState.Selector;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [SelectorPound]:" + token.Data);
                        }

                    case ParseState.InRules:
                        switch(token.Type) {
                            case TokenType.Semicolon: // extra semicolons are tolerated.
                                continue;

                            case TokenType.Identifier:
                                property.Name = token.Data;
                                state = ParseState.HaveRuleName;
                                continue;

                            case TokenType.CloseBrace: // extra semicolons are tolerated.
                                propertySheet.Rules.Add(rule.FullSelector,rule);
                                rule = new Rule();
                                state = ParseState.Global;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [InRules]:" + token.Data);
                        }

                    case ParseState.HaveRuleName:
                        switch(token.Type) {
                            case TokenType.Colon:
                                state = ParseState.HaveRuleSeparator;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleName]:" + token.Data);

                        }

                    case ParseState.HaveRuleSeparator:
                        switch(token.Type) {
                            case TokenType.StringLiteral:
                            case TokenType.NumericLiteral:
                                state = ParseState.HaveRuleCompleted;
                                property.Value = token.Data;
                                continue;

                            case TokenType.Identifier:
                                state = ParseState.HaveRuleValue;
                                property.LValue = token.Data;
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleSeparator]:" + token.Data);

                        }

                    case ParseState.HaveRuleValue:
                        switch(token.Type) {
                            case TokenType.Equal:
                                state = ParseState.HaveRuleEquals;
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
                        switch(token.Type) {
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
                        switch(token.Type) {
                            case TokenType.Semicolon:
                                state = ParseState.InRules;
                                rule.Properties.Add(property);
                                property = new RuleProperty();
                                continue;

                            default:
                                throw new Exception("Unexpected Token in stream [HaveRuleCompleted]:" + token.Data);
                        }
                }
            } while(enumerator.MoveNext());
            return propertySheet;
        }

        public static PropertySheet Parse(string propertySheetText ) {
            var p = new PropertySheetParser(propertySheetText);
            return p.Parse();
        }

    }

}
