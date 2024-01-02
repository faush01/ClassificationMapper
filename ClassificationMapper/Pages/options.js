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

    function RemoveClassification(view, c_name) {
        console.log("removing : " + c_name);
        if (!confirm("Are you sure you want to delete this mapping?")) {
            return;
        }
        ApiClient.getNamedConfiguration("classification_mapping").then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));
            delete config.Mappings[c_name];
            ApiClient.updateNamedConfiguration("classification_mapping", config);
            PopulateSettingsPage(view, config);
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

    function SaveMappings(view) {
        var mappings = view.querySelectorAll("#mapping_item");
        //console.log(mappings);

        ApiClient.getNamedConfiguration("classification_mapping").then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));
            mappings.forEach(function (map_item) {
                let key = map_item.attributes["map_key"].value;
                let value = map_item.value;
                console.log(key + " - " + value);
                var new_value_tokens = value.split(",");
                new_value_tokens = CleanMapItems(new_value_tokens);
                config.Mappings[key] = new_value_tokens;
            });
            ApiClient.updateNamedConfiguration("classification_mapping", config);
            PopulateSettingsPage(view, config);
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
            let key_span = document.createElement("span")
            key_span.style.fontWeight = "bold";
            key_span.fontSize = "25px";
            key_span.appendChild(document.createTextNode(key))
            td.appendChild(key_span);
            tr.appendChild(td);

            td = document.createElement("td");
            var input_maps = document.createElement("input");
            input_maps.type = "text";
            input_maps.style.width = "450px";
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
        var new_name = view.querySelector('#add_new_name');
        if (new_name.value == "") {
            return;
        }
        console.log(new_name.value);
        ApiClient.getNamedConfiguration("classification_mapping").then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            if (new_name.value in config.Mappings) {
                return;
            }

            config.Mappings[new_name.value] = [];
            ApiClient.updateNamedConfiguration("classification_mapping", config);
            new_name.value = "";
            PopulateSettingsPage(view, config);
        });
    }

    function IncludeMoviesChanged(view) {
        var check_include_movies = view.querySelector("#include_movies");
        ApiClient.getNamedConfiguration("classification_mapping").then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            config.IncludeMovies = check_include_movies.checked;

            console.log("Config Options : " + JSON.stringify(config));

            ApiClient.updateNamedConfiguration("classification_mapping", config);
            PopulateReportData(view, config);
        });
    }

    function IncludeSeriesChanged(view) {
        var check_include_series = view.querySelector("#include_series");
        ApiClient.getNamedConfiguration("classification_mapping").then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            config.IncludeSeries = check_include_series.checked;

            console.log("Config Options : " + JSON.stringify(config));

            ApiClient.updateNamedConfiguration("classification_mapping", config);
            PopulateReportData(view, config);
        });
    }

    function IncludeCorrectChanged(view) {
        var check_include_correct = view.querySelector("#include_correct");
        ApiClient.getNamedConfiguration("classification_mapping").then(function (config) {
            console.log("Config Options : " + JSON.stringify(config));

            config.IncludeCorrect = check_include_correct.checked;

            console.log("Config Options : " + JSON.stringify(config));

            ApiClient.updateNamedConfiguration("classification_mapping", config);
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

        var url = "class_mapper/get_report?ItemType=" + types_string + "&IncludeCorrect=" + include_correct + "&stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);

        var report_table = view.querySelector('#report_table');

        ApiClient.getApiData(url).then(function (report_data) {
            console.log("report_data : " + JSON.stringify(report_data));

            let report_rows = "";
            report_data.forEach(function (report_item, index) {
                report_rows += "<tr>"
                report_rows += "<td>" + report_item.Key + "</td>"
                report_rows += "<td>" + report_item.Value + "</td>"
                report_rows += "</tr>"
            });

            report_table.innerHTML = report_rows;
        });

    }

    function PopulateSettings(view, config) {
        view.querySelector("#include_correct").checked = config.IncludeCorrect;
        view.querySelector("#include_movies").checked = config.IncludeMovies;
        view.querySelector("#include_series").checked = config.IncludeSeries;
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            ApiClient.getNamedConfiguration("classification_mapping").then(function (config) {
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
            
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});
