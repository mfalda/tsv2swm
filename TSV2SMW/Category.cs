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
    along with tsv2smw.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace TSV2SMW
{
    public class Category : Page
    {
        public new static string templateXML;
        public List<string> parentCategories;
        string mainCategory;
        string template;
        Form form;
        bool isPropertyGroup;
        new List<TemplateField> fields;

        public Category(int id1, string name1, string mainCategory1, string template1, string parentCategories1, Form form1, List<TemplateField> fields1, bool isPropertyGroup1)
        {
            id = id1;
            name = name1;
            mainCategory = mainCategory1;
            template = template1;
            parentCategories = (parentCategories1 != "") ? parentCategories1.Split("|").ToList() : new List<string>();
            form = form1;
            fields = fields1;
            isPropertyGroup = isPropertyGroup1;

            if (templateXML == null) {
                using (var reader = new StreamReader(@"templates/category.xml")) {
                    templateXML = reader.ReadToEnd();
                }
            }
        }

        public string ToXML()
        {
            string formName = "";
            if (form != null)
                formName = $"{{{{#default_form:{form.name}}}}}";

            string parents = "";
            if (parentCategories.Count() > 0)
                parents = string.Join("\n", from parentCategory in parentCategories
                                               where !parentCategory.Contains("{") // not a parameter
                                               select $"[[Category:{parentCategory}]]");

            string filters = "";
            string label = "";
            if (fields.Count() > 0) {
                filters = "__SHOWINDRILLDOWN__\n\n";
                filters += "\n{{#drilldowninfo:filters=\n";
                foreach (var field in fields) {
                    if (field.isFilter) {
                        label = field.label;
                        // parentheses are problematic: remove them
                        if (label.Contains("("))
                            label = label.Replace("(", "- ").Replace(")", "");
                        // TODO: update the patch for SDD
                        filters += string.Join(",\n", $"  {label} (property=" + Program.capitalize(field.prop) + $", group={field.grp})\n");
                    }
                }
                string fieldsStr = string.Join(";", from field in fields
                                        where !field.isMultiple && !field.areSubpages && !field.isHidden
                                        select field.prop);

                string title = Program.langManager.Get("ExploreDataTitle");
                string exportFormat = ""; //  |export format=spreadsheet";
                filters += $"  |title={title}\n  |printouts={fieldsStr}{exportFormat}\n}}}}\n";
            }

            string propertyGroup = "";
            if (isPropertyGroup)
                propertyGroup = "{{#set: Is property group=true}}";

            name = Program.normalizeNames(name);

            return templateXML.Replace("«CNAME»", name)
                           .Replace("«ID»", id.ToString())
                           .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                           .Replace("«FNAME»", formName)
                           .Replace("«FILTERS»", filters)
                           .Replace("«TEMPLATE»", template)
                           .Replace("«PGROUP»", propertyGroup)
                           .Replace("«SUPER-CATEGORIES»", parents);
        }

    }
}