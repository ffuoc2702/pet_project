using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MatchingSports.Infrastructure.Persistence;

internal sealed class PersistenceExceptionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                ErrorCodes.ConcurrencyConflict,
                "Dữ liệu đã được thay đổi bởi một yêu cầu khác. Vui lòng tải lại và thử lại.");
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres
                    && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException(
                ErrorCodes.DuplicateResource,
                "Dữ liệu đã tồn tại hoặc trùng với một tài nguyên khác.");
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres
                    && postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new ConflictException(
                ErrorCodes.ReferenceConflict,
                "Tài nguyên đang tham chiếu dữ liệu không tồn tại hoặc không thể thay đổi.");
        }
        catch (Exception exception) when (IsTransactionConflict(exception))
        {
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Có thao tác đồng thời. Vui lòng tải lại và thử lại.");
        }
        catch (NpgsqlException)
        {
            throw new ServiceUnavailableException(
                ErrorCodes.DatabaseUnavailable,
                "Không thể kết nối đến cơ sở dữ liệu. Vui lòng thử lại sau.");
        }
        catch (TimeoutException)
        {
            throw new ServiceUnavailableException(
                ErrorCodes.DependencyTimeout,
                "Một dịch vụ phụ thuộc phản hồi quá chậm. Vui lòng thử lại sau.");
        }
    }

    private static bool IsTransactionConflict(Exception exception)
    {
        // Npgsql's execution strategy can wrap a serialization failure in InvalidOperationException.
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && postgres.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
                return true;
        }
        return false;
    }
}
