﻿Option Strict On
Imports System.Runtime.InteropServices
Imports GrapeCity.Documents.Excel
Public Class LNT0001InvoiceOutputReport
    Private WW_Workbook As New Workbook  '共通
    Private WW_SheetNo As Integer = 0
    Private WW_SheetNoTmp01 As Integer = 0
    Private WW_SheetNoTmp02 As Integer = 0
    Private WW_SheetNoTmp03 As Integer = 0
    Private WW_SheetNoTmp04 As Integer = 0
    Private WW_ArrSheetNo As Integer() = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0}

    ''' <summary>
    ''' 雛形ファイルパス
    ''' </summary>
    Private ExcelTemplatePath As String = ""
    Private UploadRootPath As String = ""
    Private UrlRoot As String = ""
    Private PrintData As DataTable
    Private PrintTankData As DataTable
    Private TaishoYm As String = ""
    Private TaishoYYYY As String = ""
    Private TaishoMM As String = ""
    Private OutputOrgCode As String = ""
    Private OutputFileName As String = ""
    Private calcZissekiNumber As Integer

    ''' <summary>
    ''' コンストラクタ
    ''' </summary>
    ''' <param name="mapId">帳票格納先のMAPID</param>
    ''' <param name="excelFileName">Excelファイル名（フルパスではない)</param>
    ''' <param name="outputFileName">(出力用)Excelファイル名（フルパスではない)</param>
    ''' <param name="printDataClass">帳票データ</param>
    ''' <remarks>テンプレートファイルを読み取りモードとして開く</remarks>
    Public Sub New(mapId As String, orgCode As String, excelFileName As String, outputFileName As String, printDataClass As DataTable, printTankDataClass As DataTable,
                   Optional ByVal taishoYm As String = Nothing,
                   Optional ByVal calcNumber As Integer = 1,
                   Optional ByVal defaultDatakey As String = C_DEFAULT_DATAKEY)
        Try
            Dim CS0050SESSION As New CS0050SESSION
            Me.PrintData = printDataClass
            Me.PrintTankData = printTankDataClass
            Me.TaishoYm = taishoYm
            Me.TaishoYYYY = Date.Parse(taishoYm + "/" + "01").ToString("yyyy")
            Me.TaishoMM = Date.Parse(taishoYm + "/" + "01").ToString("MM")
            Me.OutputOrgCode = orgCode
            Me.OutputFileName = outputFileName
            Me.calcZissekiNumber = calcNumber
            Me.ExcelTemplatePath = System.IO.Path.Combine(CS0050SESSION.UPLOAD_PATH,
                                                          "PRINTFORMAT",
                                                          defaultDatakey,
                                                          mapId, excelFileName)
            Me.UploadRootPath = System.IO.Path.Combine(CS0050SESSION.UPLOAD_PATH,
                                                       "PRINTWORK",
                                                       CS0050SESSION.USERID)
            'ディレクトリが存在しない場合は生成
            If IO.Directory.Exists(Me.UploadRootPath) = False Then
                IO.Directory.CreateDirectory(Me.UploadRootPath)
            End If
            '前日プリフィックスのアップロードファイルが残っていた場合は削除
            Dim targetFiles = IO.Directory.GetFiles(Me.UploadRootPath, "*.*")
            Dim keepFilePrefix As String = Now.ToString("yyyyMMdd")
            For Each targetFile In targetFiles
                Dim fileName As String = IO.Path.GetFileName(targetFile)
                '今日の日付が先頭のファイル名の場合は残す
                If fileName.StartsWith(keepFilePrefix) Then
                    Continue For
                End If
                Try
                    IO.File.Delete(targetFile)
                Catch ex As Exception
                    '削除時のエラーは無視
                End Try
            Next targetFile
            'URLのルートを表示
            Me.UrlRoot = String.Format("{0}://{1}/{3}/{2}/", HttpContext.Current.Request.Url.Scheme, HttpContext.Current.Request.Url.Host, CS0050SESSION.USERID, CS0050SESSION.PRINT_ROOT_URL_NAME)

            'ファイルopen
            WW_Workbook.Open(Me.ExcelTemplatePath)

            If Me.OutputOrgCode = BaseDllConst.CONST_ORDERORGCODE_020202 _
                OrElse Me.OutputOrgCode = BaseDllConst.CONST_ORDERORGCODE_023301 _
                OrElse Me.OutputOrgCode = BaseDllConst.CONST_ORDERORGCODE_022702 + "01" _
                OrElse Me.OutputOrgCode = BaseDllConst.CONST_ORDERORGCODE_022702 + "02" Then
                Dim j As Integer = 0
                For i As Integer = 0 To WW_Workbook.Worksheets.Count - 1
                    If WW_Workbook.Worksheets(i).Name = "入力表" _
                        OrElse WW_Workbook.Worksheets(i).Name = "実績入力表" Then
                        WW_SheetNo = i
                    ElseIf WW_Workbook.Worksheets(i).Name = "東北電力　TMEJ内サテライト" _
                        OrElse WW_Workbook.Worksheets(i).Name = "加藤製油" _
                        OrElse WW_Workbook.Worksheets(i).Name = "東洋ウレタン" _
                        OrElse WW_Workbook.Worksheets(i).Name = "新宮ガス" Then
                        WW_SheetNoTmp01 = i
                    ElseIf WW_Workbook.Worksheets(i).Name = "固定費" Then
                        WW_SheetNoTmp02 = i
                    ElseIf WW_Workbook.Worksheets(i).Name = "届先毎" _
                        OrElse WW_Workbook.Worksheets(i).Name = "水島（届先別）" Then
                        WW_SheetNoTmp03 = i
                    ElseIf WW_Workbook.Worksheets(i).Name = "ﾏｽﾀ" Then
                        WW_SheetNoTmp04 = i
                    ElseIf WW_Workbook.Worksheets(i).Name = "TMP" + (j + 1).ToString("00") Then
                        WW_ArrSheetNo(j) = i
                        j += 1
                    End If
                Next
            End If

        Catch ex As Exception
            Throw
        End Try

    End Sub

    ''' <summary>
    ''' テンプレートを元に帳票を作成しダウンロードURLを生成する
    ''' </summary>
    ''' <returns>ダウンロード先URL</returns>
    ''' <remarks>作成メソッド、パブリックスコープはここに収める</remarks>
    Public Function CreateExcelPrintData() As String
        'Dim tmpFileName As String = DateTime.Now.ToString("yyyyMMddHHmmss") & DateTime.Now.Millisecond.ToString & ".xlsx"
        Dim tmpFileName As String = Date.Parse(TaishoYm + "/" + "01").ToString("yyyy年MM月_") & Me.OutputFileName & ".xlsx"
        Dim tmpFilePath As String = IO.Path.Combine(Me.UploadRootPath, tmpFileName)
        Dim retByte() As Byte

        Try
            '***** TODO処理 ここから *****
            '◯ヘッダーの設定
            EditHeaderArea()
            '◯明細の設定
            EditDetailArea()
            '***** TODO処理 ここまで *****

            '保存処理実行
            Dim saveExcelLock As New Object
            SyncLock saveExcelLock '複数Excel起動で同時セーブすると落ちるので抑止
                WW_Workbook.Save(tmpFilePath, SaveFileFormat.Xlsx)
            End SyncLock

            'ストリーム生成
            Using fs As New IO.FileStream(tmpFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                Dim binaryLength = Convert.ToInt32(fs.Length)
                ReDim retByte(binaryLength)
                fs.Read(retByte, 0, binaryLength)
                fs.Flush()
            End Using
            Return UrlRoot & tmpFileName

        Catch ex As Exception
            Throw '呼出し元にThrow
        Finally
        End Try

    End Function

    ''' <summary>
    ''' 帳票のヘッダー設定
    ''' </summary>
    Private Sub EditHeaderArea()
        Dim dayCellsSub As String() = {"", "", ""}
        Try
            '◯ 年月
            Select Case Me.OutputOrgCode
                Case BaseDllConst.CONST_ORDERORGCODE_020202,
                     BaseDllConst.CONST_ORDERORGCODE_023301
                    WW_Workbook.Worksheets(WW_SheetNo).Range("B1").Value = Integer.Parse(Me.TaishoYYYY)
                    WW_Workbook.Worksheets(WW_SheetNo).Range("B2").Value = Integer.Parse(Me.TaishoMM)
                    dayCellsSub = {"91", "94", "97"}
                Case BaseDllConst.CONST_ORDERORGCODE_022702 + "01",
                     BaseDllConst.CONST_ORDERORGCODE_022702 + "02"
                    WW_Workbook.Worksheets(WW_SheetNo).Range("C4").Value = Integer.Parse(Me.TaishoYYYY)
                    WW_Workbook.Worksheets(WW_SheetNo).Range("E4").Value = Integer.Parse(Me.TaishoMM)
                    dayCellsSub = {"36", "37", "38"}
            End Select

            '〇 日付(セルチェック)
            'Dim dayCells As String() = {"91", "94", "97"}
            Dim dayCells As String() = dayCellsSub
            Dim lastDay As String = Date.Parse(Me.TaishoYYYY + "/" + Me.TaishoMM + "/01").AddMonths(1).AddDays(-1).ToString("dd")
            Dim i As Integer = 0
            For Each dayCell As String In dayCells
                '★月末日チェック
                Dim blnFlg As Boolean = True
                If Integer.Parse(lastDay) = 28 Then
                ElseIf Integer.Parse(lastDay) = 29 Then
                    If i < 1 Then blnFlg = False
                ElseIf Integer.Parse(lastDay) = 30 Then
                    If i < 2 Then blnFlg = False
                ElseIf Integer.Parse(lastDay) = 31 Then
                    blnFlg = False
                End If

                '★チェックがTRUE
                If blnFlg = True Then
                    WW_Workbook.Worksheets(WW_SheetNo).Range("A" + dayCell).Value = ""
                    WW_Workbook.Worksheets(WW_SheetNo).Range("B" + dayCell).Value = ""
                End If
                i += 1
            Next

            '〇 年月（鏡用）
            Dim lastDate As String = Me.TaishoYYYY + "/" + Me.TaishoMM + "/01"
            lastDate = Date.Parse(lastDate).AddMonths(1).AddDays(-1).ToString("yyyy/MM/dd")
            WW_Workbook.Worksheets(WW_SheetNoTmp01).Range("I1").Value = Date.Parse(lastDate)

        Catch ex As Exception
            Throw
        Finally
        End Try
    End Sub
    ''' <summary>
    ''' 帳票の明細設定
    ''' </summary>
    Private Sub EditDetailArea()
        Try
            Dim cellStay As String = ""

            'For Each PrintDatarow As DataRow In PrintData.Select("SETCELL01<>''", "ROWSORTNO, TODOKEDATE")
            For Each PrintDatarow As DataRow In PrintData.Select("SETCELL01<>''", "ROWSORTNO, SHUKADATE")
                '◯ 届先名
                WW_Workbook.Worksheets(WW_SheetNo).Range(PrintDatarow("SETCELL01").ToString()).Value = PrintDatarow("TODOKENAME_REP").ToString()
                '◯ 実績数量
                WW_Workbook.Worksheets(WW_SheetNo).Range(PrintDatarow("SETCELL02").ToString()).Value = Double.Parse(PrintDatarow("ZISSEKI").ToString()) * Me.calcZissekiNumber
                '◯ 備考
                If PrintDatarow("SETCELL03").ToString() = "" Then Continue For
                WW_Workbook.Worksheets(WW_SheetNo).Range(PrintDatarow("SETCELL03").ToString()).Value = PrintDatarow("REMARK_REP").ToString()
            Next

            '★計算エンジンの無効化
            WW_Workbook.EnableCalculation = False

            '〇陸事番号(追加)用設定
            For Each PrintDatarow As DataRow In PrintData.Select("DISPLAYCELL_START<>''")
                If cellStay <> "" AndAlso cellStay = PrintDatarow("DISPLAYCELL_START").ToString() Then
                    Continue For
                End If
                '〇シート「入力表」
                '★ 表示
                WW_Workbook.Worksheets(WW_SheetNo).Range(String.Format("{0}:{1}", PrintDatarow("DISPLAYCELL_START").ToString(), PrintDatarow("DISPLAYCELL_END").ToString())).Hidden = False
                '★ 陸事番号
                WW_Workbook.Worksheets(WW_SheetNo).Range(PrintDatarow("DISPLAYCELL_START").ToString() + "4").Value = PrintDatarow("TANKNUMBER").ToString()
                '★ 受注数量
                Dim dblZyutyu As Double = Math.Round(Double.Parse(PrintDatarow("ZYUTYU").ToString()), 1, MidpointRounding.AwayFromZero)
                WW_Workbook.Worksheets(WW_SheetNo).Range(PrintDatarow("DISPLAYCELL_END").ToString() + "4").Value = dblZyutyu.ToString() + "t"

                '〇シート「固定費」
                '★ 表示
                WW_Workbook.Worksheets(WW_SheetNoTmp02).Range(String.Format("{0}:{0}", PrintDatarow("DISPLAYCELL_KOTEICHI").ToString())).Hidden = False
                '★ トラクタ
                WW_Workbook.Worksheets(WW_SheetNoTmp02).Range("E" + PrintDatarow("DISPLAYCELL_KOTEICHI").ToString()).Value = PrintDatarow("TRACTORNUMBER").ToString()
                '★ トレーラ
                WW_Workbook.Worksheets(WW_SheetNoTmp02).Range("F" + PrintDatarow("DISPLAYCELL_KOTEICHI").ToString()).Value = PrintDatarow("TANKNUMBER").ToString()

                '表示用セル保管
                cellStay = PrintDatarow("DISPLAYCELL_START").ToString()
            Next

            '〇届名称(追加)用設定
            cellStay = ""
            For Each PrintDatarow As DataRow In PrintData.Select("TODOKECELL_REP<>''")
                If cellStay <> "" AndAlso cellStay = PrintDatarow("TODOKECELL_REP").ToString() Then
                    Continue For
                End If
                '〇シート「届先毎」
                '★ 表示
                WW_Workbook.Worksheets(WW_SheetNoTmp03).Range(String.Format("{0}:{0}", PrintDatarow("TODOKECELL_REP").ToString())).Hidden = False

                '〇シート「マスタ」
                '★ 表示
                WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("{0}:{0}", PrintDatarow("MASTERCELL_REP").ToString())).Hidden = False
                '★ 設定(配送先)
                WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("A{0}", PrintDatarow("MASTERCELL_REP").ToString())).Value = PrintDatarow("TODOKENAME_REP").ToString()
                '〇水島営業所の場合
                If PrintDatarow("ORDERORGCODE").ToString() = BaseDllConst.CONST_ORDERORGCODE_023301 Then
                    '★ 設定(向け先)
                    WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("E{0}", PrintDatarow("MASTERCELL_REP").ToString())).Value = PrintDatarow("SHEETNAME_REP").ToString()
                End If

                Try
                    '★ シート表示
                    Dim iDisp As Integer = Integer.Parse(PrintDatarow("SHEETDISPLAY_REP").ToString())
                    WW_Workbook.Worksheets(WW_ArrSheetNo(iDisp)).Visible = Visibility.Visible
                    '★ シート名変更
                    WW_Workbook.Worksheets(WW_ArrSheetNo(iDisp)).Name = PrintDatarow("TODOKENAME_REP").ToString()
                Catch ex As Exception
                End Try

                '表示用セル保管
                cellStay = PrintDatarow("TODOKECELL_REP").ToString()
            Next

            '〇届先(単価)設定
            For Each PrintDatarow As DataRow In PrintTankData.Select("", "MASTERNO")
                If PrintDatarow("MASTERNO").ToString() = "" OrElse PrintDatarow("MASTERNO").ToString() = "0" Then Continue For
                '〇シート「マスタ」
                Dim iTanka As Integer = Integer.Parse(PrintDatarow("TANKA").ToString())
                If Convert.ToString(PrintDatarow("SYAGATA")) = "1" Then
                    '★単車
                    WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("B{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka
                ElseIf Convert.ToString(PrintDatarow("SYAGATA")) = "2" Then
                    '★トレーラ
                    '〇水島営業所(三井Ｅ＆Ｓ, コカ・コーラ)独自仕様
                    If PrintDatarow("ORGCODE").ToString() = BaseDllConst.CONST_ORDERORGCODE_023301 _
                        AndAlso (PrintDatarow("TODOKECODE").ToString() = BaseDllConst.CONST_TODOKECODE_004002 _
                                 OrElse PrintDatarow("TODOKECODE").ToString() = BaseDllConst.CONST_TODOKECODE_005509) _
                        AndAlso PrintDatarow("TODOKEBRANCHCODE").ToString() = "02" Then
                        WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("D{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka

                        '〇西日本支店車庫(泉北)独自仕様
                    ElseIf PrintDatarow("ORGCODE").ToString() = BaseDllConst.CONST_ORDERORGCODE_022702 Then
                        Dim cellValue As String = ""
                        cellValue = WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("A{0}", PrintDatarow("MASTERNO").ToString())).Value.ToString()

                        '☆(日本栄船)独自仕様
                        If PrintDatarow("TODOKECODE").ToString() = BaseDllConst.CONST_TODOKECODE_004916 _
                            AndAlso PrintDatarow("SYUBETSU").ToString() = "運行単価" _
                            AndAlso PrintDatarow("BIKOU1").ToString() = "2名乗車" Then
                            WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("B{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka

                            '☆(昭和産業㈱)独自仕様※[休日加算金]以外
                        ElseIf PrintDatarow("TODOKECODE").ToString() = BaseDllConst.CONST_TODOKECODE_005866 _
                            AndAlso PrintDatarow("SYUBETSU").ToString() <> "休日加算金" Then
                            If cellValue = "昭和産業1" _
                                AndAlso PrintDatarow("SYUBETSU").ToString() = "トン単価" _
                                AndAlso PrintDatarow("BIKOU1").ToString() = "1運行目" Then
                                WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("C{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka
                            ElseIf cellValue = "昭和産業2" _
                                AndAlso PrintDatarow("SYUBETSU").ToString() = "トン単価" _
                                AndAlso PrintDatarow("BIKOU1").ToString() = "2運行目" Then
                                WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("C{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka
                            End If

                        Else
                            WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("C{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka
                        End If
                    Else
                        WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("C{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka
                    End If
                Else
                    '〇西日本支店車庫(泉北)独自仕様
                    If PrintDatarow("ORGCODE").ToString() = BaseDllConst.CONST_ORDERORGCODE_022702 Then
                        '★休日加算金
                        If PrintDatarow("SYUBETSU").ToString() = "休日加算金" Then

                            '(日本栄船)独自仕様
                            If PrintDatarow("TODOKECODE").ToString() = BaseDllConst.CONST_TODOKECODE_004916 _
                                AndAlso PrintDatarow("BIKOU1").ToString() = "3名乗車" Then
                                WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("E{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka
                            Else
                                WW_Workbook.Worksheets(WW_SheetNoTmp04).Range(String.Format("D{0}", PrintDatarow("MASTERNO").ToString())).Value = iTanka
                            End If

                        End If
                    End If
                End If
            Next

            '★計算エンジンの有効化
            WW_Workbook.EnableCalculation = True

        Catch ex As Exception
            Throw
        Finally
        End Try
    End Sub
End Class
