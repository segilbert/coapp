//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scan.Types
{
    /// <summary>
	/// Defines the type of project files.
	/// </summary>
	public enum ScannedFileType
	{
		/// <summary>
		/// No type is known for this file
		/// </summary>
		Unknown,
		/// <summary>
		/// Generic source code file
		/// </summary>
		Source,
		/// <summary>
		/// C code file (headers are seperate)
		/// </summary>
		C,
		/// <summary>
		/// C++ code file (headers are seperate)
		/// </summary>
		Cpp,
		/// <summary>
		/// Pascal source file
		/// </summary>
		Pascal,
		/// <summary>
		/// C# source file
		/// </summary>
		CSharp,
		/// <summary>
		/// Visual Basic source file
		/// </summary>
		VB,
		/// <summary>
		/// Assembly source file
		/// </summary>
		Assembly,
		/// <summary>
		/// Manifest file
		/// </summary>
		Manifest,
		/// <summary>
		/// Build file for compiling the project
		/// </summary>
		BuildFile,
		/// <summary>
		/// Script file for languages like perl, python, javascript, etc
		/// </summary>
		Script,
		/// <summary>
		/// Media files like images, videos, etc
		/// </summary>
		Media,
		/// <summary>
		/// Executable files like exe, dll, com
		/// </summary>
		PeBinary,
		/// <summary>
		/// Library files that may contain pre-compiled code
		/// </summary>
		Library,
		/// <summary>
		/// Any types of documentation files
		/// </summary>
		Document,
		/// <summary>
		/// Debug files like pdb
		/// </summary>
		Debug,
		/// <summary>
		/// Object code files which contain compiled code
		/// </summary>
		Object,
        /// <summary>
        /// Resource files (*.rc)
        /// </summary>
        Resource,
        /// <summary>
        /// Header files (*.h, etc)
        /// </summary>
        Header,
        /// <summary>
        /// Xml files (*.xml, etc)
        /// </summary>
        Xml,
        /// <summary>
        /// Xaml files (*.xml, etc)
        /// </summary>
        Xaml,
        /// <summary>
        /// Configuration files
        /// </summary>
        Configuration,

        /// <summary>
        /// Idl files
        /// </summary>
        Idl,
        /// <summary>
        /// files intended to be discarded.
        /// </summary>
        Discard,

        Invalid

	}
}
