using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PayDesk.Util
{

    public class WebUtil
    {

        private static MediaTypeHeaderValue headerTextHtml = new MediaTypeHeaderValue( "text/html" );

        public static HttpResponseMessage RawHtmlResponse( string html )
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StringContent( html );
            response.Content.Headers.ContentType = headerTextHtml;
            return response;
        }

        public static HttpResponseMessage RawHtmlExceptionResponse( string exceptionMessage )
        {
            return RawHtmlResponse( "<div style=\"padding:3cm;font-size:x-large;text-align:center;color:red;\" >" + exceptionMessage + "</div>" );
        }


        public static HttpResponseMessage DownloadResponse( byte[] file, string filename )
        {
            HttpResponseMessage response = new HttpResponseMessage( System.Net.HttpStatusCode.OK ) { Content = new ByteArrayContent( file ) } ;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue( System.Web.MimeMapping.GetMimeMapping( Path.GetExtension( filename ) ) ); 
            response.Content.Headers.ContentLength = file.Length;
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue( "attachment" ) { FileName = filename } ;
            return response;
        }

    }

}