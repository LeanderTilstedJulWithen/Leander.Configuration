// Types with the same short name in different namespaces, for comparing type names that depend on the rest of the contract.

namespace Leander.Configuration.Tooling.Tests.Billing
{
    public enum Status
    {
        Open,
        Paid,
    }
}

namespace Leander.Configuration.Tooling.Tests.Shipping
{
    public enum Status
    {
        Packed,
        Sent,
    }
}

namespace Leander.Configuration.Tooling.Tests
{
    // Ends with "Status", but not at a namespace boundary.
    public enum OrderStatus
    {
        Open,
        Paid,
    }
}
