//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Copyright (c) 2010 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.GSharp {
    using System.Collections.Generic;
    using Utility;

    /// <summary>
    ///   Transforms g# code into legal c# code
    /// </summary>
    public class GSharpProcessor {
        /// <summary>
        ///   A list of tokens
        /// </summary>
        protected List<Token> tokens;

        /// <summary>
        ///   the text to process
        /// </summary>
        protected string scriptText;

        /// <summary>
        ///   Constructor to create script processor
        /// </summary>
        /// <param name = "scriptText">g# source code to execute</param>
        public GSharpProcessor(string scriptText) {
            this.scriptText = scriptText;
        }

        /// <summary>
        ///   Performs the text processing.
        /// </summary>
        /// <returns>The c# code</returns>
        private string ProcessText() {
            tokens = GSharpTokenizer.Tokenize(scriptText);

            foreach(Token t in tokens) {
            }

            return null;
        }

        /// <summary>
        ///   Public static accessor to process script text
        /// </summary>
        /// <param name = "scriptText">Script text to process</param>
        /// <returns>c# source code</returns>
        public static string Process(string scriptText) {
            return new GSharpProcessor(scriptText).ProcessText();
        }
    }
}