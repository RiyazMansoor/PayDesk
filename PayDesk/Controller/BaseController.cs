using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Web.Http;

namespace PayDesk.Controller
{
    public class BaseApiController : ApiController
    {


        protected const string CONN_STR = "Data Source=192.168.2.213;Initial Catalog=MpaoApi;Persist Security Info=True;User ID=opsdesk;Password=opsdesk";

        protected static JObject getJsonTables( SqlCommand sqlCmd )
        {

            JObject jtables = new JObject();
            JArray jrows;
            JObject jrow;

            int tableNumber = 1;

            using ( SqlConnection sqlCon = new SqlConnection( CONN_STR ) )
            {

                sqlCon.Open();
                sqlCmd.Connection = sqlCon;

                try
                {
                    using ( SqlDataReader sqlDataReader = sqlCmd.ExecuteReader() )
                    {
                        do
                        {
                            jrows = new JArray();
                            while ( sqlDataReader.HasRows && sqlDataReader.Read() )
                            {
                                jrow = new JObject();
                                for ( int i = 0 ; i < sqlDataReader.FieldCount ; i++ )
                                {
                                    jrow.Add( sqlDataReader.GetName( i ), JToken.FromObject( sqlDataReader.GetValue( i ) ) );
                                }
                                jrows.Add( jrow );
                            }
                            // placed here to ensure empty tables are listed { table1:[], ... }
                            jtables.Add( "table" + tableNumber, jrows );
                            tableNumber++;
                            //System.Diagnostics.Debug.WriteLine("tableNumber:"+tableNumber);
                        } while ( sqlDataReader.NextResult() );
                    }
                }
                catch ( SqlException e )
                {
                    jtables.Add( "exceptionMessage", JToken.FromObject( e.Message ) );
                    return jtables;
                }

            }

            return jtables;

        }

    }

}