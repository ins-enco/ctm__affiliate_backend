namespace CopyTradeMarketApi.Shared.Exceptions;

public class TooManyRequestsException(string message) : Exception(message);
