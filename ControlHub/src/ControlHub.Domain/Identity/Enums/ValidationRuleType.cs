namespace ControlHub.Domain.Identity.Enums
{
    public enum ValidationRuleType
    {
        Required = 1,
        MinLength = 2,
        MaxLength = 3,
        Pattern = 4,
        Custom = 5,
        Range = 6,
        Email = 7,
        Phone = 8
    }
}
