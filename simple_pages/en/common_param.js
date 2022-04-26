
// MediaWiki:Common.js

// <nowiki>

const PROPS = {};
const VARS = {};

$('#mw-pwd').html('&lt;input type="password" id="pwd"&gt;&lt;br/&gt;&lt;input id="store" type="button" value="Memorizza localmente"&gt;&amp;nbsp;&amp;nbsp;&lt;input id="forget" type="button" value="Dimentica i nomi"&gt;');

$("#forget").click(function() {
  localStorage.clear();
  alert('Password cleared');
});

$("#store").click(function() {
  var pwd = $('#pwd').val();
  localStorage.setItem('pwd', pwd);
  alert('Password stored');
});

/*
$('input[type=number]').blur(function () {
  var value = parseInt($(this).val());
  var min = parseInt($(this).attr('min'));
  var max = parseInt($(this).attr('max'));
  if (value < min || value > max) {
    alert('Si prega di specificare un valore tra ' + min + ' e ' + max);
    var blurEl = $(this); 
      setTimeout(function() {
          blurEl.focus()
      }, 10);
  }
});
*/

/**
 * Add chains to an item
 *
 * @param   {string}      category                    the starting category
 * @param   {string}      item                        the original item
 *
 * @returns string        the modified item
*/
function addPropChain(category, item)
{
    for (var key in PROPS[category]) {
        var indx = item.indexOf(key);
        if (indx >= 0 && !item.includes(PROPS[category][key])) {
          return item.replace(key, PROPS[category][key] + ' .' + key);
        }
    }

    return item;
}

/**
 * Add autocompletion to a textarea
 *
 * @param   {string}      id            the ID of the textarea
 *
 * @returns string          the autocompletion code
*/
function addAtAutocomplete(id)
{
  var res = $('#' + id).atwho({
  	  at: '@',
	  spaceSelectsMatch: false,
	  startWithSpace: false,
	  lookUpOnClick: true,
	  acceptSpaceBar: true,
	  hideWithoutSuffix: false,
	  displayTimeout: 300,
	  suffix: '',
	  limit: 6,
	  callbacks: {},
  });

  for (var key in VALUES) {
  	console.log('Adding ' + key + ':: -> ' + JSON.stringify(VALUES[key]));
    res = res.atwho({
    	at: '.' + key + '::',
    	data: VALUES[key]
    });
  }

  return res;
}

/* ID and NAME tokens must begin with a letter ([A-Za-z]) and may be followed by 
 * any number of letters, digits ([0-9]), hyphens ("-"), underscores ("_"), 
 * colons (":"), and periods ("."). */
function normalizeIDs(name)
{
    var res = "";
    for (var i = 0; i < name.length; i++) {
        var char1 = name.charAt(i);
        var cc = char1.charCodeAt(0);

        if (cc == 32)
        	res += '_';
        else if (!((cc > 47 && cc < 58) || (cc > 64 && cc < 91) || (cc > 96 && cc < 123)) 
        		&& char1 != '-' && char1 != '_' && char1 != ':' && char1 != '.') {
        	res += '-';
        } else {
            res += char1;
        }
    }
    
    return res;
}

/*
   USAGE: wrap input boxes in <span class='vect' id='vect-fieldname-with-minuses'> 
   and assign them the appropriate "s" class for setting their width
*/
function rearrangeVectors()
{
  var oldId = '';
  var txt = '';
  $('.vect > pre > span > input').each(function (index, x) {
      //console.log(index, x.name);
      var paren = x.name.indexOf('[');
      var id = x.name.slice(paren + 1, x.name.lastIndexOf(' ')).replaceAll(' ', '-');
      if (oldId != id) {
        if (txt != '') {
            //console.log('vect-' + oldId + '§' + txt);
            $('#vect-' + oldId).html('〈' + txt + '〉');
            $('#vect-' + oldId).toggleClass('vect inputSpan');
            console.log(id + '/' + x.name);
            txt = $(x)[0].outerHTML;
        }
        else {
          //console.log(id + '/' + x.name);
          txt += $(x)[0].outerHTML;
        }
        oldId = id;
      }
      else {
          //console.log(id + '/' + x.name);
          txt += $(x)[0].outerHTML;
      }
  });
  //console.log('vect-' + oldId + '§' + txt);
  $('#vect-' + oldId).html('〈' + txt + '〉');
  $('#vect-' + oldId).toggleClass('vect inputSpan');
}

function GetURLParameter(sParam)
{
    var sPageURL = window.location.search.substring(1); // remove the '?' character at the start of the parameters substring
    var sURLVariables = sPageURL.split('&');
    for (var i = 0; i < sURLVariables.length; i++) {
        var sParameterName = sURLVariables[i].split('=');
        if (sParameterName[0] == sParam) {
            return sParameterName[1];
        }
    }
}

$(function() {
  // append the get parameter 'db' to all URLs, if present
  $("a:not([href^='#']").each(function() {
  	var url = $(this).attr('href');
  	var value = GetURLParameter('db');
  	if (value)
		return url + ((h.indexOf('?') != -1) ? "&db=" : "?db=") + value;
	else
		return url;
  });

  localStorage.setItem('DB', '$1');

  if ($('h1.title').text() === 'Semantic search') {
  	debugger;
  	var at = addAtAutocomplete('ask-query-condition');

    $('#ask-query-condition').blur(function() {
      debugger;
      var cond = $('#ask-query-condition').val();
      var clauses = cond.split('\n');
      var category = clauses[0].substr(clauses[0].indexOf(':') + 1, clauses[0].length - clauses[0].indexOf(':') - 3)
      console.log('Category: ' + category);
      console.log('Clauses: ' + clauses);
      var clausesCompleted = clauses.map(function (x) { return addPropChain(category, x); });
      $('#ask-query-condition').val(clausesCompleted.join('\n'));
    });

    $('#smw-property-input').blur(function() {
      debugger;
      var cond = $('#ask-query-condition').val();
      var clauses = cond.split('\n');
      var category = clauses[0].substr(clauses[0].indexOf(':') + 1, clauses[0].length - clauses[0].indexOf(':') - 3)
      console.log('Category: ' + category);
      var props = $('#smw-property-input').val();
      var printouts = props.split('\n');
      console.log('\nPrintouts: ' + printouts);
      var printoutsCompleted = printouts.map(function (x) { return addPropChain(category, x); });
      $('#smw-property-input').val(printoutsCompleted.join('\n'));
    });
  }

  if (document.title.includes("Data tables") || document.title.includes("Semantic search")) {
    $('table').find('td').each(function() {
      if ($(this).html() === '') {
        var tab = $(this).closest('.tabbertab');
        var hash = encodeURI($(tab).attr('title').replace(/ /g, '_'));
        var curr_row = $(this).closest('tr');
        var url = $(curr_row).find('a').attr('href');

        var db = url.split('/')[1].toLocaleLowerCase();
        var paz = url.split('/')[2];

        // neurologia_sla1/index.php?title=Patient_00002&action=formedit#Familiarit.C3.A0_
        var url = mw.config.get("wgScript") + "/" + paz +"?action=formedit#" + hash;
        $(this).text('');
        $(this).addClass('empty');

        $('<a>',{
            text: '...',
            href: url
        }).appendTo($(this));
      }
    });
  }

  if (document.title.includes("Modify") || document.title.includes("Edit") || document.title.includes("Create")) {
    rearrangeVectors();

	$('input[name="Patient[Patient ID]').prop('readonly', true);
    $('input[name="Patient[Gender]"]').click(function (e) {
    	var lastName = $('input[name="Patient[Last name]"').val();
        var firstName = $('input[name="Patient[First name]"').val();
        var birthDate = $('input[name="Patient[Date of birth]"').val();
        var birthPlace = $('input[name="Patient[Birthplace]"').val();
        var gender = $('input[name="Patient[Gender]"]:checked').val();
    	if (lastName === '' || firstName === '' || birthDate === '' || birthPlace === '') {
    		alert('To check if the patient already exists first fill first name, last name, date of birth and place of birth.');
    	}
    	else {
        // in the template: {{#set: Patient ID={{{Patient ID|}}} }}
        // in the form:
		    // <p>To check if the patient already exists fill the uid and then choose the gender; alternatively, try to submit the form.</p>
		    // <div id="patient-exists" class="alert alert-danger" role="alert"></div>
		    var id = firstName + ' ' + lastName + ' ' + birthDate + ' ' + birthPlace + ' '  + gender;
			mw.loader.using('ext.smw.api', function() {
			    var smwApi = new smw.api();
			    $('input[name="Patient[Patient ID]').val(id);
			    var q = new smw.query(['?PatientID'], {'limit': 1}, ['[[Category:Patients]][[PatientID::' + id + ']]']);
			    var query = q.toString();
			    var link = q.getLink();
			    console.log('Link: ' + link);

	    		// Fetch data via Ajax/SMWAPI
	    		smwApi.fetch(query)
					.done(function (result) {
						debugger;
			            console.log('Results: ' + JSON.stringify(result));
			            if (!jQuery.isEmptyObject(result.query.results)) {
			              var patient = Object.keys(result.query.results)[0];
			              var url = mw.config.values['wgServer'] + mw.config.values['wgScript'] + '?title=' + patient;
			              var msg = 'Warning: possible duplicate of <a href="' + url + '">' + patient + '</a>';
			              $('#patient-exists').html(msg);
			              $('#patient-exists').show();
			              $('input[name="Patient[Patient ID]').val('');
			              $('input[name="Patient[Gender]"]:checked').prop('checked', false);
			              return false;
			            }
			            else {
			            	$('#patient-exists').html('');
			            	$('#patient-exists').hide();
			            	$('input[name="Patient[Patient ID]').val(id);
			            	return true;
			            }
			        })
			        .fail( function ( error ) {
			            // Do something
			            document.getElementsByTagName("body")[0].style.cursor = "default";
			        });
			});
    	}
    });

    $('#contentSub').css('cursor', 'pointer');
    $(document).on('click', '#form_error_header', function() {
      console.log('error is visible');
      var tab = $('.errorMessage').parents('.tabbertab').data('hash');
      var anchor = $('[data-hash=' + tab);
      $('html,body').stop().animate({scrollTop:anchor.offset().top},'slow');
      $(anchor).click();
    });

    $('#contentSub').css('cursor', 'pointer');
    $(document).on('click', '#form_error_header', function() {
      console.log('error is visible');
      var tab = $('.errorMessage').parents('.tabbertab').data('hash');
      var anchor = $('[data-hash=' + tab);
      $('html,body').stop().animate({scrollTop:anchor.offset().top},'slow');
      $(anchor).click();
    });
  }
});

// </nowiki>
