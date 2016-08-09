workDesk = function(word){
	
  var txt = word.selectionText.trim().toUpperCase() ;
  var query = null ;
  
  if ( txt.length > 9 ) alert("Selection did not parse for Work Desk");
  else if ( txt.search(/[A-Z]{1}[A-Z]?\d{6}\d?/g) == 0 ) query = '/identifier/' + txt;
  else if ( !isNaN( txt ) ) query = ( parseInt( txt ) > 300000 ? '/paymentid/' : '/memberid/' ) + txt;
  
  if ( query != null ) chrome.tabs.create({url: "http://192.168.2.215:81/paydesk/member" + query});

};

chrome.contextMenus.create({
  title: "MPAO Workdesk",
  contexts:["selection"],
  onclick: workDesk
});