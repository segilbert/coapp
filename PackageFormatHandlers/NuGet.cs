//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.PackageFormatHandlers {
    public class NuGet : IPackageFormatHandler {
        public void Install(Engine.Package packagePath, System.Action<int> progress = null) {
            throw new System.NotImplementedException();
        }

        public void Remove(Engine.Package packagePath, System.Action<int> progress = null) {
            throw new System.NotImplementedException();
        }

        public bool IsInstalled(string productCode) {
            throw new System.NotImplementedException();
        }
    }
}
