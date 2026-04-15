namespace Mock.Application.DTOs;

public record ClientRequestDto(
    DateTime Timestamp,
    string Name,
    decimal Equity,
    string Strategy,
    string StrategyLicense);
