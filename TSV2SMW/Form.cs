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
using System.Text;
using System.Linq;

using OD;


namespace TSV2SMW
{
    using SectionsDict = OrderedDictionary<SectionId, OrderedDictionary<GroupId, List<MainLine>>>;

    /// <summary>
    /// Class <c>CoreForm</c> models a MediaWiki basic form used in embedded templates.
    /// </summary>
    public class CoreForm {

        string template;
        bool multiple;
        string mainTemplate;
        string fields;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="template1">the name of the template linked to the form.</param>
        /// <param name="fields1">a comma-separated list of form fields.</param>
        /// <param name="multiple1">is this form repeated in an embedded template?</param>
        /// <param name="mainTemplate1">the main template (for referring back to the parent).</param>
        public CoreForm(string template1, List<MainLine> fields1, bool multiple1, string mainTemplate1)
        {
            template = template1;
            multiple = multiple1;
            mainTemplate = mainTemplate1;

            fields = string.Join("\n\n", from field in fields1
                                         where !field.options.Contains(OptionType.MODULE)
                                         select new FormField(field.label, field.type, field.prop, field.domain, 
                                            field.options, field.info, field.showOnSelect, "").ToString());
        }

        /// <summary>
        /// A method to serialize the category in a string, used in embedded templates.
        /// </summary>
        /// <returns>the textual representation.</returns>
        public override string ToString()
        {
            var add = Program.langManager.Get("Add");
            // a core form is always multiple
            return $@"
    {{{{{{for template|{template}|multiple|add button text={add} {template}|embed in field={mainTemplate}[{template}]}}}}}}
    {{| class=""formtable"" style=""width: 95%; margin-left: 20px;""
    {fields}
    |}}
    {{{{{{end template}}}}}}";
        }

    }

    /// <summary>
    /// Class <c>SimpleForm</c> models a MediaWiki simple form used in repeated lists.
    /// </summary>
     public class SimpleForm : Page
     {
        public new static string templateXML;
        public string template;
        string text;
        new string fields;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="id1">a progressive ID.</param>
        /// <param name="name1">the name of the category.</param>
        /// <param name="message1">an initial message for the users.</param>
        /// <param name="text1">a shorter text for the users.</param>
        /// <param name="fields1">the list of the form fields.</param>
        /// <param name="template1">the template linked to the form.</param>
        public SimpleForm(int id1, string name1, string message1, string text1, List<MainLine> fields1, string template1)
        {
            id = id1;
            name = name1;
            text = text1;
            message = message1;
            template = template1;

            fields = "{| class=\"formtable\" style=\"width: 95%; margin-left: 20px;\"\n";
            foreach (var fg in fields1){
                if (!fg.options.Contains(OptionType.MODULE))
                    fields += new FormField(fg.label, fg.type, fg.prop, fg.domain,
                                    fg.options, fg.info, fg.showOnSelect, "").ToString();
            }
            fields += "|}\n{{{end template}}}";

            if (templateXML == null) {
                using (var reader = new StreamReader(@"templates/simple_form.xml")) {
                    templateXML = reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// A method to serialize the category in XML.
        /// </summary>
        /// <returns>the XML representation.</returns>
        public string ToXML()
        {
            name = Program.normalizeNames(name);

            return templateXML.Replace("«FNAME»", name)
                           .Replace("«ID»", id.ToString())
                           .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                           .Replace("«MESSAGE»", message)
                           .Replace("«TEXT»", text)
                           .Replace("«FIELDS»", fields);
        }

    }

    /// <summary>
    /// Class <c>Form</c> models a full general MediaWiki form.
    /// </summary>
    public class Form : SimpleForm
     {
        public new static string templateXML;
        new string fields;
        string subForms;
        string noteText;
        string linkProperty;
        string category;
        public SectionsDict sections;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="id1">a progressive ID.</param>
        /// <param name="name1">the name of the category.</param>
        /// <param name="message1">an initial message for the users.</param>
        /// <param name="sections1">a dictionary of sections for partitioning the form (usually tabs).</param>
        /// <param name="template1">the template linked to the form.</param>
        /// <param name="subForms1">a list of sub-forms, tipically embedded in templates.</param>
        /// <param name="noteText1">the text above the notes text area.</param>
        /// <param name="category1">the category associated to the pages generated with the form.</param>
        /// <param name="linkProperty1">the property that links the main category with the secondary categories (tipically used for longitudinal data).</param>
        public Form(int id1, string name1, string message1, SectionsDict sections1, string template1, List<CoreForm> subForms1, string noteText1, string category1, string linkProperty1)
                : base(id1, name1, message1, "", new List<MainLine>(), template1)
        {
            id = id1;
            name = name1;
            template = template1;
            linkProperty = linkProperty1;
            sections= sections1;

            var sectionsStr = "";
            bool firstSection = true;

            foreach (var secKey in sections1.Keys) {
                if (secKey.ToString() != "MAIN") {
                    sectionsStr += $"&lt;div id=\"sec-{secKey.ToString().Replace(" ", "-")}\"&gt;\n\n";
                    sectionsStr += $"\n=={secKey}==\n\n";
                }
                else if (name != linkProperty) { // a secondary page
                    // readonly means that only administrators can modify the field
                    sectionsStr += $"  {{{{{{field|{linkProperty}|input type=combobox|property={linkProperty}|readonly|values from category={linkProperty.Split(' ')[GlobalConsts.SECOND_PART]}}}}}}}\n\n";
                }

                sectionsStr += "&lt;tabber&gt;\n\n";
                foreach (var groupKey in sections1[secKey].Keys) {
                    var formFields = new StringBuilder();
                    bool divSection = false;
                    foreach (var fg in sections1[secKey][groupKey]) {
                        if (!fg.options.Contains(OptionType.MODULE) && !fg.options.Contains(OptionType.COMPUTED)) {
                            string formField = "", prefix = "";
                            if (fg.showOnSelect.Contains("=&gt;")) {
                                if (divSection)
                                    prefix = "|}\n   &lt;/div&gt;\n{| class=\"formtable\" style=\"width: 95%; margin-left: 20px;\"\n";
                                divSection = false;
                            }
                            else if (!divSection && fg.showOnSelect != "" && !fg.showOnSelect.Contains("=&gt;")) {
                                prefix = $"|}}\n   &lt;div id=\"{fg.showOnSelect}"
                                    /*.Replace(" ", "-")  NO, otherwise two distinct properties would be created */
                                    + "\"&gt;\n{| class=\"formtable\" style=\"width: 95%; margin-left: 20px;\"\n";
                                divSection = true;
                            }
                            else if (divSection && fg.showOnSelect == "") {
                                prefix = "|}\n    &lt;/div&gt;\n{| class=\"formtable\" style=\"width: 95%; margin-left: 20px;\"\n";
                                divSection = false;
                            }
                            formField = new FormField(fg.label, fg.type, fg.prop, fg.domain,
                                                fg.options, fg.info, fg.showOnSelect, name).ToString();
                            formFields.Append(prefix + formField);
                        }
                    }
                    // always close div when the tab ends
                    string divStr = "";
                    if (divSection)
                        divStr = "&lt;/div&gt;\n";

                    if (formFields.Length > 0) {
                        sectionsStr += $"  {groupKey} =\n    {{| class=\"formtable\" style=\"width: 95%; margin-left: 20px;\"\n";
                        if (firstSection && name == linkProperty) {
                            sectionsStr += $@"    ! style=""width: 30%""| {linkProperty}
    | style=""width: 70%""| {{{{{{field|{linkProperty}|input type=text|property=ID {linkProperty}|class=identifier}}}}}}
    |-
    ";
                            firstSection = false;
                        }
                        sectionsStr += string.Join("\n", formFields.ToString());
                        sectionsStr += $"|}}\n  {divStr}\n|-|\n\n";
                    }
                }
                sectionsStr += $"&lt;/tabber&gt;\n";
                sectionsStr += $"&lt;/div&gt;\n\n";
            }

            fields = $@"{{{{{{for template|{template}}}}}}}
{sectionsStr}
{{{{{{end template}}}}}}";

            subForms = string.Join("\n\n", from subForm in subForms1 select subForm.ToString());

            category = category1;

            noteText = noteText1;

            if (templateXML == null) {
                using (var reader = new StreamReader(@"templates/form.xml")) {
                    templateXML = reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// A method to serialize the category in XML.
        /// </summary>
        /// <returns>the XML representation.</returns>
        public new string ToXML()
        {
            name = Program.normalizeNames(name);
            string templateName = $"{template} &lt;unique number;start=00001&gt;";
            if (!linkProperty.Contains(template)) // a secondary page: add suffix
                templateName += $" - &lt;{template}[{linkProperty}]&gt;";

            return templateXML.Replace("«FNAME»", name)
                           .Replace("«ID»", id.ToString())
                           .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                           .Replace("«MESSAGE»", message)
                           .Replace("«TNAME»", templateName)
                           .Replace("«FIELDS»", fields)
                           .Replace("«MFORMS»", subForms)
                           .Replace("«NOTE»", noteText)
                           .Replace("«CATEGORY»", category);
        }

    }
}
