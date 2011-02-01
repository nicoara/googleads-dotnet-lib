﻿// Copyright 2010, Google Inc. All Rights Reserved.
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

using Google.Api.Ads.Dfa.Lib;
using Google.Api.Ads.Dfa.v111;

using System;
using System.Collections.Generic;
using System.Text;
using Google.Api.Ads.Common.Util;

namespace Google.Api.Ads.Dfa.Examples.v111 {
  /// <summary>
  /// This code example displays user name, Id, network Id, subnetwork Id,
  /// and user group Id for the given search criteria. Results are limited
  /// to the first 10 records.
  /// </summary>
  class GetUsers : SampleBase {
    /// <summary>
    /// Returns a description about the code example.
    /// </summary>
    public override string Description {
      get {
        return "This code example displays user name, Id, network Id, subnetwork Id, and user " +
            "group Id for the given search criteria. Results are limited to the first 10 records.";
      }
    }

    /// <summary>
    /// Main method, to run this code example as a standalone application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static void Main(string[] args) {
      SampleBase codeExample = new GetUsers();
      Console.WriteLine(codeExample.Description);
      codeExample.Run(new DfaUser());
    }

    /// <summary>
    /// Run the code example.
    /// </summary>
    /// <param name="user">The Dfa user object running the code example.
    /// </param>
    public override void Run(DfaUser user) {
      // Create UserRemoteService instance.
      UserRemoteService service = (UserRemoteService) user.GetService(
          DfaService.v111.UserRemoteService);

      String searchString = _T("INSERT_SEARCH_STRING_CRITERIA_HERE");

      // Set user search criteria.
      UserSearchCriteria searchCriteria = new UserSearchCriteria();
      searchCriteria.pageSize = 10;
      searchCriteria.searchString = searchString;

      try {
        // Get users that match the search criteria.
        UserRecordSet users = service.getUsersByCriteria(searchCriteria);

        // Display user names, Ids, network Ids, subnetwork Ids, and group Ids.
        if (users != null && users.records != null) {
          foreach (User userResult in users.records) {
            Console.WriteLine("User with name \"{0}\", Id \"{1}\", network Id \"{2}\", subnetwork" +
                " Id \"{3}\", and user group Id \"{4}\" was found.", userResult.name, userResult.id,
                userResult.networkId, userResult.subnetworkId, userResult.userGroupId);
          }
        } else {
          Console.WriteLine("No users found for your search criteria.");
        }
      } catch (Exception ex) {
        Console.WriteLine("Failed to retrieve users. Exception says \"{0}\"", ex.Message);
      }
    }
  }
}
