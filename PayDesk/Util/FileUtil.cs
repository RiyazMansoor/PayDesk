
namespace PayDesk.Util
{

    public class FileUtil
    {

        public static string toMimeType( string filename )
        {
            string ext = filename.Substring( filename.LastIndexOf( '.' ) );
            switch ( ext )
            {
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                default: throw new System.ArgumentException( "Filename has no extension resolved MimeType :: " + filename );
            }
        }

    }

}
