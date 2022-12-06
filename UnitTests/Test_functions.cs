using System.Collections.Generic;
using Xunit;
using TSV2SMW;

using OD;
using System;
using System.Reflection;
using System.IO;

namespace UnitTests
{
    using SectionsDict = OrderedDictionary<SectionId, OrderedDictionary<GroupId, List<MainLine>>>;

    public class UnitTest1
    {
        public static string GetProjectPath(string relativePath)
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath + "../../../../");

            return Path.Combine(codeBasePath, "TestFiles", relativePath);
        }

        [Fact]
        public void ConvertEntities_is_OK()
        {
            string result = Program.convertEntities("test&'");

            Assert.Equal("test&amp;&#39;", result);
        }

        [Fact]
        public void FormField_is_OK()
        {
            string result = new FormField("Prop 1", InputType.NUMBER, "prop 1", "min=1,max=10", new List<OptionType>() { OptionType.MANDATORY }, "info 1", "Entry", "").ToString();

            Assert.Equal(@"    ! style=""width: 30%""| Prop 1* {{#info: info 1|note}}
        | style=""width: 70%""| {{{field|prop 1|input type=number|property=prop 1|mandatory|min=1|max=10|step=any}}}
        |-
", result, false, true, true);
        }

        [Fact]
        public void ReadPanelMenu_is_OK()
        {
            string name = "MediaWiki:Sidebar";
            var result = new RawPage(1, name, NamespaceType.MEDIAWIKI, GetProjectPath("TVV2SWM/simple_pages/en") + "/sidebar_param.md", "", new List<string>{});

            string timeStamp = System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            Assert.Equal($@"  <page>
    <title>{name}</title>
    <ns>8</ns>
    <id>1</id>
    <revision>
      <timestamp>{timeStamp}</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version</comment>
      <text xml:space=""preserve"">
&lt;noinclude&gt;

&lt;/noinclude&gt;

&lt;includeonly&gt;

* Navigation
** mainpage|mainpage-description
** recentchanges-url|recentchanges
** randompage-url|randompage
* Modify
** Special:RunQuery/Search$1|Search a $1
** Special:FormEdit/$1|Add a $1
** Modify a $1|Modify a $1
** Special:Ask|Semantic search
** Special:BrowseData/$2|Explore data
** Data table|Data table
** Plots|Plots
** Maps|Maps
** Export|Export data
* SEARCH
* TOOLBOX

&lt;/includeonly&gt;
      </text>
    </revision>
  </page>
    ", result.ToXML());
        }
/*
        [Fact]
        public void Category_is_OK()
        {
            var form = new Form(1, "Form 1", "message", new SectionsDict(), "", new List<CoreForm>(), "note", "category", "");
            var result = new Category(1, "Category 1", "Main Cat", "", "Parent Cat", form, new List<TemplateField>(), false);

            Assert.Equal(@"    ! style=""width: 30%"" |  Prop 1{{#info: info 1|note}}
        | style=""width: 70%""| {{{field|Prop 1|input type=number|property=prop 1|mandatory|min=1|max=10}}}
        |-
    ", result.ToXML());
        }*/

    }
}
