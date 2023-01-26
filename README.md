# tsv2smw

This command line tool allows to design a Semantic MediaWiki site from a specification in TSV and then to populate it. It relies on a complex infrastructure that can be setup using a Docker Compose configuration [here](https://github.com/mfalda/docker-smw).

For an example of TSV files for schema and data, please look at the `schema_Virus.tsv` and `data_Virus_pos.tsv`. The schema grammar is reported at the end, however it can be described at a higher level as a set of (parent) categories, subpages, and sections. Categories can refer to a map, and in this case they need the properties they refer to and the Leaflet layer. Subpages contain data that are in a subordinate relation with the (main) section; sections are instead mere visual aggregations of properties represented as distinct tables. 

Data TSV are simple tables in which each row contains an identifier, a parent page when referring to a subpage, and then the set of properties declared in the schema. Note that as now these files are still not processed together, therefore constraints are not enforced, nor names checked against the schema. This will be one of the many planned enhancements.

A [demo site](https://dbnsdemo.neuroscienze.unipd.it/wiki/Main_Page) is available.


## Installation

You should be at ease with .NET Core in order to work with this tool (and also with Semantic MediaWiki), however there are three _scripts_: `publish_win64.sh` `publish_win64.bat` `publish_linux64.sh` to create self-contained executables for Windows (from Linux and Windows) and Windows. Once created the file, it can be run from the same root directory; note that there are auxiliary directories with templates and othe files needed to run the program. In Linux refer to [this site](https://docs.microsoft.com/en-us/dotnet/core/install/linux).

The generated schema is more than a simple set of properties, forms, and templates: It allows for interfacing with R using widgets, and the previous [Docker Compose](https://github.com/mfalda/docker-smw) configuration contains a demo setup.


## Structure of the code

The code uses XML snippets (templates) for each type of MediaWiki namespace, namely categories, forms, properties and MediaWiki templates, plus a series of other auxiliary types. There are placeholders that will be filled dynamically according to the actual inputs during execution.

There is also a set of simple pages to be added directly, and they are customized according to the desired (natural) language. Their parametrization it kept at minimum, being possible only two placeholders for singular (forms) and plural (categories) names.

## Citing this work

If you find this tool useful please cite:

> Falda, M., Atzori, M. & Corbetta, M. Semantic wikis as flexible database interfaces for biomedical applications. Sci Rep 13, 1095 (2023). https://doi.org/10.1038/s41598-023-27743-9

See also "Cite this repository" on the top right menu for BibTeX and APA formats.


## Appendix: TSV schema grammar


```
<start> ::= <Category part>* <Subpage part>* <Section part>* <Main part>

<Category part> ::= <Ordinal category> | <Map category>

<Ordinal category> ::= <Category label> <Category body>+ <Emptyline>

<Category label> ::= "Category:" <Name> <Newline>

<Name> ::= [A-Za-zàèéìòù0-9 _]+

<Emptyline> ::= <Newline> <Newline>

<Newline> ::= "\n"

<Category body> ::= <Name> <Newline>

<Map category> ::= <Map category label> <TAB> <Map category parent> <Map category body>+ <Emptyline>

<Map category label> ::= Category:<Name>"|"<Mapped fields> <TAB> "Parent:" /*its parent category*/ <TAB> "Coordinates[Geographic coordinates]:" /*the Leaflet overlay*/ <Newline>

<Map category body> ::= <Name> <TAB> /*parent*/<TAB>/*lat., long.*/<Newline>

<Subpage part> ::= <Subpage label> <Newline> <Section header> <Newline> <Section body>+ <Emptyline>

<Subpage label> ::= "Subpage:" <Name> "/" /*its category*/

<Section header> ::= "Group" <TAB> "Super-property" <TAB>"Property"
<TAB> "Type" <TAB> "Domain" <TAB> "Option" <TAB> "Info" <Newline>

<Section body> ::= <Name> <TAB> <Name> <TAB> <Name> <TAB> <Type>
<TAB> <Domain> <TAB> <Option> <TAB> <Name> <TAB> <Newline>

<Type> ::= Text | Date | Number | Geographic coordinates | List | File | Subpage | Boolean

<Domain> ::= (<Name>,(<Name>,)+) | <Subpage label> | <ParserFunction>

<ParserFunction> ::= /*A MediaWiki parser function*/

<Options> ::= Computed | Identifier | Integer|Extended | Exclusive | Repeated

<Section part> ::= <Section label> <Section header> <Section body>+ <Emptyline>

<Section label> ::= "Section:" <Name> <Newline>

<Section part> ::= "Section:Main" <Section header> <Section body>+ <Emptyline>

```
