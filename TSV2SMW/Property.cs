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
using System.IO;
using System.Collections.Generic;


namespace TSV2SMW
{
    /// <summary>
    /// Class <c>Property</c> models a MediaWiki property page.
    /// </summary>
    class Property : Page
    {
        public InputType type;
        string superProperty;
        string sameAsProperty;
        string propertyGroup;
        public new static string templateXML;
        string constraints;
        bool isComputed;
        bool isVector;
        bool isDefined;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="id1">a progressive ID.</param>
        /// <param name="name1">the name of the category.</param>
        /// <param name="superProperty1">the possible parent property (for creating hierarchies).</param>
        /// <param name="type1">the type of the property.</param>
        /// <param name="constraints1">a comma-separated list of contraints coming from the input domain.</param>
        /// <param name="options1">a set of options specified in input.</param>
        /// <param name="propertyGroup1">the group of the property (tipically rendered as a tab).</param>
        public Property(int id1, string name1, string superProperty1, InputType type1, string constraints1, List<OptionType> options1, string propertyGroup1)
        {
            id = id1;
            name = name1;
            type = type1;
            superProperty = "";
            sameAsProperty = "";
            if (superProperty1 != "") {
                if (superProperty1.Contains(":"))
                    sameAsProperty = $"Equivalent to [[Imported from::{superProperty1}]]";
                else
                    superProperty = $"Subproperty of [[Subproperty of::{superProperty1}]]";
            }
            constraints = constraints1;
            isComputed = options1.Contains(OptionType.COMPUTED);
            isVector = options1.Contains(OptionType.VECTOR);
            isDefined = options1.Contains(OptionType.DEFINED);
            propertyGroup = (propertyGroup1 != "") ? $"[[Category: {propertyGroup1}]]" : "";

            if (templateXML == null) {
                using (var reader = new StreamReader(@"templates/property.xml")) {
                    templateXML = reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// A method to serialize the property in XML.
        /// </summary>
        /// <returns>the XML representation.</returns>
        public string ToXML()
        {
            bool isSimpleList = false;

            string constraintsString = "";
            if (constraints.Length > 0) {
                if (constraints.Contains("UNIQUE"))
                    constraintsString ="[[Has uniqueness constraint::true]]\n\n";
                else if (type == InputType.QUANTITY) {
                    var measure = constraints;
                    constraintsString = $"[[Display units::{measure}]]\n\n";
                    constraintsString = $"[[Corresponds to::1 {measure}]]\n\n";
                }
                else if (type == InputType.NUMBER || type == InputType.VECTOR) {
                    var range = constraints.Split(",");
                    float min = float.NaN, max = float.NaN;

                    if (int.TryParse(range[GlobalConsts.FIRST_PART].Substring(4), out var min1))
                        min = min1;

                    if (range.Length > 1) {
                        if (int.TryParse(range[GlobalConsts.SECOND_PART].Substring(4), out var max1))
                        max = max1;
                    }
                    else if (int.TryParse(range[GlobalConsts.FIRST_PART].Substring(4), out var max1))
                        max = max1;

                    if (constraints.Contains("Positive"))
                        constraintsString = $"[[Allows value::&gt;0]]\n\n";
                    else if (!float.IsNaN(min))
                        constraintsString = $"[[Allows value::&gt;{min}]]\n\n";

                    if (!float.IsNaN(min) && !float.IsNaN(max) && max > min)
                        constraintsString += $"[[Allows value::&lt;{max}]]\n\n";
                }
                else if (!isComputed && constraints.Contains(",")) {
                    if (!isDefined)
                        constraintsString += "[[Allows value::" + GlobalConsts.NA + "]]\n\n";
                    foreach (var constraint in constraints.Split(","))
                        constraintsString += $"[[Allows value::{constraint}]]\n\n";
                    isSimpleList = true;
                }
            }

            string typeString = type.ToDescription();
            if (type == InputType.SUBPAGE || (type == InputType.LIST && !isSimpleList)
                     || type == InputType.TOKENS|| type == InputType.TREE || type == InputType.FILE)
                typeString = "Page";
            else if (isSimpleList || type == InputType.REGEXP
                     || type == InputType.OPTION)
                typeString = "Text";

            string graph = "";
            if (type == InputType.OPTION || type == InputType.LIST || type == InputType.NUMBER)
                (_, graph) = Program.generateUnivariateChartPage(name, new OD.OrderedDictionary<SectionId, OD.OrderedDictionary<GroupId, List<MainLine>>>(), type, false, true);
            else if (type == InputType.TEXT && constraints.Contains("Estesa"))
                (_, graph) = Program.generateUnivariateChartPage(name, new OD.OrderedDictionary<SectionId, OD.OrderedDictionary<GroupId, List<MainLine>>>(), type, true, true);
            if (graph != "")
                graph = @$"
&lt;div class=""toccolours mw-collapsible mw-collapsed"" style=""width:800px; overflow:auto;""&gt;
    &lt;div style=""font-weight:bold;line-height:1.6;""&gt;Distribution of the property&lt;/div&gt;
    &lt;div class=""mw-collapsible-content""&gt;
{graph}
    &lt;/div&gt;
&lt;/div&gt;";

            name = Program.normalizeNames(name);

            return templateXML.Replace("«PNAME»", name)
                           .Replace("«ID»", id.ToString())
                           .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                           .Replace("«TYPE»", typeString)
                           .Replace("«GRAPH»", graph)
                           .Replace("«SAMEAS»", sameAsProperty)
                           .Replace("«SUB-PROPERTY»", superProperty)
                           .Replace("«CONSTRAINTS»", constraintsString)
                           .Replace("«PGROUP»", propertyGroup);
        }

    }
}