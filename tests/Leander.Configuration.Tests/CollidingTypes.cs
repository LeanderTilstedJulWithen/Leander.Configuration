// Types with the same short name in different namespaces, for naming types in a contract.

namespace Leander.Configuration.Tests.Billing
{
    public enum Status
    {
        Open,
        Paid,
    }
}

namespace Leander.Configuration.Tests.Shipping
{
    public enum Status
    {
        Packed,
        Sent,
    }

    public static class Outer
    {
        public enum Status
        {
            Waiting,
        }
    }
}

namespace Leander.Configuration.Other.Billing
{
    public enum Status
    {
        Closed,
    }
}

namespace Leander.Configuration.Tests.Custom
{
    // Collides with System.Int32.
    public enum Int32
    {
        Zero,
    }
}
