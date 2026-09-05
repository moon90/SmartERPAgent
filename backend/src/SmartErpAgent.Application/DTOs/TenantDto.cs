namespace SmartErpAgent.Application.DTOs;

public record TenantDto(
    Guid Id,
    string Code,
    string Name,
    string AdminEmail,
    string SubscriptionTier,
    bool IsActive,
    DateTime CreatedAtUtc
);

public record CreateTenantRequest(
    string Code,
    string Name,
    string AdminEmail,
    string SubscriptionTier
);
