using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace PayDesk.Util
{
    public class DnrServiceUtil
    {

        public static JObject SyncDnrService( string Identifier, string conn_str )
        {
            using ( var client = new HttpClient() )
            {

                HttpRequestMessage req = new HttpRequestMessage( HttpMethod.Post, "http://10.10.10.10:60022/MPAO_WS.asmx" );
                req.Headers.Add( "SOAPAction", "http://tempuri.org/MPAO_WS/Link_MPAO/SearchByIDNumberAndDOB" );
                req.Content = new StringContent( DNR_SOAP.Replace( "$id", Identifier ), System.Text.Encoding.UTF8, "text/xml" );

                string soapString = client.SendAsync( req ).Result.Content.ReadAsStringAsync().Result;
                int dataStartPos = soapString.IndexOf( "<IDNUMBER>" );

                JObject jo = new JObject();
                if ( dataStartPos < 0 ) return jo;

                string xml = soapString.Substring( dataStartPos, soapString.IndexOf( "</PHOTO>" ) - dataStartPos + 8 );
                //string xml = soapString.Substring( dataStartPos, soapString.IndexOf( "</Results>" ) - dataStartPos );
                Debug.WriteLine( xml );

                //string xtest = "<IDNUMBER>A033234</IDNUMBER><ENGFNAME>Riyaz</ENGFNAME><ENGMNAME /><ENGLNAME>Mansoor</ENGLNAME><DHIVFNAME>cFWyir</DHIVFNAME><DHIVMNAME /><DHIVLNAME>urUBcnwm</DHIVLNAME><GENDER>0</GENDER><DOBIRTH>20 Jan 1975</DOBIRTH><ENGATOLL>K</ENGATOLL><ENGISLAND>Male'</ENGISLAND><ENGDISTRICT>Ma</ENGDISTRICT><ENGHOME>Manas</ENGHOME><DHIVATOLL>k</DHIVATOLL><DHIVISLAND>elWm</DHIVISLAND><DHIVDISTRICT>am</DHIVDISTRICT><DHIVHOME>cswnwm</DHIVHOME>";
                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml( "<r>" + xml + "</r>" );
                XmlNode root = xdoc.SelectSingleNode( "r" );

                foreach ( string key in DNR_PROPS )
                {
                    XmlNode n = root.SelectSingleNode( key );
                    //string text = ( n == null ? "null" : n.InnerText.Replace( "'", "\\'" ) );
                    //string ptext = ( key.Substring( 0, 4 ) == "DHIV" ? MvUtil.toPhoneticMv( text ) : null );
                    //Debug.WriteLine( "DnrService: " + key + " :: " + text );
                    jo.Add( key, JToken.Parse( n == null ? "null" : @"'" + n.InnerText.Replace( "'", "\\'" ) + "'" ) );
                    if ( key.Substring( 0, 4 ) == "DHIV" && n != null ) jo.Add( "MV" + key, MvUtil.toPhoneticMv( n.InnerText ) ); // guarantess non null text
                }
                DbInsertMemberMv( conn_str, jo );
                return jo;

            }
        }

        public static void DbInsertMemberMv( string conn_str, JObject jmember )
        { 
            DateTime parsedDOBIRTH, parsedDODEATH;
            DateTime.TryParseExact( jmember.GetValue( "DOBIRTH" ).ToObject<string>(), "dd MMM yyyy", null, System.Globalization.DateTimeStyles.None, out parsedDOBIRTH );
            DateTime.TryParseExact( jmember.GetValue( "DODEATH" ).ToObject<string>(), "dd MMM yyyy", null, System.Globalization.DateTimeStyles.None, out parsedDODEATH );

            JToken jtENGDISTRICT = jmember.GetValue( "ENGDISTRICT" ), jtDHIVDISTRICT = jmember.GetValue( "DHIVDISTRICT" );
            string parsedENGDISTRICT = ( jtENGDISTRICT.Type == JTokenType.Null ? null : jtENGDISTRICT.ToObject<string>() );
            string parsedDHIVDISTRICT = ( jtDHIVDISTRICT.Type == JTokenType.Null ? null : jtDHIVDISTRICT.ToObject<string>() );

            using ( SqlCommand sqlCmd = new SqlCommand() )
            {

                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandText = "MpaoApi.Report.DnrServiceInsert";

                sqlCmd.Parameters.Add( "@IDNUMBER", SqlDbType.Char ).Value = jmember.GetValue( "IDNUMBER" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@ENGFNAME", SqlDbType.VarChar ).Value = jmember.GetValue( "ENGFNAME" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@ENGMNAME", SqlDbType.VarChar ).Value = jmember.GetValue( "ENGMNAME" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@ENGLNAME", SqlDbType.VarChar ).Value = jmember.GetValue( "ENGLNAME" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@DHIVFNAME", SqlDbType.VarChar ).Value = jmember.GetValue( "DHIVFNAME" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@DHIVMNAME", SqlDbType.VarChar ).Value = jmember.GetValue( "DHIVMNAME" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@DHIVLNAME", SqlDbType.VarChar ).Value = jmember.GetValue( "DHIVLNAME" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@GENDER", SqlDbType.VarChar ).Value = jmember.GetValue( "GENDER" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@DOBIRTH", SqlDbType.Date ).Value = parsedDOBIRTH;
                sqlCmd.Parameters.Add( "@DODEATH", SqlDbType.Date ).Value = parsedDODEATH;
                sqlCmd.Parameters.Add( "@ENGATOLL", SqlDbType.VarChar ).Value = jmember.GetValue( "ENGATOLL" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@ENGISLAND", SqlDbType.VarChar ).Value = jmember.GetValue( "ENGISLAND" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@ENGDISTRICT", SqlDbType.VarChar ).Value = jmember.GetValue( "ENGDISTRICT" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@ENGHOME", SqlDbType.VarChar ).Value = jmember.GetValue( "ENGHOME" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@DHIVATOLL", SqlDbType.VarChar ).Value = jmember.GetValue( "DHIVATOLL" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@DHIVISLAND", SqlDbType.VarChar ).Value = jmember.GetValue( "DHIVISLAND" ).ToObject<string>();
                sqlCmd.Parameters.Add( "@DHIVDISTRICT", SqlDbType.VarChar ).Value = parsedDHIVDISTRICT;
                sqlCmd.Parameters.Add( "@DHIVHOME", SqlDbType.VarChar ).Value = jmember.GetValue( "DHIVHOME" ).ToObject<string>();

                using ( SqlConnection sqlCon = new SqlConnection( conn_str ) )
                {
                    sqlCon.Open();
                    sqlCmd.Connection = sqlCon;
                    sqlCmd.ExecuteNonQuery();
                }

            }

        }

        private static string[] DNR_PROPS = { "IDNUMBER", "ENGFNAME", "ENGMNAME", "ENGLNAME", "DHIVFNAME", "DHIVMNAME", "DHIVLNAME", "GENDER", "DOBIRTH", "DODEATH",
                                              "ENGATOLL", "ENGISLAND", "ENGDISTRICT", "ENGHOME", "DHIVATOLL", "DHIVISLAND", "DHIVDISTRICT", "DHIVHOME", "PHOTO" /*, "SIGNATURE" */ };

        private static string DNR_SOAP = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + 
                                            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" + 
                                            "  <soap:Body>" + 
                                            "    <SearchByIDNumberAndDOB xmlns=\"http://tempuri.org/MPAO_WS/Link_MPAO\">" + 
                                            "      <UserName>MPAO_Shafeez</UserName>" + 
                                            "      <UserPassword>gVG3G$</UserPassword>" + 
                                            "      <IDNumber>$id</IDNumber>" + 
                                            "      <DOB></DOB>" + 
                                            "    </SearchByIDNumberAndDOB>" + 
                                            "  </soap:Body>" + 
                                            "</soap:Envelope>";

    }
}