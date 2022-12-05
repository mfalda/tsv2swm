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
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

namespace TSV2SMW
{
    public struct TemplateField
    {
        public SectionId sec;
        public GroupId grp;
        public string label;
        public string prop;
        public string linkPropName;
        public string parameterName;
        public string templ;
        public string category;
        public string info;
        public string showOnSelect;
        public bool isList;
        public string[] vectorElems;
        public bool isHidden;
        public bool isMultiple;
        public bool isExclusive;
        public bool areSubpages;
        public bool isFilter;
        public bool isCheckOption;
        public bool isIdentifier;
        public bool isLiteral;
        public bool isFile;
        public bool isDate;
        public string formula;
        public string moduleVariable;
        bool isSubPageOrListField;

        private static string parseFormula(string expression, string linkPropName, SortedSet<string> properties, SortedSet<string> variables)
        {
            // parse the parameters
            var f = expression.Split(", ");
            var formula = f[GlobalConsts.FIRST_PART];
            for (int i = 1; i < f.Length; i++) {
                var pair = f[i].Split("=");
                string name = pair[GlobalConsts.FIRST_PART].Trim();
                string value = pair[GlobalConsts.SECOND_PART].Trim();
                string realValue = value;
                string realComponent = "";
                // beware of true points
                var f1 = value.Split(".");
                if (f1[GlobalConsts.FIRST_PART] == linkPropName) {
                    realValue = f1[f1.Length - 1]; // get the real property name (last piece)
                    realValue = Program.normalizeNames(realValue);
                    realValue = Program.capitalize(realValue);
                    realComponent = Program.capitalize(realValue);
                    realValue = linkPropName + "." + realComponent;
                }
                else { // just normalize and capitalize
                    value = Program.normalizeNames(value);
                    realComponent = Program.capitalize(value);
                    realValue = realComponent;
                    if (realComponent.StartsWith("$")) {
                         realValue = realComponent.Substring(1);
                         if (!variables.Contains(realValue))
                            Console.WriteLine($"WARNING: variable '{realValue}' has not been defined: Please check!");
                        formula = formula.Replace(name, "{{#var: " + realValue + "}}");
                    }
                }
                if (!properties.Contains(realComponent))
                    Console.WriteLine($"WARNING: parameter '{realComponent}' not found in properties: Please check!");
                if (name.StartsWith("?"))
                    formula = formula.Replace(name, "?" + realValue);
                else if (name.StartsWith("@"))
                    formula = formula.Replace(name, "{{{" + realValue + "|}}}");
            }
            // replace the remaining variables (TODO: refine the parsing of the expressions)
            if (formula.Contains("$")) {
                if (!variables.Contains(formula.Trim().Substring(1)))
                    Console.WriteLine($"WARNING: variable '{formula}' has not been defined: Please check!");
                formula = Regex.Replace(formula, @"\$([A-Za-z0-9_]+)", "{{#var: $1}}");
            }
            // replace the remaining parameters (TODO: refine the parsing of the expressions)
            if (formula.Contains("@")) {
                if (!variables.Contains(formula.Trim().Substring(1)))
                    Console.WriteLine($"WARNING: parameter '{formula}' not found in properties: Please check!");
                formula = Regex.Replace(formula, @"@([A-Za-z0-9_]+)", "{{{$1|}}}");
            }
            return formula;
        }

        public TemplateField(SectionId sec1, GroupId grp1, string label1, string property1, string parameterName1, 
            InputType type1, string domain1, string info1, string showOnSelect1, string category1, List<OptionType> options1, 
            SortedSet<string> properties1, SortedSet<string> variables1, string linkPropName1)
        {
            sec = sec1;
            grp = grp1;
            label = label1;
            prop = property1;
            parameterName = parameterName1;
            linkPropName = linkPropName1;
            category = category1;
            info = info1;
            showOnSelect = showOnSelect1; // .Replace(" ", "-"); NO, otherwise two distinct properties would be created
            isHidden = options1.Contains(OptionType.HIDDEN);
            isList = type1 == InputType.LIST || type1 == InputType.TREE || options1.Contains(OptionType.LIST);
            isFile = type1 == InputType.FILE;

            // deal with domains that refer to subpages: eg.: "Subpage:NPSY visit/NPSY visits"
            templ = "";
            if (domain1.Contains("/") && domain1.Contains(":")) {
                var f = domain1.Split(":");
                var f1 = f[GlobalConsts.SECOND_PART].Split("/");
                templ = f1[GlobalConsts.FIRST_PART];
            }

            if (options1.Contains(OptionType.VECTOR))
                vectorElems = (from elem in domain1.Split(",")
                                    where elem.StartsWith("elems=")
                                    select elem.Substring(6)).First().Split(":");
            else
                vectorElems = new string[] {};

            isMultiple = options1.Contains(OptionType.MULTIPLE);
            isExclusive = options1.Contains(OptionType.EXCLUSIVE);
            areSubpages = options1.Contains(OptionType.SUBPAGES);
            isDate = type1 == InputType.DATE;
            isFilter = (type1 != InputType.TEXT && type1 != InputType.REPEATED && type1 != InputType.SUBPAGE);
            isCheckOption = (type1 == InputType.OPTION && !options1.Contains(OptionType.EXCLUSIVE));
            isIdentifier = options1.Contains(OptionType.IDENTIFIER);
            isLiteral = (type1 == InputType.LITERAL);
            isSubPageOrListField = category == "";

            string expression = (options1.Contains(OptionType.COMPUTED)) ?  domain1 : "";
            formula = "";
            if (expression != "") {
                // temporary replace HTLM entities to split on ";"
                var expressions = expression.Replace("&lt;", "<")
                                    .Replace("&gt;", ">")
                                    .Split(";");
                foreach (string e in expressions) {
                    // variable definition
                    if (e.Contains(" <- ")) {
                        var parts = e.Substring(1).Split(" <- ");
                        formula += "{{#vardefine: " + parts[GlobalConsts.FIRST_PART] + " | " 
                            + parseFormula(parts[GlobalConsts.SECOND_PART], linkPropName1, properties1, variables1) + " }}";
                        variables1.Add(parts[GlobalConsts.FIRST_PART]);
                    }
                    else {
                        formula += parseFormula(e, linkPropName1, properties1, variables1);
                    }
                    // restores HTML entites
                    formula = formula.Replace("<", "&lt;").Replace(">", "&gt;");
                }
            }

            if (options1.Contains(OptionType.MODULE))
                moduleVariable = $"{{{{#arrayindex: {domain1} }}}}";
            else
                moduleVariable = "";
        }

        public string ToString(string templateName, bool inTable, bool inTemplate)
        {
            var res = ("", "");
            var set = "";
            string pipe = "|";
            if (inTemplate)
                pipe = "{{!}}";

            if (isHidden)
                res = (prop, prop);
            else if ((isList && !isExclusive) || isCheckOption) {
                if (category != "")
                    res = ($"[[Property:{prop}|{label}]]", $"{{{{#arraymap:{{{{{{{parameterName}|}}}}}}|,|x|[[{prop}::Category:x]][[Category:x]]}}}}");
                else
                    res = ($"[[Property:{prop}|{label}]]", $"{{{{#arraymap:{{{{{{{parameterName}|}}}}}}|,|x|[[{prop}::x]]}}}}");
            }
            else if (isLiteral)
                res = ($"[[Property:{prop}|{label}]]", $"[[{prop}::{parameterName}]]");
            else if (vectorElems.Length > 0) {
                set = "{{#set:\n";
                foreach (string elem in vectorElems) {
                    string name = prop + " " + elem;
                    set += $"        |{name} = {{{{{{{name}|}}}}}}\n";
                }
                set += "    }}\n    ";

                var localThis = this;
                string elems = String.Join(", ", vectorElems.Select(elem => 
                    $"{{{{{{{localThis.prop} {elem}|}}}}}}"
                ));
                res = ($"[[Property:{prop}|{label}]]", 
                       $"&#x3008;[[{prop}::{elems}]]&#x3009;");
            }
            else if (formula != "")
                res = ($"[[Property:{prop}|{label}]]", $"[[{prop}::{formula} ]]");
            else if (moduleVariable != "")
                res = ($"[[Property:{prop}|{label}]]", $"[[{prop}::{moduleVariable}]]");
            else if (isDate)
                res = ($"[[Property:{prop}|{label}]]", $"[[{prop}::{{{{{{{parameterName}|}}}}}}|{{{{#time: l d F Y | {{{{{{{parameterName}|}}}}}} }}}}]]");
            else if (areSubpages) {
                string firstName =  Program.langManager.Get("Firstname");
                string lastName = Program.langManager.Get("Lastname");
                string has = Program.langManager.Get("Has");
                var add = Program.langManager.Get("Add");
                res = ($"{label}", $"{{{{#formlink:form={templ}|link text={add} {label}|new window|query string={templ}[{has} {templateName}]={{{{PAGENAME}}}}"
                        + $"&amp;{lastName}={{{{{{{lastName}|}}}}}}&amp;{firstName}={{{{{{{firstName}|}}}}}} }}}}\n"
                        + $"{{{{#ask:[[Category:{category}]][[{linkPropName}::{{{{PAGENAME}}}}]]|format=ul}}}}");
            }
            else if (isMultiple)
                res = ($"[[Property:{prop}|{label}]]", $"{{{{{{{prop}|}}}}}}");
            else if (category != "")
                res = ($"[[Property:{prop}|{label}]]", $"[[{prop}::Category:{{{{{{{parameterName}|}}}}}}|{{{{{{{parameterName}|}}}}}}]]");
            else if (isFile)
                res = ($"[[Property:{prop}|{label}]]", $"{{{{#if: {{{{{{{parameterName}|}}}}}} | [[{prop}::File:{{{{{{{parameterName}|}}}}}}]] }}}}");
            else if (!isMultiple && !areSubpages)
                res = ($"[[Property:{prop}|{label}]]", $"[[{prop}::{{{{{{{parameterName}|}}}}}}]]");
            else
                throw new Exception("Invalid template field status!");

            if (isIdentifier) {
                if ((isList && !isExclusive) || isCheckOption)
                    res = ($"[[Property:{prop}|{label}]]", $"{{{{#arraymap:{{{{{{{parameterName}|}}}}}}|,|x|&lt;span style=\"color: red\"&gt;[[{prop}::x]]&lt;/span&gt;}}}}");
                else
                    res.Item2 = $"&lt;span style=\"color: red\"&gt;{res.Item2}&lt;/span&gt;";
            }

            if (formula != "")
                res.Item2 = $"&lt;span style=\"color: blue\"&gt;{res.Item2}&lt;/span&gt;";

            string paramCat = "";
            if (!areSubpages && isExclusive && category != "")
                res.Item2 = $"{{{{#if: {{{{{{{parameterName}|}}}}}} | [[{parameterName}::Category:{{{{{{{parameterName}|}}}}}}|{{{{{{{parameterName}|}}}}}}]] [[Category:{{{{{{{parameterName}|}}}}}}]] | }}}}";
            else if (formula.StartsWith("Category:"))
                res.Item2 = $"{{{{#vardefine: {parameterName} | {formula.Substring(9)} }}}}"
                                + $" {{{{#if: {{{{#var:{parameterName} }}}} | [[{parameterName}::Category:{{{{#var: {parameterName} }}}} | {{{{#var: {parameterName} }}}} ]] [[Category:{{{{#var: {parameterName} }}}} ]] | }}}}";

            string infoS = "";
            if (info != "")
                infoS = $"{{{{#info: {info}|note}}}}";

            if (inTable && !isHidden)
                return $@"
    {set}! style=""width: 30%""| {res.Item1}{infoS}:
    {pipe} style=""width: 70%""| {res.Item2}
    {pipe}-";
            else if (!isHidden)
                return $"'''{res.Item1}'''{infoS}: {res.Item2}\n";
            else {
                Debug.Assert(paramCat == "");
                return $"    {{{{ #set: {res.Item1} = {{{{{{{res.Item2}|}}}}}} }}}}";
            }
        }
    }
}
