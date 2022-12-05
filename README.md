# tsv2smw

This command line tool allows to design a Semantic MediaWiki site from a specification in TSV and then to populate it. For an example of the two TSV files please look at the `schema_Virus.tsv` and `data_Virus_pos.tsv`; the format is:

```
<start> ::= <Category part>* <Subpage part>* <Main part>

<Category part> ::= <Ordinal category> | <Map category>

<Ordinal category> ::= <Category label> <Category body>+ <Emptyline>

<Category label> ::= Category:<Name><Newline>

<Name> ::= [A-Za-zàèéìòù0-9_]+

<Emptyline> ::= <Newline><Newline>

<Newline> ::= \n

<Category body> ::= <Name><Newline>

<Map category> ::= <Map category label> <Map category parent> <Category body>+ <Emptyline>

<<Map category label> ::= Category:<Name>"|"<Mapped fields>\n



<Main part> ::= <Section label> <Section header> <Section body>+ <Emptyline>

<Subpage part> ::= <Subpage label> <Section header> <Section body>+ <Emptyline>

<Section label> ::= Section:<Name>\n

<Subpage label> ::= Subpage:<Name>/<Category>\n



<Category> ::= <Name>

<Section header> ::= Group<TAB>Super-property<TAB>Property
<TAB>Type<TAB>Domain<TAB>Option<TAB>Info\n

<Section body> ::= <Name><TAB><Name><TAB><Name<TAB><Type>
<TAB><Domain><TAB><Option><TAB>.*\n

<TAB> ::= \t

<Type> ::= Text|Date|Number|Geographic coordinates|List|File|Subpage|Boolean

<Domain> ::= (<Name>,(<Name>,)+)|<Subpage label>|<ParserFunction>

<ParserFunction> ::= /* A MediaWiki parser function (see text) */

<Options> ::= Integer|Extended|Exclusive|Computed|Repeated
```

## Installation

You should be at ease with .NET Core in order to work with this tool (and also with Semantic MediaWiki), however there are three _scripts_: `publish_win64.sh` `publish_win64.bat` `publish_linux64.sh` to create self-contained executables for Windows (from Linux and Windows) and Windows. Once created the file, it can be run from the same root directory; note that there are auxiliary directories with templates and othe files needed to run the program. In Linux refer to [this site](https://docs.microsoft.com/en-us/dotnet/core/install/linux). 

