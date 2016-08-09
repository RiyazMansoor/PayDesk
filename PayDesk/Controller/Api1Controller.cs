using Newtonsoft.Json.Linq;
using PayDesk.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Http;
using System.Web.Http;

namespace PayDesk.Controller
{

    [RoutePrefix("api1")]
    public class Api1Controller : BaseApiController
    {

        private static string TestDbLastSync = "";
        private static JObject JsonErrorMessage = JObject.Parse( "{ \"exceptionMessage\": \"Url Path NOT found !\" } " );

        static Api1Controller()
        {
            initStaticResources();
        }

        private static void initStaticResources()
        {
            // nothing to init yet
        }


        [Route( "dbsynctime" ), HttpGet]
        public JObject DbSyncTime( )
        {
            return StaticDbSyncTime();
        }

        public static JObject StaticDbSyncTime( )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.DbSyncTime";
                return getJsonTables( sqlCmd );
            }
        }


        [Route("refresh"), HttpGet]
        public HttpResponseMessage Api1RefreshHtml()
        {
            initStaticResources();
            return WebUtil.RawHtmlResponse("Refreshed Sources :: Api1Controller");
        }

        [Route("member/identifier/{Identifier}"), HttpGet]
        public JObject MemberOverview(string Identifier )
        {
            return StaticMemberOverviewByIdentifier( Identifier );
        }

        public static JObject StaticMemberOverviewByIdentifier( string Identifier )
        {
            JObject jobject;
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.WorkDesk.MemberOverviewByIdentifier";
                sqlCmd.Parameters.Add( "@Identifier", SqlDbType.VarChar, 12 ).Value = Identifier;
                jobject = getJsonTables( sqlCmd );
            }
            return persistDbSyncTimeAndReturn( jobject );
        }

        [Route("member/memberid/{MemberId}"), HttpGet]
        public JObject MemberOverviewByMemberId(int MemberId)
        {
            return StaticMemberOverviewByMemberId( MemberId );
        }
        public static JObject StaticMemberOverviewByMemberId( int MemberId )
        {
            JObject jobject;
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.WorkDesk.MemberOverviewByMemberId";
                sqlCmd.Parameters.Add( "@MemberId", SqlDbType.Int ).Value = MemberId;
                jobject = getJsonTables( sqlCmd );
            }
            return persistDbSyncTimeAndReturn( jobject );
        }
        private static JObject persistDbSyncTimeAndReturn( JObject jobject )
        {
            if ( jobject[ "table18" ] == null ) return jobject;
            string lastsync = jobject[ "table18" ][ 0 ][ "TestDbLastSync" ].ToObject<System.DateTime>().ToString( "yyyy-MM-dd hh:mm" );
            if ( lastsync != TestDbLastSync )
            {
                TestDbLastSync = lastsync;
                Ui1Controller.UpdateTestDbLastSync( lastsync );
            }
            return jobject;
        }

        [Route( "member/paymentid/{PaymentId}" ), HttpGet]
        public JObject MemberOverviewByPaymentId( int PaymentId )
        {
            return StaticMemberOverviewByPaymentId( PaymentId );
        }
        public static JObject StaticMemberOverviewByPaymentId( int PaymentId )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.WorkDesk.MemberOverviewByPaymentId";
                sqlCmd.Parameters.Add( "@PaymentId", SqlDbType.Int ).Value = PaymentId;
                return getJsonTables( sqlCmd );
            }
        }

        [Route("member/dnrmpaoservice/{NamePart}/{AddressPart}/{IslandPart}"), HttpGet]
        public JObject MemberDnrMpaoService( string NamePart, string AddressPart, string IslandPart )
        {
            return StaticMemberDnrMpaoService( NamePart, AddressPart, IslandPart );
        }
        public static JObject StaticMemberDnrMpaoService( string NamePart, string AddressPart, string IslandPart )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.WorkDesk.MemberMpaoDnrSearch";
                sqlCmd.Parameters.Add( "@NamePart", SqlDbType.VarChar ).Value = NamePart;
                sqlCmd.Parameters.Add( "@AddressPart", SqlDbType.VarChar ).Value = AddressPart;
                sqlCmd.Parameters.Add( "@IslandPart", SqlDbType.VarChar ).Value = IslandPart;
                return getJsonTables( sqlCmd );
            }
        }

        [Route( "member/dnrliveservice/{Identifier}" ), HttpGet]
        public JObject MemberMemberDnrLiveService( string Identifier )
        {
            return StaticMemberMemberDnrLiveService( Identifier );
        }
        public static JObject StaticMemberMemberDnrLiveService( string Identifier )
        {
            return DnrServiceUtil.SyncDnrService( Identifier, CONN_STR );
        }



        // workflow

        [Route( "workflow/" ), HttpGet]
        public JObject WorkflowsActive()
        {
            return StaticWorkflowsActive();
        }

        public static JObject StaticWorkflowsActive()
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.WorkDesk.WorkflowIssues";
                return getJsonTables( sqlCmd );
            }
        }



        // documents

        [Route( "document/anytime/{process}/{p1?}/{p2?}" ), HttpGet]
        public JObject DocumentAnytimeDataRouter( string process, string p1 = null, string p2 = null )
        {
            return StaticUrlDataRouter( "documentanytime", process, p1, p2 );
        }
        // generic
        private static JObject StaticDocumentAnytimeDataRouter( string reportName )
        {
            return StaticGenericSpCall( reportName + "Document" );
        }
        // specific
        private static JObject StaticDocumentAnytimeMemberPayoutStatementDataRouter( string reportName, string Identifier )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@MIdentifier", SqlDbType.VarChar, 10, Identifier ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }
        private static JObject StaticDocumentAnytimeCourtInheritancePaidDataRouter( string reportName, int Days = 30 )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@PeriodDays", SqlDbType.Int, 0, Days ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }
        private static JObject StaticDocumentAnytimeMemberOverpaymentStatusDataRouter( string reportName, string Identifier )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@MIdentifier", SqlDbType.VarChar, 10, Identifier ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }
        private static JObject StaticDocumentAnytimeCourtInheritanceAmountDataRouter( string reportName, string Identifier, string Court )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@MIdentifier", SqlDbType.VarChar, 10, Identifier ) );
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@CIdentifier", SqlDbType.VarChar, 20, Court ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }
        private static JObject StaticDocumentAnytimePayoutRejectionBmlDataRouter( string reportName, string Csv )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@PaymentIdsCsv", SqlDbType.VarChar, 8000, Csv ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }
        private static JObject StaticDocumentAnytimePayoutRejectionNonBmlDataRouter( string reportName, string Csv )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@PaymentIdsCsv", SqlDbType.VarChar, 8000, Csv ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }

        [Route( "document/weekly/{process}/{WeeksAgo?}" ), HttpGet]
        public JObject DocumentWeeklyDataRouter( string process, string WeeksAgo = null )
        {
            return StaticUrlDataRouter( "documentweekly", process, WeeksAgo );
        }
        private static JObject StaticDocumentWeeklyDataRouter( string reportName, int WeeksAgo = -1 )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            if ( WeeksAgo >= 0 ) sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@WeeksAgo", SqlDbType.Int, 0, WeeksAgo ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }

        [Route( "document/monthly/{process}/{MonthsAgo?}" ), HttpGet]
        public JObject DocumentMonthlyDataRouter( string process, string MonthsAgo = null )
        {
            return StaticUrlDataRouter( "documentmonthly", process, MonthsAgo );
        }
        private static JObject StaticDocumentMonthlyDataRouter( string reportName, int MonthsAgo = -1 )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            if ( MonthsAgo >= 0 ) sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@MonthsAgo", SqlDbType.Int, 0, MonthsAgo ) );
            return StaticGenericSpCall( reportName + "Document", sqlParams );
        }


        // checklists

        [Route( "checklist/{process}/" ), HttpGet]
        public JObject ChecklistDataRouter( string process )
        {
            return StaticUrlDataRouter( "checklist", process );
        }
        // generic one for Checklist
        private static JObject StaticChecklistDataRouter( string reportName )
        {
            return StaticGenericSpCall( reportName + "Checklist" );
        }

        // reports

        [Route( "report/{process}/{p1?}/" ), HttpGet]
        public JObject ReportDataRouter( string process, string p1 = null ) {
            return StaticUrlDataRouter( "report", process, p1 );
        }
        // generic one for Report
        private static JObject StaticReportDataRouter( string reportName )
        {
            return StaticGenericSpCall( reportName + "Report" );
        }
        // specialized ones for Report
        private static JObject StaticReportDeathProcessingDataRouter( string reportName, string Csv )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@IdentifiersCsv", SqlDbType.VarChar, 8000, Csv ) );
            return StaticGenericSpCall( reportName + "Report", sqlParams );
        }
        private static JObject StaticReportMemberIdListDataRouter( string reportName, string Csv )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@MemberIdsCsv", SqlDbType.VarChar, 8000, Csv ) );
            return StaticGenericSpCall( reportName + "Report", sqlParams );
        }
        private static JObject StaticReportPaymentIdListDataRouter( string reportName, string Csv )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@PaymentIdsCsv", SqlDbType.VarChar, 8000, Csv ) );
            return StaticGenericSpCall( reportName + "Report", sqlParams );
        }
        private static JObject StaticReportAtollBasicPensionerDataRouter( string reportName, string Atoll )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@DAtoll", SqlDbType.VarChar, 10, Atoll ) );
            return StaticGenericSpCall( reportName + "Report", sqlParams );
        }
        private static JObject StaticReportPayoutFilesDataRouter( string reportName, int PaymentId )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@PaymentId", SqlDbType.Int, 10, PaymentId ) );
            return StaticGenericSpCall( reportName + "Report", sqlParams );
        }
        private static JObject StaticReportKoshaaruFilesDataRouter( string reportName, string WfNumber )
        {
            List<Tuple<string, SqlDbType, int, object>> sqlParams = new List<Tuple<string, SqlDbType, int, object>>();
            sqlParams.Add( new Tuple<string, SqlDbType, int, object>( "@WfNumber", SqlDbType.VarChar, 20, WfNumber ) );
            return StaticGenericSpCall( reportName + "Report", sqlParams );
        }

        private static JObject StaticGenericSpCall( string reportNameWithSuffix, List<Tuple<string, SqlDbType, int, object>> sqlParams = null )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.WorkDesk." + reportNameWithSuffix;
                if ( sqlParams != null )
                {
                    foreach( Tuple<string, SqlDbType, int, object> t in sqlParams )
                    {
                        sqlCmd.Parameters.Add( t.Item1, t.Item2, t.Item3 ).Value = t.Item4;
                    }
                }
                return getJsonTables( sqlCmd );
            }
        }

        public static JObject StaticUrlDataRouter( string module, string process, string p1 = null, string p2 = null, string p3 = null )
        {
            module = module.ToLower();
            process = process.ToLower();

            string reportName;
            bool result = Ui1Controller.PathData.TryGetValue( module + process, out reportName );
            Debug.WriteLine( "API :: " + reportName + "; " + process + "; " + (p1)  );

            if ( !result ) return JsonErrorMessage;

            switch ( module )
            {
                case "report":
                    switch ( process )
                    {
                        case "deathprocessing": return StaticReportDeathProcessingDataRouter( reportName, p1 );
                        case "memberidlist": return StaticReportMemberIdListDataRouter( reportName, p1 );
                        case "paymentidlist": return StaticReportPaymentIdListDataRouter( reportName, p1 );
                        case "atollbasicpensioner": return StaticReportAtollBasicPensionerDataRouter( reportName, p1 );
                        case "payoutfiles": return StaticReportPayoutFilesDataRouter( reportName, int.Parse( p1 ) );
                        case "koshaarufiles": return StaticReportKoshaaruFilesDataRouter( reportName, p1 );
                        default: return StaticReportDataRouter( reportName ); 
                    }
                case "checklist":
                    return StaticChecklistDataRouter( reportName );
                case "documentmonthly":
                    return ( p1 == null ? StaticDocumentMonthlyDataRouter( reportName ) : StaticDocumentMonthlyDataRouter( reportName, int.Parse( p1 ) ) );
                case "documentweekly":
                    return ( p1 == null ? StaticDocumentWeeklyDataRouter( reportName ) : StaticDocumentWeeklyDataRouter( reportName, int.Parse( p1 ) ) );
                case "documentanytime":
                    switch ( process )
                    {
                        case "memberpayoutstatement": return StaticDocumentAnytimeMemberPayoutStatementDataRouter( reportName, p1 );
                        case "memberoverpaymentstatus": return StaticDocumentAnytimeMemberOverpaymentStatusDataRouter( reportName, p1 );
                        case "courtinheritanceamount": return StaticDocumentAnytimeCourtInheritanceAmountDataRouter( reportName, p1, p2 );
                        case "courtinheritancepaid": return ( p1 == null ? StaticDocumentAnytimeCourtInheritancePaidDataRouter( reportName ) : StaticDocumentAnytimeCourtInheritancePaidDataRouter( reportName, int.Parse( p1 ) ) );
                        case "payoutrejectionbml": return StaticDocumentAnytimePayoutRejectionBmlDataRouter( reportName, p1 );
                        case "payoutrejectionnonbml": return StaticDocumentAnytimePayoutRejectionNonBmlDataRouter( reportName, p1 );
                        default: return StaticDocumentAnytimeDataRouter( reportName );
                    }
                default: return JsonErrorMessage;
            }
        }


    }

}
