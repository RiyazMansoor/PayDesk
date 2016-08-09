
function hasException( json ) 
{

    if ( json.exceptionMessage )
    {
        $( '#waitmsg' ).html( '<span style="color:red"><b>' + json.exceptionMessage + '</span>' );
        return 1 ;
    }

    $( '#waitmsg' ).hide();
    $( '#datadiv' ).show();
    return 0;

}

function genTable( data )
{

    var columns = settings.columns;
    var datatable = data[settings.tablekey];
    console.log( data );

    var t = '<tr>';
    for ( var c = 0 ; c < columns.length ; c++ )
    {

        var formats = '';
        if ( columns[c].formats )
        {
            formats = columns[c].formats.reduce( function ( rv, cv ) { return rv + cv.type + ','; }, '' );
        }

        var h = columns[c].head;

        var w = columns[c].width;
        if ( h == 'Pension' ) w = '2cm';
        else if ( h == 'Identifier' ) w = '3cm';
        else if ( h == 'Member-Id' ) w = '3cm';
        //else if ( h == 'Member-Name' ) w = '10cm';
        else if ( h == 'Wf-Number' ) w = '4cm';
        else if ( formats.indexOf( 'fmtLinkMemberId,' ) >= 0 ) w = '3cm';
        else if ( formats.indexOf( 'fmtLinkMemberDnr,' ) >= 0 ) w = '3cm';
        else if ( formats.indexOf( 'fmtDate,' ) >= 0 ) w = '3cm';
        else if ( formats.indexOf( 'fmtDateTime,' ) >= 0 ) w = '4cm';
        else if ( formats.indexOf( 'fmtMonth,' ) >= 0 ) w = '2cm';
        else if ( formats.indexOf( 'fmtMoney,' ) >= 0 ) w = '3cm';
        else if ( formats.indexOf( 'fmtMoneyDebt,' ) >= 0 ) w = '3cm';

        w = ( w ? 'style="width:' + w + ';"' : '' );
        t += '<td ' + w + ' >' + h + '</td>';

    }
    t += '</tr>'

    for ( var r = 0 ; r < datatable.length ; r++ )
    {
        t += '<tr>';
        for ( var c = 0 ; c < columns.length ; c++ )
        {
            var v = datatable[r][columns[c].field];
            if ( columns[c].formats )
            {
                for ( var f = 0 ; f < columns[c].formats.length ; f++ )
                {
                    var fmt = columns[c].formats[f];
                    //console.log( fmt );
                    v = fmt.param ? window[fmt.type].call( null, v, fmt.param ) : window[fmt.type].call( null, v ) ;
                }
            }
            t += '<td>' + ( v == null ? '' : v ) + '</td>';
        }
        t += '</tr>'
    }

    return t;

}

function fmtBold( text )
{
    return '<b>' + text + '</b>';
}
function fmtColor( text, color )
{
    return '<span style="color:' + color + ';">' + text + '</span>';
}
function fmtTitle( text, title )
{
    return '<span title="' + title + ';">' + text + '</span>';
}
function fmtDate( date )
{
    return ( date == null ) ? '' : date.substr( 0, 10 );
}
function fmtMonth( date )
{
    return ( date == null ) ? '' : date.substr( 0, 7 );
}
function fmtDateTime( date )
{
    return ( date == null ) ? '' : date.substr( 0, 16 );
}
function fmtMoney( number )
{
    // http://stackoverflow.com/questions/149055/how-can-i-format-numbers-as-money-in-javascript
    return number == null ? '' : number.toFixed( 2 ).replace( /(\d)(?=(\d{3})+\.)/g, '$1,' );
}
function fmtMoneyDebt( number )
{
    if ( number == null ) return '';
    if ( number == 0 ) return '0.00';
    return '<span style="color:red;">' + number.toFixed( 2 ).replace( /(\d)(?=(\d{3})+\.)/g, '$1,' ) + '</span>';
}
function fmtPhoneticMv( text )
{
    return text == null ? '' : '<span style="font-family:A_Randhoo;float:right;">' + text + '</span>';
}
function fmtLinkIdentifier( identifier )
{
    if ( identifier == null ) return '';
    return '<a target="_blank" href="/paydesk/member/identifier/' + identifier + '">' + identifier + '</a>';
}
function fmtLinkMemberId( memberid )
{
    if ( memberid == null ) return '';
    return '<a target="_blank" href="/paydesk/member/memberid/' + memberid + '">' + memberid + '</a>';
}
function fmtLinkMemberDnr( identifier )
{
    if ( identifier == null ) return '';
    return '<a target="_blank" href="/paydesk/member/dnrliveservice/' + identifier + '">' + identifier + '</a>';
}
function fmtBankAccType( type )
{
    switch ( type )
    {
        case 1: return 'Single';
        case 2: return 'Joint';
        default: return 'Unknown';
    }
}
function fmtKoshaaruDownload( urlPath )
{
    if ( urlPath == null ) return '';
    urlPath = urlPath.replace( /\\/g, '/' );
    var name = urlPath.substr( urlPath.lastIndexOf( '/' ) + 1 );
    return '<a target="_blank" href="https://www.koshaaru.mv/newportal' + urlPath + '">' + name + '</a>';
}
function fmtRowValid( flag )
{
    return ( flag == 0 ? '<span style="color:red;"><b>ERROR</b></span>' : 'OK :)' );
}
/*
function fmtDataCell( value ) 
{
    if ( value == null ) value = '' ;
    return '<td class="tddata">' + value + '</td>'; 
} 
function fmtHeadCell( value )
{
    if ( value == null ) value = '';
    return '<td class="tdhead">' + value + '</td>';
}
*/

function CenteredDataTableReady( trs ) // trs == html form for custom parameters
{

    document.title = settings.title;

    trs = trs ? '<table id="inputtable" class="inputtable">' + trs + '</table><br/>' : '' ;
    var body = settings.titlelink ? 'url="' + settings.titlelink + '"' : 'goback';
    body = '<span class="thetitle" ' + body + ' ">The Payment Portal</span>';
    body += '<span id="version">Pensions &amp; Claims Department</span>' + trs;
    body += '<div id="waitmsg" /><div id="datadiv"><table id="centereddatatable" class="centereddatatable"></table></div>';
    $( 'body' ).html( body );

    $( '#datadiv' ).hide();
    $( 'span[url]' ).click( function ( e ) { window.open( $( this ).attr( 'url' ), "_self" ); } );
    $( 'span[goback]' ).click( function ( e ) { window.history.back(); } );
    if ( trs.length > 0 ) $( '#cmdGo' ).click( InsertInputTableGoClick );

    if ( trs.length == 0 )
    {
        var timer = readyTimer();
        var url = window.location.pathname.replace( '/paydesk', '/paydesk/api1' );
        $.get( url, function ( json ) { CenteredDataTableCallback( timer, json ); } );
    }

}

//function CenteredDataTableStruct()
//{
//    // set page title
//    document.title = settings.title;
//    // set headings and info
//    var body = settings.titlelink ? 'url="' + settings.titlelink + '"' : 'goback';
//    body = '<span class="thetitle" ' + body + ' ">The Payment Portal</span>';
//    body += '<span id="version">Pensions &amp; Claims Department</span>'; // - [ version:2.0, released:2016-06-01, database:' + dbsynctime + ' ] </span>';
//    body += '<div id="waitmsg" /><div id="datadiv"><table id="centereddatatable" class="centereddatatable"></table></div>';
//    $( 'body' ).html( body );
//    // commong functions to execute if elements exist
//    $( '#datadiv' ).hide();
//    $( 'span[url]' ).click( function ( e ) { window.open( $( this ).attr( 'url' ), "_self" ); } );
//    $( 'span[goback]' ).click( function ( e ) { window.history.back(); } );
//}
//function CenteredDataTableReady()
//{
//    CenteredDataTableStruct();
//    var timer = readyTimer();
//    var url = window.location.pathname.replace( '/paydesk', '/paydesk/api1' );
//    $.get( url, function ( json ) { CenteredDataTableCallback( timer, json ); } );

//}
function InsertInputTableGoClick()
{
    var timer = readyTimer();
    var url = window.location.pathname.replace( '/paydesk', '/paydesk/api1' ) + downloadUrlParams();
    url = url.replace( '//', '/' );
    $.get( url, function ( json ) { CenteredDataTableCallback( timer, json ); } );
}
//function InsertInputTable( trs )
//{
//    var inputtable = '<table id="inputtable" class="inputtable">' + trs + '</table><br/>';
//    $( '#version' ).after( inputtable );
//    $( '#cmdGo' ).click( InsertInputTableGoClick );
//}
function readyTimer()
{
    var counter = 0;
    var timer = function () 
    {
        counter += 1;
        $( '#waitmsg' ).text( '..... loading ' + counter + ' seconds .....' );
    }
    return setInterval( timer, 1000 );
}
function CenteredDataTableCallback( timer, json )
{
    clearInterval( timer );
    if ( hasException( json ) ) return;
    var downloadlabel = '<span id="filedown" class="atitle" style="margin:0;padding:0;padding-left:1cm;">↓↓↓</span>';
    $( '#centereddatatable' ).html( genTable( json ) ).append( "<caption>" + ( settings.caption || settings.title ) + downloadlabel + "</caption>" );
    $( '#filedown' ).click( function ()
    {
        var url = window.location.pathname.replace( '/paydesk', '/paydesk/file' ) ;
        if ( window["downloadUrlParams"] ) url += downloadUrlParams();
        url = url.replace( "//", "/" ); 
        window.open( url );
    } );
}

function MenuPageReady()
{
    // set page title
    document.title = menu.title;
    var units = menu.units.reduce( function ( rv, cv ) { return rv + '<option value="' + cv.code + '">' + cv.name + '</option>' ; }, '' ) ;
    var pages = menu.pages.reduce( function ( rv, cv ) { return rv + '<tr code="' + cv.code + '"><td><span class="menu" url="' + cv.value + '">' + cv.name + '</span></td></tr>'; }, '' );
    var body = menu.titlelink ? 'url="' + menu.titlelink + '"' : 'goback';
    body = '<span class="thetitle" ' + body + ' >The Payment Portal</span>';
    body += '<span id="version">Pensions &amp; Claims Department</span>'; 
    body += '<span class="atitle" style="cursor:unset;font-weight:bold;">' + menu.title + '</span><br/>';
    body += '<table id="menutable" class="menutable"><tr><td><select id="selUnits" class="textbox" style="text-align-last:center;width:10cm;">' + units + '</select><br/><br/></td></tr>' + pages + '</table>';
    $( 'body' ).html( body );
    $( '#selUnits' ).change( function ()
    {
        $( '#menutable' ).find( 'tr[code]').hide();
        $( '#menutable' ).find( 'tr[code=' + $( this ).val() + ']' ).show();
    } ).trigger( 'change' );
    // commong functions to execute if elements exist
    $( 'span[url]' ).click( function ( e ) { window.open( $( this ).attr( 'url' ), "_self" ); } );
    $( 'span[goback]' ).click( function ( e ) { window.history.back(); } );
}
function MenuPage2Ready()
{
    MenuPageReady();

    var depts = menu.depts.reduce( function ( rv, cv ) { return rv + '<option value="' + cv.code + '">' + cv.name + '</option>'; }, '' );
    $( '#menutable' ).prepend( '<tr><td><select id="selDepts" class="textbox" style="text-align-last:center;width:10cm;">' + depts + '</select><br/></td></tr>' );
    $( '#selDepts' ).change( function ()
    {
        var key = $( this ).val() + $( '#selUnits' ).val();
        $( '#menutable' ).find( 'tr[code]' ).hide();
        $( '#menutable' ).find( 'tr[code=' + key + ']' ).show();
    } );
    $( '#selUnits' ).unbind( 'change' ).change( function ()
    {
        var key = $( '#selDepts' ).val() + $( this ).val();
        $( '#menutable' ).find( 'tr[code]' ).hide();
        $( '#menutable' ).find( 'tr[code=' + key + ']' ).show();
    } ).trigger( 'change' );

}


function urlFrom( urlPart )
{
    var url = decodeURIComponent( window.location.pathname ) + '/';
    url = url.replace( '//', '/' );;
    console.log( 'urlFrom :: ' + url.substr( url.indexOf( urlPart ), 100 ) );
    return url.substr( url.indexOf( urlPart ), 100 );
}


function fldStr( l, v, t ) { if ( v == null ) return ''; if ( t == null ) t = '$l <span class="spanValue" >$v</span>'; return t.replace( '$l', l ).replace( '$v', v ); }
function addrStr( l, a, i, h, t ) { if ( a == null && i == null && h == null ) return ''; return '$l <span class="spanValue" style="">$h, $i, $a</span>'.replace( '$l', l ).replace( '$h', h.trim() ).replace( '$i', i.trim() ).replace( '$a', a.trim() ); }
function tr( l ) { return $( '<tr>' ).append( $( '<td>' ).text( l ) ).append( $( '<td>' ) ); }



