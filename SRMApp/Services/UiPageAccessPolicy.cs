namespace SRMApp.Services;

public static class UiPageAccessPolicy
{
    private static readonly HashSet<string> CustomerManagementPages =
    [
        "Customers",
        "CustomerCreate",
        "CustomerEdit",
        "CustomerDetails"
    ];

    private static readonly HashSet<string> ConfigurationMutationPages =
    [
        "ServerRoomCreate",
        "AgentCreate",
        "ShellyDeviceCreate",
        "MonitoredDeviceCreate",
        "MaintenanceWindowCreate"
    ];

    public static bool CanAccess(string pageName, AuthSessionService session)
    {
        if (CustomerManagementPages.Contains(pageName))
        {
            return session.CanManageCustomers;
        }

        if (ConfigurationMutationPages.Contains(pageName))
        {
            return session.CanManageConfiguration;
        }

        return pageName switch
        {
            "Users" => session.CanManageUsers,
            "AgentCredentials" => session.CanManageAgentCredentials,
            _ => true
        };
    }
}
