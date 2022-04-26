<!-- Widget:Iframe -->


<noinclude>__NOTOC__
This widget allows you to embed any web page on your wiki page using an <code>iframe</code> tag.

Created by [https://www.mediawikiwidgets.org/User:Sergey_Chernyshev Sergey Chernyshev].

== Using this widget ==
For information on how to use this widget, see [https://www.mediawikiwidgets.org/Iframe widget description page on MediaWikiWidgets.org].

<big>'''<font color="red">This widget should not be used on a publicly-editable wiki.</font>'''</big>

While the URL is validated to be a valid URL, there is no way the widget can check the contents of the page that is included. When enabling this widget, you allow any user that can edit to include any page, including malicious pages (containing trojans, backdoors, viruses etc), pages that brake out of the iframe and pages that look like your site, but actually is a copy used for phishing.

== Copy to your site ==
To use this widget on your site, just install [https://www.mediawiki.org/wiki/Extension:Widgets MediaWiki Widgets extension] and copy the [{{fullurl:{{FULLPAGENAME}}|action=edit}} full source code] of this page to your wiki as page '''{{FULLPAGENAME}}'''.
</noinclude><includeonly><iframe src="<!--{$url|validate:url}-->" style="border: <!--{$border|escape:html|default:0}-->" width="<!--{$width|escape:html|default:400}-->" height="<!--{$height|escape:html|default:300}-->"></iframe></includeonly>

<hr />

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]
