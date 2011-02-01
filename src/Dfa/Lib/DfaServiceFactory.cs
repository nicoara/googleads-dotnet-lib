// Copyright 2010, Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// Author: api.anash@gmail.com (Anash P. Oommen)

using Google.Api.Ads.Common.Lib;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Google.Api.Ads.Dfa.Lib {
  /// <summary>
  /// The factory class for all DFA API services.
  /// </summary>
  public class DfaServiceFactory : ServiceFactory {
    /// <summary>
    /// The config class to be used with this object.
    /// </summary>
    private DfaAppConfig config = new DfaAppConfig();

    /// <summary>
    /// The user token to be sent as part of Soap Headers.
    /// </summary>
    private UserToken authToken = null;

    Dictionary<string, string> headers = new Dictionary<string, string>();

    /// <summary>
    /// Gets an App.config reader suitable for this factory.
    /// </summary>
    public override AppConfigBase AppConfig {
      get {
        return config;
      }
    }

    /// <summary>
    /// Default public constructor.
    /// </summary>
    public DfaServiceFactory() {
    }

    /// <summary>
    /// Create SOAP headers based on a set of key-value pairs.
    /// </summary>
    /// <param name="headers">A dictionary, with key-value pairs as headername,
    /// headervalue.</param>
    public override void SetHeaders(Dictionary<string, string> headers) {
      this.headers = headers;
      authToken = null;
    }

    /// <summary>
    /// Create a service object.
    /// </summary>
    /// <param name="signature">Signature of the service being created.</param>
    /// <param name="user">The user for which the service is being created.
    /// <param name="serverUrl">The server to which the API calls should be
    /// made.</param>
    /// </param>
    /// <returns>An object of the desired service type.</returns>
    public override AdsClient CreateService(ServiceSignature signature, AdsUser user,
        Uri serverUrl) {
      if (serverUrl == null) {
        serverUrl = new Uri(config.DfaApiServer);
      }

      if (user == null) {
        throw new ArgumentNullException("user");
      }

      if (signature == null) {
        throw new ArgumentNullException("signature");
      }

      if (!(signature is DfaServiceSignature)) {
        throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture,
            DfaErrorMessages.SignatureIsOfWrongType, typeof(DfaServiceSignature)));
      }

      AdsClient service = CreateServiceWithoutAuthHeaders(signature, user, serverUrl);

      if (signature.ServiceName != "LoginRemoteService") {
        if (authToken == null) {
          authToken = GetAuthenticationToken(signature, user, serverUrl);
        }

        service.GetType().GetProperty("Token").SetValue(service, authToken, null);
        if (Convert.ToDecimal(signature.Version.Substring(1)) > 1.11M) {
          service.GetType().GetProperty("RequestHeader").SetValue(service,
              GetRequestHeader(), null);
          SetRequestHeaderNameSpace(signature as DfaServiceSignature, service);
        }
      }

      return service;
    }

    /// <summary>
    /// Gets the request header.
    /// </summary>
    /// <returns>The request header.</returns>
    private RequestHeader GetRequestHeader() {
      RequestHeader reqHeader = new RequestHeader();
      if (headers.ContainsKey("applicationName")) {
        reqHeader.ApplicationName = config.Signature + "|" + headers["applicationName"];
      }
      return reqHeader;
    }

    /// <summary>
    /// Generates an auth token using login service.
    /// </summary>
    /// <param name="signature">Signature of the service being created.</param>
    /// <param name="user">The user for which the service is being created.
    /// <param name="serverUrl">The server to which the API calls should be
    /// made.</param>
    /// </param>
    /// <returns>A token which may be used for future API calls.</returns>
    private UserToken GetAuthenticationToken(ServiceSignature signature, AdsUser user,
        Uri serverUrl) {
      if (headers.ContainsKey("authToken") && !String.IsNullOrEmpty(headers["authToken"])) {
        return new UserToken(headers["userName"], headers["authToken"]);
      }
      try {
        DfaServiceSignature loginServiceSignature = new DfaServiceSignature(signature.Version,
              "LoginRemoteService");
        AdsClient loginService = CreateServiceWithoutAuthHeaders(loginServiceSignature, user,
            serverUrl);

        object userProfile = loginService.GetType().GetMethod("authenticate").Invoke(
            loginService, new object[] { headers["userName"], headers["password"] });
        return new UserToken(
            userProfile.GetType().GetProperty("name").GetValue(userProfile, null).ToString(),
            userProfile.GetType().GetProperty("token").GetValue(userProfile, null).ToString());
      } catch (Exception ex) {
        throw new DfaException("Failed to authenticate user. See inner exception for details.", ex);
      }
    }

    /// <summary>
    /// Sets the request header namespace in outgoing Soap Requests.
    /// </summary>
    /// <param name="signature">The service creation parameters.</param>
    /// <param name="service">The service object for which RequestHeader
    /// needs to be patched.</param>
    private static void SetRequestHeaderNameSpace(DfaServiceSignature signature,
        AdsClient service) {
      // Set the header namespace prefix. For all /cm services, the members
      // shouldn't have xmlns. For all other services, the members should have
      // /cm as xmlns.
      object[] attributes = service.GetType().GetCustomAttributes(false);
      foreach (object attribute in attributes) {
        if (attribute is WebServiceBindingAttribute) {
          WebServiceBindingAttribute binding = (WebServiceBindingAttribute) attribute;

          string xmlns = binding.Namespace;
          RequestHeader svcRequestHeader = null;
          PropertyInfo propInfo = service.GetType().GetProperty("RequestHeader");
          if (propInfo != null) {
            svcRequestHeader = (RequestHeader) propInfo.GetValue(service, null);

            if (svcRequestHeader != null) {
              PropertyInfo wsPropInfo = svcRequestHeader.GetType().GetProperty("TargetNamespace");
              if (wsPropInfo != null) {
                wsPropInfo.SetValue(svcRequestHeader, xmlns, null);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Creates a service object without setting auth headers.
    /// </summary>
    /// <param name="signature">Signature of the service being created.</param>
    /// <param name="user">The user for which the service is being created.
    /// <param name="serverUrl">The server to which the API calls should be
    /// made.</param>
    /// </param>
    /// <returns>An object of the desired service type.</returns>
    private AdsClient CreateServiceWithoutAuthHeaders(ServiceSignature signature, AdsUser user,
        Uri serverUrl) {
      DfaServiceSignature dfaapiSignature = signature as DfaServiceSignature;

      AdsClient service = (AdsClient) Activator.CreateInstance(dfaapiSignature.ServiceType);

      if (config.Proxy != null) {
        service.Proxy = config.Proxy;
      }

      service.Url = string.Format("{0}{1}/api/dfa-api/{2}",
          serverUrl, dfaapiSignature.Version, dfaapiSignature.ServiceEndpoint);

      service.User = user;
      return service;
    }

    /// <summary>
    /// Reads the headers from App.config.
    /// </summary>
    /// <param name="config">The configuration class.</param>
    /// <returns>A dictionary, with key-value pairs as headername, headervalue.</returns>
    public override Dictionary<string, string> ReadHeadersFromConfig(AppConfigBase config) {
      DfaAppConfig dfaConfig = (DfaAppConfig) config;
      Dictionary<string, string> configHeaders = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(dfaConfig.AuthToken)) {
        configHeaders["authToken"] = dfaConfig.AuthToken;
      }

      configHeaders["userName"] = dfaConfig.UserName;
      configHeaders["password"] = dfaConfig.Password;
      configHeaders["applicationName"] = dfaConfig.ApplicationName;

      return configHeaders;
    }
  }
}
