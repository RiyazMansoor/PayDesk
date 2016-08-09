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
                    string templateType, templateName = null;
                    byte[] templateFile = null;
                    foreach( JObject template in rptTemplates )
                    {
                        templateType = template.GetValue( "templateType" ).ToObject<string>();
                        // CVS files are dynamically created - NO templates
                       if ( templateType != "csv" )
                        {
                            templateName = template.GetValue( "templateName" ).ToObject<string>();
                            templates.TryGetValue( templateName, out templateFile );
                        }
                        foreach ( JObject entity in rptData.GetValue( "table" + template.GetValue( "separatorDbTable" ).ToObject<int>() ).ToObject<JArray>() )
                        {
                            Tuple<string, byte[]> file = null;
                            string rptSeparator = entity.GetValue( REPORTSEPARATOR ).ToObject<string>();
                            switch ( templateType)
                            {
                                case "docx": file = createDocReport( templateName, templateFile, rptData, template, rptSeparator ); break;
                                case "xlsx": file = createXlxReport( templateName, templateFile, rptData, template, rptSeparator ); break;
                                case "csv" : file = createCsvReport(                             rptData, template, rptSeparator ); break;
                                default    : break;
                            }
                            if ( file == null ) continue;
                            using ( Stream zip_entry_stream = zarchive.CreateEntry( file.Item1 ).Open() )
                            {
                                zip_entry_stream.Write( file.Item2, 0, file.Item2.Length );
                            }
                        }
                    }
                    Tuple<string, byte[]> excel = createExcelFile( rptName, rptData );
                    using ( Stream zip_entry_stream = zarchive.CreateEntry( excel.Item1 ).Open() )
                    {
                        zip_entry_stream.Write( excel.Item2, 0, excel.Item2.Length );
                    }
                }
                return WebUtil.DownloadResponse( zstream.ToArray(), zipName );
            }
        }


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
                                    b.InsertAfterSelf( new W.Run( new W.Text( value ) ) );
                                }
                                foreach ( HeaderPart h in doc.MainDocumentPart.HeaderParts )
                                {
                                    foreach ( W.BookmarkStart b in h.RootElement.Descendants<W.BookmarkStart>() )
                                    {
                                        if ( bookmark_name != b.Name ) continue;
                                        if ( bookmark_name == REFNUMBER ) value = rptSeparator.ToUpper() + "/" + stamp;
                                        Debug.WriteLine( "RefNumber: " + value );
                                        b.InsertAfterSelf( new W.Run( new W.Text( value ) ) );
                                    }
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
                                        entryFilename = value.Replace( "yyyymmdd", stamp.Substring( 0, 8 ) ) ;
                                        continue;
                                    }
                                    W.Paragraph para = doc_table.Elements<W.TableRow>().ElementAt( row ).Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First();
                                    if ( para.Elements<W.Run>().Count() == 0 ) para.Append( new W.Run( new W.Text( value ) ) );
                                    if ( para.Elements<W.Run>().Count() != 0 ) para.Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
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
                                //new_row.Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First().Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
                                W.Paragraph para = new_row.Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First();
                                if ( para.Elements<W.Run>().Count() == 0 ) para.Append( new W.Run( new W.Text( value ) ) );
                                if ( para.Elements<W.Run>().Count() != 0 ) para.Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
                                // special formatting - if NO owned amount then row is grayed else amount is in red
                                if ( format == "RedRufiya" )
                                {
                                    if ( value == "00.00" )
                                        foreach ( W.RunProperties rp in new_row.Descendants<W.RunProperties>().ToList() ) rp.Color = new W.Color() { Val = "808080" };
                                    else
                                        para.Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
                                }
                                // special formatting - if NO owned amount then row is grayed else amount is in red
                                if ( format == "CourtRufiya" && value.Substring(0,1) == "-" )
                                {
                                    para.Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
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
                                    //doc_table_last_row.Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First().Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
                                    W.Paragraph para = doc_table_last_row.Elements<W.TableCell>().ElementAt( col ).Elements<W.Paragraph>().First();
                                    if ( para.Elements<W.Run>().Count() == 0 ) para.Append( new W.Run( new W.Text( value ) ) );
                                    if ( para.Elements<W.Run>().Count() != 0 ) para.Elements<W.Run>().First().Elements<W.Text>().First().Text = value;
                                    // Special formatting :: RedRufiya implies value color should be turned RED if NOT zero
                                    if ( format == "RedRufiya" && value != "00.00" )
                                    {
                                        //doc_table_last_row.Elements<W.TableCell>().ElementAt( col ).Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
                                        para.Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
                                    }
                                    // special formatting - 
                                    if ( format == "CourtRufiya" && value.Substring( 0, 1 ) == "-" )
                                    {
                                        //doc_table_last_row.Elements<W.TableCell>().ElementAt( col ).Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
                                        para.Descendants<W.RunProperties>().First().Color = new W.Color() { Val = "FF0000" };
                                    }
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
                using ( SpreadsheetDocument xmlx = SpreadsheetDocument.Open( dstream, true ) )
                {

                    // change the type of document from template to standard document, set properties
                    xmlx.ChangeDocumentType( SpreadsheetDocumentType.Workbook );
                    xmlx.PackageProperties.Title = entryFilename;

                    JToken spotsSettingsToken = rptSettings.GetValue( "spot_settings" );
                    if ( !( spotsSettingsToken == null || spotsSettingsToken.Type == JTokenType.Null ) )
                    {
                        JArray spotsTableArray = spotsSettingsToken.ToObject<JArray>();
                        foreach ( JObject spotsSettings in spotsTableArray )
                        {
                            JArray spotsDbTable = rptData.GetValue( "table" + spotsSettings.GetValue( "db_table" ).ToObject<int>() ).ToObject<JArray>();
                            JArray spotsArray = spotsSettings.GetValue( "spots" ).ToObject<JArray>();
                            // xlx table being modified
                            // TODO more parsing if more than just filename;
                            // iterate over all table cells to be filled => after first match, breaks
                            foreach ( JObject table_row in spotsDbTable )
                            {
                                if ( rptSeparator != table_row.GetValue( REPORTSEPARATOR ).ToObject<string>() ) continue;
                                // match found - populate report
                                foreach ( JObject spot in spotsArray )
                                {
                                    // bookmark props
                                    string dbTableFieldName = spot.GetValue( "db_table_field_name" ).ToObject<string>();
                                    string fieldFormat = spot.GetValue( "format" ).ToObject<string>();
                                    // TODO more parsing if more than just filename;
                                    string value = FormatValue( table_row.GetValue( dbTableFieldName ), fieldFormat ) ;
                                    // TODO: only filename spot is handled as yet
                                    if ( dbTableFieldName == "XlsxFilename" )
                                    {
                                        entryFilename = value.Replace( "yyyymmdd", stamp.Substring( 0, 8 ) ) ;
                                        continue;
                                    }
                                }
                                break; // if one match found - break out
                            }
                        }
                    }

                    // multi row table - t parameter
                    JToken gridSettingsToken = rptSettings.GetValue( "grid_settings" );
                    if ( !( gridSettingsToken == null || gridSettingsToken.Type == JTokenType.Null ) )
                    {
                        foreach ( JObject grids_settings in gridSettingsToken.ToObject<JArray>() ) {
                            JArray grids_table = rptData.GetValue( "table" + grids_settings.GetValue( "db_table" ).ToObject<int>() ).ToObject<JArray>();
                            uint startRow = grids_settings.GetValue( "grid_start_row" ).ToObject<uint>();
                            JArray grids = grids_settings.GetValue( "grid_columns" ).ToObject<JArray>();
                            string gridSheetName = grids_settings.GetValue( "xlxSheetName" ).ToObject<string>();
                            X.Sheet sheet = xmlx.WorkbookPart.Workbook.Descendants<X.Sheet>().FirstOrDefault( s => s.Name == gridSheetName );
                            if ( sheet == null ) throw new ArgumentException( "sheet [" + gridSheetName + "] not found" );
                            sheet.Name = gridSheetName.Replace( "yyyymmdd", stamp.Substring( 0, 8 ) );
                            WorksheetPart worksheetPart = (WorksheetPart) xmlx.WorkbookPart.GetPartById( sheet.Id );
                            X.Worksheet worksheet = worksheetPart.Worksheet;
                            X.SheetData sheetdata = worksheet.GetFirstChild<X.SheetData>();
                            // iterate over all table rows to be filled
                            sheetPopulate( rptSeparator, sheetdata, grids_table, startRow, grids );
                            worksheet.Save();
                        }
                    }

                    xmlx.WorkbookPart.Workbook.Save();

                }
                // finally - the document/report is stored as bytes
                return new Tuple<string, byte[]>( entryFilename, dstream.ToArray() );
            }
        }

        private static Tuple<string, byte[]> createCsvReport( JObject rptData, JObject dbTableSettings, string rptSeparator )
        {
            string stamp = DateTime.Now.ToString( "yyyyMMddHHmmss" );
            string entryFilename = "DefaultName-" + rptSeparator + "-" + stamp + ".csv";
            List<string> row = new List<string>();

            // start processing zip file entryreport generation
            using ( MemoryStream cstream = new MemoryStream() )
            {
                using ( StreamWriter csvWriter = new StreamWriter( cstream, System.Text.Encoding.UTF8 ) )
                {

                    foreach ( JObject dbTable in dbTableSettings.GetValue( "dbTables" ).ToObject<JArray>() )
                    {

                        entryFilename = dbTable.GetValue( "csvFileName" ).ToObject<string>().Replace( "yyyymmdd", stamp.Substring( 0, 8 ) ).Replace( REPORTSEPARATOR, rptSeparator );

                        row.Clear();
                        foreach ( JObject column in dbTable.GetValue( "dbTableColumns" ).ToObject<JArray>() )
                        {
                            row.Add( column.GetValue( "columnName" ).ToObject<string>() );
                        }
                        csvWriter.WriteLine( string.Join( ",", row.ToArray() ) );

                        JArray dataTable = rptData.GetValue( "table" + dbTable.GetValue( "dbTableIndex" ).ToObject<int>() ).ToObject<JArray>();
                        foreach ( JObject dataRow in dataTable )
                        {
                            JToken separatorToken = dataRow.GetValue( REPORTSEPARATOR ).ToObject<string>();
                            if ( separatorToken.Type != JTokenType.Null && rptSeparator != separatorToken.ToObject<string>() ) continue;
                            row.Clear();
                            foreach ( JObject column in dbTable.GetValue( "dbTableColumns" ).ToObject<JArray>() )
                            {
                                string columnName = column.GetValue( "columnName" ).ToObject<string>();
                                string columnTransform = column.GetValue( "columnTransform" ).ToObject<string>();
                                JToken columnDataToken = dataRow.GetValue( columnName );
                                row.Add( columnDataToken.Type == JTokenType.Null ? "" : FormatValue( columnDataToken, columnTransform ) );
                            }
                            csvWriter.WriteLine( string.Join( ",", row.ToArray() ) );
                        }

                    }
                }
                // finally - the document/report is returned
                return new Tuple<string, byte[]>( entryFilename, cstream.ToArray() );
            }
        }

        public static HttpResponseMessage createExcel( string rptName, JObject rptData )
        {
            Tuple<string, byte[]> excel = createExcelFile( rptName, rptData ) ;
            return WebUtil.DownloadResponse( excel.Item2, excel.Item1 );

        }

        private static Tuple<string, byte[]> createExcelFile( string rptName, JObject rptData )
        {
            string stamp = DateTime.Now.ToString( "yyyyMMddHHmmss" );
            string excelFilename = rptName + "-" + stamp + ".xlsx";
            // start processing zip file entryreport generation
            using ( MemoryStream xstream = new MemoryStream() )
            {
                using ( SpreadsheetDocument xmlx = SpreadsheetDocument.Create( xstream, SpreadsheetDocumentType.Workbook ) )
                {

                    xmlx.PackageProperties.Title = excelFilename;

                    xmlx.AddWorkbookPart();
                    xmlx.WorkbookPart.Workbook = new X.Workbook();  
                    xmlx.WorkbookPart.AddNewPart<WorksheetPart>();

                    X.Sheets sheets = xmlx.WorkbookPart.Workbook.AppendChild( new X.Sheets() );

                    // parsing tables => table[n]
                    foreach ( KeyValuePair<string, JToken> KeyValue in rptData)
                    {

                        //if ( KeyValue.Key == "table12" ) continue; // debug
                        X.SheetData sheetData = new X.SheetData();
                        JArray tblArray = KeyValue.Value.ToObject<JArray>();

                        X.Row row = sheetRow( sheetData, 1 );

                        // header row
                        uint colIndex = 0;
                        foreach ( JObject tblRow in tblArray )
                        {
                            colIndex = 0 ;
                            foreach ( KeyValuePair<string, JToken> tblRowCell in tblRow )
                            {
                                colIndex++;
                                X.Cell cell = sheetRowCell( row, colIndex, JTokenType.String );
                                cell.CellValue = new X.CellValue( tblRowCell.Key );
                            }
                            break; // only need one row for header
                        }

                        // data rows
                        uint rowIndex = 2;
                        foreach( JObject tblRow in tblArray )
                        {
                            colIndex = 0;
                            row = sheetRow( sheetData, rowIndex );
                            foreach ( KeyValuePair<string, JToken> tblRowCell in tblRow )
                            {
                                colIndex++;
                                X.Cell cell = sheetRowCell( row, colIndex, tblRowCell.Value.Type );
                                cell.CellValue = new X.CellValue( tblRowCell.Value.ToObject<string>() );
                            }
                            rowIndex++;
                        }

                        // save data to sheets
                        WorksheetPart workSheetPart = xmlx.WorkbookPart.AddNewPart<WorksheetPart>();
                        X.Sheet sheet = new X.Sheet()
                        {
                            Id = xmlx.WorkbookPart.GetIdOfPart( workSheetPart ),
                            SheetId = (uint) ( sheets.Count() + 1 ),
                            Name = KeyValue.Key
                        };
                        sheets.Append( sheet );
                        //
                        X.Worksheet workSheet = new X.Worksheet();
                        workSheet.AppendChild( sheetData );
                        workSheetPart.Worksheet = workSheet;
                        workSheet.Save();
                    }

                    xmlx.WorkbookPart.Workbook.Save();

                }
                // finally - the document/report is stored as bytes
                return new Tuple<string, byte[]>( excelFilename, xstream.ToArray() );
            }

        }

        private static void sheetPopulate( string rptSeparator, X.SheetData sheetData, JArray tableRows, uint startRow, JArray columnSettings )
        {
            uint rowIndex = startRow, colIndex ;
            foreach ( JObject tableRow in tableRows )
            {
                // table must have a REPORTSEPARATOR <string> field ; a null means not filtered
                JToken separatorToken = tableRow.GetValue( REPORTSEPARATOR ).ToObject<string>();
                if ( separatorToken.Type != JTokenType.Null && rptSeparator != separatorToken.ToObject<string>() ) continue;
                // find/create row 
                X.Row row = sheetRow( sheetData, rowIndex );
                colIndex = 0;
                foreach ( JObject columnSetting in columnSettings )
                {
                    colIndex++;
                    // fields
                    string fldName = columnSetting.GetValue( "db_table_field_name" ).ToObject<string>();
                    string fldFormat = columnSetting.GetValue( "format" ).ToObject<string>();
                    JToken jtValue = tableRow.GetValue( fldName );
                    // no need to insert if value is null
                    // if ( jtValue.Type == JTokenType.Null ) continue; 
                    string value = ( jtValue.Type == JTokenType.Null ? null : FormatValue( jtValue, fldFormat ) ) ;
                    //Debug.WriteLine( "sheet :: " + gridSheetName  + " :: db_field :: " + db_table_field_name + " :: value :: " + value + " :: cell :: " + ((char) ( 64 + colIndex ) + "" + rowIndex) );
                    // find cell 
                    X.Cell cell = sheetRowCell( row, colIndex, jtValue.Type );
                    cell.CellValue = new X.CellValue( value );
                }
                rowIndex++;
            }
        }

        private static X.Row sheetRow( X.SheetData sheetData, uint rowIndex )
        {
            // find/create row 
            IEnumerable<X.Row> rows = sheetData.Elements<X.Row>().Where( r => r.RowIndex == rowIndex );
            // if row found, return it
            if ( rows.Count() != 0 ) return rows.First();
            // if not found, create row, attach it to sheet and return it
            X.Row row = new X.Row() { RowIndex = rowIndex };
            sheetData.Append( row );
            return row;
        }

        private static X.Cell sheetRowCell( X.Row row, uint colIndex, JTokenType jtokentype )
        {
            string cellRef = sheetColumnIntToName( colIndex ) + row.RowIndex.Value;
            X.Cell cell = null;
            foreach ( X.Cell acell in row.Elements<X.Cell>() )
            {
                // this checking is done because the cell MUST in the correct order within row
                uint acellColIndex = sheetColumnRefToUint( acell.CellReference.Value );
                if ( acellColIndex < colIndex ) continue;
                if ( acellColIndex == colIndex ) { cell = acell; break; } // cell found
                if ( acellColIndex > colIndex ) { cell = new X.Cell() { CellReference = cellRef }; row.InsertBefore( cell, acell ); break; } 
            }
            if ( cell == null ) { cell = new X.Cell() { CellReference = cellRef }; row.Append( cell ); } // last -> create and append
            // setting cell data type
            //cell.DataType = X.CellValues.String; 
            switch ( jtokentype )
            {
                //case JTokenType.Boolean: cell.DataType = X.CellValues.Boolean; break;
                //case JTokenType.Date: cell.DataType =  X.CellValues.Date; break;
                case JTokenType.Integer: cell.DataType = X.CellValues.Number; break;
                case JTokenType.Float: cell.DataType = X.CellValues.Number; break;
                default: cell.DataType = X.CellValues.String; break;
            }
            return cell;
        }

        private static string sheetColumnIntToName( uint colIndex )
        {
            string name = string.Empty;
            int icolIndex = (int) colIndex;
            while ( --icolIndex >= 0 )
            {
                name = (char) ( 'A' + icolIndex % 26 ) - 1 + name;
                icolIndex /= 26;
            }
            return name;
        }

        private static uint sheetColumnRefToUint( string cellref )
        {
            int index = cellref.IndexOfAny( "0123456789".ToCharArray() );
            char[] colLetters = cellref.Substring( 0, index ).ToCharArray().Reverse().ToArray();
            uint colIndex = 0;
            for ( uint i = 0; i < colLetters.Length; i++ )
            {
                colIndex += i * 26 + (uint) ( colLetters[ i ] - 'A' ) + 1;
            }
            return colIndex;
        }

        private static string FormatValue( JToken value, string fldformat )
        {
            fldformat = fldformat.ToLower();
            if ( value.Type == JTokenType.Null ) return "--";
            if ( fldformat == "none" ) return value.ToObject<string>();
            if ( fldformat == "pensionname" ) return PensionName( value.ToObject<int>() );
            if ( fldformat == "pensiontype" ) return MvUtil.toPensionMv( value.ToObject<int>() );
            if ( fldformat == "portfolioname" ) return MvUtil.toPortfolioNameMv( value.ToObject<int>() );
            if ( fldformat == "yearmonth" ) return value.ToObject<DateTimeValue>().Value.ToString( "yyyy-MM" );
            if ( fldformat == "yearmonthdate" ) return value.ToObject<DateTimeValue>().Value.ToString( "yyyy-MM-dd" );
            if ( fldformat == "rufiya" ) return string.Format( System.Globalization.CultureInfo.InvariantCulture, "{0:0,0.00}", value.ToObject<decimal>() );
            if ( fldformat == "redrufiya" ) return string.Format( System.Globalization.CultureInfo.InvariantCulture, "{0:0,0.00}", value.ToObject<decimal>() );
            if ( fldformat == "courtrufiya" ) return string.Format( System.Globalization.CultureInfo.InvariantCulture, "{0:0,0.00}", value.ToObject<decimal>() );
            // a singular dhivehi term used
            if ( fldformat == "jumlamv" ) return MvUtil.toStaticTotalMv( value.ToObject<string>() );
            if ( fldformat == "phoneticmv" ) return MvUtil.toPhoneticMv( value.ToObject<string>() );
             if ( fldformat == "courtresult" ) return MvUtil.toCourtResultMv( value.ToObject<decimal>() );
            if ( fldformat == "livetitlemv" ) return MvUtil.toLiveTitleMv( value.ToObject<string>() );
            if ( fldformat == "deadtitlemv" ) return MvUtil.toDeadTitleMv( value.ToObject<string>() );
            // bank letter addressing
            //if ( fldformat == "addresseename" ) return BankAddresseeName( value.ToObject<string>() );
            //if ( fldformat == "addresseedesignation" ) return BankAddresseeDesignation( value.ToObject<string>() );
            //if ( fldformat == "addresseebank" ) return BankAddresseeBank( value.ToObject<string>() );
            return null;
        }

        private const string REFNUMBER = "RefNumber";
        private const string REPORTSEPARATOR = "ReportSeparator";

        private static string PensionName( int PensionType )
        {
            switch ( PensionType )
            {
                case 10101: return "State Service; 20 Years";
                case 10102: return "State Service; 40 Years";
                case 10103: return "State Service; 60 Years";
                case 11113:
                case 11114:
                case 11115: return "Retirement Package";
                case 11116: return "2 Year Salary Package";
                case 11: return "Basic Pension";
                case 333: return "Senior Citizen Allowance";
                default: return "Unknown: " + PensionType;
            }
        }

        //private static string BankAddresseeName( string bankCode )
        //{
        //    switch ( bankCode )
        //    {
        //        case "BML": return "Mr Andrew Healy,";
        //        case "SBI": return "Mr C Sankar Narayanan,";
        //        case "HBL": return "Mr Muhammad Javed Khan,";
        //        case "MCI": return "Mr Gilles Marie-Jeanne,";
        //        default: return "UNKNOWN " + bankCode;
        //    }
        //}
        //private static string BankAddresseeDesignation( string bankCode )
        //{
        //    switch ( bankCode )
        //    {
        //        case "BML": return "CEO and Managing Director,";
        //        case "SBI": return "Chief Executive Officer,";
        //        case "HBL": return "Country Manager, ";
        //        case "MCI": return "Managing Director,";
        //        default: return "UNKNOWN " + bankCode;
        //    }
        //}
        //private static string BankAddresseeBank( string bankCode )
        //{
        //    switch ( bankCode )
        //    {
        //        case "BML": return "Bank of Maldives";
        //        case "SBI": return "State Bank of India";
        //        case "HBL": return "Habib Bank Limited";
        //        case "MCI": return "Mauritius Commercial Bank,";
        //        default: return "UNKNOWN " + bankCode;
        //    }
        //}
    }

}