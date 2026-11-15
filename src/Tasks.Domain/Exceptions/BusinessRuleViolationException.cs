namespace Tasks.Domain.Exceptions;

/// <summary>A domain invariant or business rule was violated.</summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string rule, string message)
        : base(message)
    {
        Rule = rule;
    }

    public string Rule { get; }

    public override string ErrorCode => "business_rule.violated";
}
