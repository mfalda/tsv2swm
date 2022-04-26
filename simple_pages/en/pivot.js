// MediaWiki:Pivot.js

$('li#n-Search-an-entry a').prepend('<div id="drop-icon"><i class="fa fa-search"></i></div>');
$('li#n-Add-an-entry a').prepend('<div id="drop-icon"><i class="fa fa-plus-square"></i></div>');
$('li#n-Modify-an-entry a').prepend('<div id="drop-icon"><i class="fa fa-edit"></i></div>');
$('li#n-Property-chains-helper a').prepend('<div id="drop-icon"><i class="fa fa-search-plus"></i></div>');
$('li#n-Semantic-search a').prepend('<div id="drop-icon"><i class="fa fa-search-plus"></i></div>');
$('li#n-Explore-data a').prepend('<div id="drop-icon"><i class="fa fa-filter"></i></div>');
$('li#n-Data-tables a').prepend('<div id="drop-icon"><i class="fa fa-table"></i></div>');
$('li#n-Plots a').prepend('<div id="drop-icon"><i class="fa fa-pie-chart"></i></div>');
$('li#n-Maps a').prepend('<div id="drop-icon"><i class="fa fa-map-marker"></i></div>');
$('li#n-Export-data a').prepend('<div id="drop-icon"><i class="fa fa-download"></i></div>');

$('#searchInput').focusin(function () {
    $(this).val('~*term*');
 });
 