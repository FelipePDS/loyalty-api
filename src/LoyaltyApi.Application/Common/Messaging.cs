using MediatR;

namespace LoyaltyApi.Application.Common;

/// <summary>Marker interface for all commands (used by TransactionBehavior to detect commands).</summary>
public interface IBaseCommand;

/// <summary>Command that returns a typed result.</summary>
public interface ICommand<TResponse> : IBaseCommand, IRequest<Result<TResponse>>;

/// <summary>Command that returns a non-generic Result (void success).</summary>
public interface ICommand : IBaseCommand, IRequest<Result>;

/// <summary>Query that returns a typed result.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
