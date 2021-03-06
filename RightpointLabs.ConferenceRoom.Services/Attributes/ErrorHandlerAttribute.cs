﻿using log4net;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Services.Controllers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Http.Filters;

namespace RightpointLabs.ConferenceRoom.Services.Attributes
{
    /// <summary>
    /// Filter attribute for handling exceptions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ErrorHandlerAttribute : ExceptionFilterAttribute
    {
        private static readonly ILog __log = LogManager.GetLogger(typeof(ErrorHandlerAttribute));

        /// <summary>
        /// Raises the exception event.
        /// </summary>
        /// <param name="actionExecutedContext">The context for the action.</param>
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnException(actionExecutedContext);

            var log = __log;

            try
            {
                var baseController = actionExecutedContext.ActionContext.ControllerContext.Controller as BaseController;

                if (baseController != null)
                {
                    log = baseController.Log;
                }

                if (actionExecutedContext.Exception != null &&
                    IsAccessDeniedException(actionExecutedContext.Exception))
                {
                    actionExecutedContext.Response = actionExecutedContext.Request
                        .CreateResponse(HttpStatusCode.OK, new { error = "Access Denied", });
                }
            }
            catch (Exception ex)
            {
                //We had an error handling the error...
                log.ErrorFormat("Time: {0}, Message: {1}", DateTime.Now, ex);
            }
        }

        /// <summary>
        /// Is the exception an <see cref="AccessDeniedException"/> or does it contain an <see cref="AccessDeniedException"/>?
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private bool IsAccessDeniedException(Exception ex)
        {
            if (ex is AccessDeniedException)
            {
                return true;
            }
            else if (ex is AggregateException)
            {
                var aggEx = ex as AggregateException;
                return aggEx.InnerExceptions.Any(IsAccessDeniedException);
            }
            else if (ex.InnerException != null)
            {
                return IsAccessDeniedException(ex.InnerException);
            }
            else
            {
                return false;
            }
        }

        protected static string GetClientIp(HttpRequestMessage request = null)
        {
            // from https://trikks.wordpress.com/2013/06/27/getting-the-client-ip-via-asp-net-web-api/
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }

    }
}