using Microsoft.AspNetCore.Http;

namespace Respira.ServiceDefaults.Contracts.Results;

/*
 * Rule of status code in this application context:
 * 1. For resource not found (database not exists, cache miss,...), there are 2 cases:
 * a. If you try to read that resource, it would be a RESOUCE-NOT-FOUND error
 * b. If you try to do some operation on that resource, it would be a BAD-REQUEST error
 * (trying to do something with non existing data is more suited with BAD-REQUEST than
 * RESOURCE-NOT-FOUND)
 * c. If you try to read sub resources of a resource, and that parent resource is not found,
 * it would be a BAD-REQUEST error. For example, let's say that you want to read a specific
 * treatment of a specific patient, you would have to provide 2 IDs, patient and treatment:
 * - If the patient ID exists, treatment don't, then that a RESOUCE-NOT-FOUND error
 * - If the patient ID don't even exists, then that a BAD-REQUEST error.
 */

public static class Status
{
    #region STATUS CODES FOR NORMAL FLOWS

    #region SUCCESS CODES

    /// <summary>
    /// This is a standard success code, which can be used to indicate that the request was successful.
    /// </summary>
    public const string Success = "SUCCESS";

    /// <summary>
    /// An extension of success code, specifically used to indicate that the request was successful
    /// and a new resource was created.
    /// </summary>
    public const string Created = "SUCCESS-CREATED";

    /// <summary>
    /// An extension of success code, specifically used to indicate that the request was successful
    /// and an existing resource was updated.
    /// </summary>
    public const string Updated = "SUCCESS-UPDATED";

    /// <summary>
    /// An extension of success code, specifically used to indicate that the request was successful
    /// and an existing resource was deleted.
    /// </summary>
    public const string Deleted = "SUCCESS-DELETED";

    #endregion

    #region FAILURE CODES

    /// <summary>
    /// This is a standard failure code, which can be used to indicate that the request was not successful.
    /// Although it's recommended to use a more specific code for better error handling.
    /// </summary>
    public const string Failure = "FAILURE";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the request input is invalid
    /// </summary>
    public const string BadRequest = "FAILURE-BAD-REQUEST";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the request if proceeds will
    /// violate business rules.
    /// </summary>
    public const string BusinessRuleViolation = "FAILURE-BUSINESS-RULE-VIOLATION";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the request is unauthorized.
    /// </summary>
    public const string Unauthorized = "FAILURE-UNAUTHORIZED";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the requester doesn't have
    /// enough permissions to perform the requested operation.
    /// </summary>
    public const string Restricted = "FAILURE-RESTRICTED";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the requested resource was not found.
    /// </summary>
    public const string ResourceNotFound = "FAILURE-RESOURCE-NOT-FOUND";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the request was not successful
    /// because of our side problems
    /// </summary>
    public const string ServerError = "FAILURE-SERVER-ERROR";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the request was not successful
    /// becuase of a timeout
    /// </summary>
    public const string Timeout = "FAILURE-TIMEOUT";

    /// <summary>
    /// An extension of failure code, specifically used to indicate that the request was not successful
    /// beacuse of a third party service failure (database crash, S3 service unavailable, etc.)
    /// </summary>
    public const string ThirdPartyServiceFailure = "FAILURE-THIRD-PARTY-SERVICE-FAILURE";

    #endregion

    #endregion


    #region STATUS CODES FOR SAGA FLOWS

    /*
     * SAGA codes mostly have identical meaning to normal codes, except that they are prefixed with "SAGA-",
     * to distinguish them from normal codes. This is because SAGA often have rollback logic, which
     * normal flows don't have.
     */

    #region SAGA SUCCESS CODES

    /// <summary>
    /// An extension of success code for SAGA flow, used for all SAGA success cases.
    /// </summary>
    public const string SagaSuccess = "SAGA-SUCCESS";

    /// <summary>
    /// An extension of success code for SAGA flow, used for all SAGA success cases and create new resources
    /// </summary>
    public const string SagaCreated = "SAGA-CREATED";

    /// <summary>
    /// An extension of success code for SAGA flow, used for all SAGA success cases and update existing resources
    /// </summary>
    public const string SagaUpdated = "SAGA-UPDATED";

    /// <summary>
    /// An extension of success code for SAGA flow, used for all SAGA success cases and delete existing resources
    /// </summary>
    public const string SagaDeleted = "SAGA-DELETED";

    #endregion

    #region SAGA FAILURE CODES

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA failure cases.
    /// </summary>
    public const string SagaFailure = "SAGA-FAILURE";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA bad request cases
    /// </summary>
    public const string SagaBadRequest = "SAGA-BAD-REQUEST";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA cases that cause business rule violation
    /// </summary>
    public const string SagaBusinessRuleViolation = "SAGA-BUSINESS-RULE-VIOLATION";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA unauthorized failure cases
    /// </summary>
    public const string SagaUnauthorized = "SAGA-UNAUTHORIZED";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA cases that lack permissions
    /// </summary>
    public const string SagaRestricted = "SAGA-RESTRICTED";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA cases where resources not found
    /// </summary>
    public const string SagaResourceNotFound = "SAGA-RESOURCE-NOT-FOUND";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA cases where it's the server issues
    /// </summary>
    public const string SagaServerError = "SAGA-SERVER-ERROR";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA timeout failure cases
    /// </summary>
    public const string SagaTimeout = "SAGA-TIMEOUT";

    /// <summary>
    /// An extension of failure code for SAGA flow, used for all SAGA cases where it's the third party
    /// service issues
    /// </summary>
    public const string SagaThirdPartyServiceFailure = "SAGA-THIRD-PARTY-SERVICE-FAILURE";

    #endregion

    #endregion

    public static bool IsSupportedStatusCode(string customStatusCode)
    {
        return customStatusCode switch
        {
            Success => true,
            Created => true,
            Updated => true,
            Deleted => true,
            Failure => true,
            BadRequest => true,
            BusinessRuleViolation => true,
            Unauthorized => true,
            Restricted => true,
            ResourceNotFound => true,
            ServerError => true,
            Timeout => true,
            ThirdPartyServiceFailure => true,

            SagaSuccess => true,
            SagaCreated => true,
            SagaUpdated => true,
            SagaDeleted => true,
            SagaFailure => true,
            SagaBadRequest => true,
            SagaBusinessRuleViolation => true,
            SagaUnauthorized => true,
            SagaRestricted => true,
            SagaResourceNotFound => true,
            SagaServerError => true,
            SagaTimeout => true,
            SagaThirdPartyServiceFailure => true,

            _ => false
        };
    }

    public static bool IsSuccess(string customStatusCode)
    {
        return customStatusCode switch
        {
            Success => true,
            Created => true,
            Updated => true,
            Deleted => true,
            Failure => false,
            BadRequest => false,
            BusinessRuleViolation => false,
            Unauthorized => false,
            Restricted => false,
            ResourceNotFound => false,
            ServerError => false,
            Timeout => false,
            ThirdPartyServiceFailure => false,

            SagaSuccess => true,
            SagaCreated => true,
            SagaUpdated => true,
            SagaDeleted => true,
            SagaFailure => false,
            SagaBadRequest => false,
            SagaBusinessRuleViolation => false,
            SagaUnauthorized => false,
            SagaRestricted => false,
            SagaResourceNotFound => false,
            SagaServerError => false,
            SagaTimeout => false,
            SagaThirdPartyServiceFailure => false,

            _ => throw new ArgumentException("Invalid status code", nameof(customStatusCode))
        };
    }

    public static int ToHttpStatusCode(string customStatusCode)
    {
        return customStatusCode switch
        {
            Success => StatusCodes.Status200OK,
            Created => StatusCodes.Status201Created,
            Updated => StatusCodes.Status204NoContent,
            Deleted => StatusCodes.Status204NoContent,
            Failure => StatusCodes.Status500InternalServerError,
            BadRequest => StatusCodes.Status400BadRequest,
            BusinessRuleViolation => StatusCodes.Status400BadRequest,
            Unauthorized => StatusCodes.Status401Unauthorized,
            Restricted => StatusCodes.Status403Forbidden,
            ResourceNotFound => StatusCodes.Status404NotFound,
            ServerError => StatusCodes.Status500InternalServerError,
            Timeout => StatusCodes.Status504GatewayTimeout,
            ThirdPartyServiceFailure => StatusCodes.Status503ServiceUnavailable,

            SagaSuccess => StatusCodes.Status202Accepted,
            SagaCreated => StatusCodes.Status201Created, // Should be 202?
            SagaUpdated => StatusCodes.Status204NoContent, // Should be 202?
            SagaDeleted => StatusCodes.Status204NoContent, // Should be 202?
            SagaFailure => StatusCodes.Status500InternalServerError,
            SagaBadRequest => StatusCodes.Status400BadRequest,
            SagaBusinessRuleViolation => StatusCodes.Status400BadRequest,
            SagaUnauthorized => StatusCodes.Status401Unauthorized,
            SagaRestricted => StatusCodes.Status403Forbidden,
            SagaResourceNotFound => StatusCodes.Status404NotFound,
            SagaServerError => StatusCodes.Status500InternalServerError,
            SagaTimeout => StatusCodes.Status504GatewayTimeout,
            SagaThirdPartyServiceFailure => StatusCodes.Status503ServiceUnavailable,

            _ => throw new ArgumentException("Invalid status code", nameof(customStatusCode))
        };
    }
}
