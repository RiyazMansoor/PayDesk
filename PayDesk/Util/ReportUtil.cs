using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using X = DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;

namespace PayDesk.Util
{

    public class ReportUtil
    {

        public static HttpResponseMessage ZipReport( string rptName, string rptParam, Dictionary<string, byte[]> templates, JArray rptTemplates, JObject rptData )
        {
            string stamp = DateTime.Now.ToString( "yyyyMMddHHmmss" );
            string zipName = rptName + "-" + rptParam + "-" + stamp + ".zip";
            // start processing zip file report generation
            using ( MemoryStream zstream = new MemoryStream() )
            {
                using ( ZipArchive zarchive = new ZipArchive( zstream, ZipArchiveMode.Create, true ) )
                {
                    foreach( JObject template in rptTemplates )
                    {
                        byte[] templateFile;
                        string templateName = template.GetValue( "templateName" ).ToObject<string>();
                        templates.TryGetValue( templateName, out templateFile );
                        string templateType = template.GetValue( "templateType" ).ToObject<string>();
                        // different template types
                        foreach ( JObject entity in rptData.GetValue( "table" + template.GetValue( "separatorDbTable" ).ToObject<int>() ).ToObject<JArray>() )
                        {
                            Tuple<string, byte[]> file = null ;
                            string rptSeparator = entity.GetValue( REPORTSEPARATOR ).ToObject<string>();
                            if ( templateType == "docx" ) file = createDocReport( templateName, templateFile, rptData, template, rptSeparator ) ;
                            if ( templateType == "xlsx" ) file = createXlxReport( templateName, templateFile, rptData, template, rptSeparator ) ;
                            // TODO other filetypes not handled
                            if ( file == null ) continue;
                            // zip it
                            using ( Stream zip_doc_stream = zarchive.CreateEntry( file.Item1 ).Open() )
                            {
                                zip_doc_stream.Write( file.Item2, 0, file.Item2.Length );
                            }
                        }
                    }
                }
                return WebUtil.DownloadResponse( zstream.ToArray(), zipName );
            }
        }


        /*
                public static Tuple<string, byte[]> createZipReport( string rpt_name, byte[] rpt_template, JObject rpt_data, JObject rpt_settings, string date_period )
                {
                    string stamp = DateTime.Now.ToString( "yyyyMMddHHmmss" );
                    string zip_name = rpt_name + "-" + date_period.Replace(" ", "") + "-" + stamp + ".zip";
                    // start processing zip file report generation
                    using ( MemoryStream zstream = new MemoryStream() )
                    {
                        using ( ZipArchive zarchive = new ZipArchive( zstream, ZipArchiveMode.Create, true ) )
                        {
                            JArray entities = rpt_data.GetValue( "table1" ).ToObject<JArray>();
                            foreach ( JObject entity in entities )
                            {
                                string rpt_entity_identifier = entity.GetValue( REPORTSEPARATOR ).ToObject<string>();
                                Tuple<string, byte[]> tuple_doc = createDocReport( rpt_name, rpt_template, rpt_data, rpt_settings, rpt_entity_identifier) ;
                                // add to zip archive
                                ZipArchiveEntry zip_entry = zarchive.CreateEntry( tuple_doc.Item1 ) ;
                                using ( Stream zip_doc_stream = zip_entry.Open() )
                                {
                                    byte[] doc_bytes = tuple_doc.Item2 ;
                                    zip_doc_stream.Write( doc_bytes, 0, doc_bytes.Length );
                                }
                            }
                        }
                        return new Tuple<string, byte[]>( zip_name, zstream.ToArray() );
                    }
                }
        */

        private static Tuple<string, byte[]> createDocReport( string rptName, byte[] rptTemplate, JObject rptData, JObject rptSettings, string rptSeparator )
        {
            string stamp = DateTime.Now.ToString( "yyyyMMddHHmmss" );
            string entryFilename = rptName + "-" + rptSeparator + "-" + stamp + ".docx";
            // start processing zip file entry report generation
            using ( MemoryStream dstream = new MemoryStream() )
            {
                dstream.Write( rptTemplate, 0, rptTemplate.Length );
                using ( WordprocessingDocument doc = WordprocessingDocument.Open( dstream, true ) )
                {

                    // change the type of document from template to standard document, set properties
                    doc.ChangeDocumentType( WordprocessingDocumentType.Document );
                    doc.PackageProperties.Title = entryFilename;

                    // fill bookmarks {"jtable":1,"onceb":[{"db_table_field_name":"?","format":"?","bmname":"?"},...]}
                    JToken bookmark_settings_token = rptSettings.GetValue( "bookmark_settings" ) ;
                    if ( ! ( bookmark_settings_token == null || bookmark_settings_token.Type == JTokenType.Null ) )
                    {
                        JObject bookmark_settings = bookmark_settings_token.ToObject<JObject>();
                        JArray bookmark_table = rptData.GetValue( "table" + bookmark_settings.GetValue( "db_table" ).ToObject<int>() ).ToObject<JArray>();
                        JArray bookmarks = bookmark_settings.GetValue( "bookmarks" ).ToObject<JArray>();
                        // populate multiple table cells from all matching objects => after first match, breaks
                        foreach ( JObject table_row in bookmark_table )
                        {
                            if ( rptSeparator != table_row.GetValue( REPORTSEPARATOR ).ToObject<string>() ) continue;
                            // match found - populate report
                            foreach ( JObject bookmark in bookmarks )
                            {
                                // bookmark props
                                string db_table_field_name = bookmark.GetValue( "db_table_field_name" ).ToObject<string>();
                                string format = bookmark.GetValue( "format" ).ToObject<string>();
                                string bookmark_name = bookmark.GetValue( "bookmark_name" ).ToObject<string>();
                                string value = FormatValue( table_row.GetValue( db_table_field_name ), format );
                                foreach ( W.BookmarkStart b in doc.MainDocumentPart.Document.Descendants<W.BookmarkStart>() )
                                {
                                    if ( bookmark_name != b.Name ) continue;
                                    b.InsertAfterSelf( new DocumentFormat.OpenXml.Wordprocessing.Run( new W.Text( value ) ) );
                                }
                                foreach ( W.BookmarkStart b in doc.MainDocumentPart.HeaderParts.ElementAt( 0 ).RootElement.Descendants<W.BookmarkStart>() )
                                {
                                    if ( bookmark_name != b.Name ) continue;
                                    if ( bookmark_name == REFNUMBER ) value = rptSeparator + "/" + stamp;
                                    b.InsertAfterSelf( new W.Run( new W.Text( value ) ) );
                                }
                            }
                            break ; // if one match found - break out
                        }

                    }

                    // fill once off table cells 
                    JToken spots_settings_token = rptSettings.GetValue( "spot_settings" );
                    if ( ! ( spots_settings_token == null || spots_settings_token.Type == JTokenType.Null ) )
                    {
                        JArray spotsArray = spots_settings_token.ToObject<JArray>();
                        foreach( JObject spots_settings in spotsArray ) { 
                            JArray spots_table = rptData.GetValue( "table" + spots_settings.GetValue( "db_table" ).ToObject<int>() ).ToObject<JArray>();
                            JArray spots = spots_settings.GetValue( "spots" ).ToObject<JArray>();
                            // doc table being modified
                            W.Table doc_table = doc.MainDocumentPart.Document.Body.Elements<W.Table>().ElementAt( spots_settings.GetValue( "doc_table" ).ToObject<int>() );
                            // iterate over all table cells to be filled => after first match, breaks
                            foreach ( JObject table_row in spots_table )
                            {
                                if ( rptSeparator != table_row.GetValue( REPORTSEPARATOR ).ToObject<string>() ) continue;
                                // match found - populate report
                                foreach ( JObject spot in spots )
                                {
                                    // bookmark props
                                    string db_table_field_name = spot.GetValue( "db_table_field_name" ).ToObject<string>();
                                    string format = spot.GetValue( "format" ).ToObject<string>();
                                    int row = spot.GetValue( "doc_table_row" ).ToObject<int>();
                                    int col = spot.GetValue( "doc_table_col" ).ToObject<int>();
                                    string value = FormatValue( table_row.GetValue( db_table_field_name ), format );
                                    if ( db_table_field_name == "DocxFilename" )
                                    {
                                        entryFilename = value;
                                        continue;
                                    }
                                    doc_table.Elements<W.TableRow>().ElementAt( row ).Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First().Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
                                }
                                break; // if one match found - break out
                            }
                        }
                    }

                    // multi row table filling
                    JToken grid_settings_token = rptSettings.GetValue( "grid_settings" );
                    if ( ! ( grid_settings_token == null || grid_settings_token.Type == JTokenType.Null ) )
                    {
                        JObject grids_settings = grid_settings_token.ToObject<JObject>();
                        JArray grids_table = rptData.GetValue( "table" + grids_settings.GetValue( "db_table" ).ToObject<int>() ).ToObject<JArray>();
                        JArray grids = grids_settings.GetValue( "grid_columns" ).ToObject<JArray>();
                        // doc table being modified
                        W.Table doc_table = doc.MainDocumentPart.Document.Body.Elements<W.Table>().ElementAt( grids_settings.GetValue( "doc_table" ).ToObject<int>() );
                        // this row is used as a template to append more rows
                        W.TableRow doc_table_last_row = doc_table.Elements<W.TableRow>().Last();
                        // iterate over all table rows to be filled
                        foreach ( JObject table_row in grids_table )
                        {
                            if ( rptSeparator != table_row.GetValue( REPORTSEPARATOR ).ToObject<string>() ) continue;
                            // populate new row from object and insert
                            W.TableRow new_row = (W.TableRow) doc_table_last_row.CloneNode( true );
                            foreach ( JObject grid in grids )
                            {
                                // fields
                                string db_table_field_name = grid.GetValue( "db_table_field_name" ).ToObject<string>();
                                string format = grid.GetValue( "format" ).ToObject<string>();
                                int col = grid.GetValue( "doc_table_col" ).ToObject<int>();
                                string value = FormatValue( table_row.GetValue( db_table_field_name ), format );
                                new_row.Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First().Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
                                // special formatting - if NO owned amount then row is grayed else amount is in red
                                if ( format == "RedRufiya" )
                                {
                                    if ( value == "00.00" )
                                        foreach ( W.RunProperties rp in new_row.Descendants< W.RunProperties>().ToList() ) rp.Color = new W.Color() { Val = "808080" };
                                    else
                                        new_row.Elements<W.TableCell>().ElementAt( col ).Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
                                }
                            }
                            // append to table
                            doc_table.InsertBefore( new_row, doc_table_last_row );
                        }
                        // table footer - if none remove the last ( templated above ) row
                        JToken grid_footer_token = grids_settings.GetValue( "footer_settings" );
                        if ( grid_footer_token == null || grid_footer_token.Type == JTokenType.Null ) doc_table_last_row.Remove();
                        else
                        {
                            JArray footer_grids_table = rptData.GetValue( "table" + grid_footer_token.ToObject<JObject>().GetValue( "db_table" ).ToObject<string>() ).ToObject<JArray>();
                            JArray footer_grids = grid_footer_token.ToObject<JObject>().GetValue( "doc_table_row" ).ToObject<JArray>();
                            // iterate over all table rows to be filled => IF more than one, the row will be overwritten by the last table_row
                            foreach ( JObject table_row in footer_grids_table )
                            {
                                if ( rptSeparator != table_row.GetValue( REPORTSEPARATOR ).ToObject<string>() ) continue;
                                // populate last row from object
                                foreach ( JObject footer_grid in footer_grids )
                                {
                                    // fields
                                    string db_table_field_name = footer_grid.GetValue( "db_table_field_name" ).ToObject<string>();
                                    string format = footer_grid.GetValue( "format" ).ToObject<string>();
                                    int col = footer_grid.GetValue( "doc_table_col" ).ToObject<int>();
                                    string value = FormatValue( table_row.GetValue( db_table_field_name ), format );
                                    doc_table_last_row.Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First().Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
                                    // Special formatting :: RedRufiya implies value color should be turned RED if NOT zero
                                    if ( format == "RedRufiya" && value != "00.00" ) doc_table_last_row.Elements<W.TableCell>().ElementAt( col ).Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
                                }
                            }
                            // all text in last row will be bold
                            foreach ( W.RunProperties rp in doc_table_last_row.Descendants<W.RunProperties>().ToList() ) rp.Bold = new W.Bold() ;
                            foreach ( W.TableCellProperties tcp in doc_table_last_row.Descendants<W.TableCellProperties>().ToList() ) tcp.Append( new W.Shading() { Fill = "D9D9D9" } );
                        }
                    }

                }
                // finally - the document/report is stored as bytes
                return new Tuple<string, byte[]>( entryFilename, dstream.ToArray() ) ;
            }
        }

        private static Tuple<string, byte[]> createXlxReport( string rptName, byte[] rptTemplate, JObject rptData, JObject rptSettings, string rptSeparator )
        {
            string stamp = DateTime.Now.ToString( "yyyyMMddHHmmss" );
            string entryFilename = rptName + "-" + rptSeparator + "-" + stamp + ".xlsx";
            // start processing zip file entryreport generation
            using ( MemoryStream dstream = new MemoryStream() )
            {
                dstream.Write( rptTemplate, 0, rptTemplate.Length );
                using ( SpreadsheetDocument xlx = SpreadsheetDocument.Open( dstream, true ) )
                {

                    // change the type of document from template to standard document, set properties
                    xlx.ChangeDocumentType( SpreadsheetDocumentType.Workbook );
                    xlx.PackageProperties.Title = entryFilename;

                    JToken spots_settings_token = rptSettings.GetValue( "spot_settings" );
                    if ( !( spots_settings_token == null || spots_settings_token.Type == JTokenType.Null ) )
                    {
                        JArray spotsArray = spots_settings_token.ToObject<JArray>();
                        foreach ( JObject spots_settings in spotsArray )
                        {
                            JArray spots_table = rptData.GetValue( "table" + spots_settings.GetValue( "db_table" ).ToObject<int>() ).ToObject<JArray>();
                            JArray spots = spots_settings.GetValue( "spots" ).ToObject<JArray>();
                            // xlx table being modified
                            int gridSheetIndex = spots_settings.GetValue( "xlx_sheet_index" ).ToObject<int>();
                            // iterate over all table cells to be filled => after first match, breaks
                            foreach ( JObject table_row in spots_table )
                            {
                                if ( rptSeparator != table_row.GetValue( REPORTSEPARATOR ).ToObject<string>() ) continue;
                                // match found - populate report
                                foreach ( JObject spot in spots )
                                {
                                    // bookmark props
                                    string db_table_field_name = spot.GetValue( "db_table_field_name" ).ToObject<string>();
                                    string format = spot.GetValue( "format" ).ToObject<string>();
                                    int row = spot.GetValue( "cell_row" ).ToObject<int>();
                                    int col = spot.GetValue( "cell_col" ).ToObject<int>();
                                    string value = FormatValue( table_row.GetValue( db_table_field_name ), format ) ;
                                    // TODO: only filename spot is handled as yet
                                    if ( db_table_field_name == "XlsxFilename" )
                                    {
                                        entryFilename = value;
                                        continue;
                                    }
                                }
                                break; // if one match found - break out
                            }
                        }
                    }

                    // multi row table - t parameter
                    JToken grid_settings_token = rptSettings.GetValue( "grid_settings" );
                    if ( !( grid_settings_token == null || grid_settings_token.Type == JTokenType.Null ) )
                    {
                        foreach ( JObject grids_settings in grid_settings_token.ToObject<JArray>() ) {
                            JArray grids_table = rptData.GetValue( "table" + grids_settings.GetValue( "db_table" ).ToObject<int>() ).ToObject<JArray>();
                            int gridSheetIndex = grids_settings.GetValue( "xlx_sheet_index" ).ToObject<int>();
                            int gridStartRow = grids_settings.GetValue( "grid_start_row" ).ToObject<int>();
                            JArray grids = grids_settings.GetValue( "grid_columns" ).ToObject<JArray>();
                            // doc table being modified
                            SharedStringTablePart sharedStringPart = xlx.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
                            //int sharedStringIndex = sharedStringPart.SharedStringTable.Elements<X.SharedStringItem>().Count();
                            X.Worksheet worksheet = xlx.WorkbookPart.WorksheetParts.ElementAt( gridSheetIndex ).Worksheet;
                            X.SheetData sheetdata = worksheet.GetFirstChild<X.SheetData>();
                            // iterate over all table rows to be filled
                            uint rowIndex = 2;
                            foreach ( JObject table_row in grids_table )
                            {
                                string separator = table_row.GetValue( REPORTSEPARATOR ).ToObject<string>();
                                if ( REPORTSEPARATOR != separator && rptSeparator != separator ) continue;
                                // create populate new row from object
                                //int col = 0;
                                X.Row row = new X.Row() { RowIndex = rowIndex++ };
                                foreach ( JObject column in grids )
                                {
                                    // fields
                                    string db_table_field_name = column.GetValue( "db_table_field_name" ).ToObject<string>();
                                    string format = column.GetValue( "format" ).ToObject<string>();
                                    string value = FormatValue( table_row.GetValue( db_table_field_name ), format );
                                    Debug.WriteLine( "db_field :: " + db_table_field_name + " :: value :: " + value + " :: col :: " + CellReference( row.Elements().Count(), row.RowIndex.Value ) );
                                    sharedStringPart.SharedStringTable.AppendChild( new X.SharedStringItem( new X.Text( value ) ) );
                                    sharedStringPart.SharedStringTable.Save();
                                    X.Cell cell = new X.Cell() { CellReference = CellReference( row.Elements().Count(), row.RowIndex.Value ) };
                                    cell.DataType = new EnumValue<X.CellValues>( X.CellValues.SharedString );
                                    cell.CellValue = new X.CellValue( ( sharedStringPart.SharedStringTable.Count() - 1 ).ToString() );
                                    row.Append( cell );
                                }
                                sheetdata.Append( row );
                            }
                            worksheet.Save();
                        }
                    }
                }
                // finally - the document/report is stored as bytes
                return new Tuple<string, byte[]>( entryFilename, dstream.ToArray() );
            }
        }

        private static string FormatValue( JToken value, string fldformat )
        {
            fldformat = fldformat.ToLower();
            if ( value.Type == JTokenType.Null ) return "--";
            if ( fldformat == "none" ) return value.ToObject<string>();
            if ( fldformat == "pensiontype" ) return MvUtil.toPensionMv( value.ToObject<int>() );
            if ( fldformat == "portfolioname" ) return MvUtil.toPortfolioNameMv( value.ToObject<int>() );
            if ( fldformat == "yearmonth" ) return value.ToObject<DateTimeValue>().Value.ToString( "yyyy-MM" );
            if ( fldformat == "yearmonthdate" ) return value.ToObject<DateTimeValue>().Value.ToString( "yyyy-MM-dd" );
            if ( fldformat == "rufiya" ) return string.Format( System.Globalization.CultureInfo.InvariantCulture, "{0:0,0.00}", value.ToObject<decimal>() );
            if ( fldformat == "redrufiya" ) return string.Format( System.Globalization.CultureInfo.InvariantCulture, "{0:0,0.00}", value.ToObject<decimal>() );
            // a singular dhivehi term used
            if ( fldformat == "jumla" ) return MvUtil.toStaticTotalMv( value.ToObject<string>() );
            // mv person titles
            if ( fldformat == "livetitlemv" ) return MvUtil.toLiveTitleMv( value.ToObject<string>() );
            if ( fldformat == "deadtitlemv" ) return MvUtil.toDeadTitleMv( value.ToObject<string>() );
            // bank letter addressing
            if ( fldformat == "addresseename" ) return BankAddresseeName( value.ToObject<string>() );
            if ( fldformat == "addresseedesignation" ) return BankAddresseeDesignation( value.ToObject<string>() );
            if ( fldformat == "addresseebank" ) return BankAddresseeBank( value.ToObject<string>() );
            return null;
        }

        private static string CellReference( int col, uint row )
        {
            return (char)( 65 + col ) + "" + row ;
        }

        private const string REFNUMBER = "RefNumber";
        private const string REPORTSEPARATOR = "ReportSeparator";

        private static string BankAddresseeName( string bankCode )
        {
            switch ( bankCode)
            {
                case "BML": return "Mr Andrew Healy,";
                case "SBI": return "Mr C Sankar Narayanan,";
                case "HBL": return "Mr Muhammad Javed Khan,";
                case "MCI": return "Mr Gilles Marie-Jeanne,";
                default: return "UNKNOWN " + bankCode;
            }
        }
        private static string BankAddresseeDesignation( string bankCode )
        {
            switch ( bankCode )
            {
                case "BML": return "CEO and Managing Director,";
                case "SBI": return "Chief Executive Officer,";
                case "HBL": return "Country Manager, ";
                case "MCI": return "Managing Director,";
                default: return "UNKNOWN " + bankCode;
            }
        }
        private static string BankAddresseeBank( string bankCode )
        {
            switch ( bankCode )
            {
                case "BML": return "Bank of Maldives";
                case "SBI": return "State Bank of India";
                case "HBL": return "Habib Bank Limited";
                case "MCI": return "Mauritius Commercial Bank,";
                default: return "UNKNOWN " + bankCode;
            }
        }
    }

}