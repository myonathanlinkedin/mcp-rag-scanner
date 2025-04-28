using Microsoft.AspNetCore.Identity;

internal class IdentityDbInitializer : DbInitializer
{
    private readonly UserManager<User> userManager;
    private readonly RoleManager<IdentityRole> roleManager;

    public IdentityDbInitializer(
        IdentityDbContext db,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager)
        : base(db)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
    }

    public override void Initialize()
    {
        base.Initialize();

        SeedAdministrator();
    }

    private void SeedAdministrator()
        => Task
            .Run(async () =>
            {
                var existingRole = await roleManager.FindByNameAsync(CommonModelConstants.Common.AdministratorRoleName);

                if (existingRole != null)
                {
                    return;
                }

                var adminRole = new IdentityRole(CommonModelConstants.Common.AdministratorRoleName);

                await roleManager.CreateAsync(adminRole);

                var adminUser = new User("admin@localhost");

                await userManager.CreateAsync(adminUser, "Admin01");
                await userManager.AddToRoleAsync(adminUser, CommonModelConstants.Common.AdministratorRoleName);
            })
            .GetAwaiter()
            .GetResult();
}