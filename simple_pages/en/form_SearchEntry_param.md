<!-- Form:SearchEntry -->

<noinclude>
Form for searching $2

{{#forminput:form=SearchEntry|autocomplete on category=}}

</noinclude>

<includeonly>
<div id="wikiPreview" style="display: none; padding-bottom: 25px; margin-bottom: 25px; border-bottom: 1px solid #AAAAAA;"></div>

{{{info|query form at top}}}

{{{for template|SearchEntry}}}
<div id="sec-Main">

Search is case sensitive.

==Main==

<tabber>

  Data =
    {| class='formtable'
    ! name
    | {{{field|Name|input type=text|property=Name|class=identifier}}}
    |-
    ! surname
    | {{{field|Surname|input type=text|property=Surname|class=identifier}}}
    |-
    ! year of birth
    | {{{field|Year of birth|input type=number|class=identifier}}}
    |-
    ! birthplace
    | {{{field|Birthplace|input type=text|property=Birthplace|class=identifier}}}
    |-
|}
</tabber>
</div>


{{{end template}}}

</includeonly>
