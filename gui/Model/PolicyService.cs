using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoApp.Updater.Model.Interfaces;

namespace CoApp.Updater.Model
{
    public class PolicyService : IPolicyService
    {
        public Task<PolicyResult> InstallPolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetInstallPolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> UpdatePolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetUpdatePolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> RemovePolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetRemovePolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> BlockPolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetBlockPolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> FreezePolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetFreezePolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> ActivePolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetActivePolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> RequirePolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetRequirePolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> SystemFeedsPolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetSystemFeedsPolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }

        public Task<PolicyResult> SessionFeedsPolicy
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetSessionFeedsPolicy(PolicyResult result)
        {
            throw new NotImplementedException();
        }
    }
}
