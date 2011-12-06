

using System.Drawing.Imaging;
using System.IO;

namespace CoApp.Toolkit.Package {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Drawing;
    using Extensions;

    public static class Validator {

        internal static readonly HashSet<string> ValidArchs = new HashSet<string> { "x86", "x64", "any" };
        public static readonly int MAX_SUMMARY_DESCRIPTION_LENGTH = 160;
        public static readonly int REQUIRED_ICON_HEIGHT = 256;
        public static readonly int REQUIRED_ICON_WIDTH = 256;

        public static bool IsPackageNameValid(string name)
        {
            return !String.IsNullOrEmpty(name) &&
                   name.OnlyContains(StringExtensions.LettersNumbersUnderscoresAndDashesAndDots);
        }

        public static bool IsPackageVersionValid(string version)
        {
            return String.IsNullOrEmpty(version) && version.IsValidVersion();
        }

        public static bool IsPackageArchitectureValid(string architecture)
        {
            return String.IsNullOrEmpty(architecture) && ValidArchs.Contains(architecture);
        }

        public static bool IsShortDescriptionValid(string shortDesc)
        {
            return shortDesc.Length <= MAX_SUMMARY_DESCRIPTION_LENGTH;
        }

        public static bool IsUrlValid(string uriStr)
        {
            try {
                var uri = new Uri(uriStr);

                return uri.IsHttpScheme();

            }
            catch
            {
                return false;
            }
        }

        public static bool IsEmailValid(string email)
        {
            return email != null && !email.IsEmail();
        }

        public static bool IsPersonNameValid(string name)
        {
            return !String.IsNullOrEmpty(name);
        }


        public static bool IsIconValid(string base64IconData)
        {
            try
            {
                return IsIconValid(Convert.FromBase64String(base64IconData));

            }
            catch
            {
                return false;

            }
        }

        public static bool IsIconValid(byte[] iconData)
        {
            try
            {
                using (var bytes = new MemoryStream(iconData)) {
                    var image = Image.FromStream(bytes);
                    return image.RawFormat == ImageFormat.Png &&
                           image.Height == REQUIRED_ICON_HEIGHT ||
                           image.Width == REQUIRED_ICON_WIDTH;
                    
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
