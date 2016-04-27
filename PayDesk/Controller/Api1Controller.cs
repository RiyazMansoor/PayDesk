using Newtonsoft.Json.Linq;
using PayDesk.Util;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Web.Http;

namespace PayDesk.Controller
{

    [RoutePrefix("api1")]
    public class Api1Controller : BaseApiController
    {

        static Api1Controller()
        {
            initStaticResources();
        }

        private static void initStaticResources()
        {
            // nothing to init yet
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
            return StaticMemberOverview( Identifier );
        }

        public static JObject StaticMemberOverview( string Identifier )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.MemberOverview";
                sqlCmd.Parameters.Add( "@Identifier", SqlDbType.VarChar, 12 ).Value = Identifier;
                return getJsonTables( sqlCmd );
            }
        }

        [Route("member/memberid/{MemberId}"), HttpGet]
        public JObject MemberOverviewById(int MemberId)
        {
            return StaticMemberOverviewById( MemberId );
        }

        public static JObject StaticMemberOverviewById( int MemberId )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.MemberOverviewById";
                sqlCmd.Parameters.Add( "@MemberId", SqlDbType.Int ).Value = MemberId;
                return getJsonTables( sqlCmd );
            }
        }

        [Route("member/dnr/{NamePart}/{AddressPart}/{IslandPart}"), HttpGet]
        public JObject MemberDnrSearch( string NamePart, string AddressPart, string IslandPart )
        {
            return StaticMemberDnrSearch( NamePart, AddressPart, IslandPart );
        }

        public static JObject StaticMemberDnrSearch( string NamePart, string AddressPart, string IslandPart )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.MemberDnrSearch";
                sqlCmd.Parameters.Add( "@NamePart", SqlDbType.VarChar ).Value = NamePart;
                sqlCmd.Parameters.Add( "@AddressPart", SqlDbType.VarChar ).Value = AddressPart;
                sqlCmd.Parameters.Add( "@IslandPart", SqlDbType.VarChar ).Value = IslandPart;
                return getJsonTables( sqlCmd );
            }
        }


        // reports

        [Route( "report/memberoverpaymentstatus/{Identifier}" ), HttpGet]
        public JObject MemberOverpaymentStatusReport( string Identifier )
        {
            return StaticMemberOverpaymentStatusReport( Identifier );
        }

        public static JObject StaticMemberOverpaymentStatusReport( string Identifier )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.MemberOverpaymentStatusReport";
                sqlCmd.Parameters.Add( "@MIdentifier", SqlDbType.VarChar, 12 ).Value = Identifier;
                return getJsonTables( sqlCmd );
            }
        }

        [Route( "report/bankoverpaymentrecovery/{WeeksAgo}" ), HttpGet]
        public JObject BankOverpaymentRecoveryReport( int WeeksAgo )
        {
            return StaticBankOverpaymentRecoveryReport( WeeksAgo );
        }

        public static JObject StaticBankOverpaymentRecoveryReport( int WeeksAgo = 1 )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.BankOverpaymentRecoveryReport";
                sqlCmd.Parameters.Add( "@WeeksAgo", SqlDbType.Int ).Value = WeeksAgo;
                return getJsonTables( sqlCmd );
            }
        }

        [Route( "report/retirementnotifications/{MonthsAgo}" ), HttpGet]
        public JObject RetirementNotificationReport( int MonthsAgo )
        {
            return StaticRetirementNotificationReport( MonthsAgo );
        }

        public static JObject StaticRetirementNotificationReport( int MonthsAgo = 1 )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.RetirementNotificationReport";
                sqlCmd.Parameters.Add( "@MonthsAgo", SqlDbType.Int ).Value = MonthsAgo;
                return getJsonTables( sqlCmd );
            }
        }

        [Route( "report/bankverification" ), HttpGet]
        public JObject BankVerificationReport( )
        {
            return StaticBankVerificationReport();
        }

        public static JObject StaticBankVerificationReport( )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.BankVerificationReport";
                //sqlCmd.Parameters.Add( "@MonthsAgo", SqlDbType.Int ).Value = MonthsAgo;
                return getJsonTables( sqlCmd );
            }
        }

        [Route( "report/statepensiontopup" ), HttpGet]
        public JObject StatePensionTopUpReport( )
        {
            return StaticStatePensionTopUpReport();
        }

        public static JObject StaticStatePensionTopUpReport( )
        {
            using ( SqlCommand sqlCmd = new SqlCommand() )
            {
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.StatePensionTopUpReport";
                //sqlCmd.Parameters.Add( "@MonthsAgo", SqlDbType.Int ).Value = MonthsAgo;
                return getJsonTables( sqlCmd );
            }
        }


    }

}
