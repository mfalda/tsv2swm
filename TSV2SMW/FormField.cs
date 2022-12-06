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
using System.Linq;


namespace TSV2SMW
{
    /// <summary>
    /// Class <c>FormField</c> models a MediaWiki field in an embedded form.
    /// </summary>
    public class FormField
    {
        string label;
        string domain;
        string info;
        string showOnSelect;
        InputType type;
        string prop;
        string queryString;
        string category;
        bool isMandatory;
        bool isList;
        string[] vectorElems;
        bool isExtended;
        bool isExclusive;
        bool isDefined;
        bool isIdentifier;
        bool areRadioButtons;
        bool isMultiple;
        bool isComputed;
        bool isInteger;
        bool isPositive;
        bool isRestricted;

/// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="label1">the label of the field.</param>
        /// <param name="type1">the type of the field.</param>
        /// <param name="property1">the property associated to the field.</param>
        /// <param name="domain1">the domain of the field (ranges, modalities, et c.).</param>
        /// <param name="options1">the specified options for the field.</param>
        /// <param name="info1">the tooltip to be added near the field label.</param>
        /// <param name="showOnSelect1">whether to show it conditionally according to selections on another field.</param>
        /// <param name="mainForm1">the parent form used for creating URL for additional sub-forms.</param>
        public FormField(string label1, InputType type1, string property1, string domain1, List<OptionType> options1, string info1, string showOnSelect1, string mainForm1)
        {
            label = label1;
            domain = domain1;
            info = info1;
            showOnSelect = showOnSelect1; // .Replace(" ", "-"); NO, otherwise two distinct properties would be created
            type = type1;
            prop = property1;
   
            // deal with categories
            if (type == InputType.TREE) {
                var tmpSplit = domain.Split(':');
                category = tmpSplit[GlobalConsts.SECOND_PART];
            }

            // deal with subpages
            if (type == InputType.SUBPAGE) {
                var tmpSplit = domain.Split('/');
                queryString = tmpSplit[GlobalConsts.FIRST_PART].Split(':')[GlobalConsts.SECOND_PART];
                if (!queryString.Contains('['))
                    queryString += "[" + Program.langManager.Get("Has") + mainForm1 + "]";
                category = tmpSplit[GlobalConsts.SECOND_PART];
            }

            isMandatory = options1.Contains(OptionType.MANDATORY);
            isExtended = options1.Contains(OptionType.TEXTAREA);
            isExclusive = options1.Contains(OptionType.EXCLUSIVE);
            isDefined = options1.Contains(OptionType.DEFINED);
            isIdentifier = options1.Contains(OptionType.IDENTIFIER);
            isList = options1.Contains(OptionType.LIST);

            isMultiple = options1.Contains(OptionType.MULTIPLE);
            isComputed = options1.Contains(OptionType.COMPUTED);
            isInteger = options1.Contains(OptionType.INTEGER);
            isPositive = options1.Contains(OptionType.POSITIVE);
            isRestricted = options1.Contains(OptionType.RESTRICTED);
            areRadioButtons = isExclusive;

            vectorElems = new string[] {};
            if (domain1.Contains("=")) {
                var constraints = new Dictionary<string, string>();
                var constrs = domain1.Split(",");
                foreach (string constr in constrs) {
                    var parts = constr.Split("=");
                    constraints.Add(parts[GlobalConsts.FIRST_PART], parts[GlobalConsts.SECOND_PART]);
                }
                if (options1.Contains(OptionType.VECTOR) && constraints.ContainsKey("elems"))
                    vectorElems = constraints["elems"].Split(":");
            }
        }

        /// <summary>
        /// A method to serialize the field in a string.
        /// </summary>
        public override string ToString()
        {
            string inputType = "";
            string prefix = "|input type=";
            string id = prop; // .Replace(" ", "-"); NO, otherwise two distinct properties would be created
            string options = "";

            if (isMandatory)
                options = "mandatory";

            if (domain.Contains("=")) {
                var constraints = new Dictionary<string, string>();
                var constrs = domain.Split(",");
                foreach (string constr in constrs) {
                    var parts = constr.Split("=");
                    constraints.Add(parts[GlobalConsts.FIRST_PART], parts[GlobalConsts.SECOND_PART]);
                }

                if (constraints.ContainsKey("min"))
                    options += "|min=" + constraints["min"];
                if (constraints.ContainsKey("max"))
                    options += "|max=" + constraints["max"];
                if (constraints.ContainsKey("step"))
                    options += "|step=" + constraints["step"];
            }

            if (type == InputType.OPTION || type == InputType.LIST) {
                options += $"|values={domain}";
                if (!isDefined)
                    options += "," + GlobalConsts.NA;
            }

            if (type == InputType.REGEXP) { // specify as "Motivo:\d{5}"
                var values = domain.Split("|");
                var regexp = from value in values
                             where value.StartsWith("Motivo")
                             select value.Split("=")[GlobalConsts.FIRST_PART];
                options += $"|regexp=/{regexp}/";
            }

            switch (type) {
                case InputType.PAGE:
                    inputType = prefix + "text";
                    break;
                 case InputType.NUMBER:
                 case InputType.VECTOR:
                    inputType = prefix + "number";
                    if (!options.Contains("step") && !isInteger)
                        options += "|step=any";
                    if (!options.Contains("min") && isPositive)
                        inputType += "|min=0";

                    int numElems = 0;
                    if (vectorElems != null)
                        numElems = vectorElems.Length;
                    if (numElems > 0) {
                        // s4, s5, s6, s7, s10
                        if (numElems < 5)
                            options += "|class=s4";
                        else if (numElems == 5)
                            options += "|class=s5";
                        else if (numElems == 6)
                            options += "|class=s6";
                        else if (numElems == 7)
                            options += "|class=s7";
                        else
                            options += "|class=s10";
                    }
                    break;
                case InputType.FILE:
                    inputType = prefix + "text";
                    options += $"|uploadable|default filename={prop} for &lt;page name&gt;...";
                    break;
                case InputType.TEXT:
                    if (isExtended) {
                        inputType = prefix + "textarea";
                        options += "|rows=10";
                    }
                    else {
                        inputType = prefix + "text";
                    }
                    break;
                case InputType.BOOL:
                    inputType = prefix + "checkbox";
                    break;
                case InputType.OPTION:
                    // let's "none" appear (to disallow it always use "mandatory" and default set in radiobuttons)
                    if (areRadioButtons) {
                        inputType = prefix + "radiobutton";
                        if (isMandatory)
                            options += "|default=None";
                    }
                    else {
                        inputType = prefix + "checkboxes";
                        if (isMandatory)
                            options += "|default=None";
                    }
                    break;
                case InputType.LIST:
                    if (isList || !isExclusive)
                        inputType = prefix + "tokens";
                    else
                        inputType = prefix + "combobox";
                    break;
                 case InputType.TREE:
                    inputType = prefix + "tree";
                    options = "|width=800|height=300";
                    break;
                case InputType.DATE:
                    inputType = prefix + "datepicker";
                    break;
                case InputType.REGEXP:
                    inputType = prefix + "regexp";
                    break;
                case InputType.COORDS:
                    inputType = prefix + "leaflet";
                    break;
                default: // it is able to deduce the correct type (e.g.: email, telephone etc.)
                    inputType = "";
                    break;
            }

            if (showOnSelect.Contains("=&gt;")) {
                string sos = showOnSelect; // .Replace(" ", "-");  NO, otherwise two distinct properties would be created
                if (sos.IndexOf("=") == 0)
                    sos = sos.Substring(5);
                options += $"|show on select={sos}";
            }

            if (isRestricted)
                options += "|restricted";
            if (isIdentifier)
                options += "|class=identifier";

            string infoS = "";
            if (info != "")
                infoS = $"{{{{#info: {info}|note}}}}";

            string mandatory = isMandatory ? "* " : "";
            if (isComputed)
                return "";
            else if (type == InputType.SUBPAGE) {  
                string childTemplate = queryString.Split('[')[GlobalConsts.FIRST_PART];              
                return String.Format(Program.langManager.Get("AddSubPage"), label,  mandatory, infoS, childTemplate, queryString);
            }
            else if (type == InputType.REPEATED) {
                return $@"    ! style=""width: 30%""| {label}{mandatory}{infoS}
    | style=""width: 70%""| {{{{{{field|{prop}|holds template}}}}}}
    |-
";
            }
            else {
                    string cat = "";
                    if (category != null && category != "") {
                        if (type == InputType.TREE)
                            cat = $"|top category={category}";
                        else
                            cat = $"|values from category={category}";
                        if (!inputType.Contains("tokens"))
                            Console.WriteLine($"WARNING: choice from category without a 'Tokens' control: check whether this causes errors in '{label}'.");
                    }

                    string field = "";
                    if (vectorElems.Length > 0) {
                        field = $"      | &lt;span class='vect' id='vect-{Program.normalizeIDs(id)}'&gt;\n";
                        foreach (string vectorElem in vectorElems)
                            field += $"         {{{{{{field|{id} {vectorElem}{inputType}|title={vectorElem}|property={prop} {vectorElem}{cat}|{options}}}}}}}\n";
                        field += "      &lt;/span&gt;\n";
                    }
                    else
                        field = $"    | style=\"width: 70%\"| {{{{{{field|{id}{inputType}|property={prop}{cat}|{options}}}}}}}\n";
                    return $"    ! style=\"width: 30%\"| {label}{mandatory}{infoS}\n{field}    |-\n";
            }
        }

    }
}
