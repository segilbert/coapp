//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Utility {
    /// <summary>
    ///   Enumeration of different token types
    /// </summary>
    public enum TokenType {
        Plus,
        PlusPlus,
        PlusEquals,

        Minus,
        MinusMinus,
        MinusEquals,
        DashArrow,

        Asterisk,
        AsteriskEquals,

        Equal,
        EqualEqual,
        Lambda,

        Slash,
        SlashEquals,
        LineComment,
        MultilineComment,

        Bar,
        BarBar,
        BarEquals,

        Ampersand,
        AmpersandAmpersand,
        AmpersandEquals,

        Percent,
        PercentEquals,

        LessThan,
        LessThanEquals,
        BitShiftLeft,
        BitShiftLeftEquals,

        GreaterThan,
        GreaterThanEquals,
        BitShiftRight,
        BitShiftRightEquals,

        Bang,
        BangEquals,

        Dollar,

        Power,
        PowerEquals,

        Tilde,

        QuestionMark,
        QuestionMarkQuestionMark,

        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
        OpenParenthesis,
        CloseParenthesis,

        Dot,
        Comma,
        Colon,
        Semicolon,

        Pound,

        Unicode,

        Keyword,

        Identifier,

        StringLiteral,
        NumericLiteral,
        CharLiteral,

        SelectorParameter,

        WhiteSpace,

        Unknown
    }
}