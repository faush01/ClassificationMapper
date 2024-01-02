/*
Copyright(C) 2024

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<http://www.gnu.org/licenses/>.
*/

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ClassificationMapper
{
    // http://localhost:8096/emby/class_mapper/get_report?ItemType=Movie
    [Route("/class_mapper/get_report", "GET", Summary = "Gets a classification report")]
    //[Authenticated]
    public class GetReport : IReturn<Object>
    {
        [ApiMember(Name = "ItemType", Description = "Type of items to include in the report", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ItemType { get; set; }
        [ApiMember(Name = "IncludeCorrect", Description = "Include correct classifications", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string IncludeCorrect { get; set; }
    }

    public class ApiEndpoint : IService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;

        public ApiEndpoint(ILogManager logger, ILibraryManager libraryManager, IServerConfigurationManager config)
        {
            _logger = logger.GetLogger("ClassificationMapper - ApiEndpoint");
            _logger.Info("Loaded");
            _libraryManager = libraryManager;
            _config = config;
        }

        public object Get(GetReport request)
        {
            Dictionary<string, int> classification_count = new Dictionary<string, int>();

            if (string.IsNullOrEmpty(request.ItemType))
            {
                return classification_count.ToList();
            }

            PluginOptions config = _config.GetClassificationMappingOptions();
            string[] item_types = request.ItemType.Split(',');
            bool include_correct = request.IncludeCorrect.Equals("true", StringComparison.InvariantCultureIgnoreCase);

            InternalItemsQuery query = new InternalItemsQuery();
            query.IncludeItemTypes = item_types;
            BaseItem[] results = _libraryManager.GetItemList(query);

            foreach (BaseItem item in results)
            {
                string classification = item.OfficialRating;
                if(string.IsNullOrEmpty(classification))
                {
                    classification = "none";
                }

                if(config.Mappings.ContainsKey(classification) && !include_correct)
                {
                    continue;
                }

                if(!classification_count.ContainsKey(classification))
                {
                    classification_count[classification] = 0;
                }
                classification_count[classification]++;
            }

            List<KeyValuePair<string, int>> report_data = classification_count.ToList();
            report_data.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            return report_data;
        }
    }

}
