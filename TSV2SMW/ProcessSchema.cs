/*
    This file is part of tsv2smw.

    tsv2smw is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    tsv2smw is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with tsv2smw. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

using OD;

namespace TSV2SMW
{
    using ParamField = ValueTuple<(GroupId param, HeaderOptions options), string>;
    using SectionsDict = OrderedDictionary<SectionId, OrderedDictionary<GroupId, List<MainLine>>>;

    enum ParseSchemaState
    {
        START,
        CATEGORY,
        LIST,
        SUBPAGE,
        MAIN
    }

    public partial class Program
    {
        enum PhotoPageType {
            LINK,
            TEMPLATE,
            FORM
        }
        static string generateMultimediaPages(PhotoPageType type)
        {
            string link = $@"
{{#formlink:form=Foto
| link text=Carica una foto
| link type=button
}}";

            switch (type) {
                case PhotoPageType.LINK:
                    break;
                case PhotoPageType.TEMPLATE:
                    break;
                case PhotoPageType.FORM:
                    break;
            }

            return link;
        }

        static string generateDataTable(string catName, SectionsDict sections)
        {
            string res = $@"
<div style=""overflow-x: auto; white-space: nowrap;"">
";

            foreach (var secKey in sections.Keys) {
                res += $"\n=={secKey}==\n\n";

                res += "<tabber>\n\n";
                foreach (var groupKey in sections[secKey].Keys) {
                    bool empty = true;
                    string sec = $@"{groupKey} =
{{{{#ask:
    [[Category:{catName}]]
";
                    foreach (var fg in sections[secKey][groupKey]) {
                        if (!fg.options.Contains(OptionType.SUBPAGES) && !fg.options.Contains(OptionType.HIDDEN)) {
                            sec += $"      |?{fg.prop} = {fg.label}\n";
                            empty = false;
                        }
                    }
                    sec += $@"      |mainlabel={catName}
      |format=table
      |limit=25
      |class=datatable
  }}}}
|-|
";
                    if (!empty)
                        res += sec;
                }

                res += $"</tabber>\n\n";
            }

            return res + "\n</div>";
       }

        // TODO: do not use the nexus trick
        /// <summary>
        /// A method to generate a page with univariate plots for a (set of) properties.
        /// </summary>
        /// <param name="name">the name of the page.</param>
        /// <param name="sections">the sections containing the fields to plot (they will be sected according to thei types).</param>
        /// <param name="inputType">the input type of the property.</param>
        /// <param name="withCats">whether to consider categorical facets.</param>
        /// <param name="onServer">whether to generate a code for server processing.</param>
        /// <returns>a tuple with the title and the content of the page.</returns>
        public static (string, string) generateUnivariateChartPage(string name, SectionsDict sections, InputType inputType, bool withCats, bool onServer)
        {
            string res = "";
            string propLabel1 = Program.langManager.Get("Property");
            string propLabel2 = Program.langManager.Get("Class");
            var compatibleProperties1 = new List<string>();
            var compatibleProperties2 = new List<string>();
            string plotType = "";
            string widget = "ShinyPlot";
            string title = Program.langManager.Get("PropDistribution");

            switch (inputType) {
                case InputType.OPTION:
                    plotType = "pies";
                    break;
                case InputType.LIST:
                    plotType = "hbars";
                    break;
                case InputType.NUMBER:
                    plotType = "bars";
                    break;
                case InputType.DATE:
                    if (withCats)
                        plotType = "scurves";
                    else
                        plotType = "timelines";
                    break;
                case InputType.COORDS:
                    plotType = "maps";
                    break;
                case InputType.NEXUS:
                    plotType = "mapsAnat";
                    break;
                case InputType.TEXT:
                    plotType = "wordclouds";
                    break;
            }
            if (onServer) {
                plotType += "SrvAPI";
                widget += "Srv";
            }

            // either a single property in name or retrieve a set of them from the sections
            if (name != "")
                compatibleProperties1.Add(name);
            else {
                foreach (var section in sections.Values) {
                    foreach (var group in section.Values) {
                        foreach (var fg in group) {
                            if (fg.type == inputType &&
                                    (inputType != InputType.TEXT ||
                                    (inputType == InputType.TEXT && fg.options.Contains(OptionType.TEXTAREA))))
                                compatibleProperties1.Add(fg.prop);
                            if (fg.type == InputType.TEXT && !fg.options.Contains(OptionType.TEXTAREA))
                                compatibleProperties2.Add(fg.prop);
                        }
                    }
                }
            }

            res = string.Format(@"{{{{#widget:{0}
  |prop1_label={1}|prop1_data={2}
", widget, propLabel1, string.Join(",", compatibleProperties1));

            if (withCats)
                res += string.Format("  |prop2_label={0}|prop2_data={1}\n", propLabel2, string.Join(",", compatibleProperties2));
            else
                res += "  |prop2_label=\n";

            res += "|prop3_label=|prop3_data=\n";

            res += $"  |plot={plotType}|title={title}\n}}}}\n";

            return (title, res);
        }

        /// <summary>
        /// A method to generate a page with bivariate plots for a pair of properties.
        /// </summary>
        /// <param name="catName">the name of the category.</param>
        /// <param name="sections">the sections containing the fields to plot (they will be sected according to thei types).</param>
        /// <param name="inputType1">the input type of the first property.</param>
        /// <param name="inputType2">the input type of the second property.</param>
        /// <param name="onServer">whether to generate a code for server processing.</param>
        /// <returns>the content of the page.</returns>
        static string generateBivariateChartPage(string catName, SectionsDict sections, InputType inputType1, InputType inputType2, bool onServer)
        {
            string res = "";
            var compatibleProperties1 = new List<string>();
            var compatibleProperties2 = new List<string>();
            string plotType = "";
            string title = "";
            string widget = "ShinyPlot";            
            string propLabel1 = "", propLabel2 = "";
            string propType1 = "", propType2 = "";

            if (inputType1 == InputType.NUMBER && inputType2 == InputType.NUMBER) {
                plotType = "scatterplots";
                title = langManager.Get("PropCorrelation");
                propLabel1 = langManager.Get("Property") + " 1";
                propLabel2 = langManager.Get("Property") + " 2";
            }
            else if (inputType1 == InputType.NUMBER && (inputType2 == InputType.OPTION || inputType2 == InputType.LIST)) {
                plotType = "boxplots";
                title = langManager.Get("PropDistribution");
                propLabel1 = langManager.Get("Property");
                propLabel2 = langManager.Get("Class");
            }
            if (onServer) {
                plotType += "SrvAPI";
                widget += "Srv";
            }

            foreach (var section in sections.Values) {
                foreach (var group in section.Values) {
                    foreach (var fg in group) {
                        if (fg.type == inputType1)
                            compatibleProperties1.Add(fg.prop);
                        if (fg.type == inputType2)
                            compatibleProperties2.Add(fg.prop);
                    }
                }
            }

            if (onServer)
                res = string.Format(@"{{{{#widget:{0} |prop1_label={1}|prop1_data={2}
  |prop2_label={3}|prop2_data={4}
  |prop3_label=|prop3_data=
  |plot={5}|title={6}
}}}}", widget, propLabel1, string.Join(",", compatibleProperties1),
            propLabel2, string.Join(",", compatibleProperties2),
            plotType, title);
            else
                res = string.Format(@"{{{{#widget:{0} |prop1_label={1}|prop1_type={2}|prop1_data={3}
  |prop2_label={4}|prop2_type={5}|prop2_data={6}
  |prop3_label=|prop3_data=
  |plot={7}|title={8}
}}}}", widget, propLabel1, propType1, string.Join(",", compatibleProperties1),
            propLabel2, propType2, string.Join(",", compatibleProperties2),
            plotType, title);

            return res;
        }

        /// <summary>
        /// A method to generate a page with a timeline.
        /// </summary>
        /// <param name="catName">the name of the category.</param>
        /// <param name="sections">the sections containing the fields to plot (they will be sected according to thei types).</param>
        /// <returns>a tuple with the title and the content of the page.</returns>
        static Tuple<string, string> generateTimelinePage(string catName, SectionsDict sections)
        {
            string res = "";
            string props = "";
            var compatibleProperties = new List<string>();

            foreach (var section in sections.Values) {
                foreach (var group in section.Values) {
                    foreach (var fg in group) {
                        if (fg.type == InputType.DATE)
                            compatibleProperties.Add(fg.prop);
                    }
                }
            }

            string property = langManager.Get("Property");
            string timeline = langManager.Get("timeline");
            props = string.Format(@"<includeonly>
<div id='wikiPreview' style='display: none; padding-bottom: 25px; margin-bottom: 25px; border-bottom: 1px solid #AAAAAA;'></div>
{{{{{{info|page name={0}}}}}}}

{{{{{{for template|{0}}}}}}}
'''{1}''' {{{{{{field|{1}|input type=combobox|values={2} }}}}}}
{{{{{{end template}}}}}}

{{{{{{standard input|save}}}}}} {{{{{{standard input|cancel}}}}}}
</includeonly>", timeline, property, string.Join(",", compatibleProperties));

            // newlines cause errors
            res = $@"{{{{#formlink:form={timeline}|link text={langManager.Get("EditProperty")}|}}}}

{{{{#ask: [[Category:{catName}]] |?{{{{{{{langManager.Get("PropChart")}|}}}}}}= |format=timeline |limit=10000|headers=hide|timelinesize=300px|timelineposition=middle|timelinebands=MONTH,YEAR,DECADE }}}}
";

            return new Tuple<string, string>(res, props);
        }

        /// <summary>
        /// A method to generate a page with a plot for each property.
        /// </summary>
        /// <param name="catName">the name of the category.</param>
        /// <param name="sections">the sections containing the fields to plot (they will be sected according to thei types).</param>
        /// <returns>the content of the page.</returns>
        static string generateChartsPage(string catName, SectionsDict sections)
        {
            string res = "";
            foreach (var section in sections.Values) {
                foreach (var groupKey in section.Keys) {
                    res += $"\n=={groupKey}==\n";
                    foreach (var fg in section[groupKey]) {
                        // newlines cause errors
                        switch (fg.type) {
                            case InputType.LIST:
                            case InputType.TOKENS:
                                res += $"\n==={fg.prop}===\n";
                                res += $"{{{{#widget:Iframe|url=http://{GlobalConsts.IP_ADDRESS}:3838/hbars/?title={{{{urlencode: {langManager.Get("PropChart")} \"{fg.prop}\" }}}}&data={{{{urlencode: {{{{#ask: [[Category:{catName}]] |?{fg.prop}= |format=array|mainlabel=-|sep=,|manysep=,|headers=hide|hidegaps=all|limit=10000}}}} }}}}|width=800|height=400}}}}\n\n";
                                break;
                            case InputType.OPTION:
                                res += $"\n==={fg.prop}===\n"; // repeated in order to skip empty sections
                                res += $"{{{{#widget:Iframe|url=http://{GlobalConsts.IP_ADDRESS}:3838/pies/?title={{{{urlencode: {langManager.Get("PropChart")} \"{fg.prop}\" }}}}&data={{{{urlencode: {{{{#ask: [[Category:{catName}]] |?{fg.prop}= |format=array|mainlabel=-|sep=,|headers=hide|hidegaps=all|limit=10000}}}} }}}}|width=800|height=400}}}}\n\n";
                                break;
                            case InputType.NUMBER:
                                res += $"\n==={fg.prop}===\n";
                                res += $"{{{{#widget:Iframe|url=http://{GlobalConsts.IP_ADDRESS}:3838/bars/?title={{{{urlencode: {langManager.Get("PropHistogram")} \"{fg.prop}\" }}}}&data={{{{urlencode: {{{{#ask: [[Category:{catName}]] |?{fg.prop}= |format=array|mainlabel=-|sep=,|headers=hide|hidegaps=all|limit=10000}}}} }}}}|width=800|height=400}}}}\n\n";
                                break;
                            case InputType.DATE:
                                res += $"\n==={fg.prop}===\n";
                                res += $"{{{{#widget:Iframe|url=http://{GlobalConsts.IP_ADDRESS}:3838/timelinesSrvAPI/?title={{{{urlencode: {langManager.Get("PropHistogram")} \"{fg.prop}\" }}}}&data={{{{urlencode: {{{{#ask: [[Category:{catName}]] |?{fg.prop}= |format=array|mainlabel=-|sep=,|headers=hide|hidegaps=all|limit=10000}}}} }}}}|width=800|height=400}}}}\n\n";
                                break;
                            default:
                                if (fg.type == InputType.TEXT && !fg.options.Contains(OptionType.TEXTAREA))
                                Console.WriteLine($"INFO: skipping property '{fg.prop}' of type {fg.type} in graph");
                                break;
                        }
                    }
                    res += "\n";
                }

            }

            return res;
        }

        /// <summary>
        /// A method to generate a page with a map for each property.
        /// </summary>
        /// <param name="pageName">the name of the page.</param>
        /// <param name="fields">a list with the fields to map.</param>
        /// <param name="layerName">the name of the map overlay (it must be configured in the Leaflet map extension).</param>
        /// <param name="clickable">whether to generate clickable tooltips or not.</returns>
        public static string getMapCode(string pageName, List<string> fields, string layerName, bool clickable)
        {
            string fieldsStr = string.Join("\n", from field in fields select $" |?{field}");
            string clickCtrl = clickable ? "|copycoords=1\n |clicktarget=javascript:alert('Lat: %lat%, long: %long%')" : "";

            string layer = "";
            string clustermaxzoom = "";
            if (layerName != "default") {
                layer = "|layers=" + layerName;
                clustermaxzoom = "|clustermaxzoom=1";
            }

            return $@"
{{{{#ask: [[{pageName}]]
{fieldsStr}
 |format=leaflet
 |offset=0
 |link=all
 |headers=show
 |width=auto
 |height=auto
 {clustermaxzoom}
 |markercluster=on
 {layer}
 |scrollwheelzoom=1
 |pagelabel=true
 {clickCtrl}
 |showtitle=1
}}}}

";
        }

        /// <summary>
        /// A method to generate a page with a category tree.
        /// </summary>
        /// <param name="name">the name of the page.</param>
        /// <param name="mainCategory">the top category.</param>
        /// <returns>the content of the page.</returns>
        public static string createListPagesInCat(string name, string mainCategory)
        {
            if (mainCategory != "" && mainCategory != name)
                return "\n\n{{#categorytree:{{PAGENAME}}|mode=all|showcount=on}}";
            else
                return "";
        }

        /// <summary>
        /// A method to generate a page with a set of ASK queries for exporting data.
        /// </summary>
        /// <param name="catName">the name of the category.</param>
        /// <param name="templateFields">a list with the fields to export.</param>
        /// <param name="auxTemplates">a list with the auxiliary categories templates.</param>
        /// <returns>the content of the page.</returns>
        static string generateExportLinks(string catName, List<TemplateField> templateFields, List<Template> auxTemplates)
        {
            string res = "\n==Excel spreadsheets==\n";

            string sec = "", lastSec = "";
            bool first = true;
            var fields = new List<string>();
            int i = 0;
            string label = langManager.Get("Category") + $" '{catName}' " + langManager.Get("AsExcel");
            foreach (var field in templateFields) {
                sec = field.sec.ToString();
                if (!field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden)
                    fields.Add($"  |?{field.prop}" + ((field.prop == field.label) ? "" : " = " + field.label));
                if (i > 0 && sec != lastSec) {
                    label = langManager.Get("Category") + $" '{catName} - {lastSec}' " + langManager.Get("AsExcel");
                    string fieldsStr = string.Join("\n", fields);
                    string item = first ? "" : "* ";
                    res += $@"
{item}{{{{#ask:
  [[Category:{catName}]]
{fieldsStr}
  |searchlabel={label}
  |format=spreadsheet
}}}}";
                    first = false;
                    fields.Clear();
                }
                lastSec = sec;
                i++;
            }

            // print the remaining fields
            label = langManager.Get("Category") + $" '{catName} - {lastSec}' " + langManager.Get("AsExcel");
            string fieldsStr1 = string.Join("\n", fields);
                    res += $@"
* {{{{#ask:
  [[Category:{catName}]]
{fieldsStr1}
  |searchlabel={label}
  |format=spreadsheet
}}}}";

            res += "\n===Auxiliary data===\n";
            foreach (Template auxTemplate in auxTemplates) {
                string fields1 = string.Join("\n  ", from TemplateField field in auxTemplate.fieldsT
                                                     where !field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden
                                                     select $"  |?{field.prop}" + ((field.prop == field.label) ? "" : " = " + field.label));
                string cat = auxTemplate.categories.First();
                string labelAux = langManager.Get("Category") + " '" + auxTemplate.categories[GlobalConsts.FIRST_PART] + "' " + langManager.Get("AsExcel");
                res += $@"

* {{{{#ask:
  [[Category:{cat}]]
    |?{auxTemplate.linkProperty} = Parent
  {fields1}
  |searchlabel={labelAux}
  |format=spreadsheet
  |sort={auxTemplate.linkProperty}
}}}}";
            }

            sec = "";
            lastSec = "";
            first = true;
            fields.Clear();
            i = 0;
            res += "\n\n==R dataframes==\n";
            label = langManager.Get("Category") + $" '{catName}' " + langManager.Get("AsR");

            foreach (var field in templateFields) {
                sec = field.sec.ToString();
                if (!field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden)
                    fields.Add($"  |?{field.prop}" + ((field.prop == field.label) ? "" : " = " + field.label));
                if (i > 0 && sec != lastSec) {
                    label = langManager.Get("Category") + $" '{catName} - {lastSec}' " + langManager.Get("AsR");
                    string fieldsStr = string.Join("\n", fields);
                    string item = first ? "" : "* ";
                    res += $@"
{item}{{{{#ask:
  [[Category:{catName}]]
{fieldsStr}
  |searchlabel={label}
  |format=dataframe
}}}}";
                    first = false;
                    fields.Clear();
                }
                lastSec = sec;
                i++;
            }

            // print the remaining fields
            label = langManager.Get("Category") + $" '{catName} - {lastSec}' " + langManager.Get("AsR");
            fieldsStr1 = string.Join("\n", fields);
            res += $@"
* {{{{#ask:
  [[Category:{catName}]]
{fieldsStr1}
  |searchlabel={label}
  |format=dataframe
}}}}";

            res += "\n===Auxiliary data===\n";

            foreach (Template auxTemplate in auxTemplates) {
                string fields1 = string.Join("\n  ", from TemplateField field in auxTemplate.fieldsT
                                                     where !field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden
                                                     select $"  |?{field.prop} = {field.label}");
                string cat = auxTemplate.categories.First();
                string labelAux = langManager.Get("Category") + " '" + auxTemplate.categories[GlobalConsts.FIRST_PART] + "' " + langManager.Get("AsR");
                res += $@"

* {{{{#ask:
  [[Category:{cat}]]
    |?{auxTemplate.linkProperty} = Parent
  {fields1}
  |searchlabel={labelAux}
  |format=dataframe
  |sort={auxTemplate.linkProperty}
}}}}";
            }

            sec = "";
            lastSec = "";
            first = true;
            fields.Clear();
            i = 0;

            res += "\n\n==Prolog predicates==\n";
            label = langManager.Get("Category") + $" '{catName}' " + langManager.Get("AsProlog");

            foreach (var field in templateFields) {
                sec = field.sec.ToString();
                if (!field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden)
                    fields.Add($"  |?{field.prop}" + ((field.prop == field.label) ? "" : " = " + field.label));
                if (i > 0 && sec != lastSec) {
                    label = langManager.Get("Category") + $" '{catName} - {lastSec}' " + langManager.Get("AsProlog");
                    string fieldsStr = string.Join("\n", fields);
                    string item = first ? "" : "* ";
                    res += $@"
{item}{{{{#ask:
  [[Category:{catName}]]
{fieldsStr}
  |searchlabel={label}
  |format=prolog
}}}}";
                    first = false;
                    fields.Clear();
                }
                lastSec = sec;
                i++;
            }

            // print the remaining fields
            label = langManager.Get("Category") + $" '{catName} - {lastSec}' " + langManager.Get("AsProlog");
            fieldsStr1 = string.Join("\n", fields);
            res += $@"
* {{{{#ask:
  [[Category:{catName}]]
{fieldsStr1}
  |searchlabel={label}
  |format=prolog
}}}}";

            res += "\n===Auxiliary data===\n";

            foreach (Template auxTemplate in auxTemplates) {
                string fields1 = string.Join("\n  ", from TemplateField field in auxTemplate.fieldsT
                                                     where !field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden
                                                     select $"  |?{field.prop} = {field.label}");
                string cat = auxTemplate.categories.First();
                string labelAux = langManager.Get("Category") + " '" + auxTemplate.categories[GlobalConsts.FIRST_PART] + "' " + langManager.Get("AsProlog");
                res += $@"

* {{{{#ask:
  [[Category:{cat}]]
    |?{auxTemplate.linkProperty} = Parent
  {fields1}
  |searchlabel={labelAux}
  |format=prolog
  |sort={auxTemplate.linkProperty}
}}}}";
            }

            /*res += @"
[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::sysop|sysop]]";*/

            return res;
        }

        /// <summary>
        /// A method to generate the PropChainsHelper configuration.
        /// </summary>
        /// <param name="catName">the name of the category.</param>
        /// <param name="templateFields">a list with the fields to export.</param>
        /// <param name="auxTemplates">a list with the auxiliary categories templates.</param>
        /// <returns>the configuration to be placed in the LocalSettings.php file.</returns>
        static string createPropChainHelperConf(string catName, List<TemplateField> templateFields, List<Template> auxTemplates)
        {
            string res = "$pchCatLevels = [\n";
            res += $"  '{catName}' => 0,\n";

            var fields = new List<string>();
            int i = 0;
            foreach (var field in templateFields) {
                if (!field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden)
                    fields.Add($"  \"{field.prop}\" => [0, 0],");
                i++;
            }

            // Auxiliary data
            int chain = 0;
            var linkProps = new List<string>();
            foreach (Template auxTemplate in auxTemplates) {
                fields.Add(string.Join("\n", from TemplateField field in auxTemplate.fieldsT
                                                     where !field.isIdentifier && !field.isMultiple && !field.areSubpages && !field.isHidden
                                                     select $"  \"{field.prop}\" => [{chain}, 1],"));
                res += $"  '{auxTemplate.categories[GlobalConsts.FIRST_PART]}' => 1,\n";
                linkProps.Add(auxTemplate.linkProperty);
                chain++;
            }

            // print the fields            
            string fieldsStr = string.Join("\n", fields);
                    res += $@"];

$pchPropLevels = [
{fieldsStr}
];

$pchLinkProps = [
    {string.Join(",\n    ", from elem in linkProps select "['" + elem + "']")}
];";

            return res;
        }
        static void processSchema(Options options)
        {
            string templateXML;
            using (var reader = new StreamReader(@"templates/siteinfo.xml")) {
                templateXML = reader.ReadToEnd();
            }
            int endOfSiteInfo = templateXML.IndexOf("</siteinfo>") + 12;
            string templateXMLprefix = templateXML.Substring(0, endOfSiteInfo);
            string templateXMLsuffix = "</mediawiki>";

            SectionId currSection = new SectionId("MAIN");
            bool isFirstSection = true;

            string currList = "";
            string currSubPage = "";
            string currSubPageCat = "";
            string currCat = "";
            var names = new HashSet<string>();
            var subFields = new List<MainLine>();
            var properties = new List<Property>();
            var emptyOptionList = new List<OptionType>();
            // redundant because in this way just strings can be easily traced
            // sorted because the first one is special
            var existingProperties = new SortedSet<string>();
            var definedVariables = new SortedSet<string>();
            var superProperties = new Dictionary<string, Property>();
            var auxSimpleTemplates = new List<SimpleTemplate>();
            var auxTemplates = new List<Template>();
            var subForms = new List<CoreForm>();
            var auxForms = new List<Form>();
            var auxSimpleForms = new List<SimpleForm>();
            var auxCats = new List<Category>();
            string noteText = "Note";
            var categories = new List<Category>();
            var catHeaders = new List<Tuple<string, InputType>>();
            var categoryPages = new List<RawPage>();
            var groupPCats = new HashSet<string>();
            string lastGroupP = "";
            // keep track of (common) properties
            var propSet = new HashSet<string>();
            var pagesForCatLevels = new List<RawPage>();
            // category, fields, layer
            var maps = new Dictionary<string, ValueTuple<string, List<string>, string>>();
            int id = 1;
            if (options.StartID > 0)
                id = options.StartID;

            var sections = new SectionsDict();
            var emptyList = new List<string>();
            // a core template is a core page
            var emptyTemplates = new List<TemplateCall>();
            var emptyTemplateFields = new List<TemplateField>();
            var templateFields = new List<TemplateField>();

            var emptyFields = new List<ParamField>();

            Language language = AttributesHelperExtension.FromDescription<Language>(options.Language);
            Program.langManager = new LangManager();
            Program.langManager.SetLanguage(language);

            // a property to avoid the duplicate entries
            string idPropName = options.TFName + " ID";
            var idProp = new Property(id++, idPropName, "", InputType.TEXT, "", emptyOptionList, "");
            names.Add(idPropName);

            // for Semantic Dependency Updater
            Debug.Assert(!names.Contains("Semantic Dependency"), $"Duplicate property name 'Semantic Dependency'!");
            var sdUpdaterProp = new Property(id++, "Semantic Dependency", "", InputType.TEXT, "", emptyOptionList, "");
            names.Add("Semantic Dependency");

            // for Semantic Rating
            Debug.Assert(!names.Contains("Rating"), $"Duplicate property name 'Rating'!");
            var srComplProp = new Property(id++, "Rating", "General", InputType.TEXT, "", emptyOptionList, "");
            names.Add("Rating");

            var usedModules = new HashSet<string>();

            var state = ParseSchemaState.START;

            string linkPropName = Program.langManager.Get("Has") + " " + options.TFName;
            var linkProperties = new List<string>();

            int lineNum = 2;

            if (!File.Exists(options.InputFile)) {
                Console.WriteLine(string.Format("Cannot find input file '{0}'", options.InputFile));
                return;
            }
            using (var reader = new StreamReader(options.InputFile)) {
                var fields = new List<string>();

                while (!reader.EndOfStream) {
                    var line = convertEntities(reader.ReadLine().TrimEnd());
                    var values = line.Split('\t', StringSplitOptions.None);
                    if (line.TrimEnd() == "") {
                        var subTemplateFields = new List<TemplateField>();
                        var usedModules1 = new HashSet<string>();
                        if (currList != "") {
                            foreach (var subField in subFields) {
                                 subTemplateFields.Add(new TemplateField(currSection, subField.grp, subField.label, subField.prop,
                                    subField.prop, subField.type, "", subField.info, subField.showOnSelect, currCat, subField.options,
                                    existingProperties, definedVariables, linkPropName));
                                 if (subField.options.Contains(OptionType.MODULE)) {
                                    Debug.Assert(false, $"At line {lineNum}: Better not to use modules (in property '{subField.prop}')!");
                                    string module = subField.domain.Split("|")[GlobalConsts.FIRST_PART];
                                    usedModules.Add(module);
                                 }
                            }
                            var auxTemplate = new Template(id++, currList, langManager.Get("TemplateCaption") + " " + currList, subTemplateFields, linkPropName, new List<string> { currCat }, usedModules1, true);
                            auxTemplates.Add(auxTemplate);
                            subForms.Add(new CoreForm(currList, subFields, false, options.TFName));
                        }
                        else if (currSubPage != "") {
                            foreach (var secKey in sections.Keys) {
                                foreach (var groupKey in sections[secKey].Keys) {
                                    foreach (var fg in sections[secKey][groupKey]) {
                                        subTemplateFields.Add(new TemplateField(secKey, groupKey, fg.label, fg.prop, fg.prop, fg.type, fg.domain, fg.info,
                                            fg.showOnSelect, fg.category, fg.options, existingProperties, definedVariables, linkPropName));
                                        if (fg.options.Contains(OptionType.MODULE)) {
                                            Debug.Assert(false, $"At line {lineNum}: Better not to use modules (in property '{fg.prop}')!");
                                            string module = fg.domain.Split("|")[GlobalConsts.FIRST_PART];
                                            usedModules.Add(module);
                                        }
                                    }
                                }
                            }
                            var auxTemplate = new Template(id++, currSubPage, "", subTemplateFields, linkPropName, new List<string> { currSubPageCat }, usedModules1, false);
                            auxTemplates.Add(auxTemplate);
                            var auxForm = new Form(id++, currSubPage, "", sections, currSubPage, new List<CoreForm>(), noteText, currSubPageCat, linkPropName);
                            auxForms.Add(auxForm);
                            auxCats.Add(new Category(id++, currSubPageCat, "", "", "", auxForm, subTemplateFields, false));
                        }
                        state = ParseSchemaState.START;
                        currCat = "";
                        currList = "";
                        subFields.Clear();
                    }
                    else if (line.StartsWith("Group"))
                        continue;
                    else if (line.StartsWith("-")) {
                        linkPropName = values[GlobalConsts.PROPERTY];
                        //Debug.Assert(!names.Contains(linkPropName));
                        names.Add(linkPropName);
                        linkProperties.Add(linkPropName);
                    }
                    else if (values.Length > GlobalConsts.TYPE && values[GlobalConsts.TYPE] == "Note")
                        noteText = values[GlobalConsts.PROPERTY];
                    else if (state == ParseSchemaState.LIST) {
                        Debug.Assert(currList != "");
                        string domain = "";
                        string notes = "";
                        string info = "";
                        string showOnSelect = "";
                        string groupP = values[GlobalConsts.GROUP];
                        
                        if (values.Length >= 5)
                            domain = values[GlobalConsts.DOMAIN];
                        if (values.Length >= 6)
                            notes = values[GlobalConsts.NOTES];
                        if (values.Length >= 7)
                            info = values[GlobalConsts.INFO];
                        if (values.Length >= 8)
                            showOnSelect = values[GlobalConsts.SHOW_ON_SELECT];
                        var mainLine = new MainLine(new GroupId(values[GlobalConsts.GROUP]), values[GlobalConsts.SUPER_PROPERTY], values[GlobalConsts.PROPERTY],
                                                        values[GlobalConsts.TYPE], domain, notes, info, showOnSelect);
                        // add the group as a category for property groups (just one)
                        if (!groupPCats.Contains(groupP)) {
                            categories.Add(new Category(id++, groupP, groupP, "", "", null, emptyTemplateFields, true));
                            groupPCats.Add(groupP);
                        }
                        else if (lastGroupP != groupP)
                        {
                            Debug.Assert(false, $"At line {lineNum}: The group {groupP} has already been used!");
                        }
                        lastGroupP = groupP;

                        if (mainLine.superProperty != "" && !superProperties.TryGetValue(mainLine.superProperty, out var sp)) {
                            Debug.Assert(!names.Contains(mainLine.superProperty), $"At line {lineNum}: Duplicate super-property '{mainLine.superProperty}'!");
                            superProperties.Add(mainLine.superProperty, new Property(id++, mainLine.superProperty, "", mainLine.type, mainLine.domain, emptyOptionList, groupP));
                            names.Add(mainLine.superProperty);
                        }
                        Debug.Assert(!names.Contains(mainLine.prop), $"At line {lineNum}: Duplicate property '{mainLine.prop}'!");
                        properties.Add(new Property(id++, mainLine.prop, mainLine.superProperty, mainLine.type, mainLine.domain, mainLine.options, groupP));
                        names.Add(mainLine.prop);
                        existingProperties.Add(mainLine.prop);
                        subFields.Add(mainLine);
                    }
                    else if (state == ParseSchemaState.CATEGORY) {
                        Debug.Assert(currCat != "");
                        string parent = "";
                        var forCategory = new HeaderOptions(false, true, "");
                        var tFields = new List<ParamField> {};

                        for (int i = 2; i < values.Length; i++) {
                            string name = catHeaders[i - 2].Item1;
                            tFields.Add(((new GroupId(name), forCategory), values[i]));
                        }
                        // add the current category
                        tFields.Add(((new GroupId("Parent"), forCategory), currCat));
                        if (values.Length > 1) {
                            var values1 = values[GlobalConsts.PARENT_CATEGORY].Split("|");
                            // in the template there are only four parameters for parents
                            Debug.Assert(values1.Length < 5, $"At line {lineNum}: There are at most four parameters for parents!");
                            // parents are categories and have to be normalized
                            for (int i = 1; i <= values1.Length; i++)
                                tFields.Add(((new GroupId("Parent" + i.ToString()), forCategory), normalizeNames(values1[i - 1])));
                        }
                        else
                            parent = currCat;

                        // all categories are categories, but we can group them by "level", that is a group having the same value for the "group category" property
                        string template1 = new TemplateCall("-", currCat, false, tFields).ToString();
                        categories.Add(new Category(id++, values[GlobalConsts.GROUP], currCat, template1, "", null, emptyTemplateFields, false));

                        // the leaves of a category hierarchy are pages (with properties)
                        /*if (superCat)
                            categories.Add(new Category(id++, values[GlobalConsts.GROUP], options.CatName, currCatProps, currCat, null, emptyTemplateFields, false));
                        else {
                            var tCall = new TemplateCall("-", currCat, false, tFields);
                            categoryPages.Add(new RawPage(id++, values[GlobalConsts.GROUP], NamespaceType.PAGE, "", tCall.ToString(), emptyList));
                        }*/
                    }
                    else if (values[GlobalConsts.FIRST_PART].StartsWith("List:") || values[GlobalConsts.CATEGORY].StartsWith("Repeated:")) {
                        var values1 = values[GlobalConsts.FIRST_PART].Split(':');
                        currList = values1[GlobalConsts.SECOND_PART];
                        if (currCat == "")
                            currCat = options.CatName;
                        state = ParseSchemaState.LIST;
                        sections.Clear();
                    }
                    else if (values[GlobalConsts.FIRST_PART].StartsWith("Subpage:")) {
                        var values1 = values[GlobalConsts.FIRST_PART].Split(':');
                        var values2 = values1[GlobalConsts.SECOND_PART].Split('/');
                        currSubPage = values2[GlobalConsts.FIRST_PART];
                        currSubPageCat = values2[GlobalConsts.SECOND_PART];
                        state = ParseSchemaState.SUBPAGE;
                        currSection = new SectionId("MAIN");
                        sections.Clear();
                        sections.Add(currSection, new OrderedDictionary<GroupId, List<MainLine>>());
                    }
                    else if (values[GlobalConsts.FIRST_PART].Contains("Category:")) {
                        var values1 = values[GlobalConsts.FIRST_PART].Split(':');
                        var valuesF = values1[GlobalConsts.SECOND_PART].Split("|");
                        currCat = valuesF[GlobalConsts.FIRST_PART];

                        string parentCat = "";
                        if (values.Count() > 1)
                            parentCat = values[GlobalConsts.SECOND_PART].Split(':')[GlobalConsts.SECOND_PART];

                        string propName = "";
                        string coordName = "";
                        propName = currCat;
                        var formFields = new List<MainLine> {};
                        for (int i = 1; i < values.Length; i++) {
                            var value = values[i];
                            var values2 = value.Split('[');
                            if (values2.Length > 1) {
                                coordName = values2[GlobalConsts.FIRST_PART];
                                var values3 = values2[GlobalConsts.SECOND_PART].Split(':');
                                string typeStr = values3[GlobalConsts.FIRST_PART];
                                typeStr = typeStr.Substring(0, typeStr.Length - 1);
                                if (values3.Length > 1) {
                                    var mapFields = valuesF[GlobalConsts.SECOND_PART].Split(',');
                                    var coordNames = from mf in mapFields select Program.capitalize(Program.normalizeNames(mf)) + "." + coordName;
                                    maps.Add(currCat, (propName, coordNames.ToList(), values3[GlobalConsts.SECOND_PART]));
                                }
                                InputType @type = AttributesHelperExtension.FromDescription<InputType>(typeStr);
                                catHeaders.Add(new Tuple<string, InputType>(coordName, InputType.LITERAL));
                                if (!propSet.Contains(coordName)) {
                                    Debug.Assert(!names.Contains(coordName), $"At line {lineNum}: Duplicate property '{coordName}'");
                                    properties.Add(new Property(id++, coordName, "", @type, "", emptyOptionList, ""));
                                    names.Add(coordName);
                                    propSet.Add(coordName);
                                }
                                formFields.Add(new MainLine(new GroupId("-"), "", coordName, typeStr, "", "", "", ""));
                            }
                        }
                        string pagesQuery = createListPagesInCat(currCat, options.CatName);
                        string coordLabel = langManager.Get("Coordinates");
                        string formName = currCat;
                        string body = "";
                        string layerName = "";
                        if (coordName != "") {
                            body += $"[[Property:{coordLabel}|{coordName}]]: [[{coordLabel}::{{{{{{{coordName}|}}}}}}]]\n\n";
                            //body += $"{{{{#formlink:form={formName}|link text=Modifica {coordLabel}|target={{{{PAGENAME}}}}|query string={formName}[{parent}]={{{{{{{parent}|}}}}}}&amp;{formName}[{coordName}]={{{{{{{coordName}|}}}}}}&amp;name={{{{urlencode:{{{{PAGENAME}}}} }}}} }}}}\n\n";
                            layerName = maps[currCat].Item3;
                            body += getMapCode(":Category:{{PAGENAME}}", new List<string> { coordName }, layerName, false);
                        }
                        body += pagesQuery;
                        var template1 = new SimpleTemplate(id++, propName, Program.langManager.Get("TemplateCaption") +  $" '{propName}'", body, options.TFName, new List<string> { "{{{Parent|}}}", "{{{Parent1|}}}", "{{{Parent2|}}}", "{{{Parent3|}}}", "{{{Parent4|}}}" }, new HashSet<string>());
                        auxSimpleTemplates.Add(template1);
                        /*if (coordName != "") {
                            formFields.Add(new MainLine(new GroupId("-"), "", "Parent", "List", $"Category:{currCat}", "Restricted,Exclusive", ""));
                            body = $"{{{{{{for template|{propName}}}}}}}\n=={{langManager.Get("PropertyMap")}} {{{{#urlget:name}}}}==\n";
                            body += Program.getMapCode(":Category:{{#urlget:name}}", new List<string> { coordName }, layerName, true);
                            auxSimpleForms.Add(new SimpleForm(id++, propName, $"{langManager.Get("FormPCaption")} '{propName}'", body, formFields, options.TFName));
                        }*/

                        // add the top category
                        // using the "template" parameter a bit improperly
                        string template2 = "{{#categorytree:{{PAGENAME}}|mode=all|showcount=on}}";
                        categories.Add(new Category(id++, propName, "", template2, "", null, emptyTemplateFields, false));
                        state = ParseSchemaState.CATEGORY;
                    }
                    else if (values[GlobalConsts.GROUP].StartsWith("Section:")) {
                        currSection = new SectionId(values[GlobalConsts.GROUP].Split(":")[GlobalConsts.SECOND_PART]);
                        if (isFirstSection) {
                            sections.Clear();
                            templateFields.Clear();
                            isFirstSection = false;
                        }
                        if (!sections.ContainsKey(currSection))
                            sections.Add(currSection, new OrderedDictionary<GroupId, List<MainLine>>());
                        currSubPage = "";
                        currList = "";
                        currCat = "";
                        state = ParseSchemaState.MAIN;
                    }
                    else if (state == ParseSchemaState.MAIN || state == ParseSchemaState.SUBPAGE) {
                        var groupkey = new GroupId(values[GlobalConsts.GROUP]);
                        // add a list the first time
                        if (!sections[currSection].ContainsKey(groupkey))
                            sections[currSection].Add(groupkey, new List<MainLine>());
                        string domain = "";
                        string notes = "";
                        string info = "";
                        string showOnSelect = "";
                        string groupP = "Group_" + values[GlobalConsts.GROUP];
                        if (values.Length < 4)
                            throw new Exception($"At line {lineNum}: Too few fields!");
                        if (values.Length >= 5)
                            domain = values[GlobalConsts.DOMAIN];
                        if (values.Length >= 6)
                            notes = values[GlobalConsts.NOTES];
                        if (values.Length >= 7)
                            info = values[GlobalConsts.INFO];
                        if (values.Length >= 8)
                            showOnSelect = values[GlobalConsts.SHOW_ON_SELECT];
                        var mainLine = new MainLine(new GroupId(values[GlobalConsts.GROUP]), values[GlobalConsts.SUPER_PROPERTY], values[GlobalConsts.PROPERTY],
                                                    values[GlobalConsts.TYPE], domain, notes, info, showOnSelect);
                        sections[currSection][groupkey].Add(mainLine);
                        
                        // add the group as a category for property groups (just one)
                        if (!groupPCats.Contains(groupP)) {
                            categories.Add(new Category(id++, groupP, groupP, "", "", null, emptyTemplateFields, true));
                            groupPCats.Add(groupP);
                        }
                        else if (lastGroupP != groupP)
                        {
                            Debug.Assert(false, $"At line {lineNum}: The group {groupP} has already been used!");
                        }
                        lastGroupP = groupP;

                        // add the super-property the first time
                        if (mainLine.superProperty != "" && !superProperties.TryGetValue(mainLine.superProperty, out var sp)) {
                            Debug.Assert(!names.Contains(mainLine.superProperty), $"At line {lineNum}: Duplicate property '{mainLine.superProperty}'!");
                            superProperties.Add(mainLine.superProperty, new Property(id++, mainLine.superProperty, "", mainLine.type, "", emptyOptionList, groupP));
                            names.Add(mainLine.superProperty);
                        }
                        templateFields.Add(new TemplateField(currSection, mainLine.grp, mainLine.label, mainLine.prop, mainLine.prop, mainLine.type, mainLine.domain,
                                                        mainLine.info, mainLine.showOnSelect, mainLine.category, mainLine.options, existingProperties,
                                                        definedVariables, linkPropName));
                        if (mainLine.options.Contains(OptionType.MODULE)) {
                            Debug.Assert(false, $"At line {lineNum}: Better not to use modules (in property '{mainLine.prop}')!");
                            string module = mainLine.domain.Split("|")[GlobalConsts.FIRST_PART];
                            usedModules.Add(module);
                        }
                        if (mainLine.type != InputType.REPEATED) {
                            if (mainLine.options.Contains(OptionType.VECTOR)) {
                                // a vector has type NUMBER but its property must be a text
                                Debug.Assert(!names.Contains(mainLine.prop), $"At line {lineNum}: Duplicate property '{mainLine.prop}'!");
                                properties.Add(new Property(id++, mainLine.prop, mainLine.superProperty, InputType.TEXT, mainLine.domain, mainLine.options, groupP));
                                names.Add(mainLine.prop);
                                var vectorElems = (from elem in mainLine.domain.Split(",")
                                                   where elem.StartsWith("elems=")
                                                   select elem.Substring(6)).First().Split(":");
                                foreach (string elem in vectorElems) {
                                    string name = mainLine.prop + " " + elem;
                                    Debug.Assert(!names.Contains(name), $"At line {lineNum}: Duplicate name '{name}'!");
                                    properties.Add(new Property(id++, name, mainLine.prop, mainLine.type, "", emptyOptionList, groupP));
                                    names.Add(name);
                                }
                            }
                            else {
                                Debug.Assert(!names.Contains(mainLine.prop), $"At line {lineNum}: Duplicate property '{mainLine.prop}'!");
                                properties.Add(new Property(id++, mainLine.prop, mainLine.superProperty, mainLine.type, mainLine.domain, mainLine.options, groupP));
                                names.Add(mainLine.prop);
                            }
                            existingProperties.Add(mainLine.prop);
                        }
                    }
                    else
                        throw new Exception($"At line {lineNum}: INVALID STATE (id = {id})!");
                    id++;
                    lineNum++;
                }
            }

            // language dependent parametrized pages
            if (language == Language.Neutral)
                language = Language.Italiano;

            // the "name" parameter is overwritten by the commented text when the parameter "text" is empty
            var introduction = new RawPage(id++, "", NamespaceType.PAGE, "introduction_param.md", "", new List<string>() { options.WikiName, options.TFName, options.CatName });
            var panelMenu = new RawPage(id++, "", NamespaceType.MEDIAWIKI, "sidebar_param.md", "", new List<string>() { options.TFName, options.CatName });
            var findEntryForm = new RawPage(id++, "", NamespaceType.FORM, "form_SearchEntry_param.md", "", new List<string>() { options.TFName, options.CatName });
            var findEntryTemplate = new RawPage(id++, "", NamespaceType.TEMPLATE, "template_SearchEntry_param.md", "", new List<string>() { options.TFName, options.CatName });
            var editPage = new RawPage(id++, "", NamespaceType.PAGE, "modify_entry_param.md", "", new List<string>() { options.TFName });
            
            var mainScripts = new RawPage(id++, "MediaWiki:Common.js", NamespaceType.MEDIAWIKI, "common_param.js", "", new List<string>() { options.WikiName });

            var shinyPlotSrv = new RawPage(id++, "Widget:ShinyPlotSrv", NamespaceType.WIDGET, "widgets_ShinyPlotSrv_param.md", "", new List<string>() { options.CatName });

            var trueWords = new RawPage(id++, "MediaWiki:Smw_true_words", NamespaceType.MEDIAWIKI, "", "vero,v,sì,s,true,t,yes,y", emptyList);

            var dataPage = new RawPage(id++, Program.langManager.Get("DataTables"), NamespaceType.PAGE, "", generateDataTable(options.CatName, sections), emptyList);

            var chartsPage = new RawPage(id++, Program.langManager.Get("DataDistribution"), NamespaceType.PAGE, "", generateChartsPage(options.CatName, sections), emptyList);

            // in the following two templates and forms it is easier to add a prefix in the name and use a Page
            var (_, chartText) = generateUnivariateChartPage("", sections, InputType.OPTION, false, true);

            foreach (var auxForm in auxForms) {
                var (_, chartText1) = generateUnivariateChartPage("", auxForm.sections, InputType.OPTION, false, true);
                chartText += "\n\n" + chartText1;
            }
            var pieChartPage = new RawPage(id++, Program.langManager.Get("PieCharts"), NamespaceType.PAGE, "", chartText, emptyList);

            (_, chartText) = generateUnivariateChartPage("", sections, InputType.LIST, false, true);
            var barChartPage = new RawPage(id++, Program.langManager.Get("BarCharts"), NamespaceType.PAGE, "", chartText, emptyList);

            (_, chartText) = generateUnivariateChartPage("", sections, InputType.NUMBER, false, true);
            var histPage = new RawPage(id++, Program.langManager.Get("Histograms"), NamespaceType.PAGE, "", chartText, emptyList);

            (_, chartText) = generateUnivariateChartPage("", sections, InputType.DATE, true, true);
            var sCurvePage = new RawPage(id++, Program.langManager.Get("SurvivalCurves"), NamespaceType.PAGE, "", chartText, emptyList);

            (_, chartText) = generateUnivariateChartPage("", sections, InputType.DATE, false, true);
            var timelinesPage = new RawPage(id++, Program.langManager.Get("Timelines"), NamespaceType.PAGE, "", chartText, emptyList);

            // tag clouds
            (_, chartText) = generateUnivariateChartPage("", sections, InputType.TEXT, true, true);
            var tcPage = new RawPage(id++, Program.langManager.Get("WordClouds"), NamespaceType.PAGE, "", chartText, emptyList);

            // TODO: to be generalized
            /*var audiogramPage1 = new RawPage(id++, "Audiogrammi vocali", NamespaceType.PAGE, "", @"
{{#widget:ShinyPlot
|prop1_label=Proprietà|prop1_type=text|prop1_data=Esame vocale VA sx alla diagnosi,Esame vocale VA dx alla diagnosi
|prop2_label=|prop2_type=text|prop2_data=
|plot=audioplots|title=Audiogramma vocale della proprietà|entry=true|labels=20,30,40,50,60,70,80,90,100,110}}
", emptyList);
            var audiogramPage2 = new RawPage(id++, "Audiogrammi vocali per confronto", NamespaceType.PAGE, "", @"
{{#widget:ShinyPlot
|prop1_label=Primo campo|prop1_type=text|prop1_data=Esame vocale VA sx alla diagnosi,Esame vocale VA dx alla diagnosi
|prop2_label=Secondo campo|prop2_type=text|prop2_data=Esame vocale VA sx alla diagnosi,Esame vocale VA dx alla diagnosi
|plot=audioplots|title=Audiogrammi vocali|labels=20,30,40,50,60,70,80,90,100,110}}
", emptyList);
            var audiogramPage3 = new RawPage(id++, "Audiogrammi delta vocali alla visita con protesi", NamespaceType.PAGE,  "", @"
{{#widget:ShinyPlot
|prop1_label=Diagnosi|prop1_type=text|prop1_data=Esame vocale VA sx alla diagnosi,Esame vocale VA dx alla diagnosi
|prop2_label=Esame|prop2_type=text|prop2_data=Esame vocale VA sx con protesi,Esame vocale VA dx alla diagnosi con protesi
|visitCat=Esami con protesi|numberLabel=Numero esame con protesi
|plot=audioplots|title=Audiogramma vocale delle proprietà|labels=20,30,40,50,60,70,80,90,100,110}}
", emptyList);
            var audiogramPage4 = new RawPage(id++, "Audiogrammi delta vocali alla visita con IC", NamespaceType.PAGE, "", @"
{{#widget:ShinyPlot
|prop1_label=Diagnosi|prop1_type=text|prop1_data=Esame vocale VA sx alla diagnosi,Esame vocale VA dx alla diagnosi
|prop2_label=Esame|prop2_type=text|prop2_data=Esame vocale VA sx con IC,Esame vocale VA dx alla diagnosi con IC
|visitCat=Esami con IC|numberLabel=Numero esame con IC
|plot=audioplots|title=Audiogramma vocale delle proprietà|labels=20,30,40,50,60,70,80,90,100,110}}
", emptyList);

            var audiogramPage5 = new RawPage(id++, "Audiogrammi tonali", NamespaceType.PAGE, "", @"
{{#widget:ShinyPlot
|prop1_label=Proprietà|prop1_type=text|prop1_data=Esame tonale VA sx alla diagnosi,Esame tonale VA dx alla diagnosi,Esame tonale VO in dB nHL sx alla diagnosi,Esame tonale VO in dB nHL dx alla diagnosi
|prop2_label=|prop2_type=text|prop2_data=
|plot=audiograms|title=Audiogramma della proprietà|patient=true|labels=250,500,1000,2000,4000,6000}}
", emptyList);
            var audiogramPage6 = new RawPage(id++, "Audiogrammi tonali per confronto", NamespaceType.PAGE, "", @"
{{#widget:ShinyPlot
|prop1_label=Primo campo|prop1_type=text|prop1_data=Esame tonale VA sx alla diagnosi,Esame tonale VA dx alla diagnosi,Esame tonale VO in dB nHL sx alla diagnosi,Esame tonale VO in dB nHL dx alla diagnosi
|prop2_label=Secondo campo|prop2_type=text|prop2_data=Esame tonale VA sx alla diagnosi,Esame tonale VA dx alla diagnosi,Esame tonale VO in dB nHL sx alla diagnosi,Esame tonale VO in dB nHL dx alla diagnosi
|plot=audiograms|title=Audiogrammi|labels=250,500,1000,2000,4000,6000}}
", emptyList);
            var audiogramPage7 = new RawPage(id++, "Audiogrammi delta tonali alla visita con protesi", NamespaceType.PAGE, "", @"
{{#widget:ShinyPlot
|prop1_label=Diagnosi|prop1_type=text|prop1_data=Esame tonale VA sx alla diagnosi,Esame tonale VA dx alla diagnosi,Esame tonale VO in dB nHL sx alla diagnosi,Esame tonale VO in dB nHL dx alla diagnosi
|prop2_label=Esame|prop2_type=text|prop2_data=Esame tonale VA sx con protesi,Esame tonale VA dx con protesi,Esame tonale VO in dB nHL sx con protesi,Esame tonale VO in dB nHL dx con protesi
|visitCat=Esami con protesi|numberLabel=Numero esame con protesi
|plot=audiograms|title=Audiogramma della proprietà|labels=250,500,1000,2000,4000,6000}}
", emptyList);
            var audiogramPage8 = new RawPage(id++, "Audiogrammi delta tonali alla visita con IC", NamespaceType.PAGE, "", @"
{{#widget:ShinyPlot
|prop1_label=Diagnosi|prop1_type=text|prop1_data=Esame tonale VA sx alla diagnosi,Esame tonale VA dx alla diagnosi,Esame tonale VO in dB nHL sx alla diagnosi,Esame tonale VO in dB nHL dx alla diagnosi
|prop2_label=Esame|prop2_type=text|prop2_data=Esame tonale VA sx con IC,Esame tonale VA dx con IC,Esame tonale VO in dB nHL sx con IC,Esame tonale VO in dB nHL dx con IC
|visitCat=Esami con IC|numberLabel=Numero esame con IC
|plot=audiograms|title=Audiogramma della proprietà|labels=250,500,1000,2000,4000,6000}}
", emptyList);*/

            string spText = generateBivariateChartPage(options.CatName, sections, InputType.NUMBER, InputType.NUMBER, true);
            var spPage = new RawPage(id++, Program.langManager.Get("Scatterplots"), NamespaceType.PAGE, "", spText, emptyList);

            string bpText = generateBivariateChartPage(options.CatName, sections, InputType.NUMBER, InputType.OPTION, true);
            var bpPage = new RawPage(id++, Program.langManager.Get("Boxplots"), NamespaceType.PAGE, "", bpText, emptyList);

            var (tmQuery, tmFormText) = generateTimelinePage(options.CatName, sections);
            var tmPage = new RawPage(id++, Program.langManager.Get("TimeLineTemplate"), NamespaceType.PAGE, "", tmQuery, emptyList);
            var tmForm = new RawPage(id++, Program.langManager.Get("TimeLineForm"), NamespaceType.PAGE, "", tmFormText, emptyList);

            var exportPage = new RawPage(id++, Program.langManager.Get("ExportPageTitle"), NamespaceType.PAGE, "", generateExportLinks(options.CatName, templateFields, auxTemplates), emptyList);

            var template = new Template(id++, options.TFName, Program.langManager.Get("TemplateCaption") +  $" '{options.CatName}'", templateFields, "", new List<string>() { options.CatName }, usedModules, false);
            string formCaption = Program.langManager.Get("FormCCaption");
            var form = new Form(id++, options.TFName, formCaption + " " + options.CatName, sections, options.TFName, subForms, noteText, options.CatName, linkPropName);
            var mainCat = new Category(id++, options.CatName, options.CatName, "", "", form, templateFields, false);
            var superPropertiesString = string.Join("\n", from sp in superProperties.Values
                                                          select sp.ToXML());
            var propertiesString = string.Join("\n", from prop in properties
                                                     select prop.ToXML());

            StreamWriter sw = null;
            if (options.OutputFile == null) {
                sw = new StreamWriter(Console.OpenStandardOutput());
                sw.AutoFlush = true;
                Console.SetOut(sw);
            }
            else
                sw = new StreamWriter(options.OutputFile);

            sw.Write(templateXMLprefix.Replace("«HOST»", GlobalConsts.IP_ADDRESS).Replace("«NAME»", options.WikiName));

            sw.Write(introduction.ToXML());
            sw.Write(panelMenu.ToXML());
            sw.Write(mainScripts.ToXML());

            sw.Write(shinyPlotSrv.ToXML());

            sw.Write(findEntryForm.ToXML());
            sw.Write(findEntryTemplate.ToXML());
            sw.Write(editPage.ToXML());

            sw.Write(trueWords.ToXML());

            //Getting non parametric files based on language
            string dir = "";
            if (language == Language.English)
                dir = "en/";
            if (language == Language.English)
                dir = "it/";
            DirectoryInfo d = new DirectoryInfo("simple_pages/" + dir);
            FileInfo[] Files = d.GetFiles("*.*"); //Getting non parametric files
            foreach(FileInfo file in Files) {
                if (!file.Name.Contains("param")) {
                    var page = new RawPage(id++, "", NamespaceType.AUTO, file.Name, "", emptyList);
                    sw.Write(page.ToXML());
                }
            }

            // Getting non parametric files not based on language
            d = new DirectoryInfo("simple_pages");
            Files = d.GetFiles("*.*"); //Getting non parametric files
            foreach(FileInfo file in Files) {
                if (!file.Name.Contains("param")) {
                    var page = new RawPage(id++, "", NamespaceType.AUTO, file.Name, "", emptyList);
                    sw.Write(page.ToXML());
                }
            }

            sw.Write(exportPage.ToXML());
            sw.Write(mainCat.ToXML());

            foreach (var linkPropName1 in linkProperties) {
                var linkProperty = new Property(id++, linkPropName1, "", InputType.PAGE, "", emptyOptionList, "");
                sw.Write(linkProperty.ToXML());
            }

            sw.Write(sdUpdaterProp.ToXML());
            sw.Write(srComplProp.ToXML());
            sw.Write(template.ToXML());
            sw.Write(form.ToXML());
            sw.Write(dataPage.ToXML());
            sw.Write(chartsPage.ToXML());
            sw.Write(pieChartPage.ToXML());
            sw.Write(barChartPage.ToXML());
            sw.Write(histPage.ToXML());
            sw.Write(sCurvePage.ToXML());
            sw.Write(timelinesPage.ToXML());
            sw.Write(tcPage.ToXML());

            /* TODO: generalize
            sw.Write(audiogramPage1.ToXML());
            sw.Write(audiogramPage2.ToXML());
            sw.Write(audiogramPage3.ToXML());
            sw.Write(audiogramPage4.ToXML());
            sw.Write(audiogramPage5.ToXML());
            sw.Write(audiogramPage6.ToXML());
            sw.Write(audiogramPage7.ToXML());
            sw.Write(audiogramPage8.ToXML());*/

            sw.Write(spPage.ToXML());
            sw.Write(bpPage.ToXML());
            sw.Write(tmPage.ToXML());
            sw.Write(tmForm.ToXML());
            sw.Write(exportPage.ToXML());
            sw.Write(superPropertiesString);
            sw.Write(propertiesString);

            foreach (var auxSimpleTemplate in auxSimpleTemplates)
                sw.Write(auxSimpleTemplate.ToXML());

            foreach (var auxTemplate in auxTemplates)
                sw.Write(auxTemplate.ToXML());

            foreach (var auxForm in auxForms)
                sw.Write(auxForm.ToXML());

            foreach (var auxCat in auxCats)
                sw.Write(auxCat.ToXML());

            foreach (var auxSimpleForm in auxSimpleForms)
                sw.Write(auxSimpleForm.ToXML());

            foreach (var category in categories)
                sw.Write(category.ToXML());
            foreach (var pageForCatLevels in pagesForCatLevels)
                sw.Write(pageForCatLevels.ToXML());
            foreach (var categoryPage in categoryPages)
                sw.Write(categoryPage.ToXML());

            // Maps
            string mapsPage = "\n==" + Program.langManager.Get("AvailableMaps") + "==\n\n";
            if (maps.Count == 0) {
                var mapText = "";
                foreach (var prop in properties) {
                    if (prop.type == InputType.COORDS) {
                        var mapName = Program.langManager.Get("PropertyMap") + " " + prop.name;
                        //mapText += $"<h2>{mapName}</h2>\n";
                        mapText += getMapCode($"Category:{options.CatName}", new List<string> {prop.name}, "", true);
                        mapText += "<hr />\n";
                        var mapPage = new RawPage(id++, mapName, NamespaceType.PAGE, "", mapText, emptyList);
                        sw.Write(mapPage.ToXML());
                        mapsPage += $"* [[{mapName}]]\n\n";
                    }
                }
            }
            else {
                foreach (var map in maps) {
                    var mapText = "<h2>R Shiny maps</h2>\n";
                    string geo_names = string.Join(", ", from item in map.Value.Item2 select item.Split(".")[GlobalConsts.FIRST_PART]);
                    // TODO: do not use the nexus trick
                    (_, chartText) = generateUnivariateChartPage(geo_names, sections, (map.Value.Item3 == "default" ? InputType.COORDS : InputType.NEXUS), false, true);
                    mapText += chartText + "\n<hr />\n";
                    foreach (string mf in map.Value.Item2) {
                        mapText += "<h2>" + Program.langManager.Get("PropertyMap") + $" '{mf.Split(".")[GlobalConsts.FIRST_PART]}'</h2>\n";
                        mapText += getMapCode($"Category:{map.Key}", new List<string> {mf}, map.Value.Item3, true);
                        mapText += "<hr />\n";
                    }
                    var mapPage = new RawPage(id++, Program.langManager.Get("CategoryMap") + " " + map.Value.Item1, NamespaceType.PAGE, "", mapText, emptyList);
                    sw.Write(mapPage.ToXML());
                    mapsPage += "* [[" + Program.langManager.Get("CategoryMap") + " " + $" {map.Value.Item1}]]\n\n";
                }
            }
            sw.Write(new RawPage(id++, Program.langManager.Get("Maps"), NamespaceType.PAGE, "", mapsPage, emptyList).ToXML());

            sw.Write(templateXMLsuffix);

            sw.Flush();
            Console.WriteLine("\nPropertyChainsHelper configuration\n\n" 
                +  createPropChainHelperConf(options.CatName, templateFields, auxTemplates) + "\n");

            Console.WriteLine($"\n\tLast ID: {id}\n");
        }
    }
}
