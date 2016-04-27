using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Web.Http;
using System.Web.Hosting;
using System.Web.Routing;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using PayDesk.Util;
using System;
using Newtonsoft.Json;

namespace PayDesk.Controller
{
    public class Ui1Controller : ApiController
    {

        protected static string AppPath = ""; //"/paydesk" ;

        private static string htmlPaymentPortal, htmlMemberOverview, htmlMemberDnrSearch ;

        private static char[] TemplateFilenameDelimiters = new char[] { '-' };
        private static Dictionary<string, JArray> Reports = new Dictionary<string, JArray>();
        private static Dictionary<string, byte[]> Templates = new Dictionary<string, byte[]>();


        static Ui1Controller()
        {
            initStaticResources();
        }

        private static void initStaticResources()
        {

            // load dotx template files. filename format[ key-Vyyyymmdd.dotx ] where the hightest Vyyymmdd is stored under key
            // note: the desc sort ensures that ONLY the newest version is stored under key - duplicate keys are old versions
            string[] rptJsonFiles = Directory.GetFiles( HostingEnvironment.MapPath( "~/reports/templates" ), "*.json" );
            Debug.WriteLine( rptJsonFiles );
            Array.Sort( rptJsonFiles );
            Array.Reverse( rptJsonFiles );
            Reports.Clear();
            foreach ( string rptJsonFile in rptJsonFiles )
            {
                string rptName = Path.GetFileName( rptJsonFile ) ;
                string rptKey = rptName.Substring( 0, rptName.IndexOf( '-' ) ) ;
                if ( Reports.ContainsKey( rptKey ) ) continue ;
                Debug.WriteLine( "ReportKey :: " + rptKey + " :: filename :: " + rptJsonFile );
                Reports.Add( rptKey, JsonConvert.DeserializeObject<JArray>( File.ReadAllText( rptJsonFile )  ) );
            }

            string[] templateFiles = Directory.GetFiles( HostingEnvironment.MapPath( "~/reports/templates" ), "*.??tx" );
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

            // load html sources to be served as static resources
            string KeyAppPath = "$$AppPath";
            htmlPaymentPortal = File.ReadAllText( HostingEnvironment.MapPath( "~/html/PaymentPortal.html" ) ).Replace( KeyAppPath, AppPath );
            htmlMemberOverview = File.ReadAllText( HostingEnvironment.MapPath( "~/html/MemberOverview.html" ) ).Replace( KeyAppPath, AppPath );
            htmlMemberDnrSearch = File.ReadAllText( HostingEnvironment.MapPath( "~/html/MemberDnrSearch.html" ) ).Replace( KeyAppPath, AppPath );

            // fill in select controls
            JArray ui;
            Reports.TryGetValue( "PaydeskUserInterface", out ui );
            List<string> anytimes = new List<string>(), weeklys = new List<string>(), monthlys = new List<string>();
            foreach( JObject option in ui )
            {
                string rptName = option.GetValue( "rptName" ).ToObject<string>();
                string rptUrl = option.GetValue( "rptUrl" ).ToObject<string>();
                switch ( option.GetValue( "rptType" ).ToObject<string>() )
                {
                    case "Anytime": anytimes.Add( rptName + "|" + rptUrl ); break;
                    case "Weekly": weeklys.Add( rptName + "|" + rptUrl ); break;
                    case "Monthly": monthlys.Add( rptName + "|" + rptUrl ); break;
                    default: anytimes.Add( rptName + "|" + rptUrl ); break; // easy check visually
                }
            }
            anytimes.Sort(); 
            weeklys.Sort();
            monthlys.Sort();
            string anytimeO = "", weeklyO = "", monthlyO = "";
            foreach ( string s in anytimes )
            {
                string[] a = s.Split( '|' );
                anytimeO += "<option value='" + a[ 1 ] + "' >" + a[ 0 ] + "</option>";
            }
            foreach ( string s in weeklys )
            {
                string[] a = s.Split( '|' );
                weeklyO += "<option value='" + a[ 1 ] + "' >" + a[ 0 ] + "</option>";
            }
            foreach ( string s in monthlys )
            {
                string[] a = s.Split( '|' );
                monthlyO += "<option value='" + a[ 1 ] + "' >" + a[ 0 ] + "</option>";
            }
            htmlPaymentPortal = htmlPaymentPortal.Replace( "$$rptAnytimeSelectOptions", anytimeO );
            htmlPaymentPortal = htmlPaymentPortal.Replace( "$$rptWeeklySelectOptions", weeklyO );
            htmlPaymentPortal = htmlPaymentPortal.Replace( "$$rptMonthlySelectOptions", monthlyO );

        }

        [HttpGet]
        public HttpResponseMessage PaymentPortalHtml( )
        {
            return WebUtil.RawHtmlResponse( htmlPaymentPortal );
        }

        [Route("ui1/refresh/{*.}"), HttpGet]
        public HttpResponseMessage MemberRefreshHtml()
        {
            initStaticResources();
            return WebUtil.RawHtmlResponse( "Refreshed Sources :: Ui1Controller");
        }

        [Route("member/identifier/{Identifier}"), HttpGet]
        public HttpResponseMessage MemberOverviewByIdentifierHtml( string Identifier )
        {
            return WebUtil.RawHtmlResponse( htmlMemberOverview ) ; 
        }

        [Route("member/memberid/{MemberId}"), HttpGet]
        public HttpResponseMessage MemberOverviewByMemberIdHtml( int MemberId )
        {
            return WebUtil.RawHtmlResponse( htmlMemberOverview ); 
        }

        [Route("member/dnr/{NamePart}/{AddressPart}/{IslandPart}"), HttpGet]
        public HttpResponseMessage MemberDnrSearchHtml( string NamePart, string AddressPart, string IslandPart )
        {
            return WebUtil.RawHtmlResponse( htmlMemberDnrSearch ); 
        }

        [Route( "report/memberoverpaymentstatus/{Identifier}" ), HttpGet]
        public HttpResponseMessage MemberOverpaymentStatusReport( string Identifier )
        {

            const string rptName = "MemberOverpaymentStatus" ;
            JArray rptTemplates;
            try { rptTemplates = validateTemplate( rptName ); } catch ( ArgumentException e ) { return WebUtil.RawHtmlResponse( e.Message ); }

            JObject rptData = Api1Controller.StaticMemberOverpaymentStatusReport( Identifier );

            JArray db_table1 = rptData.GetValue( "table1" ).ToObject<JArray>();
            if ( db_table1.Count == 0 ) return WebUtil.RawHtmlExceptionResponse( "Member [ " + Identifier + " ] NOT Found ! " );
            JArray db_table2 = rptData.GetValue( "table2" ).ToObject<JArray>();
            if ( db_table2.Count == 0 ) return WebUtil.RawHtmlResponse( "Member [ " + Identifier + " ] has NO overpayments ! " );

            return ReportUtil.ZipReport( rptName, Identifier, Templates, rptTemplates, rptData ) ;

        }

        [Route( "report/bankoverpaymentrecovery/{weeksAgo}/{datePeriod}" ), HttpGet]
        public HttpResponseMessage BankOverpaymentRecoveryReport( int weeksAgo, string datePeriod )
        {

            const string rptName = "BankOverpaymentRecovery";
            JArray rptTemplates;
            try { rptTemplates = validateTemplate( rptName ); } catch ( ArgumentException e ) { return WebUtil.RawHtmlResponse( e.Message ); }

            JObject rptData = Api1Controller.StaticBankOverpaymentRecoveryReport( weeksAgo );
            
            JArray db_table = rptData.GetValue( "table1" ).ToObject<JArray>();
            if ( db_table.Count == 0 ) return WebUtil.RawHtmlResponse( "There are NO bank recovery overpayments for this period ! " );

            return ReportUtil.ZipReport( rptName, datePeriod, Templates, rptTemplates, rptData );

        }

        [Route( "report/retirementnotification/{monthsAgo}/{datePeriod}" ), HttpGet]
        public HttpResponseMessage RetirementNotificationReport( int monthsAgo, string datePeriod )
        {

            const string rptName = "RetirementNotification";
            JArray rptTemplates;
            try { rptTemplates = validateTemplate( rptName ); } catch ( ArgumentException e ) { return WebUtil.RawHtmlResponse( e.Message ); }

            JObject rptData = Api1Controller.StaticRetirementNotificationReport( monthsAgo );

            JArray db_table = rptData.GetValue( "table1" ).ToObject<JArray>();
            if ( db_table.Count == 0 ) return WebUtil.RawHtmlResponse( "There are NO retirees for this period ! " );

            return ReportUtil.ZipReport( rptName, datePeriod, Templates, rptTemplates, rptData );

        }

        [Route( "report/bankverification/{*.}" ), HttpGet]
        public HttpResponseMessage BankVerificationReport( )
        {

            const string rptName = "BankVerification";
            JArray rptTemplates;
            try { rptTemplates = validateTemplate( rptName ); } catch ( ArgumentException e ) { return WebUtil.RawHtmlResponse( e.Message ); }

            JObject rptData = Api1Controller.StaticBankVerificationReport();

            return ReportUtil.ZipReport( rptName, "Status31Now", Templates, rptTemplates, rptData );

        }

        [Route( "report/statepensiontopup/{*.}" ), HttpGet]
        public HttpResponseMessage StatePensionTopUpReport( )
        {

            const string rptName = "StatePensionTopUp";
            JArray rptTemplates;
            try { rptTemplates = validateTemplate( rptName ); } catch ( ArgumentException e ) { return WebUtil.RawHtmlResponse( e.Message ); }

            JObject rptData = Api1Controller.StaticStatePensionTopUpReport();

            return ReportUtil.ZipReport( rptName, "AsNow", Templates, rptTemplates, rptData );

        }

        private JArray validateTemplate( string rptName )
        {
            // very report settings have been loaded
            JArray rptTemplates;
            if ( !Reports.TryGetValue( rptName, out rptTemplates ) ) throw new ArgumentException( "Report [ " + rptName + " ] NOT Found ! " );

            // verify report related templates have been loaded
            string templatesNotFound = "";
            foreach ( JObject template in rptTemplates )
            {
                string templateName = template.GetValue( "templateName" ).ToObject<string>();
                if ( !Templates.ContainsKey( templateName ) ) templatesNotFound += templateName + ", ";

            }
            if ( templatesNotFound.Length > 0 ) throw new ArgumentException( "For [ " + rptName + " ] ; templates [ " + templatesNotFound + "] NOT Found ! " );

            return rptTemplates;
        }

    }

}