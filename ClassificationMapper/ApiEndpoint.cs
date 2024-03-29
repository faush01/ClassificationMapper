﻿/*
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

using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ClassificationMapper
{
    // http://localhost:8096/emby/class_mapper/get_report?ItemType=Movie
    [Route("/class_mapper/get_report", "GET", Summary = "Gets a classification report")]
    //[Authenticated]
    public class GetReport : IReturn<Object>
    {
        public string ItemType { get; set; }
        public string IncludeCorrect { get; set; }
        public long ParentId { get; set; } = -1;
    }

    // http://localhost:8096/emby/class_mapper/get_libs
    [Route("/class_mapper/get_libs", "GET", Summary = "Get a list of libraries")]
    //[Authenticated]
    public class GetLibs : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/class_mapper/get_config
    [Route("/class_mapper/get_config", "GET", Summary = "Gets plugin config")]
    //[Authenticated]
    public class GetConfig : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/class_mapper/save_config
    [Route("/class_mapper/save_config", "POST", Summary = "Saves plugin config")]
    //[Authenticated]
    public class SaveConfig : PluginOptions, IReturn<Object>
    {
    }

    public class ApiEndpoint : IService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationPaths _applicationPaths;
        private readonly IApplicationHost _appHost;

        public ApiEndpoint(ILogManager logger, 
            ILibraryManager libraryManager, 
            IServerConfigurationManager config, 
            IJsonSerializer jsonSerializer, 
            IFileSystem fileSystem,
            IApplicationPaths applicationPaths,
            IApplicationHost appHost)
        {
            _logger = logger.GetLogger("ClassificationMapper - ApiEndpoint");
            _libraryManager = libraryManager;
            _config = config;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _applicationPaths = applicationPaths;
            _appHost = appHost;
        }

        public object Get(GetLibs request)
        {
            List<Dictionary<string, object>> libs = new List<Dictionary<string, object>>();

            InternalItemsQuery query_collections = new InternalItemsQuery();
            query_collections.IncludeItemTypes = new string[] { "CollectionFolder" };
            query_collections.IsVirtualItem = false;
            query_collections.Recursive = true;
            BaseItem[] collections = _libraryManager.GetItemList(query_collections);
            foreach (BaseItem collection in collections)
            {
                Dictionary<string, object> lib = new Dictionary<string, object>();
                //_logger.Info("Lib Found : " + collection.Name + " - " + collection.InternalId);
                lib["Name"] = collection.Name;
                lib["Id"] = collection.InternalId;
                libs.Add(lib);
            }

            return libs;
        }

        public object Post(SaveConfig request)
        {
            string some_data = _jsonSerializer.SerializeToString(request);
            //_logger.Info("Submitted config data : " + some_data);

            ConfigStore config_store = ConfigStore.GetInstance(_appHost);
            config_store.SaveConfig(request);

            Dictionary<string, object> responce = new Dictionary<string, object>();
            responce["message"] = "config saved";
            return responce;
        }

        public object Get(GetConfig request)
        {
            ConfigStore config_store = ConfigStore.GetInstance(_appHost);
            PluginOptions loaded_config = config_store.GetConfig();

            return loaded_config;
        }

        public object Get(GetReport request)
        {
            Dictionary<string, object> report_result = new Dictionary<string, object>();

            if (string.IsNullOrEmpty(request.ItemType))
            {
                report_result["classification_counts"] = new Dictionary<string, int>().ToList();
                report_result["locked_items"] = 0;
                report_result["locked_fields"] = 0;
                report_result["total_count"] = 0;
                return report_result;
            }

            Dictionary<string, int> classification_count = new Dictionary<string, int>();
            int locked_field_count = 0;
            int locked_item_count = 0;
            int total_items = 0;
            
            ConfigStore config_store = ConfigStore.GetInstance(_appHost);
            PluginOptions config = config_store.GetConfig();

            string[] item_types = request.ItemType.Split(',');
            bool include_correct = request.IncludeCorrect.Equals("true", StringComparison.InvariantCultureIgnoreCase);

            InternalItemsQuery query = new InternalItemsQuery();
            query.IncludeItemTypes = item_types;
            query.IsVirtualItem = false;
            if(request.ParentId != -1)
            {
                query.ParentIds = new long[] { request.ParentId };
            }
            query.Recursive = true;
            BaseItem[] results = _libraryManager.GetItemList(query);

            foreach (BaseItem item in results)
            {
                total_items++;
                if (item.IsLocked)
                {
                    _logger.Info("Item Is Locked : " + item.Name);
                    locked_item_count++;
                }
                if(item.IsFieldLocked(MediaBrowser.Model.Entities.MetadataFields.OfficialRating))
                {
                    _logger.Info("Field Is Locked : " + item.Name);
                    locked_field_count++;
                }

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

            report_result["classification_counts"] = report_data;
            report_result["locked_items"] = locked_item_count;
            report_result["locked_fields"] = locked_field_count;
            report_result["total_count"] = total_items;

            return report_result;
        }
    }

}
