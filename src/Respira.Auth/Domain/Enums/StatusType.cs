namespace Domain.Enums
{
    /// <summary>
    /// Account status. Inactive accounts cannot authenticate.
    /// </summary>
    public enum StatusType
    {
        /// <summary>The account is active and may authenticate.</summary>
        Active,

        /// <summary>The account is disabled and cannot authenticate.</summary>
        Inactive,
    }
}
