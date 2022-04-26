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