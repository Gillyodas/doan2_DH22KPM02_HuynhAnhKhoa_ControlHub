using ControlHub.Application.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ControlHub.Infrastructure.Authorization.Permissions
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private const string POLICY_PREFIX = "Permission";

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Kiểm tra xem policy có bắt đầu bằng "Permission:" không
            if (policyName.StartsWith(POLICY_PREFIX + ":", StringComparison.OrdinalIgnoreCase))
            {
                // Tách tên permission ra, ví dụ: "permission.create"
                var permission = policyName.Substring(POLICY_PREFIX.Length + 1);

                // Tạo một policy builder
                var policy = new AuthorizationPolicyBuilder();

                // Thêm requirement (đã tạo ở trên) vào policy
                policy.AddRequirements(new PermissionRequirement(permission));

                // Build và trả về policy
                return policy.Build();
            }

            // Nếu không phải policy "Permission:", trả về xử lý mặc định (ví dụ: [Authorize])
            return await base.GetPolicyAsync(policyName);
        }
    }
}