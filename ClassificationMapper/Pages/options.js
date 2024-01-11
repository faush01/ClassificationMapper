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

define(['mainTabsManager', 'dialogHelper'], function (dialogHelper) {
    'use strict';

    ApiClient.getApiData = function (url_to_get) {
        console.log("getApiData Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    ApiClient.sendPostQuery = function (url_to_get, query_data) {
        var post_data = JSON.stringify(query_data);
        console.log("sendPostQuery url  = " + url_to_get);
        //console.log("sendPostQuery data = " + post_data);
        return this.ajax({
            type: "POST",
            url: url_to_get,
            dataType: "json",
            data: post_data,
            contentType: 'application/json'
        });
    };

    function SaveConfigUrl() {
        return ApiClient.getUrl("class_mapper/save_config?stamp=" + new Date().getTime());
    }

    function GetConfigUrl() {
        return ApiClient.getUrl("class_mapper/get_config?stamp=" + new Date().getTime());
    }

    function SaveConfigData() {
        let url = "class_mapper/save_config?stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);
        console.log("Save config data url  : " + url);

        let config_data = {
            'IncludeMovies': true,
            'IncludeSeries': true,
            'IncludeCorrect': true,
            'Mappings': {'AU-ALL': ['PG', 'MA', 'R', 'X']}
        };

        ApiClient.sendPostQuery(url, config_data).then(function (result) {
            console.log("Save config data result : " + JSON.stringify(result));
        });
    }

    function RemoveClassification(view, c_name) {
        console.log("removing : " + c_name);
        if (!confirm("Are you sure you want to remove this mapping?")) {
            return;
        }
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));
            delete config.Mappings[c_name];
            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
                PopulateSettingsPage(view, config);
                PopulateReportData(view, config);
            });
        });
    }

    function CleanMapItems(map_items) {
        let new_map_list = [];
        map_items.forEach(function (item) {
            if (item.trim() != "") {
                new_map_list.push(item.trim());
            }
        });
        return new_map_list;
    }

    function VerifyMappings(map_data) {
        let running_list = []
        let keys = Object.keys(map_data);
        keys.forEach(function (key) {
            let value = map_data[key];
            let new_list = [];
            value.forEach(function (v) {
                if (!(v in map_data) && running_list.indexOf(v) == -1) {
                    new_list.push(v);
                    running_list.push(v);
                }
            });
            map_data[key] = new_list;
        });
        return map_data;
    }

    function SaveMappings(view) {
        var mappings = view.querySelectorAll("#mapping_item");
        //console.log(mappings);

        let new_maps = {}
        mappings.forEach(function (map_item) {
            let key = map_item.attributes["map_key"].value;
            let value = map_item.value;
            console.log(key + " - " + value);
            let new_value_tokens = value.split(",");
            new_maps[key] = CleanMapItems(new_value_tokens);           
        });
        new_maps = VerifyMappings(new_maps);

        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));
            config.Mappings = new_maps;
            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
                PopulateSettingsPage(view, config);
                PopulateReportData(view, config);
            });
        });
    }

    function PopulateSettingsPage(view, config) {
        console.log("Config Options : " + JSON.stringify(config));

        var mapping_table = view.querySelector('#mapping_table');

        // clear table
        while (mapping_table.firstChild) {
            mapping_table.removeChild(mapping_table.firstChild);
        }

        // populate table
        //for (const [key, value] of Object.entries(config.Mappings)) {
        Object.keys(config.Mappings).forEach(function (key, index) {
            var value = config.Mappings[key];
            var tr = document.createElement("tr");
            var td = null;

            td = document.createElement("td");
            td.style.width = "1%";
            let key_span = document.createElement("span")
            key_span.style.fontWeight = "bold";
            key_span.style.whiteSpace = "nowrap";
            key_span.fontSize = "25px";
            key_span.appendChild(document.createTextNode(key))
            td.appendChild(key_span);
            tr.appendChild(td);

            td = document.createElement("td");
            var input_maps = document.createElement("input");
            input_maps.type = "text";
            input_maps.style.width = "99%";
            input_maps.value = value.join(',');
            input_maps.setAttribute("map_key", key);
            input_maps.id = "mapping_item";
            input_maps.setAttribute("placeholder", "Comma (,) seperated list of classifications to map to this target");
            td.appendChild(input_maps);
            tr.appendChild(td);

            td = document.createElement("td");

            let i = document.createElement("i");
            i.title = "Remove";
            i.className = "md-icon";
            i.style.fontSize = "25px";
            i.style.cursor = "pointer";
            i.appendChild(document.createTextNode("highlight_off"));
            i.addEventListener("click", function () { RemoveClassification(view, key); });
            td.appendChild(i);

            tr.appendChild(td);

            mapping_table.appendChild(tr);
        });

    }

    function AddNewClassification(view) {
        var new_name_field = view.querySelector('#add_new_name');
        if (new_name_field.value == "") {
            return;
        }
        let new_name = new_name_field.value;
        new_name = new_name.trim();
        console.log(new_name);
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            if (new_name in config.Mappings) {
                return;
            }

            config.Mappings[new_name] = [];
            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
                new_name_field.value = "";
                PopulateSettingsPage(view, config);
                PopulateReportData(view, config);
            });
        });
    }

    function IncludeMoviesChanged(view) {
        var check_include_movies = view.querySelector("#include_movies");
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            config.IncludeMovies = check_include_movies.checked;

            console.log("Config Options : " + JSON.stringify(config));

            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
                PopulateReportData(view, config);
            });
        });
    }

    function IncludeSeriesChanged(view) {
        var check_include_series = view.querySelector("#include_series");
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            config.IncludeSeries = check_include_series.checked;

            console.log("Config Options : " + JSON.stringify(config));

            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
                PopulateReportData(view, config);
            });
        });
    }

    function IncludeCorrectChanged(view) {
        var check_include_correct = view.querySelector("#include_correct");
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            config.IncludeCorrect = check_include_correct.checked;

            console.log("Config Options : " + JSON.stringify(config));

            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
                PopulateReportData(view, config);
            });
        });
    }

    function OverrideLockedChanged(view) {
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            config.OverrideLocked = view.querySelector("#override_locked").checked;
            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
            });
        });
    }
    function FieldLockActionChanged(view) {
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            config.FieldLockAction = parseInt(view.querySelector('#field_lock_action').value);
            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
            });
        });
    }

    function BackupOriginalChanged(view) {
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            config.BackupOriginal = view.querySelector("#backup_original").checked;
            ApiClient.sendPostQuery(SaveConfigUrl(), config).then(function (result) {
                console.log("Save config data result : " + JSON.stringify(result));
            });
        });
    }

    function LibSelectChanged(view) {
        ApiClient.getApiData(GetConfigUrl()).then(function (config) {
            PopulateReportData(view, config);
        });
    }
    
    function PopulateReportData(view, config) {

        console.log("Config Options : " + JSON.stringify(config));

        let types = [];
        if (config.IncludeMovies) {
            types.push("Movie");
        }
        if (config.IncludeSeries) {
            types.push("Series");
        }
        let types_string = types.join(",");

        let include_correct = "false";
        if (config.IncludeCorrect) {
            include_correct = "true";
        }

        let parent_id = view.querySelector('#lib_select').value;

        var url = "class_mapper/get_report?ItemType=" + types_string + "&IncludeCorrect=" + include_correct + "&ParentId=" + parent_id + "&stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);

        ApiClient.getApiData(url).then(function (report_data) {
            console.log("report_data : " + JSON.stringify(report_data));

            let report_rows = "";
            report_data["classification_counts"].forEach(function (report_item, index) {
                report_rows += "<tr>"
                report_rows += "<td>" + report_item.Key + "</td>"
                report_rows += "<td>" + report_item.Value + "</td>"
                report_rows += "</tr>"
            });

            view.querySelector('#report_table').innerHTML = report_rows;
            view.querySelector('#locked_item_count').innerHTML = report_data["locked_items"];
            view.querySelector('#locked_field_count').innerHTML = report_data["locked_fields"];
            view.querySelector('#total_item_count').innerHTML = report_data["total_count"];
        });

    }

    function PopulateSettings(view, config) {
        view.querySelector("#include_correct").checked = config.IncludeCorrect;
        view.querySelector("#include_movies").checked = config.IncludeMovies;
        view.querySelector("#include_series").checked = config.IncludeSeries;
        view.querySelector("#override_locked").checked = config.OverrideLocked;
        view.querySelector('#field_lock_action').value = config.FieldLockAction;
        view.querySelector("#backup_original").checked = config.BackupOriginal;
    }

    function PupulateLibList(view) {
        var url = ApiClient.getUrl("class_mapper/get_libs?stamp=" + new Date().getTime());

        ApiClient.getApiData(url).then(function (lib_list) {
            console.log("lib_list : " + JSON.stringify(lib_list));

            let option_list = "<option value='-1'>All</option>";
            lib_list.forEach(function (lib_item, index) {
                option_list += "<option value='" + lib_item.Id + "'>"
                option_list += lib_item.Name
                option_list += "</option>"
            });

            view.querySelector('#lib_select').innerHTML = option_list;
        });
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            PupulateLibList(view);

            ApiClient.getApiData(GetConfigUrl()).then(function (config) {
                PopulateSettingsPage(view, config);
                PopulateReportData(view, config);
                PopulateSettings(view, config);
            });

            view.querySelector('#add_new_button').addEventListener("click", function () {
                AddNewClassification(view);
            });

            view.querySelector('#save_mappings').addEventListener("click", function () {
                SaveMappings(view);
            });

            view.querySelector('#include_movies').addEventListener("change", function () {
                IncludeMoviesChanged(view);
            });

            view.querySelector('#include_series').addEventListener("change", function () {
                IncludeSeriesChanged(view);
            });

            view.querySelector('#include_correct').addEventListener("change", function () {
                IncludeCorrectChanged(view);
            });

            view.querySelector('#override_locked').addEventListener("change", function () {
                OverrideLockedChanged(view);
            });

            view.querySelector('#field_lock_action').addEventListener("change", function () {
                FieldLockActionChanged(view);
            });

            view.querySelector('#backup_original').addEventListener("change", function () {
                BackupOriginalChanged(view);
            });

            view.querySelector('#lib_select').addEventListener("change", function () {
                LibSelectChanged(view);
            });
            
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});
