//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Utility {
    using System;
    using System.Linq;
    using Configuration;
    using Extensions;
    using Win32;

    public enum ToolVendor {
        Unknown,
        Microsoft,
        Cygwin,
        Mingw,
        Watcom,
        Intel,
        IBM,
    }

    public enum ToolType {
        Unknown,
        CCompiler,
        CppCompiler,
        Linker,
        Assember,
        ILAssembler,
        ILDisasembler,
        AssemblyLinker,
        Lib,
        Make,
        MessageCompiler,
        ResourceCompiler,
        IDLCompiler,
        ManifestTool
    }

    public struct ToolInfo {
        public int MajorVersion;
        public int MinorVersion;
        public ToolVendor Vendor;
        public ToolType Type;
    }

    public class ToolSniffer {
        private static readonly RegistryView _settings = RegistryView.CoAppUser["Tools"];
        private static readonly Lazy<ToolSniffer> _sniffer = new Lazy<ToolSniffer>(() => new ToolSniffer());

        private ToolSniffer() {
        }

        public static ToolSniffer Sniffer {
            get { return _sniffer.Value; }
        }

        public ToolInfo Identify(string executablePath) {
            var result = new ToolInfo();
            executablePath = executablePath.GetFullPath();
            var key = executablePath.Replace("\\", "/");

            var vendor = _settings[key, "Vendor"].StringValue;
            if (string.IsNullOrEmpty(vendor)) {
                var peInfo = PEInfo.Scan(executablePath);

                if (!string.IsNullOrEmpty(peInfo.VersionInfo.CompanyName)) {
                    if (peInfo.VersionInfo.CompanyName.Contains("Microsoft")) {
                        result = IdentifyMicrosoftProduct(peInfo);
                    }
                }
                else {
                    // hmm. not embedding info. bad developer. no cookie!
                    if ((from di in peInfo.DependencyInformation where di.Filename.Contains("cygwin1") select di).Any()) {
                        var pi = new ProcessUtility(executablePath);
                        pi.Exec("-dumpversion");
                        var ver = pi.StandardOut.Split('.');
                        if (ver.Length > 1 && ver.Length < 4) {
                            result.MajorVersion = ver[0].ToInt32(0);
                            result.MinorVersion = ver[1].ToInt32(0);
                            if (result.MajorVersion > 0) {
                                result.Vendor = ToolVendor.Cygwin;
                            }
                        }
                    }
                }

                if (result.Vendor != ToolVendor.Unknown) {
                    _settings[key, "Vendor"].StringValue = result.Vendor.ToString();
                    _settings[key, "MajorVersion"].IntValue = result.MajorVersion;
                    _settings[key, "MinorVersion"].IntValue = result.MinorVersion;
                    _settings[key, "Type"].SetEnumValue(result.Type);
                }
            }
            else {
                Enum.TryParse(vendor, true, out result.Vendor);
                result.MajorVersion = _settings[key, "MajorVersion"].IntValue;
                result.MinorVersion = _settings[key, "MinorVersion"].IntValue;
                result.Type = _settings[key, "Type"].GetEnumValue<ToolType>();
            }

            return result;
        }

        private ToolInfo IdentifyMicrosoftProduct(PEInfo peInfo) {
            var result = new ToolInfo {
                Vendor = ToolVendor.Microsoft,
                MajorVersion = peInfo.VersionInfo.ProductMajorPart,
                MinorVersion = peInfo.VersionInfo.ProductMinorPart
            };

            switch (peInfo.VersionInfo.InternalName.ToLower()) {
                case "cl.exe":
                    result.Type = ToolType.CCompiler;
                    break;
                case "lib.exe":
                    result.Type = ToolType.Lib;
                    break;
                case "link.exe":
                    result.Type = ToolType.Linker;
                    break;

                case "masm.exe":
                case "ml.exe":
                    result.Type = ToolType.Assember;
                    break;


                case "ilasm.exe":
                    result.Type = ToolType.ILAssembler;
                    break;


                case "ildasm.exe":
                    result.Type = ToolType.ILDisasembler;
                    break;


                case "as.exe":
                    result.Type = ToolType.AssemblyLinker;
                    break;

                case "nmake.exe":
                    result.Type = ToolType.Make;
                    break;

                case "mc.exe":
                    result.Type = ToolType.MessageCompiler;
                    break;

                case "rc.exe":
                    result.Type = ToolType.ResourceCompiler;
                    break;

                case "midl":
                case "midlc":
                case "midl.exe":
                    result.Type = ToolType.IDLCompiler;
                    break;

                case "mt.exe":
                case "mt2.exe":
                    result.Type = ToolType.ManifestTool;
                    break;

                default:
                    result.Type = ToolType.Unknown;
                    break;
            }
            return result;
        }
    }
}