﻿using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Microsoft.Azure;

namespace SFA.DAS.Commitments.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            //TODO: enable this code
            //var apiKeySecret = CloudConfigurationManager.GetSetting("ApiTokenSecret");
            //var apiIssuer = CloudConfigurationManager.GetSetting("ApiIssuer");
            //var apiAudiences = CloudConfigurationManager.GetSetting("ApiAudiences").Split(' ');

            //config.MessageHandlers.Add(new ApiKeyHandler("Authorization", apiKeySecret, apiIssuer, apiAudiences));

            config.MapHttpAttributeRoutes();

            config.Services.Replace(typeof(IExceptionHandler), new CustomExceptionHandler());
        }
    }
}
