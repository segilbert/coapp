using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoApp.Updater.Model.Interfaces
{
    public interface IPolicyService
    {
        Task<PolicyResult> InstallPolicy { get; }
        Task SetInstallPolicy(PolicyResult result);
        Task<PolicyResult> UpdatePolicy { get; }
        Task SetUpdatePolicy(PolicyResult result);

        Task<PolicyResult> RemovePolicy { get; }
        Task SetRemovePolicy(PolicyResult result);

        Task<PolicyResult> BlockPolicy { get; }
        Task SetBlockPolicy(PolicyResult result);

        Task<PolicyResult> FreezePolicy { get; }
        Task SetFreezePolicy(PolicyResult result);

        Task<PolicyResult> ActivePolicy { get; }
        Task SetActivePolicy(PolicyResult result);

        Task<PolicyResult> RequirePolicy { get; }
        Task SetRequirePolicy(PolicyResult result);

        Task<PolicyResult> SystemFeedsPolicy { get; }
        Task SetSystemFeedsPolicy(PolicyResult result);

        Task<PolicyResult> SessionFeedsPolicy { get; }
        Task SetSessionFeedsPolicy(PolicyResult result);

        

        

    }

    public enum PolicyResult
    {
        CurrentUser,
        Other,
        Everyone,
        
    }
}
