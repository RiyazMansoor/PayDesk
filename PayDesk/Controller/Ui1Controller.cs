using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Web.Http;
using System.Web.Hosting;
using System.Web.Routing;
using System.Collections.Generic;
using System.Diagnostics;
using PayDesk.Util;
using System;
using Newtonsoft.Json;

namespace PayDesk.Controller
{

    public class Ui1Controller : ApiController
    {

        private static char[] TemplateFilenameDelimiters = new char[] { '-' };
        private static Dictionary<string, JArray> Reports = new Dictionary<string, JArray>();
        private static Dictionary<string, byte[]> Templates = new Dictionary<string, byte[]>();
        private static Dictionary<string, string> PathPage = new Dictionary<string, string>();
        public  static Dictionary<string, string> PathData = new Dictionary<string, string>();

        private static string[] MemberSearchKeys = { "identifier", "memberid", "paymentid" };

        static Ui1Controller()
        {
            initStaticResources();
        }

        public static void initStaticResources()
        {

            // loads all .json files in templates directory - they must be REPORT settings
            string[] rptJsonFiles = Directory.GetFiles( HostingEnvironment.MapPath( "~/reports/templates" ), "*.json" );
            Debug.WriteLine( "Report Count : " + rptJsonFiles.Length );
            Array.Sort( rptJsonFiles );
            Array.Reverse( rptJsonFiles );
            Reports.Clear();
            foreach ( string rptJsonFile in rptJsonFiles )
            {
                string rptName = Path.GetFileName( rptJsonFile ) ;
                int pos = rptName.IndexOf( '-' );
                if ( pos < 0 )
                {
                    Debug.WriteLine( "IGNORING :: filename :: " + rptJsonFile );
                    continue;
                }
                string rptKey = rptName.Substring( 0, pos ) ;
                if ( Reports.ContainsKey( rptKey ) ) continue ;
                Debug.WriteLine( "ReportKey :: " + rptKey + " :: filename :: " + rptJsonFile );
                Reports.Add( rptKey, JsonConvert.DeserializeObject<JArray>( File.ReadAllText( rptJsonFile )  ) );
            }

            // load all .??tx template files. filename format[ key-Vyyyymmdd.dotx ] where the hightest Vyyymmdd is stored under key
            // note: the asc sort ensures that ONLY the newest version is stored under key - duplicate keys are old versions
            string[] templateFiles = Directory.GetFiles( HostingEnvironment.MapPath( "~/reports/templates" ), "*.??tx" );
            Debug.WriteLine( "Template Count : " + templateFiles.Length );
            Array.Sort( templateFiles );
            Array.Reverse( templateFiles );
            Templates.Clear();
            foreach ( string templateFile in templateFiles )
            {
                string tmplName = Path.GetFileName( templateFile );
                string tmplKey = tmplName.Substring( 0, tmplName.IndexOf( '-' ) );
                if ( Templates.ContainsKey( tmplKey ) ) continue;
                Debug.WriteLine( "TemplateKey :: " + tmplKey + " :: filename :: " + templateFile );
                Templates.Add( tmplKey, File.ReadAllBytes( templateFile ) );
            }


            // loads all .json files in templates directory - they must be REPORT settings

            PathPage.Clear();
            PathData.Clear();
            readHtml( "member" );
            readHtml( "checklist" );
            readHtml( "report" );
            readHtml( "document/monthly" );
            readHtml( "document/weekly" );
            readHtml( "document/anytime" );
            readHtml( "" );
            //updateSyncTime();

        }

        private static void readHtml( string folderName )
        {
            string[] htmlFiles = Directory.GetFiles( HostingEnvironment.MapPath( "~/html/" + folderName ), "*.html" );
            Array.Sort( htmlFiles );
            Array.Reverse( htmlFiles );
            foreach ( string htmlFile in htmlFiles )
            {
                string fileName = Path.GetFileName( htmlFile );
                string procName = fileName.Substring( 0, fileName.IndexOf( '-' ) );
                string pathKey = folderName.Replace( "/", "" ) + procName.ToLower();
                Debug.WriteLine( "pathKey :: " + pathKey + " :: filename :: " + htmlFile );
                if ( PathPage.ContainsKey( pathKey ) ) continue;
                PathPage.Add( pathKey, File.ReadAllText( htmlFile ) );
                PathData.Add( pathKey, procName );
            }
        }

        public static void UpdateTestDbLastSync( string TestDbLastSync )
        {
            string html;
            bool found = PathPage.TryGetValue( "portal", out html ); // must be found !!!
            int start = html.IndexOf( "Pensions &amp;" );
            int end = html.IndexOf( "<br", start );
            string oldVersion = html.Substring( start, ( end - start ) );
            Debug.WriteLine( "oldVersion# :: " + oldVersion );
            string newVersion = "Pensions &amp; Claims Department - [ version:1.0, released:2016-06-07, db-sync:" + TestDbLastSync + " ]";
            html = html.Replace( oldVersion, newVersion );
            PathPage[ "portal" ] = html;
        }

        [HttpGet]
        public HttpResponseMessage PortalUiHtml( )
        {
            string html;
            bool result = PathPage.TryGetValue( "portal", out html );
            if ( result ) return WebUtil.RawHtmlResponse( html );
            return WebUtil.RawHtmlExceptionResponse( "ROOT Page NOT Found" );
        }

        [Route( "ui1/refresh/{*.}" ), HttpGet]
        public HttpResponseMessage MemberRefreshHtml( )
        {
            initStaticResources();
            return WebUtil.RawHtmlResponse( "Refreshed Sources :: Ui1Controller" );
        }


        [Route( "{Path}/" ), HttpGet]
        public HttpResponseMessage RootPageRouter( string Path )
        {
            string html;
            bool result = PathPage.TryGetValue( Path.ToLower(), out html );
            if ( result ) return WebUtil.RawHtmlResponse( html );
            return PaydeskRedirectRelative( Request, "../portal" );
        }

        [Route( "member/{SearchKey}/{Parameter}" ), HttpGet]
        public HttpResponseMessage MemberPageRouter( string SearchKey, string Parameter )
        {
            SearchKey = SearchKey.ToLower();
            if ( MemberSearchKeys.ToList().IndexOf( SearchKey ) >= 0 ) {
                string html;
                bool result = PathPage.TryGetValue( "membermemberoverview", out html );
                if ( result ) return WebUtil.RawHtmlResponse( html );
            }
            else if ( "dnrliveservice" == SearchKey )
            {
                string html;
                bool result = PathPage.TryGetValue( "memberdnrliveservice", out html );
                if ( result ) return WebUtil.RawHtmlResponse( html );
            }
            // redirect to path home
            return PaydeskRedirectRelative( Request, "../../" );
        }
        [Route( "file/member/{SearchKey}/{Parameter}" ), HttpGet]
        public HttpResponseMessage MemberDataRouter( string SearchKey, string Parameter )
        {
            SearchKey = SearchKey.ToLower();
            if ( MemberSearchKeys.ToList().IndexOf( SearchKey ) < 0 ) return WebUtil.RawHtmlExceptionResponse( "URL path INCORRECT ! Go Back " );
            // 
            JObject rptData = null;
            if ( SearchKey == "identifier" ) rptData = Api1Controller.StaticMemberOverviewByIdentifier( Parameter );
            else {
                int paramId = -1;
                bool toInt = int.TryParse( Parameter, out paramId );
                if ( !toInt ) return WebUtil.RawHtmlExceptionResponse( "Parameter is NOT an int ! Go Back " );
                else if ( SearchKey == "memberid" ) rptData = Api1Controller.StaticMemberOverviewByMemberId( paramId );
                else if ( SearchKey == "paymentid" ) rptData = Api1Controller.StaticMemberOverviewByPaymentId( paramId );
            }
            return ReportUtil.createExcel( "MemberOverview", rptData );
        }
        [Route("member/dnrmpaoservice/{NamePart}/{AddressPart}/{IslandPart}"), HttpGet]
        public HttpResponseMessage MemberDnrSearchHtml( string NamePart, string AddressPart, string IslandPart )
        {
            string html;
            bool result = PathPage.TryGetValue( "memberdnrmpaoservice", out html );
            if ( result ) return WebUtil.RawHtmlResponse( html );
            return PaydeskRedirectRelative( Request, "../../../../" );
        }

        [Route( "document/anytime/{process}/{p1?}/{p2?}/" ), HttpGet]
        public HttpResponseMessage DocumentAnytimePageRouter( string process, string p1 = null, string p2 = null )
        {
            return GenericPageRouter( "documentanytime", process );
        }
        [Route( "file/document/anytime/{process}/{p1?}/{p2?}/" ), HttpGet]
        public HttpResponseMessage DocumentAnytimeFileRouter( string process, string p1 = null, string p2 = null )
        {
            return GenericFileRouter( "documentanytime", process, p1, p2 );
        }

        [Route( "document/weekly/{process}/{WeeksAgo?}" ), HttpGet]
        public HttpResponseMessage DocumentWeeklyPageRouter( string process, string WeeksAgo = null )
        {
            return GenericPageRouter( "documentweekly", process );
        }
        [Route( "file/document/weekly/{process}/{WeeksAgo?}" ), HttpGet]
        public HttpResponseMessage DocumentWeeklyFileRouter( string process, string WeeksAgo = null )
        {
            return GenericFileRouter( "documentweekly", process, WeeksAgo );
        }

        [Route( "document/monthly/{process}/{MonthsAgo?}" ), HttpGet]
        public HttpResponseMessage DocumentMonthlyPageRouter( string process, string MonthsAgo = null )
        {
            return GenericPageRouter( "documentmonthly", process );
        }
        [Route( "file/document/monthly/{process}/{MonthsAgo?}" ), HttpGet]
        public HttpResponseMessage DocumentMonthlyFileRouter( string process, string MonthsAgo = null )
        {
            return GenericFileRouter( "documentmonthly", process, MonthsAgo );
        }


        [Route( "checklist/{process}" ), HttpGet]
        public HttpResponseMessage ChecklistPageRouter( string process )
        {
            return GenericPageRouter( "checklist", process );
        }

        [Route( "file/checklist/{process}/{p1?}/{p2?}/{p3?}/" ), HttpGet]
        public HttpResponseMessage ChecklistFileRouter( string process, string p1 = null, string p2 = null, string p3 = null )
        {
            return GenericFileRouter( "checklist", process, p1, p2, p3 );
        }


        [Route( "report/{process}" ), HttpGet]
        public HttpResponseMessage ReportPageRouter( string process )
        {
            return GenericPageRouter( "report", process );
        }

        [Route( "file/report/{process}/{p1?}/{p2?}/{p3?}/" ), HttpGet]
        public HttpResponseMessage ReportFileRouter( string process, string p1 = null, string p2 = null, string p3 = null )
        {
            return GenericFileRouter( "report", process, p1, p2, p3 );
        }

        private static HttpResponseMessage GenericPageRouter( string module, string process )
        {
            string html;
            bool result = PathPage.TryGetValue( module.ToLower() + process.ToLower(), out html );
            if ( result ) return WebUtil.RawHtmlResponse( html );
            return WebUtil.RawHtmlExceptionResponse( "Buddy, you're lost ! Don't type URLs, click links if unsure." );
        }
        private static HttpResponseMessage GenericFileRouter( string module, string process, string p1 = null, string p2 = null, string p3 = null )
        {
            module = module.ToLower();
            process = process.ToLower();

            string fileName;
            bool result = PathData.TryGetValue( module + process, out fileName );
            if ( !result ) return WebUtil.RawHtmlExceptionResponse( "FileName NOT Found ! URL == " + module + "," + process );

            JObject data = Api1Controller.StaticUrlDataRouter( module, process, p1, p2, p3 );

            JArray rptTemplates;
            bool reportFound = Reports.TryGetValue( fileName, out rptTemplates );
            if ( !reportFound ) return ReportUtil.createExcel( module + "-" + fileName, data ); // TODO as below

            string templatesNotFound = validateReport( rptTemplates );
            if ( templatesNotFound.Length > 0 ) return WebUtil.RawHtmlExceptionResponse( "Templates NOT Found :: " + templatesNotFound );

            return ReportUtil.ZipReport( fileName, module, Templates, rptTemplates, data ); // TODO include module in reportName

        }


        [Route( "debug/phonetic/{ascii}" ), HttpGet]
        public HttpResponseMessage DebugtoPhonetic( string ascii )
        {
            return WebUtil.RawHtmlResponse( MvUtil.toPhoneticMv( ascii ) );
        }

        private static HttpResponseMessage PaydeskRedirectRelative( HttpRequestMessage Request, string RelativePath )
        {
            string uri = Request.RequestUri.AbsoluteUri;
            if ( !uri.EndsWith( "/" ) ) uri += "/";
            HttpResponseMessage response = Request.CreateResponse( System.Net.HttpStatusCode.Moved );
            response.Headers.Location = new Uri( uri + RelativePath );
            return response;
        }

        private static JArray validateTemplate( string rptName )
        {
            // very report settings have been loaded
            JArray rptTemplates;
            if ( !Reports.TryGetValue( rptName, out rptTemplates ) ) throw new ArgumentException( "Report [ " + rptName + " ] NOT Found ! " );

            // verify report related templates have been loaded
            string templatesNotFound = "";
            foreach ( JObject template in rptTemplates )
            {
                if ( template.GetValue( "templateType" ).ToObject<string>() == "csv" ) continue;
                string templateName = template.GetValue( "templateName" ).ToObject<string>();
                if ( !Templates.ContainsKey( templateName ) ) templatesNotFound += templateName + ", ";

            }
            if ( templatesNotFound.Length > 0 ) throw new ArgumentException( "For [ " + rptName + " ] ; templates [ " + templatesNotFound + "] NOT Found ! " );

            return rptTemplates;
        }

        private static string validateReport( JArray rptTemplates )
        {
            // verify report related templates have been loaded
            string templatesNotFound = "";
            foreach ( JObject template in rptTemplates )
            {
                if ( template.GetValue( "templateType" ).ToObject<string>() == "csv" ) continue;
                string templateName = template.GetValue( "templateName" ).ToObject<string>();
                if ( !Templates.ContainsKey( templateName ) ) templatesNotFound += templateName + ", ";

            }
            return templatesNotFound;
        }


        private static string weeksAgo( int p )
        {
            return p + "WeeksAgoOn" + DateTime.Today.AddDays( -(int) DateTime.Today.DayOfWeek - p * 7 ).ToString( "yyyyMMdd" );
        }

        private static string monthsAgo( int p )
        {
            return p + "MonthsAgoOn" + DateTime.Today.AddDays( -DateTime.Today.Day + 1 ).AddMonths( -p ).ToString( "yyyyMMdd" );
        }


        private static HttpResponseMessage ExcelReport( JObject rptData, string rptName, params string[] tables )
        {

            int count = 0;
            foreach ( string table in tables ) count += rptData.GetValue( table ).ToObject<JArray>().Count;

            if ( count == 0 ) return WebUtil.RawHtmlExceptionResponse( "There are NO members that qualify ! " );

            return ReportUtil.createExcel( rptName, rptData );

        }


    }

}