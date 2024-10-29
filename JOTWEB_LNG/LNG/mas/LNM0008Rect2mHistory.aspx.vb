﻿''************************************************************
' コード変換特例２マスタメンテ変更履歴画面
' 作成日 2023/12/21
' 更新日 
' 作成者 大浜
' 更新者 
'
' 修正履歴 : 2023/12/21 新規作成
'          : 
''************************************************************
Imports MySQL.Data.MySqlClient
Imports System.Drawing
Imports GrapeCity.Documents.Excel

''' <summary>
''' コード変換特例２マスタ変更履歴
''' </summary>
''' <remarks></remarks>
Public Class LNM0008Rect2mHistory
    Inherits Page

    '○ 検索結果格納Table
    Private LNM0008tbl As DataTable                                  '一覧格納用テーブル

    ''' <summary>
    ''' 定数
    ''' </summary>
    Private Const CONST_DISPROWCOUNT As Integer = 19                '1画面表示用
    Private Const CONST_SCROLLCOUNT As Integer = 19                 'マウススクロール時稼働行数

    '〇 帳票用
    Private Const CONST_COLOR_HATCHING_MODIFY As String = "#FFFF00" '変更項目強調表示色(黄)
    Private Const CONST_COLOR_HATCHING_HEADER As String = "#002060" 'ヘッダ網掛け色
    Private Const CONST_COLOR_FONT_HEADER As String = "#FFFFFF" 'ヘッダフォント色
    Private Const CONST_COLOR_BLACK As String = "#000000" '黒
    Private Const CONST_COLOR_RED As String = "#FF0000" '赤

    Private Const CONST_HEIGHT_PER_ROW As Integer = 15 'セルのコメントの一行あたりの高さ
    Private Const CONST_DATA_START_ROW As Integer = 3 'データ開始行

    '○ 共通関数宣言(BASEDLL)
    Private CS0011LOGWrite As New CS0011LOGWrite                    'ログ出力
    Private CS0013ProfView As New CS0013ProfView                    'Tableオブジェクト展開
    Private CS0020JOURNAL As New CS0020JOURNAL                      '更新ジャーナル出力
    Private CS0030REPORT As New CS0030REPORT                        '帳票出力
    Private CS0050SESSION As New CS0050SESSION                      'セッション情報操作処理

    ''' <summary>
    ''' サーバー処理の遷移先
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load

        Try
            If IsPostBack Then
                '○ 各ボタン押下処理
                If Not String.IsNullOrEmpty(WF_ButtonClick.Value) Then
                    '○ 画面表示データ復元
                    Master.RecoverTable(LNM0008tbl)

                    Select Case WF_ButtonClick.Value
                        Case "WF_ButtonDOWNLOAD"        'ダウンロードボタン押下
                            WF_EXCELPDF(LNM0008WRKINC.FILETYPE.EXCEL)
                        Case "WF_ButtonPRINT"           '一覧印刷ボタン押下
                            WF_EXCELPDF(LNM0008WRKINC.FILETYPE.PDF)
                        Case "WF_ButtonEND"             '戻るボタン押下
                            WF_ButtonEND_Click()
                        Case "WF_ButtonFIRST"           '先頭頁ボタン押下
                            WF_ButtonFIRST_Click()
                        Case "WF_ButtonLAST"            '最終頁ボタン押下
                            WF_ButtonLAST_Click()
                        Case "WF_SelectMODIFYYMChange"  '変更年月フィールドチェンジ
                            Using SQLcon As MySqlConnection = CS0050SESSION.getConnection
                                SQLcon.Open()       'DataBase接続
                                '変更日取得
                                MODIFYDDGet(SQLcon)
                                '変更ユーザ取得
                                MODIFYUSERGet(SQLcon)
                            End Using
                        Case "WF_SelectMODIFYDDChange"  '変更日フィールドチェンジ
                            Using SQLcon As MySqlConnection = CS0050SESSION.getConnection
                                SQLcon.Open()       'DataBase接続
                                '変更ユーザ取得
                                MODIFYUSERGet(SQLcon)
                            End Using
                        Case "WF_ButtonMODIFYVIEW"  '表示するボタン押下
                            Using SQLcon As MySqlConnection = CS0050SESSION.getConnection
                                SQLcon.Open()  ' DataBase接続
                                MAPDataGet(SQLcon)
                                Master.SaveTable(LNM0008tbl)
                                '〇 一覧の件数を取得
                                Me.ListCount.Text = "件数：" + LNM0008tbl.Rows.Count.ToString()
                            End Using
                    End Select

                    '○ 一覧再表示処理
                    DisplayGrid()
                End If
            Else
                '○ 初期化処理
                Initialize()
            End If

            '○ 画面モード(更新・参照)設定
            If Master.MAPpermitcode = C_PERMISSION.UPDATE Then
                WF_MAPpermitcode.Value = "TRUE"
            Else
                WF_MAPpermitcode.Value = "FALSE"
            End If

        Finally
            '○ 格納Table Close
            If Not IsNothing(LNM0008tbl) Then
                LNM0008tbl.Clear()
                LNM0008tbl.Dispose()
                LNM0008tbl = Nothing
            End If
        End Try

    End Sub

    ''' <summary>
    ''' 初期化処理
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub Initialize()

        '○ 画面ID設定
        Master.MAPID = LNM0008WRKINC.MAPIDH
        '○ HELP表示有無設定
        Master.dispHelp = False
        '○ D&D有無設定
        Master.eventDrop = True
        '○ Grid情報保存先のファイル名
        Master.CreateXMLSaveFile()

        '○ 初期値設定
        WF_FIELD.Value = ""
        WF_ButtonClick.Value = ""
        WF_LeftboxOpen.Value = ""
        WF_RightboxOpen.Value = ""
        rightview.ResetIndex()
        leftview.ActiveListBox()

        '○ 右Boxへの値設定
        rightview.MAPID = Master.MAPID
        rightview.MAPVARI = Master.MAPvariant
        rightview.COMPCODE = Master.USERCAMP
        rightview.PROFID = Master.PROF_REPORT
        rightview.Initialize("")

        '○ 画面の値設定
        WW_MAPValueSet()

        '○ GridView初期設定
        GridViewInitialize()

    End Sub

    ''' <summary>
    ''' 画面初期値設定処理
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub WW_MAPValueSet()

        Using SQLcon As MySqlConnection = CS0050SESSION.getConnection
            SQLcon.Open()       'DataBase接続

            '変更年月取得
            MODIFYYMGet(SQLcon)
            '変更日取得
            MODIFYDDGet(SQLcon)
            '変更ユーザ取得
            MODIFYUSERGet(SQLcon)
        End Using
    End Sub

    ''' <summary>
    ''' GridViewデータ設定
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub GridViewInitialize()

        '○ 画面表示データ取得
        Using SQLcon As MySqlConnection = CS0050SESSION.getConnection
            SQLcon.Open()  ' DataBase接続

            MAPDataGet(SQLcon)
        End Using

        '○ 画面表示データ保存
        Master.SaveTable(LNM0008tbl)

        '〇 一覧の件数を取得
        Me.ListCount.Text = "件数：" + LNM0008tbl.Rows.Count.ToString()

        '○ 一覧表示データ編集(性能対策)
        Dim TBLview As DataView = New DataView(LNM0008tbl)

        TBLview.RowFilter = "LINECNT >= 1 and LINECNT <= " & CONST_DISPROWCOUNT

        CS0013ProfView.CAMPCODE = Master.USERCAMP
        CS0013ProfView.PROFID = Master.PROF_VIEW
        CS0013ProfView.MAPID = Master.MAPID
        CS0013ProfView.VARI = Master.VIEWID
        CS0013ProfView.SRCDATA = TBLview.ToTable
        CS0013ProfView.TBLOBJ = pnlListArea
        CS0013ProfView.SCROLLTYPE = CS0013ProfView.SCROLLTYPE_ENUM.Both
        CS0013ProfView.LEVENT = "ondblclick"
        CS0013ProfView.LFUNC = "ListDbClick"
        CS0013ProfView.TITLEOPT = True
        CS0013ProfView.CS0013ProfView()
        If Not isNormal(CS0013ProfView.ERR) Then
            Master.Output(CS0013ProfView.ERR, C_MESSAGE_TYPE.ABORT, "一覧設定エラー")
            Exit Sub
        End If

        '○ 先頭行に合わせる
        WF_GridPosition.Text = "1"

        TBLview.Dispose()
        TBLview = Nothing

        '変更箇所を強調表示
        ClientScript.RegisterStartupScript(Me.GetType(), "key", "f_ModifyHatching();", True)

    End Sub

    ''' <summary>
    ''' 画面表示データ取得
    ''' </summary>
    ''' <param name="SQLcon"></param>
    ''' <remarks></remarks>
    Protected Sub MAPDataGet(ByVal SQLcon As MySqlConnection)

        If IsNothing(LNM0008tbl) Then
            LNM0008tbl = New DataTable
        End If

        If LNM0008tbl.Columns.Count <> 0 Then
            LNM0008tbl.Columns.Clear()
        End If

        LNM0008tbl.Clear()

        '○ 検索SQL
        '　検索説明
        '     条件指定に従い該当データをコード変換特例２変更履歴テーブルから取得する
        Dim SQLStr = New StringBuilder
        SQLStr.AppendLine(" Select                                                                                               ")
        SQLStr.AppendLine("     1                                                                    AS 'SELECT'                 ")
        SQLStr.AppendLine("   , 0                                                                    AS HIDDEN                   ")
        SQLStr.AppendLine("   , 0                                                                    AS LINECNT                  ")
        SQLStr.AppendLine("   , ''                                                                   AS OPERATION                ")
        SQLStr.AppendLine("   , UPDTIMSTP                                                            AS UPDTIMSTP                ")
        SQLStr.AppendLine("   , coalesce(RTRIM(DELFLG), '')                                    AS DELFLG                   ")
        SQLStr.AppendLine("   , coalesce(RTRIM(ORGCODE), '')                                   AS ORGCODE                  ")
        SQLStr.AppendLine("   , coalesce(RTRIM(BIGCTNCD), '')                                  AS BIGCTNCD                 ")
        SQLStr.AppendLine("   , coalesce(RTRIM(MIDDLECTNCD), '')                               AS MIDDLECTNCD              ")
        SQLStr.AppendLine("   , coalesce(RTRIM(PURPOSE), '')                                   AS PURPOSE                  ")
        SQLStr.AppendLine("   , coalesce(RTRIM(STACKFREEKBN), '')                              AS STACKFREEKBN             ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRDEPTRUSTEECD), '')                           AS SPRDEPTRUSTEECD          ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRDEPTRUSTEESUBCD), '')                        AS SPRDEPTRUSTEESUBCD       ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRDEPTRUSTEESUBZKBN), '')                      AS SPRDEPTRUSTEESUBZKBN     ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRDEPSHIPPERCD), '')                           AS SPRDEPSHIPPERCD          ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRARRTRUSTEECD), '')                           AS SPRARRTRUSTEECD          ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRARRTRUSTEESUBCD), '')                        AS SPRARRTRUSTEESUBCD       ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRARRTRUSTEESUBZKBN), '')                      AS SPRARRTRUSTEESUBZKBN     ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRJRITEMCD), '')                               AS SPRJRITEMCD              ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRSTACKFREEKBN), '')                           AS SPRSTACKFREEKBN          ")
        SQLStr.AppendLine("   , coalesce(RTRIM(SPRSTATUSKBN), '')                              AS SPRSTATUSKBN             ")
        SQLStr.AppendLine("   , coalesce(RTRIM(OPERATEKBN), '')                                        AS OPERATEKBN                ")
        SQLStr.AppendLine("   , CASE                 ")
        SQLStr.AppendLine("      WHEN coalesce(RTRIM(OPERATEKBN), '') ='2' AND coalesce(RTRIM(MODIFYKBN), '') ='2' THEN '変更前 更新' ")
        SQLStr.AppendLine("      WHEN coalesce(RTRIM(OPERATEKBN), '') ='2' AND coalesce(RTRIM(MODIFYKBN), '') ='3' THEN '変更後 更新' ")
        SQLStr.AppendLine("      WHEN coalesce(RTRIM(OPERATEKBN), '') ='3' AND coalesce(RTRIM(MODIFYKBN), '') ='2' THEN '変更前 削除' ")
        SQLStr.AppendLine("      WHEN coalesce(RTRIM(OPERATEKBN), '') ='3' AND coalesce(RTRIM(MODIFYKBN), '') ='3' THEN '変更後 削除' ")
        SQLStr.AppendLine("      ELSE ''                                                                                          ")
        SQLStr.AppendLine("    END AS OPERATEKBNNAME                                                                              ")
        SQLStr.AppendLine("   , coalesce(RTRIM(MODIFYKBN), '')                                        AS MODIFYKBN                　")
        SQLStr.AppendLine("   ,CASE coalesce(RTRIM(MODIFYKBN), '')                                                                  ")
        SQLStr.AppendLine("      WHEN '1' THEN '新規'                                                                             ")
        SQLStr.AppendLine("      WHEN '2' THEN '変更前'                                                                           ")
        SQLStr.AppendLine("      WHEN '3' THEN '変更後'                                                                           ")
        SQLStr.AppendLine("      ELSE ''                                                                                          ")
        SQLStr.AppendLine("    END AS MODIFYKBNNAME                                                                               ")
        SQLStr.AppendLine("   , FORMAT(MODIFYYMD, 'yyyy/MM/dd HH:mm:ss')                              AS MODIFYYMD                ")
        SQLStr.AppendLine("   , coalesce(RTRIM(MODIFYUSER), '')                                         AS MODIFYUSER               ")
        SQLStr.AppendLine(" FROM                                                                                                  ")
        SQLStr.AppendLine("     LNG.LNT0113_RECT2HIST                                                                             ")
        SQLStr.AppendLine(" WHERE                                                                                                 ")
        '変更日が指定されている場合
        If Not WF_DDL_MODIFYDD.SelectedValue = "" Then
            SQLStr.AppendLine("    FORMAT(MODIFYYMD, 'yyyy/MM/dd')  = @MODIFYYMD                                                  ")
        Else
            SQLStr.AppendLine("    FORMAT(MODIFYYMD, 'yyyy/MM/01')  = @MODIFYYMD                                                  ")
        End If
        '変更ユーザが指定されている場合
        If Not WF_DDL_MODIFYUSER.SelectedValue = "" Then
            SQLStr.AppendLine(" AND coalesce(RTRIM(MODIFYUSER), '')  =  @MODIFYUSER               　　　　　　　　　　　　　　　　  ")
        End If
        SQLStr.AppendLine(" ORDER BY                                                                                              ")
        SQLStr.AppendLine("    MODIFYYMD DESC                                                                                     ")
        SQLStr.AppendLine("   ,ORGCODE                                                                                            ")
        SQLStr.AppendLine("   ,BIGCTNCD                                                                                           ")
        SQLStr.AppendLine("   ,MIDDLECTNCD                                                                                        ")
        SQLStr.AppendLine("   ,MODIFYKBN                                                                                          ")

        Try
            Using SQLcmd As New MySqlCommand(SQLStr.ToString, SQLcon)
                Dim P_MODIFYYMD As MySqlParameter = SQLcmd.Parameters.Add("@MODIFYYMD", MySqlDbType.VarChar, 10)         '変更年月
                '変更日が指定されている場合
                If Not WF_DDL_MODIFYDD.SelectedValue = "" Then
                    P_MODIFYYMD.Value = WF_DDL_MODIFYYM.SelectedValue + "/" + WF_DDL_MODIFYDD.SelectedValue
                Else
                    P_MODIFYYMD.Value = WF_DDL_MODIFYYM.SelectedValue + "/01"
                End If

                Dim P_MODIFYUSER As MySqlParameter = SQLcmd.Parameters.Add("@MODIFYUSER", MySqlDbType.VarChar, 20)         '変更ユーザ
                '変更ユーザが指定されている場合
                If Not WF_DDL_MODIFYUSER.SelectedValue = "" Then
                    P_MODIFYUSER.Value = WF_DDL_MODIFYUSER.SelectedValue
                Else
                    P_MODIFYUSER.Value = ""
                End If

                Using SQLdr As MySqlDataReader = SQLcmd.ExecuteReader()
                    '○ フィールド名とフィールドの型を取得
                    For index As Integer = 0 To SQLdr.FieldCount - 1
                        LNM0008tbl.Columns.Add(SQLdr.GetName(index), SQLdr.GetFieldType(index))
                    Next

                    '○ テーブル検索結果をテーブル格納
                    LNM0008tbl.Load(SQLdr)
                End Using

                Dim i As Integer = 0
                For Each LNM0008row As DataRow In LNM0008tbl.Rows
                    i += 1
                    LNM0008row("LINECNT") = i        'LINECNT
                Next
            End Using
        Catch ex As Exception
            Master.Output(C_MESSAGE_NO.DB_ERROR, C_MESSAGE_TYPE.ABORT, "LNM0008H SELECT")

            CS0011LOGWrite.INFSUBCLASS = "MAIN"                         'SUBクラス名
            CS0011LOGWrite.INFPOSI = "DB:LNM0008H Select"
            CS0011LOGWrite.NIWEA = C_MESSAGE_TYPE.ABORT
            CS0011LOGWrite.TEXT = ex.ToString()
            CS0011LOGWrite.MESSAGENO = C_MESSAGE_NO.DB_ERROR
            CS0011LOGWrite.CS0011LOGWrite()                             'ログ出力
            Exit Sub
        End Try

    End Sub

    ''' <summary>
    ''' 変更年月取得
    ''' </summary>
    ''' <param name="SQLcon"></param>
    ''' <remarks></remarks>
    Protected Sub MODIFYYMGet(ByVal SQLcon As MySqlConnection)

        Dim SQLStr = New StringBuilder
        SQLStr.AppendLine(" SELECT DISTINCT ")
        SQLStr.AppendLine("     FORMAT(MODIFYYMD, 'yyyy/MM') AS MODIFYYM ")
        SQLStr.AppendLine(" FROM lng.LNT0113_RECT2HIST ")
        SQLStr.AppendLine(" ORDER BY MODIFYYM DESC ")

        Try
            Using SQLcmd As New MySqlCommand(SQLStr.ToString, SQLcon)
                Using SQLdr As MySqlDataReader = SQLcmd.ExecuteReader()

                    Dim WW_Tbl = New DataTable
                    Dim WW_CURRENTMONTH As String = Date.Now.ToString("yyyy/MM")

                    '○ フィールド名とフィールドの型を取得
                    For index As Integer = 0 To SQLdr.FieldCount - 1
                        WW_Tbl.Columns.Add(SQLdr.GetName(index), SQLdr.GetFieldType(index))
                    Next
                    WW_Tbl.Load(SQLdr)

                    WF_DDL_MODIFYYM.Items.Clear()
                    'WF_DDL_MODIFYYM.Items.Add("")
                    For Each WW_ROW As DataRow In WW_Tbl.Rows
                        WF_DDL_MODIFYYM.Items.Add(WW_ROW("MODIFYYM"))
                    Next

                    '当月がドロップダウンリストに存在しない場合追加
                    If WF_DDL_MODIFYYM.Items.FindByValue(WW_CURRENTMONTH) Is Nothing Then
                        WF_DDL_MODIFYYM.Items.Insert(0, WW_CURRENTMONTH)
                    End If

                    WF_DDL_MODIFYYM.SelectedValue = WW_CURRENTMONTH

                End Using
            End Using
        Catch ex As Exception
            Master.Output(C_MESSAGE_NO.DB_ERROR, C_MESSAGE_TYPE.ABORT, "LNT0113_RECT2HIST SELECT")

            CS0011LOGWrite.INFSUBCLASS = "MAIN"                         'SUBクラス名
            CS0011LOGWrite.INFPOSI = "DB:LNT0113_RECT2HIST Select"
            CS0011LOGWrite.NIWEA = C_MESSAGE_TYPE.ABORT
            CS0011LOGWrite.TEXT = ex.ToString()
            CS0011LOGWrite.MESSAGENO = C_MESSAGE_NO.DB_ERROR
            CS0011LOGWrite.CS0011LOGWrite()                             'ログ出力
            Exit Sub
        End Try

    End Sub

    ''' <summary>
    ''' 変更日取得
    ''' </summary>
    ''' <param name="SQLcon"></param>
    ''' <remarks></remarks>
    Protected Sub MODIFYDDGet(ByVal SQLcon As MySqlConnection)

        Dim SQLStr = New StringBuilder
        SQLStr.AppendLine(" SELECT DISTINCT ")
        SQLStr.AppendLine("     FORMAT(MODIFYYMD, 'dd') AS MODIFYDD ")
        SQLStr.AppendLine(" FROM lng.LNT0113_RECT2HIST ")
        SQLStr.AppendLine(" WHERE                                                                                                 ")
        SQLStr.AppendLine("    FORMAT(MODIFYYMD, 'yyyy/MM')  = @MODIFYYM                                                         ")
        SQLStr.AppendLine(" ORDER BY MODIFYDD ")

        Try
            Using SQLcmd As New MySqlCommand(SQLStr.ToString, SQLcon)
                Dim P_MODIFYYM As MySqlParameter = SQLcmd.Parameters.Add("@MODIFYYM", MySqlDbType.VarChar, 7)         '変更年月
                P_MODIFYYM.Value = WF_DDL_MODIFYYM.SelectedValue

                Using SQLdr As MySqlDataReader = SQLcmd.ExecuteReader()

                    Dim WW_Tbl = New DataTable

                    '○ フィールド名とフィールドの型を取得
                    For index As Integer = 0 To SQLdr.FieldCount - 1
                        WW_Tbl.Columns.Add(SQLdr.GetName(index), SQLdr.GetFieldType(index))
                    Next
                    WW_Tbl.Load(SQLdr)

                    WF_DDL_MODIFYDD.Items.Clear()
                    WF_DDL_MODIFYDD.Items.Add("")
                    For Each WW_ROW As DataRow In WW_Tbl.Rows
                        WF_DDL_MODIFYDD.Items.Add(WW_ROW("MODIFYDD"))
                    Next

                End Using
            End Using
        Catch ex As Exception
            Master.Output(C_MESSAGE_NO.DB_ERROR, C_MESSAGE_TYPE.ABORT, "LNT0113_RECT2HIST SELECT")

            CS0011LOGWrite.INFSUBCLASS = "MAIN"                         'SUBクラス名
            CS0011LOGWrite.INFPOSI = "DB:LNT0113_RECT2HIST Select"
            CS0011LOGWrite.NIWEA = C_MESSAGE_TYPE.ABORT
            CS0011LOGWrite.TEXT = ex.ToString()
            CS0011LOGWrite.MESSAGENO = C_MESSAGE_NO.DB_ERROR
            CS0011LOGWrite.CS0011LOGWrite()                             'ログ出力
            Exit Sub
        End Try

    End Sub

    ''' <summary>
    ''' 変更ユーザ取得
    ''' </summary>
    ''' <param name="SQLcon"></param>
    ''' <remarks></remarks>
    Protected Sub MODIFYUSERGet(ByVal SQLcon As MySqlConnection)

        Dim SQLStr = New StringBuilder
        SQLStr.AppendLine(" SELECT DISTINCT ")
        SQLStr.AppendLine("     MODIFYUSER ")
        SQLStr.AppendLine(" FROM lng.LNT0113_RECT2HIST ")
        SQLStr.AppendLine(" WHERE                                                                                                 ")
        '変更日が指定されている場合
        If Not WF_DDL_MODIFYDD.SelectedValue = "" Then
            SQLStr.AppendLine("    FORMAT(MODIFYYMD, 'yyyy/MM/dd')  = @MODIFYYMD                                                  ")
        Else
            SQLStr.AppendLine("    FORMAT(MODIFYYMD, 'yyyy/MM/01')  = @MODIFYYMD                                                  ")
        End If

        SQLStr.AppendLine(" ORDER BY MODIFYUSER  ")

        Try
            Using SQLcmd As New MySqlCommand(SQLStr.ToString, SQLcon)
                Dim P_MODIFYYMD As MySqlParameter = SQLcmd.Parameters.Add("@MODIFYYMD", MySqlDbType.VarChar, 10)         '変更年月日
                '変更日が指定されている場合
                If Not WF_DDL_MODIFYDD.SelectedValue = "" Then
                    P_MODIFYYMD.Value = WF_DDL_MODIFYYM.SelectedValue + "/" + WF_DDL_MODIFYDD.SelectedValue
                Else
                    P_MODIFYYMD.Value = WF_DDL_MODIFYYM.SelectedValue + "/01"
                End If

                Using SQLdr As MySqlDataReader = SQLcmd.ExecuteReader()

                    Dim WW_Tbl = New DataTable

                    '○ フィールド名とフィールドの型を取得
                    For index As Integer = 0 To SQLdr.FieldCount - 1
                        WW_Tbl.Columns.Add(SQLdr.GetName(index), SQLdr.GetFieldType(index))
                    Next
                    WW_Tbl.Load(SQLdr)

                    WF_DDL_MODIFYUSER.Items.Clear()
                    WF_DDL_MODIFYUSER.Items.Add("")
                    For Each WW_ROW As DataRow In WW_Tbl.Rows
                        WF_DDL_MODIFYUSER.Items.Add(WW_ROW("MODIFYUSER"))
                    Next

                End Using
            End Using
        Catch ex As Exception
            Master.Output(C_MESSAGE_NO.DB_ERROR, C_MESSAGE_TYPE.ABORT, "LNT0113_RECT2HIST SELECT")

            CS0011LOGWrite.INFSUBCLASS = "MAIN"                         'SUBクラス名
            CS0011LOGWrite.INFPOSI = "DB:LNT0113_RECT2HIST Select"
            CS0011LOGWrite.NIWEA = C_MESSAGE_TYPE.ABORT
            CS0011LOGWrite.TEXT = ex.ToString()
            CS0011LOGWrite.MESSAGENO = C_MESSAGE_NO.DB_ERROR
            CS0011LOGWrite.CS0011LOGWrite()                             'ログ出力
            Exit Sub
        End Try

    End Sub

    ''' <summary>
    ''' 一覧再表示処理
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub DisplayGrid()

        Dim WW_GridPosition As Integer          '表示位置(開始)
        Dim WW_DataCNT As Integer = 0           '(絞り込み後)有効Data数

        '○ 表示対象行カウント(絞り込み対象)
        For Each LNM0008row As DataRow In LNM0008tbl.Rows
            If LNM0008row("HIDDEN") = 0 Then
                WW_DataCNT += 1
                ' 行(LINECNT)を再設定する。既存項目(SELECT)を利用
                LNM0008row("SELECT") = WW_DataCNT
            End If
        Next

        '○ 表示LINECNT取得
        If WF_GridPosition.Text = "" Then
            WW_GridPosition = 1
        Else
            Try
                Integer.TryParse(WF_GridPosition.Text, WW_GridPosition)
            Catch ex As Exception
                WW_GridPosition = 1
            End Try
        End If

        '○ 表示格納位置決定
        ' 表示開始_格納位置決定(次頁スクロール)
        If WF_ButtonClick.Value = "WF_MouseWheelUp" Then
            If (WW_GridPosition + CONST_SCROLLCOUNT) <= WW_DataCNT Then
                WW_GridPosition += CONST_SCROLLCOUNT
            End If
        End If
        ' 表示開始_格納位置決定(前頁スクロール)
        If WF_ButtonClick.Value = "WF_MouseWheelDown" Then
            If (WW_GridPosition - CONST_SCROLLCOUNT) > 0 Then
                WW_GridPosition -= CONST_SCROLLCOUNT
            Else
                WW_GridPosition = 1
            End If
        End If

        '○ 画面(GridView)表示
        Dim TBLview As DataView = New DataView(LNM0008tbl)

        '○ ソート
        TBLview.Sort = "LINECNT"
        TBLview.RowFilter = "HIDDEN = 0 and SELECT >= " & WW_GridPosition.ToString() & " and SELECT < " & (WW_GridPosition + CONST_DISPROWCOUNT).ToString()

        '○ 一覧作成
        CS0013ProfView.CAMPCODE = Master.USERCAMP
        CS0013ProfView.PROFID = Master.PROF_VIEW
        CS0013ProfView.MAPID = Master.MAPID
        CS0013ProfView.VARI = Master.VIEWID
        CS0013ProfView.SRCDATA = TBLview.ToTable
        CS0013ProfView.TBLOBJ = pnlListArea
        CS0013ProfView.SCROLLTYPE = CS0013ProfView.SCROLLTYPE_ENUM.Both
        CS0013ProfView.LEVENT = "ondblclick"
        CS0013ProfView.LFUNC = "ListDbClick"
        CS0013ProfView.TITLEOPT = True
        CS0013ProfView.CS0013ProfView()

        '○ クリア
        If TBLview.Count = 0 Then
            WF_GridPosition.Text = "1"
        Else
            WF_GridPosition.Text = TBLview.Item(0)("SELECT")
        End If

        TBLview.Dispose()
        TBLview = Nothing

        '変更箇所を強調表示
        ClientScript.RegisterStartupScript(Me.GetType(), "key", "f_ModifyHatching();", True)

    End Sub

    ''' <summary>
    ''' 戻るボタン押下時処理
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub WF_ButtonEND_Click()

        Master.TransitionPrevPage()

    End Sub

    ''' <summary>
    ''' 先頭頁ボタン押下時処理
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub WF_ButtonFIRST_Click()

        '○ 先頭頁に移動
        WF_GridPosition.Text = "1"

    End Sub

    ''' <summary>
    ''' 最終頁ボタン押下時処理
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub WF_ButtonLAST_Click()

        '○ ソート
        Dim TBLview As New DataView(LNM0008tbl)
        TBLview.RowFilter = "HIDDEN = 0"

        '○ 最終頁に移動
        If TBLview.Count Mod 10 = 0 Then
            WF_GridPosition.Text = TBLview.Count - (TBLview.Count Mod 10)
        Else
            WF_GridPosition.Text = TBLview.Count - (TBLview.Count Mod 10) + 1
        End If

        TBLview.Dispose()
        TBLview = Nothing

    End Sub

#Region "ﾀﾞｳﾝﾛｰﾄﾞ"

    ''' <summary>
    ''' ﾀﾞｳﾝﾛｰﾄﾞ(Excel出力)ボタン、ﾀﾞｳﾝﾛｰﾄﾞ(PDF出力)・一覧印刷ボタン押下時処理
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub WF_EXCELPDF(ByVal WW_FILETYPE As Integer)
        'ファイル保存先
        Dim UploadRootPath As String = System.IO.Path.Combine(CS0050SESSION.UPLOAD_PATH,
                                                   "PRINTWORK",
                                                   CS0050SESSION.USERID)
        'ディレクトリが存在しない場合は生成
        If IO.Directory.Exists(UploadRootPath) = False Then
            IO.Directory.CreateDirectory(UploadRootPath)
        End If
        '前日プリフィックスのアップロードファイルが残っていた場合は削除
        Dim targetFiles = IO.Directory.GetFiles(UploadRootPath, "*.*")
        Dim keepFilePrefix As String = Now.ToString("yyyyMMdd")
        For Each targetFile In targetFiles
            Dim targetfileName As String = IO.Path.GetFileName(targetFile)
            '今日の日付が先頭のファイル名の場合は残す
            If targetfileName.StartsWith(keepFilePrefix) Then
                Continue For
            End If
            Try
                IO.File.Delete(targetFile)
            Catch ex As Exception
                '削除時のエラーは無視
            End Try
        Next targetFile


        Dim UrlRoot As String
        'URLのルートを表示
        UrlRoot = String.Format("{0}://{1}/{3}/{2}/", HttpContext.Current.Request.Url.Scheme, HttpContext.Current.Request.Url.Host, CS0050SESSION.USERID, CS0050SESSION.PRINT_ROOT_URL_NAME)

        'Excel新規作成
        Dim wb As Workbook = New GrapeCity.Documents.Excel.Workbook

        '最大列(RANGE)を取得
        Dim WW_MAXCOL As Integer = [Enum].GetValues(GetType(LNM0008WRKINC.HISTORYEXCELCOL)).Cast(Of Integer)().Max()

        'シート名
        wb.ActiveSheet.Name = Left(WF_DDL_MODIFYYM.SelectedValue, 4) + "年" + Right(WF_DDL_MODIFYYM.SelectedValue, 2) + "月"

        'シート全体設定
        SetALL(wb.ActiveSheet)

        '行幅設定
        SetROWSHEIGHT(wb.ActiveSheet)

        '明細設定
        Dim WW_ACTIVEROW As Integer = 3
        SetDETAIL(wb.ActiveSheet, WW_ACTIVEROW)

        '明細の線を引く
        Dim WW_MAXRANGE As String = wb.ActiveSheet.Cells(WW_ACTIVEROW - 1, WW_MAXCOL).Address
        wb.ActiveSheet.Range("A4:" + WW_MAXRANGE).Borders.LineStyle = BorderLineStyle.Dotted
        wb.ActiveSheet.Range("A4:" + WW_MAXRANGE).Borders(BordersIndex.EdgeTop).LineStyle = BorderLineStyle.Thin
        wb.ActiveSheet.Range("A4:" + WW_MAXRANGE).Borders(BordersIndex.EdgeBottom).LineStyle = BorderLineStyle.Thin
        wb.ActiveSheet.Range("A4:" + WW_MAXRANGE).Borders(BordersIndex.EdgeLeft).LineStyle = BorderLineStyle.Thin
        wb.ActiveSheet.Range("A4:" + WW_MAXRANGE).Borders(BordersIndex.EdgeRight).LineStyle = BorderLineStyle.Thin

        'ヘッダ設定
        SetHEADER(wb.ActiveSheet, WW_MAXCOL)

        'その他設定
        wb.ActiveSheet.Range("A1").Value = "ID:" + Master.MAPID
        wb.ActiveSheet.Range("A2").Interior.Color = ColorTranslator.FromHtml(CONST_COLOR_HATCHING_MODIFY)
        wb.ActiveSheet.Range("B2").Value = "は変更項目"
        wb.ActiveSheet.Range("C1").Value = "コード変換特例２変更履歴一覧"

        '列幅自動調整
        wb.ActiveSheet.Range("A3:" + WW_MAXRANGE).EntireColumn.AutoFit()

        '印刷設定
        With wb.ActiveSheet.PageSetup
            .PrintArea = "A1:" + WW_MAXRANGE '印刷範囲
            .PaperSize = PaperSize.A4 '用紙サイズ　
            .Orientation = PageOrientation.Landscape '横向き
            '.Zoom = 80 '倍率
            .IsPercentScale = False 'FalseでFitToPages有効化
            .FitToPagesWide = 1 'すべての列を1ページに印刷
            .FitToPagesTall = 99 '設定しないと全て1ページにされる
            .LeftMargin = 16 '左余白(ポイント)
            .RightMargin = 16 '右余白(ポイント)
            .PrintTitleRows = "$3:$3" 'ページヘッダ
            .RightFooter = "&P / &N" 'ページフッタにページ番号設定
        End With

        Dim FileName As String
        Dim FilePath As String
        Select Case WW_FILETYPE
            Case LNM0008WRKINC.FILETYPE.EXCEL
                FileName = "コード変換特例２変更履歴.xlsx"
                FilePath = IO.Path.Combine(UploadRootPath, FileName)

                '保存
                wb.Save(FilePath, SaveFileFormat.Xlsx)

                'ダウンロード
                WF_PrintURL.Value = UrlRoot & FileName
                ClientScript.RegisterStartupScript(Me.GetType(), "key", "f_ExcelPrint();", True)
            Case LNM0008WRKINC.FILETYPE.PDF
                FileName = "コード変換特例２変更履歴.pdf"
                FilePath = IO.Path.Combine(UploadRootPath, FileName)

                '保存
                wb.Save(FilePath, SaveFileFormat.Pdf)

                'ダウンロード
                WF_PrintURL.Value = UrlRoot & FileName
                ClientScript.RegisterStartupScript(Me.GetType(), "key", "f_PDFPrint();", True)
            Case Else
        End Select
    End Sub

    ''' <summary>
    ''' シート全体設定
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub SetALL(ByVal sheet As IWorksheet)
        ' ウィンドウ枠を固定
        'sheet.FreezePanes(1, 3)
        sheet.FreezePanes(3, 0)

        ' ワークシートのビューを構成
        Dim sheetView As IWorksheetView = sheet.SheetView
        'sheetView.DisplayFormulas = False
        'sheetView.DisplayRightToLeft = True
        '表示倍率
        sheetView.Zoom = 90

        '列幅
        sheet.Columns.ColumnWidth = 5
        '行幅
        sheet.Rows.RowHeight = 15.75
        'フォント
        With sheet.Columns.Font
            .Color = Color.FromArgb(0, 0, 0)
            .Name = "Meiryo UI"
            .Size = 11
        End With
        '配置
        sheet.Columns.VerticalAlignment = VerticalAlignment.Center
        'sheet.Rows.HorizontalAlignment = HorizontalAlignment.Center
    End Sub

    ''' <summary>
    ''' 行幅設定
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub SetROWSHEIGHT(ByVal sheet As IWorksheet)

    End Sub

    ''' <summary>
    ''' ヘッダ設定
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub SetHEADER(ByVal sheet As IWorksheet, ByVal WW_MAXCOL As Integer)
        '行幅
        sheet.Rows(0).RowHeight = 15.75 '１行目
        sheet.Rows(1).RowHeight = 15.75 '２行目
        sheet.Rows(2).RowHeight = 31.5 '３行目

        Dim WW_MAXRANGE As String = sheet.Cells(2, WW_MAXCOL).Address

        '線
        sheet.Range("A3:" + WW_MAXRANGE).Borders.LineStyle = BorderLineStyle.Thin
        sheet.Range("A3:" + WW_MAXRANGE).Borders.Color = ColorTranslator.FromHtml(CONST_COLOR_BLACK)

        '背景色
        sheet.Range("A3:" + WW_MAXRANGE).Interior.Color = ColorTranslator.FromHtml(CONST_COLOR_HATCHING_HEADER)

        'フォント
        sheet.Range("A3:" + WW_MAXRANGE).Font.Color = ColorTranslator.FromHtml(CONST_COLOR_FONT_HEADER)
        sheet.Range("A3:" + WW_MAXRANGE).Font.Bold = True

        '配置
        sheet.Range("A3:" + WW_MAXRANGE).HorizontalAlignment = HorizontalAlignment.Center

        'オートフィルタ
        sheet.Range("A3:" + WW_MAXRANGE).AutoFilter()

        '折り返して全体を表示
        'sheet.Range("J1:M1").WrapText = True

        '値
        Dim WW_HEADERROW As Integer = 2
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.OPERATEKBNNAME).Value = "操作区分"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.MODIFYKBNNAME).Value = "変更区分"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.MODIFYYMD).Value = "変更日時"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.MODIFYUSER).Value = "変更USER"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.DELFLG).Value = "削除フラグ"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.ORGCODE).Value = "組織コード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.BIGCTNCD).Value = "大分類コード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.MIDDLECTNCD).Value = "中分類コード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.PURPOSE).Value = "使用目的"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.STACKFREEKBN).Value = "積空区分"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPTRUSTEECD).Value = "特例置換項目-発受託人コード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPTRUSTEESUBCD).Value = "特例置換項目-発受託人サブコード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPTRUSTEESUBZKBN).Value = "特例置換項目-発受託人サブゼロ変換区分"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPSHIPPERCD).Value = "特例置換項目-発荷主コード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRARRTRUSTEECD).Value = "特例置換項目-着受託人コード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRARRTRUSTEESUBCD).Value = "特例置換項目-着受託人サブコード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRARRTRUSTEESUBZKBN).Value = "特例置換項目-着受託人サブゼロ変換区分"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRJRITEMCD).Value = "特例置換項目-ＪＲ品目コード"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRSTACKFREEKBN).Value = "特例置換項目-積空区分"
        sheet.Cells(WW_HEADERROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRSTATUSKBN).Value = "特例置換項目-状態区分"

    End Sub

    ''' <summary>
    ''' 明細設定
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub SetDETAIL(ByVal sheet As IWorksheet, ByRef WW_ACTIVEROW As Integer)


        For Each Row As DataRow In LNM0008tbl.Rows
            '値
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.OPERATEKBNNAME).Value = Row("OPERATEKBNNAME") '操作区分
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.MODIFYKBNNAME).Value = Row("MODIFYKBNNAME") '変更区分
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.MODIFYYMD).Value = Row("MODIFYYMD") '変更日時
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.MODIFYUSER).Value = Row("MODIFYUSER") '変更USER
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.DELFLG).Value = Row("DELFLG") '削除フラグ
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.ORGCODE).Value = Row("ORGCODE") '組織コード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.BIGCTNCD).Value = Row("BIGCTNCD") '大分類コード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.MIDDLECTNCD).Value = Row("MIDDLECTNCD") '中分類コード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.PURPOSE).Value = Row("PURPOSE") '使用目的
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.STACKFREEKBN).Value = Row("STACKFREEKBN") '積空区分
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPTRUSTEECD).Value = Row("SPRDEPTRUSTEECD") '特例置換項目-発受託人コード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPTRUSTEESUBCD).Value = Row("SPRDEPTRUSTEESUBCD") '特例置換項目-発受託人サブコード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPTRUSTEESUBZKBN).Value = Row("SPRDEPTRUSTEESUBZKBN") '特例置換項目-発受託人サブゼロ変換区分
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRDEPSHIPPERCD).Value = Row("SPRDEPSHIPPERCD") '特例置換項目-発荷主コード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRARRTRUSTEECD).Value = Row("SPRARRTRUSTEECD") '特例置換項目-着受託人コード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRARRTRUSTEESUBCD).Value = Row("SPRARRTRUSTEESUBCD") '特例置換項目-着受託人サブコード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRARRTRUSTEESUBZKBN).Value = Row("SPRARRTRUSTEESUBZKBN") '特例置換項目-着受託人サブゼロ変換区分
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRJRITEMCD).Value = Row("SPRJRITEMCD") '特例置換項目-ＪＲ品目コード
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRSTACKFREEKBN).Value = Row("SPRSTACKFREEKBN") '特例置換項目-積空区分
            sheet.Cells(WW_ACTIVEROW, LNM0008WRKINC.HISTORYEXCELCOL.SPRSTATUSKBN).Value = Row("SPRSTATUSKBN") '特例置換項目-状態区分

            '変更区分が変更後の行の場合
            If Row("MODIFYKBN") = LNM0008WRKINC.MODIFYKBN.AFTDATA Then
                '変更箇所を塗りつぶし
                SetMODIFYHATCHING(sheet, WW_ACTIVEROW)
            End If

            WW_ACTIVEROW += 1
        Next
    End Sub

    ''' <summary>
    ''' 変更箇所を塗りつぶし
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub SetMODIFYHATCHING(ByVal sheet As IWorksheet, ByRef WW_ACTIVEROW As Integer)
        '最大列(RANGE)を取得
        Dim WW_MAXCOL As Integer = [Enum].GetValues(GetType(LNM0008WRKINC.HISTORYEXCELCOL)).Cast(Of Integer)().Max()

        '変更チェック開始列を取得
        Dim WW_STCOL As Integer = LNM0008WRKINC.HISTORYEXCELCOL.DELFLG   '削除フラグ

        '開始列から最大列まで変更前後の値を確認
        For index As Integer = WW_STCOL To WW_MAXCOL
            '変更前と変更後が不一致の場合
            If Not sheet.Cells(WW_ACTIVEROW - 1, index).Value = sheet.Cells(WW_ACTIVEROW, index).Value Then

                '変更後の背景色を塗りつぶし
                sheet.Cells(WW_ACTIVEROW, index).Interior.Color = ColorTranslator.FromHtml(CONST_COLOR_HATCHING_MODIFY)

                '変更後のフォント色を変える
                sheet.Cells(WW_ACTIVEROW, index).Font.Color = ColorTranslator.FromHtml(CONST_COLOR_RED)
                sheet.Cells(WW_ACTIVEROW, index).Font.Bold = True

            End If
        Next
    End Sub
#End Region


End Class

