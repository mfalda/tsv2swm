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
using System.Diagnostics;
using System.Linq;


namespace TSV2SMW
{

    /// <summary>
    /// Class <c>SimpleTemplate</c> models a MediaWiki basic template used in embedded templates.
    /// </summary>
    public class SimpleTemplate : Page
    {
        public string body;
        public string linkProperty;
        public new static string templateXML;
        public static string basePath;
        public List<string> categories;
        public string usedModules;

        public SimpleTemplate() {}

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="id1">a progressive ID.</param>
        /// <param name="name1">the name of the template.</param>
        /// <param name="message1">an initial message for the users.</param>
        /// <param name="body1">the body of the template.</param>
        /// <param name="linkProperty1">the link property.</param>
        /// <param name="categories1">a list of categories associated with the template.</param>
        /// <param name="usedModules1">a set of user modules (Scribunto) used in the template.</param>
        /// <param name="basePath1">the path where auxliary templates are stored (for unit tests, mainly).</param>
        public SimpleTemplate(int id1, string name1, string message1, string body1, string linkProperty1, List<string> categories1, HashSet<string> usedModules1, string basePath1 = ".")
        {
            id = id1;
            name = name1;
            message = message1;
            body = body1;
            linkProperty = linkProperty1;
            categories = categories1;
            basePath = basePath1;

/* examples:
{{#arraydefine: srs22 | {{#srs22: {{{Quesito 1|}}} | {{{Quesito 2|}}} | {{{Quesito 3|}}} | {{{Quesito 4|}}} | {{{Quesito 5|}}} | {{{Quesito 6|}}} | {{{Quesito 7|}}} | {{{Quesito 8|}}} | {{{Quesito 9|}}} | {{{Quesito 10|}}} | {{{Quesito 11|}}} | {{{Quesito 12|}}} | {{{Quesito 13|}}} | {{{Quesito 14|}}} | {{{Quesito 15|}}} | {{{Quesito 16|}}} | {{{Quesito 17|}}} | {{{Quesito 18|}}} | {{{Quesito 19|}}} | {{{Quesito 20|}}} | {{{Quesito 21|}}} | {{{Quesito 22|}}} }} }}

{{#arraydefine: indiciD | {{#dyn_indices: {{{Fless T12 ‹x1›|}}} | {{{Fless T12 ‹x2›|}}} | {{{Fless T12 ‹x3›|}}} | {{{Fless S2 ‹x1›|}}} | {{{Fless S2 ‹x2›|}}} | {{{Fless S2 ‹x3›|}}} | {{{Est T12 ‹x1›|}}} | {{{Est T12 ‹x2›|}}} | {{{Est T12 ‹x3›|}}} | {{{Est S2 ‹x1›|}}} | {{{Est S2 ‹x2›|}}} | {{{Est S2 ‹x3›|}}} | {{{Inclinazione dx ‹x1›|}}} | {{{Inclinazione dx ‹x2›|}}} | {{{Inclinazione dx ‹x3›|}}} | {{{Inclinazione sx ‹x1›|}}} | {{{Inclinazione sx ‹x2›|}}} | {{{Inclinazione sx ‹x3›|}}} }} }}
*/
            usedModules = string.Join("\n\n", from u in usedModules1
                                              select $"{{{{#arraydefine: {u} | {{{{#{u}: | ... }}}} }}}}");

            if (templateXML == null) {
                using (var reader = new StreamReader(basePath + "/templates/simple_template.xml")) {
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

            string categoriesS = string.Join("\n", from cat in categories
                                                   select $"{{{{#if: {cat} | [[Category:{cat}]] |}}}}\n");

            return templateXML.Replace("«TNAME»", name)
                           .Replace("«ID»", id.ToString())
                           .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                           .Replace("«MESSAGE»", message)
                           .Replace("«MODULES»", usedModules)
                           .Replace("«BODY»", body)
                           .Replace("«CATEGORIES»", categoriesS);
        }

    }

    /// <summary>
    /// Class <c>Template</c> models a MediaWiki full-fledged template.
    /// </summary>
    public class Template : SimpleTemplate
    {
        public List<TemplateField> fieldsT;
        public bool needSubobject;
        public new static string templateXML;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="id1">a progressive ID.</param>
        /// <param name="name1">the name of the template.</param>
        /// <param name="message1">an initial message for the users.</param>
        /// <param name="fields1">the fields (parameters) of the template.</param>
        /// <param name="linkProperty1">the link property.</param>
        /// <param name="categories1">a list of categories associated with the template.</param>
        /// <param name="usedModules1">a set of user modules (Scribunto) used in the template.</param>
        /// <param name="needSubobject1">whether it is part of a sub-object.</param>
        /// <param name="basePath1">the path where auxliary templates are stored (for unit tests, mainly).</param>
        public Template(int id1, string name1, string message1, List<TemplateField> fields1, string linkProperty1, List<string> categories1, HashSet<string> usedModules1, bool needSubobject1, string basePath1 = ".")
            : base(id1, name1, message1, "", linkProperty1, categories1, usedModules1, basePath1)
        {
            fieldsT = fields1;
            needSubobject = needSubobject1;

/*
{{#arraydefine: srs22 | {{#srs22: {{{Quesito 1|}}} | {{{Quesito 2|}}} | {{{Quesito 3|}}} | {{{Quesito 4|}}} | {{{Quesito 5|}}} | {{{Quesito 6|}}} | {{{Quesito 7|}}} | {{{Quesito 8|}}} | {{{Quesito 9|}}} | {{{Quesito 10|}}} | {{{Quesito 11|}}} | {{{Quesito 12|}}} | {{{Quesito 13|}}} | {{{Quesito 14|}}} | {{{Quesito 15|}}} | {{{Quesito 16|}}} | {{{Quesito 17|}}} | {{{Quesito 18|}}} | {{{Quesito 19|}}} | {{{Quesito 20|}}} | {{{Quesito 21|}}} | {{{Quesito 22|}}} }} }}

{{#arraydefine: indiciD | {{#dyn_indices: {{{Fless T12 ‹x1›|}}} | {{{Fless T12 ‹x2›|}}} | {{{Fless T12 ‹x3›|}}} | {{{Fless S2 ‹x1›|}}} | {{{Fless S2 ‹x2›|}}} | {{{Fless S2 ‹x3›|}}} | {{{Est T12 ‹x1›|}}} | {{{Est T12 ‹x2›|}}} | {{{Est T12 ‹x3›|}}} | {{{Est S2 ‹x1›|}}} | {{{Est S2 ‹x2›|}}} | {{{Est S2 ‹x3›|}}} | {{{Inclinazione dx ‹x1›|}}} | {{{Inclinazione dx ‹x2›|}}} | {{{Inclinazione dx ‹x3›|}}} | {{{Inclinazione sx ‹x1›|}}} | {{{Inclinazione sx ‹x2›|}}} | {{{Inclinazione sx ‹x3›|}}} }} }}
*/
            usedModules = string.Join("\n\n", from u in usedModules1
                                              select $"{{{{#arraydefine: {u} | {{{{#{u}: | ... }}}} }}}}");

            /*subObject = "";
            if (subObject1) {
                subObject = string.Format(@"
        {{{{#subobject:
            {0}
            | Has {1}={{{{PAGENAME}}}}
        }}}}", string.Join("\n", from field in fields1 select $"| {field.parameterName}={{{{{{{field.prop}|}}}}}}"), 
            mainTemplate);
                subObject = $@"
           {{{{#set: Has {mainTemplate}={{{{{{Has {mainTemplate}|}}}}}}}}}}";
        }*/
            categories = categories1;

            if (templateXML == null) {
                using (var reader = new StreamReader(basePath + "/templates/template.xml")) {
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
            string tableClass = "wikitable";
            var fieldsStr = new StringBuilder();
            if (linkProperty != "" && !needSubobject)
                fieldsStr.Append("\n\n" + $"'''[[Property:{linkProperty}|{linkProperty}]]''': [[{linkProperty}::{{{{{{{linkProperty}|}}}}}}]]\n\n");
            else if (needSubobject) {
                tableClass = "wikitable";
                // better not to name subobjects (with e.g. Record {name})
                fieldsStr.Append($@"
{{{{#subobject:
 |{linkProperty}={{{{PAGENAME}}}}
");
                foreach (var field in fieldsT)
                    fieldsStr.Append($" |{field.label}={{{{{{{field.prop}|}}}}}}\n");
                fieldsStr.Append("}}\n");
            }

            string categoriesS = string.Join("\n", from cat in categories
                                                   select $"{{{{#if: {cat} | [[Category:{cat}]] |}}}}");
            var lastSection = new SectionId("");
            var lastGroup = new GroupId("");
            bool section = false;
            string endIf = "";
            // dictionary of the conditions indexed by the control
            var sos = new Dictionary<string, string>();

            string displayName;
            string firstName = ""; // Program.langManager.Get("Firstname");
            string lastName = "Uid"; // Program.langManager.Get("Lastname");
            string setPatientID = "";
            string semDep = "";
            if (linkProperty != "") { // secondary page
                if (firstName == "") // Uid
                    displayName = @"{{DISPLAYTITLE: {{#show: {{PAGENAME}} |?" + linkProperty + "." + lastName + "}} ({{PAGENAME}}) }}";
                else
                    displayName = @"{{DISPLAYTITLE: {{#show: {{PAGENAME}} |?" + linkProperty + "." + lastName + "}} "
                        + "{{#show: {{PAGENAME}} |?" + linkProperty + "." + firstName + "}} ({{PAGENAME}}) }}";
                string linkPropertyName = linkProperty.Split(" ")[GlobalConsts.SECOND_PART];
                // e.g.: {{{Has Patient|}}}|Part of {{{Has Patient|}}}
                semDep = $"[['''Semantic Dependency'''::{{{{{{{linkProperty}}}}}}}|Part of {{{{{{{linkProperty}}}}}}}]]";
            }
            else if (firstName != "") { // primary page
                displayName = "{{DISPLAYTITLE:{{#if: {{#urlget: " + lastName + "}} | {{#urlget: " + lastName + "}} | {{#show: {{PAGENAME}} |?" + lastName + "}} }} "
                    + "{{#if: {{#urlget: " + firstName + "}} | {{#urlget: " + firstName + "}} | {{#show: {{PAGENAME}} |?" + firstName + "}} }} {{PAGENAME}} }}";
                setPatientID = "{{#set: ID " + name + "={{{" + Program.langManager.Get("PatientID") + "|}}} }}";
            }
            else { // UID
                displayName = "{{DISPLAYTITLE:{{#if: {{#urlget: " + lastName + "}} | {{#urlget: " + lastName + "}} | {{#show: {{PAGENAME}} |?" + lastName + "}} }} ";
                setPatientID = "{{#set: ID " + name + "={{{" + Program.langManager.Get("PatientID") + "|}}} }}";
            }


            foreach (var field in fieldsT) {
                string tField = "", prefix = "";
                if (field.sec != lastSection) {
                    string sec = field.sec.ToString().Replace(" ", "-");
                    if (lastSection.ToString() != "")
                        fieldsStr.Append("    |}\n&lt;/tabber&gt;\n&lt;/div&gt;\n\n");
                    string id = "";
                    if (sos.TryGetValue("sec-" + sec, out id))
                        fieldsStr.Append($"&lt;div id=\"sec-{sec}\" style=\"display: {{{{#if: {{{{#pos: {sos["sec-" + sec]} }}}} | block | none }}}}\"&gt;\n\n");
                    else
                        fieldsStr.Append($"&lt;div id=\"sec-{sec}\"&gt;\n\n");
                    fieldsStr.Append($"\n=={field.sec}==\n\n");
                    fieldsStr.Append("&lt;tabber&gt;\n");
                    lastGroup = new GroupId("");
                }
                if (field.grp != lastGroup) {
                    if (section) {
                        endIf = "}}\n"; // #switch
                        section = false;
                    }
                    else
                        endIf = "";
                    if (lastGroup.ToString() != "")
                        fieldsStr.Append($"\n{endIf}    |}}\n |-|\n");
                    fieldsStr.Append($"  {field.grp} =\n");
                    fieldsStr.Append($"    {{| class=\"{tableClass}\" style=\"width: 95%; margin-left: 20px;\"\n");
                }
                if (field.showOnSelect.Contains("=&gt;")) {
                    if (field.showOnSelect.IndexOf("=") == 0) { //checkbox (show on select=id)
                        string values;
                        string key = field.showOnSelect.Substring(5);
                        if (sos.TryGetValue(key, out values))
                            values += " | Sì";
                        else
                            sos.Add(key, $"{{{{{{{field.prop}}}}}}} | Sì");
                    }
                    else { // checkbox (show on select=val1=>id1;val2=>id2)
                        var elems = field.showOnSelect.Replace("&gt;", ">").Split(";");
                        foreach (string elem in elems) {
                            var parts = elem.Replace(">", "&gt;").Split("=&gt;");
                            string condition = parts[GlobalConsts.FIRST_PART];
                            string id = parts[GlobalConsts.SECOND_PART];
                            string values;
                            if (sos.TryGetValue(id, out values))
                                values += " | " + condition;
                            else
                                sos.Add(id, $"{{{{{{{field.prop}}}}}}} | {condition}");
                        }
                    }
                    if (section) {
                        fieldsStr.Append("\n}}"); //# switch
                        section = false;
                    }
                }
                else if (!section && field.showOnSelect != "" && !field.showOnSelect.Contains("=&gt;")) {
                    string id = field.showOnSelect; // .Replace(" ", "-"); NO, otherwise two distinct properties would be created
                    Debug.Assert(sos.ContainsKey(id), $"ID {id} refers to a non-existing section.");
                    prefix = $"{{{{#switch: {sos[id]} =\n";
                    section = true;
                }
                else if (section && field.showOnSelect == "") {
                    fieldsStr.Append("\n}}"); // #switch
                    section = false;
                }
                tField = field.ToString(name, true, section);
                fieldsStr.Append(prefix + tField + "\n");
                lastGroup = field.grp;
                lastSection = field.sec;
            }

            if (section)
                endIf = "| }}\n";
            fieldsStr.Append($"{endIf}    |}}\n");
            fieldsStr.Append("&lt;/tabber&gt;\n&lt;/div&gt;\n\n");

            var ratingFields = from fieldT in fieldsT
                    where (!fieldT.parameterName.ToLower().StartsWith("note") && fieldT.parameterName != "Hash")
                    select "{{#if: {{{" + fieldT.parameterName + "|}}} | 1 | 0 }}\n";

            string rating = @"
{{#vardefine: rating |
  {{#expr:
   (" + string.Join("    + ", ratingFields) + ") / " + ratingFields.Count() + @" * 5
  }}
}}
";

            return templateXML.Replace("«TNAME»", name)
                           .Replace("«ID»", id.ToString())
                           .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                           .Replace("«MESSAGE»", message)
                           .Replace("«MODULES»", usedModules)
                           .Replace("«DNAME»", displayName)
                           .Replace("«PatientID»", setPatientID)
                           .Replace("«RATING»", rating)
                           .Replace("«FIELDS»", fieldsStr.ToString())
                           .Replace("«CATEGORIES»", categoriesS)
                           .Replace("«SEMDEP»", semDep);
        }

    }
}
