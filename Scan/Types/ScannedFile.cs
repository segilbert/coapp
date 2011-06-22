//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010 Trevor Dennis. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using CoApp.Toolkit.Extensions;

namespace CoApp.Toolkit.Scan.Types
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
	/// Contains information about a scanned file.
	/// </summary>
	[XmlRoot("file")]
	public class ScannedFile
	{
        /// <summary>
        /// Gets or sets the numeric ID of the file.
        /// </summary>
        /// <value>The ID.</value>
        [XmlIgnore]
        public bool Used { get; set; }

        /// <summary>
        /// Gets or sets the numeric ID of the file.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public int ID { get; set; }

		/// <summary>
		/// Gets or sets the directory the file was found in.
		/// </summary>
		/// <value>The directory.</value>
		[XmlAttribute("path")]
		public string Directory { get; set; }

		/// <summary>
		/// Gets or sets the full name of the file.  This is used internally only.
		/// </summary>
		/// <value>The full name.</value>
		[XmlIgnore]
		public string FullName { get; set; }

        /// <summary>
        /// Gets the relative path 
        /// </summary>
        public string GetRelativePath(string directory) {
            return directory.RelativePathTo(FullName);
        }

		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		/// <value>The name.</value>
		[XmlAttribute("name")]
		public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of file.
        /// </summary>
        /// <value>The type.</value>
        [XmlAttribute("type")]
        public ScannedFileType Type { get; set; }

        /// <summary>
        /// Does the file have a main() function.
        /// </summary>
        /// <value>The type.</value>
        [XmlAttribute("hasMain")]
        public bool HasMain{ get; set; }

		//[XmlArray("includes"), XmlArrayItem("fileid", typeof(int))]
		[XmlIgnore]
		public List<int> Includes { get; set; }

		//[XmlArray("includedby"),XmlArrayItem("fileid", typeof(int))]
		[XmlIgnore]
		public List<int> IncludedBy { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ScannedFile"/> class.
		/// </summary>
		public ScannedFile()
		{
			ID = 0;
			Includes = new List<int>();
		}


        private static readonly Dictionary<string,ScannedFileType> _scannedFileType = new Dictionary<string,ScannedFileType>();

		/// <summary>
		/// Determines the type of the file by checking extensions and full names.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>The type of the file</returns>
		public static ScannedFileType DetermineFileType(string fileName) {
            if (_scannedFileType.ContainsKey(fileName)) {
                return _scannedFileType[fileName];
            }

		    var lowerfileName = fileName.ToLower();

			var extension = Path.GetExtension(lowerfileName );

		    var result = (from pattern in KnownPatterns
		        where pattern.Value.Contains(lowerfileName ) || pattern.Value.HasWildcardMatch(lowerfileName ) || pattern.Value.Contains(extension)
		        select pattern.Key).DefaultIfEmpty(ScannedFileType.Unknown).First();

            lock (_scannedFileType) {
                _scannedFileType.Add(fileName, result);
            }

		    return result;


		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return string.Format("File - Name: {0}, Path: {1}, Type: {2}", Name, Directory, Type);
		}

        // yeah, this doesn't make it exactly trivial to use, but it's easier to fill coming from the config files.
        public static Dictionary<ScannedFileType, List<string>> KnownPatterns = new Dictionary<ScannedFileType, List<string>> {
            { ScannedFileType.C , new List<string> {".c"}},
            { ScannedFileType.Cpp, new List<string>{".cpp", ".cxx", ".cc", ".c++"} },
            { ScannedFileType.Header, new List<string>{".h", ".hpp", ".hxx", ".hh"} },
            { ScannedFileType.Resource , new List<string>{".rc"} },
            { ScannedFileType.Assembly , new List<string>{".asm" } },
            { ScannedFileType.Idl, new List<string>{".idl"} },
            { ScannedFileType.Source, new List<string>{".s", ".xs" } }, 
            { ScannedFileType.Pascal, new List<string>{ ".pas", ".inc" } }, 
            { ScannedFileType.CSharp, new List<string>{ ".cs"} }, 
            { ScannedFileType.VB, new List<string>{".vb" } }, 
            { ScannedFileType.Manifest, new List<string>{ ".manifest" } }, 
            { ScannedFileType.BuildFile, new List<string>{ ".mak", ".sln", ".csproj",".vcproj",".vcxproj", ".spec", ".buildinfo", "makefile", "makefile.*" , "build.xml", "config", "configure" , "configure.*" , } }, 
            { ScannedFileType.Script, new List<string>{ ".bat",".cmd",".js", ".vbs",".sh", ".ps1",".wsh",".py", ".pl", ".pm", ".pod",".pem",".php",".phps",".m4", ".awk", } }, 
            { ScannedFileType.Media, new List<string>{ ".png", ".gif", ".avi", ".mpg", ".mp2", ".mp3", ".mp4", ".mkv", ".ico", ".wav", ".jpg", ".jpeg",".xpm", } }, 
            { ScannedFileType.PeBinary, new List<string>{ ".exe",".dll",".sys",".com", } }, 
            { ScannedFileType.Library, new List<string>{".lib",".a", } }, 
            { ScannedFileType.Document, new List<string>{ ".txt", ".man", ".xslt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".readme", "install", "install.*" , "readme" , "readme.*", "license", "faq", "issues", "news", "problems" , "copying"  , "changes" , "changelog" , "copyright" , "version" , "authors", "copying"} }, 
            { ScannedFileType.Debug, new List<string>{ ".pdb" } }, 
            { ScannedFileType.Object, new List<string>{ ".o" , ".obj" } }, 
            { ScannedFileType.Xml, new List<string>{".xml" } }, 
            { ScannedFileType.Xaml, new List<string>{".xaml" } }, 
            { ScannedFileType.Configuration, new List<string>{".config",".properties" } }, 
            { ScannedFileType.Invalid, new List<string>{ } }, 
            { ScannedFileType.Unknown, new List<string>{ } }, 
            { ScannedFileType.Discard, new List<string>{".bak" } }, 
        };


	}
}
