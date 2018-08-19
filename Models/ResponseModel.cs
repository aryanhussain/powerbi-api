using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Http.ModelBinding;

// ReSharper disable once CheckNamespace
namespace AirFusion.WindEdition.API
{
    public class ResponseViewModel<T> where T : class
    {
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public IList<string> Errors { get; set; }
    }
    public class ResponseViewModelWithAction<T> where T : class
    {
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public IList<string> Errors { get; set; }
        public bool ActionRequired { get; set; }
    }



    public enum ResponseResultTypes
    {
        [Description("Ok")]
        // ReSharper disable once InconsistentNaming
        OK = 200,
        [Description("Bad Request")]
        // ReSharper disable once InconsistentNaming
        BAD_REQUEST = 400,
        [Description("Resource Not Found")]
        // ReSharper disable once InconsistentNaming
        NOT_FOUND = 404,
        [Description("Server Error")]
        // ReSharper disable once InconsistentNaming
        ERROR = 500,
        [Description("Session Expired")]
        // ReSharper disable once InconsistentNaming
        Session_Expired = 401,
        /// <summary>
        /// Unsupported Media Type
        /// </summary>
        [Description("Unsupported Media Type")]
        UnsupportedMediaType = 415,
        /// <summary>
        /// Accepted indicates that the request 
        /// has been accepted for further processing.
        /// </summary>
        [Description("Accepted")]
        Accepted = 202,
    }

    public static class ResponseResult<T> where T : class
    {
        public static ResponseViewModel<T> GetResult(ResponseResultTypes type, string message, T data, IList<string> errors)
        {
            ResponseViewModel<T> response = new ResponseViewModel<T>
            {
                StatusCode = $"{(int)type}",
                Message = message,
                Data = data,
                Errors = errors
            };
            return response;
        }

        public static ResponseViewModelWithAction<T> GetResult(ResponseResultTypes type, string message, T data, IList<string> errors, bool isRestore)
        {
            ResponseViewModelWithAction<T> response = new ResponseViewModelWithAction<T>
            {
                StatusCode = $"{(int)type}",
                Message = message,
                Data = data,
                Errors = errors,
                ActionRequired = isRestore
            };
            return response;
        }

        public static ResponseViewModel<T> GetInvalidModel(ModelStateDictionary modelState)
        {
            List<string> modelErrors = new List<string>();
            foreach (string error in modelState.Keys)
            {
                if (modelState[error] != null && modelState[error].Errors != null && modelState[error].Errors.Any())
                    modelErrors.Add(modelState[error].Errors[0].ErrorMessage);
            }
            return GetResult(ResponseResultTypes.BAD_REQUEST, "Invalid data", null, modelErrors);
        }

        public static ResponseViewModel<T> GetException(Exception ex, string message)
        {
            List<string> exceptions = new List<string> { ex.Message };
            if (ex.InnerException != null)
                exceptions.Add(ex.InnerException.Message);
            return GetResult(ResponseResultTypes.ERROR, message, null, exceptions);
        }

        public static ResponseViewModel<T> GetErrorResult(string message)
        {
            List<string> exceptions = new List<string> { message };
            return GetResult(ResponseResultTypes.ERROR, message, null, exceptions);
        }

        public static ResponseViewModelWithAction<T> GetErrorResult(string message, bool isActionRequired)
        {
            List<string> exceptions = new List<string> { message };
            return GetResult(ResponseResultTypes.ERROR, message, null, exceptions, isActionRequired);
        }

        public static ResponseViewModel<T> GetErrorResult(ResponseResultTypes resultType, string message)
        {
            List<string> exceptions = new List<string> { message };
            return GetResult(resultType, message, null, exceptions);
        }

        public static ResponseViewModel<T> GetException(Exception ex)
        {
            return GetException(ex, "Server error");
        }

        public static ResponseViewModel<T> GetNotFound()
        {
            return GetResult(ResponseResultTypes.NOT_FOUND, "No results found.", null, null);
        }

        public static ResponseViewModel<T> GetSessionExpired()
        {
            return GetResult(ResponseResultTypes.Session_Expired, "Session Expired.", null, null);
        }

        public static ResponseViewModel<T> GetResult(T result)
        {
            return GetResult(ResponseResultTypes.OK, "Success", result, null);
        }

        public static ResponseViewModel<T> GetIdentityErrorResult(IdentityResult result)
        {
            if (result == null) return GetResult(ResponseResultTypes.ERROR, "Server error", null, null);

            if (!result.Succeeded && result.Errors != null)
                return GetResult(ResponseResultTypes.ERROR, "Server error", null, result.Errors.ToList());

            return null;
        }

        public static ResponseViewModel<T> GetIdentityErrorResult(List<string> result)
        {
            if (result == null) return GetResult(ResponseResultTypes.ERROR, "Server error", null, null);

            if (result.Any())
                return GetResult(ResponseResultTypes.ERROR, "Server error", null, result);

            return null;
        }

        public static ResponseViewModel<T> GetResult()
        {
            return GetResult(ResponseResultTypes.OK, "Success", null, null);
        }
    }
}