using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Play.Identity.Service.Consts;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Settings;

namespace Play.Identity.Service.HostedServices
{
    public class IdentitySeedHostedService : IHostedService
    {
        private IServiceScopeFactory _serviceScopeFactory;

        private IdentitySettings _identitySettings;

        public IdentitySeedHostedService(IServiceScopeFactory serviceScopeFactory, IOptions<IdentitySettings> identityOptions)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _identitySettings = identityOptions.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();

                var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();

                await CreateRoleIfNotExist(Roles.Admin, roleManager);
                await CreateRoleIfNotExist(Roles.Player, roleManager);

                var user = await CreateUserIfNotExist(_identitySettings.AdminUserEmail, _identitySettings.AdminUserPassword, userManager);

                if (user == null)
                {
                    return;
                }

                await userManager.AddToRoleAsync(user, Roles.Admin);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task CreateRoleIfNotExist(string roleName, RoleManager<ApplicationRole> roleManager)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);

            if (roleExist)
            {
                return;
            }

            await roleManager.CreateAsync(new ApplicationRole() { Name = roleName });
        }

        private async Task<ApplicationUser?> CreateUserIfNotExist(string userEmail, string userPassword, UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.FindByEmailAsync(userEmail);

            if (user != null)
            {
                return null;
            }

            user = new ApplicationUser() { UserName = userEmail, Email = userEmail };

            var result = await userManager.CreateAsync(user, userPassword);
            if (!result.Succeeded)
            {
                return null;
            }
            return user;
        }
    }
}